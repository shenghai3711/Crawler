using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using HZ.Crawler.Common;
using HZ.Crawler.Common.Extensions;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Data;
using HZ.Crawler.Model.Shiweijia;
using Microsoft.Extensions.Configuration;

namespace HZ.Crawler.DataSpider
{
    public class ShiweijiaProduct : BaseSpider
    {
        private IHttpClient Client { get; }
        private ShiweijiaContext Context { get; }
        private List<Model.Shiweijia.CategoryModel> CategoryList { get; set; }
        public ShiweijiaProduct(IConfiguration configuration, DataContext context)
        : base(configuration, context)
        {
            this.Context = context as ShiweijiaContext;
            this.Client = HttpClientFactory.Create();
            this.Client.HttpRequest.Accept = "application/json, text/plain, */*";
            this.Client.HttpRequest.Referer = "https://www.shiweijia.com/";
            this.Client.HttpRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";
            this.Client.HttpRequest.Headers = new System.Collections.Specialized.NameValueCollection
            {
                {"X-Requested-With","XMLHttpRequest"}
            };
            _Nonce = Guid.NewGuid().ToString("N").Substring(0, 11);
        }
        private string _Nonce = string.Empty;
        private string _ProductUrl = "https://api.shiweijia.com/api/Mall/QueryProductByPage";
        private string _ProductDetailUrl = "https://api.shiweijia.com/api/Product/GetProductDetail";
        private int _PageSize = 30;
        protected override List<string> InitSpider()
        {
            this.CategoryList = this.Context.CategoryModels.ToList();
            //return this.Context.CategoryModels.Where(c => c.ParentId != null).Select(c => $"{c.Id}").ToList();
            return this.CategoryList.Where(c => c.ParentId != null).Select(c => $"{c.Id}").ToList();
        }
        protected override string LoadHTML(string url, string param = null)
        {
            try
            {
                if (!int.TryParse(url, out int pageIndex))
                {
                    pageIndex = 1;
                }
                string category = param;
                string reqTime = DateTime.Now.GetMilliseconds().ToString();
                string sign = Encrypt.ToMd5($"Category={category}&MaxPrice=0&MinPrice=0&Nonce={this._Nonce}&OrderType=0&PageIndex={pageIndex}&PageSize={this._PageSize}&ReqTime={reqTime}&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8).ToUpper();
                string postData = JsonSerializer.Serialize(new
                {
                    ReqTime = reqTime,
                    Nonce = this._Nonce,
                    Signature = sign,
                    TerminalType = "web",
                    TerminalVersion = "lenovo",
                    Category = category,
                    Pattern = "",
                    Brand = "",
                    KeyWord = "",
                    OrderType = 0,
                    PageSize = this._PageSize,
                    PageIndex = pageIndex,
                    MinPrice = 0,
                    MaxPrice = 0,
                    UserID = ""
                });
                string result = this.Client.Request(this._ProductUrl, HttpMethod.POST, postData, Encoding.UTF8, "application/json;charset=UTF-8");
                return result;
            }
            catch (System.Exception)
            {//TODO:加载异常
                return string.Empty;
            }
        }

        protected override string ParseSave(string html, string param = null)
        {
            try
            {
                int categoryId = Convert.ToInt32(param);
                using (var jsonDoc = JsonDocument.Parse(html))
                {
                    var jsonElement = jsonDoc.RootElement;
                    bool isSuccess = jsonElement.GetProperty("IsSuccess").GetBoolean();
                    if (!isSuccess)
                    {//TODO:请求失败
                        return string.Empty;
                    }
                    if (jsonElement.TryGetProperty("Data", out var dataElement) && dataElement.TryGetProperty("Rows", out var rowElement))
                    {
                        var resultList = ParseItem(rowElement.EnumerateArray(), categoryId);
                        base.SaveData(ts: resultList.ToArray());
                        int pageIndex = dataElement.GetProperty("PageIndex").GetInt32();
                        int total = dataElement.GetProperty("Total").GetInt32();
                        int pageCount = Convert.ToInt32(Math.Ceiling(total / Convert.ToDouble(this._PageSize)));
                        return pageIndex > pageCount ? string.Empty : (pageIndex + 1).ToString();
                    }
                }
            }
            catch (System.Exception)
            {//TODO:解析异常
                base.SaveFile(html);
            }
            return string.Empty;
        }
        List<Model.Shiweijia.ProductModel> ParseItem(JsonElement.ArrayEnumerator elements, int categoryId)
        {
            //TODO:判断是否为空
            var list = new List<Model.Shiweijia.ProductModel>();
            foreach (var item in elements)//每页的数据
            {
                //需要加载商品详情（每一种规格都需要加载）
                try
                {
                    var products = GetAllProductDetail(item.GetProperty("ID").GetInt32(), categoryId);
                    list.AddRange(products);
                }
                catch (System.Exception)
                {
                }
            }
            return list;
        }
        private List<int> _OtherProductIds = new List<int>();
        /// <summary>
        /// 获取所有规格商品详情
        /// </summary>
        List<Model.Shiweijia.ProductModel> GetAllProductDetail(int productId, int categoryId)
        {
            var products = new List<Model.Shiweijia.ProductModel>();
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 11);
            do
            {
                try
                {
                    var product = GetProductDetail(productId, nonce);
                    if (product == null)
                    {
                        break;
                    }
                    product.CategoryId = categoryId;
                    products.Add(product);
                    this.ImportMaterial(product);
                }
                catch (System.Exception)
                {
                }
                if (this._OtherProductIds == null || this._OtherProductIds.Count <= 1)
                {
                    break;
                }
                productId = this._OtherProductIds.FirstOrDefault(pid => !products.Any(p => p.Id == pid));
                System.Threading.Thread.Sleep(new Random().Next(1000, 3000));
            } while (productId != 0);
            return products;
        }

        Model.Shiweijia.ProductModel GetProductDetail(int productId, string nonce)
        {
            var otherIds = new List<int>();
            string result = GetProductDetailJson(productId, nonce);
            using (var jsonDoc = JsonDocument.Parse(result))
            {
                bool isSuccess = jsonDoc.RootElement.GetProperty("IsSuccess").GetBoolean();
                if (!isSuccess)
                {//TODO:获取商品详情失败
                    return null;
                }
                if (jsonDoc.RootElement.TryGetProperty("Data", out var dataElement))
                {
                    return ParseProduct(dataElement);
                }
            }
            return null;
        }
        string GetProductDetailJson(int productId, string nonce)
        {
            string reqTime = DateTime.Now.GetMilliseconds().ToString();
            string sign = Encrypt.ToMd5($"Id={productId}&Nonce={nonce}&ReqTime={reqTime}&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8);
            string data = JsonSerializer.Serialize(new
            {
                ReqTime = reqTime,
                Nonce = nonce,
                Signature = sign,
                TerminalType = "web",
                TerminalVersion = "lenovo",
                Id = productId,
                UserId = ""
            });
            return this.Client.Request(this._ProductDetailUrl, HttpMethod.POST, data, Encoding.UTF8, "application/json;charset=UTF-8");
        }
        Model.Shiweijia.ProductModel ParseProduct(JsonElement dataElement)
        {
            var product = new Model.Shiweijia.ProductModel
            {
                Id = dataElement.GetProperty("ID").GetInt32(),
                BrandName = dataElement.GetProperty("Brand").GetString(),
                BrandImg = dataElement.GetProperty("BrandImg").GetString(),
                MainImgs = this.ArrayToJson(dataElement, "MainImgs"),
                DetailImgs = this.ArrayToJson(dataElement, "DetailImgs"),
                ProductCode = dataElement.GetProperty("ProductCode").GetString(),
                Name = dataElement.TryGetProperty("Name", out var nameElement) ? nameElement.GetString() : string.Empty,
                Style = dataElement.TryGetProperty("Pattern", out var patternElement) ? patternElement.GetString() : string.Empty,
                SalePrice = dataElement.GetProperty("SalePrice").GetDecimal()
            };
            if (dataElement.TryGetProperty("Paras", out var specificationsElement) && specificationsElement.ValueKind == JsonValueKind.Array)
            {
                product.Specifications = GetSpecifications(specificationsElement.EnumerateArray());
            }
            if (dataElement.TryGetProperty("Specification", out var featuresElement) && featuresElement.ValueKind == JsonValueKind.Array && dataElement.TryGetProperty("ProductSpecifications", out var psElement) && psElement.ValueKind == JsonValueKind.Array)
            {
                var features = GetFeatures(featuresElement.EnumerateArray());
                this._OtherProductIds = GetAllProducts(psElement.EnumerateArray(), features, product);
            }
            return product;
        }
        /// <summary>
        /// 获取商品规格参数
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        string GetSpecifications(JsonElement.ArrayEnumerator elements)
        {
            var dic = new Dictionary<string, string>();
            foreach (var item in elements)
            {
                //item.GetProperty("GroupName").GetString();
                if (item.TryGetProperty("Paras", out var parasElement) && parasElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var para in parasElement.EnumerateArray())
                    {
                        dic.Add(para.GetProperty("Name").GetString(), para.GetProperty("ParameterValue").GetString());
                    }
                }
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(dic);
        }

        List<int> GetAllProducts(JsonElement.ArrayEnumerator elements, Dictionary<int, string> features, Model.Shiweijia.ProductModel product)
        {
            var list = new List<int>();
            foreach (var item in elements)
            {
                int productId = item.GetProperty("ProductId").GetInt32();
                if (productId == product.Id)
                {
                    var featureList = new List<string>();
                    if (item.TryGetProperty("SpecificationValueIds", out var svIdElement) && svIdElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var value in svIdElement.EnumerateArray())
                        {
                            featureList.Add(features[value.GetInt32()]);
                        }
                    }
                    product.Thumbnails = item.TryGetProperty("Thumbnails", out var thumbnailsElement) ? thumbnailsElement.GetString() : string.Empty;
                    product.Features = JsonSerializer.Serialize(featureList, options: new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All) });
                }
                list.Add(productId);
            }
            return list;
        }
        /// <summary>
        /// 获取商品特性
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        Dictionary<int, string> GetFeatures(JsonElement.ArrayEnumerator elements)
        {
            var dic = new Dictionary<int, string>();
            foreach (var item in elements)
            {
                //item.GetProperty("Name").GetString()
                if (item.TryGetProperty("Values", out var valuesElement) && valuesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var value in valuesElement.EnumerateArray())
                    {
                        dic.Add(value.GetProperty("Id").GetInt32(), value.GetProperty("Name").GetString());
                    }
                }
            }
            return dic;
        }
        /// <summary>
        /// 数组转json字符串
        /// </summary>
        /// <returns></returns>
        Func<JsonElement, string, string> ArrayToJson = (element, tagName) =>
         {
             var resultList = new List<string>();
             if (element.TryGetProperty(tagName, out var result))
             {
                 switch (result.ValueKind)
                 {
                     case JsonValueKind.Array:
                         foreach (var item in result.EnumerateArray())
                         {
                             resultList.Add(item.GetString());
                         }
                         break;
                     case JsonValueKind.Null:
                     default:
                         break;
                 }
             }
             return JsonSerializer.Serialize(resultList, options: new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All) });
         };

        void ImportMaterial(Model.Shiweijia.ProductModel product)
        {
            var childCategory = this.CategoryList.FirstOrDefault(c => c.Id == product.CategoryId);
            var category = this.CategoryList.FirstOrDefault(c => c.Id == childCategory.ParentId);
            var dataDic = new Dictionary<string, string>
            {
                {"platformType","1"},
                {"materialTypeID","1"},
                {"typeID","3"},
                {"productCode",product.ProductCode},
                {"productID",product.Id.ToString()},
                {"materialName",product.Name},
                {"categoryName",category.CategoryName},
                {"categoryCoverPath",category.CategoryImg},
                {"mincategoryName",childCategory.CategoryName},
                {"mincategoryCoverPath",childCategory.CategoryImg},
                {"brandName",product.BrandName},
                {"brandCoverPath",product.BrandImg},
                {"saleprice",product.SalePrice.ToString()},
                {"Attribute",GetProductAttributeJson(product.Specifications)},//属性json
                {"coverPath",product.Thumbnails},
                {"materialPicture",""},//产品多图json
                {"materialDetails",""}//产品介绍
            };
            base.SubmitProduct(dataDic);
        }
        string GetProductAttributeJson(string json)
        {
            var dic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            var attributes = dic.Where(a => !string.IsNullOrEmpty(a.Value)).Select(a => new
            {
                AttributeName = a.Key,
                AttributeValue = a.Value
            });
            return Newtonsoft.Json.JsonConvert.SerializeObject(attributes);
        }
    }
}
