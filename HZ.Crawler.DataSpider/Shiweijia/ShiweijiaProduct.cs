using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using HZ.Crawler.Common;
using HZ.Crawler.Common.Extensions;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Data;
using HZ.Crawler.Model.Shiweijia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HZ.Crawler.DataSpider
{
    public class ShiweijiaProduct : BaseSpider
    {
        private IHttpClient Client { get; }
        private ConcurrentBag<CategoryModel> CategoryList { get; set; }
        private ConcurrentBag<BrandModel> BrandList { get; set; }
        private ConcurrentBag<ProductModel> ProductList { get; set; }
        private IConfiguration Configuration { get; }
        ShiweijiaContext Context { get; }
        public ShiweijiaProduct(IConfiguration configuration, DataContext context)
        : base(configuration, context)
        {
            this.Configuration = configuration;
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
        private int _PageSize = 50;
        protected override List<string> InitSpider()
        {
            return this.CategoryList.OrderBy(c => c.UpdateDate).Where(c => c.ParentId != null).Select(c => c.Id.ToString()).ToList();
        }
        protected override void Begin()
        {
            this.ProductList = new ConcurrentBag<ProductModel>();
            this.CategoryList = new ConcurrentBag<CategoryModel>(this.Context.CategoryModels.ToList());
            this.BrandList = new ConcurrentBag<BrandModel>(this.Context.BrandModels.ToList());
            this.Context.BrandModels.RemoveRange(this.BrandList);
        }
        protected override void End()
        {
            foreach (var item in this.ProductList.GroupBy(p => p.CategoryId))
            {
                var childCategory = this.CategoryList.FirstOrDefault(c => c.Id == item.Key);
                var category = this.CategoryList.FirstOrDefault(c => c.Id == childCategory.ParentId);
                this.Logger.Info($"{category.CategoryName}-{childCategory.CategoryName} 共抓取到{item.Count()}件");
            }
            this.Context.BrandModels.AddRangeAsync(this.BrandList);
            this.Context.ProductModels.RemoveRange(this.ProductList);
            this.Context.ProductModels.AddRangeAsync(this.ProductList);
            this.Context.SaveChangesAsync();
        }
        protected override string LoadHTML(string url, string param = null)
        {
            if (!int.TryParse(url, out int pageIndex))
            {
                pageIndex = 1;
            }
            string category = param;
            this.Logger.Info($"{this.CategoryList.FirstOrDefault(c => c.Id.ToString() == category).CategoryName} 正在采集第{pageIndex}页");
            string reqTime = DateTime.Now.GetMilliseconds().ToString();
            string sign = Encrypt.ToMd5($"AppId=9900&Category={category}&MaxPrice=0&MinPrice=0&Nonce={this._Nonce}&OrderType=0&PageIndex={pageIndex}&PageSize={this._PageSize}&ReqTime={reqTime}&Suffix=shengshi&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8).ToUpper();
            string postData = JsonSerializer.Serialize(new
            {
                AppId = 9900,
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
                Suffix = "shengshi"
            });
            string result = this.Client.Request(this._ProductUrl, HttpMethod.POST, postData, Encoding.UTF8, "application/json;charset=UTF-8");
            return result;
        }

        protected override string ParseSave(string html, string param = null)
        {
            int categoryId = Convert.ToInt32(param);
            using (var jsonDoc = JsonDocument.Parse(html))
            {
                var jsonElement = jsonDoc.RootElement;
                bool isSuccess = jsonElement.GetProperty("IsSuccess").GetBoolean();
                if (!isSuccess)
                {//TODO:请求失败
                    this.Logger.Warn($"解析商品列表失败:{html}");
                    return string.Empty;
                }
                if (jsonElement.TryGetProperty("Data", out var dataElement) && dataElement.TryGetProperty("Rows", out var rowElement))
                {
                    var resultList = ParseItem(rowElement.EnumerateArray(), categoryId);
                    int pageIndex = dataElement.GetProperty("PageIndex").GetInt32();
                    int total = dataElement.GetProperty("Total").GetInt32();
                    int pageCount = Convert.ToInt32(Math.Ceiling(total / Convert.ToDouble(this._PageSize)));
                    return pageIndex >= pageCount ? string.Empty : (pageIndex + 1).ToString();
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 解析每一个商品
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        List<ProductModel> ParseItem(JsonElement.ArrayEnumerator elements, int categoryId)
        {
            var list = new List<ProductModel>();
            foreach (var item in elements)//一页数据
            {
                //需要加载每个商品详情（每一种规格都需要加载）
                try
                {
                    var products = GetAllProductDetail(item.GetProperty("ID").GetInt32(), categoryId);
                    list.AddRange(products);
                    // this.Context.ProductModels.AddRangeAsync(list);
                    // this.Context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    this.Logger.Error("解析商品详情异常", ex);
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return list;
        }
        /// <summary>
        /// 获取所有规格商品详情
        /// </summary>
        List<ProductModel> GetAllProductDetail(int productId, int categoryId)
        {
            var products = new List<ProductModel>();
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 11);
            var allProductIds = new List<int>();
            do
            {
                this.Logger.Info($"开始获取 {productId} 详情信息......");
                try
                {
                    ProductModel product = null;
                    (product, allProductIds) = GetProductDetail(productId, nonce);
                    product.CategoryId = categoryId;
                    this.ProductList.Add(product);
                    products.Add(product);
                    this.Submit(product);
                }
                catch (Exception ex)
                {
                    this.Logger.Error($"获取 {productId} 详情信息异常", ex);
                }
                if (allProductIds == null || allProductIds.Count == 0)
                {
                    break;
                }
                productId = allProductIds.FirstOrDefault(pid => !products.Any(p => p.Id == pid));
                System.Threading.Thread.Sleep(new Random().Next(1000, 3000));
            } while (productId != 0);
            return products;
        }

        Tuple<ProductModel, List<int>> GetProductDetail(int productId, string nonce)
        {
            ProductModel product = null;
            var allProductIds = new List<int>();
            string result = GetProductDetailJson(productId, nonce);
            using (var jsonDoc = JsonDocument.Parse(result))
            {
                bool isSuccess = jsonDoc.RootElement.GetProperty("IsSuccess").GetBoolean();
                if (!isSuccess)
                {
                    throw new Exception($"解析商品详情失败:{result}");
                }
                if (jsonDoc.RootElement.TryGetProperty("Data", out var dataElement))
                {
                    (product, allProductIds) = ParseProduct(dataElement);
                }
            }
            return new Tuple<ProductModel, List<int>>(product, allProductIds);
        }
        string GetProductDetailJson(int productId, string nonce)
        {
            string reqTime = DateTime.Now.GetMilliseconds().ToString();
            string sign = Encrypt.ToMd5($"AppId=9900&Id={productId}&Nonce={nonce}&ReqTime={reqTime}&Suffix=shengshi&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8);
            string data = JsonSerializer.Serialize(new
            {
                AppId = 9900,
                Suffix = "shengshi",
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
        Tuple<ProductModel, List<int>> ParseProduct(JsonElement dataElement)
        {
            int brandId = dataElement.GetProperty("BrandId").GetInt32();
            var brand = this.BrandList.FirstOrDefault(b => b.Id == brandId);
            if (brand == null)
            {//上传图片，并替换实体
                brand = new BrandModel
                {
                    Id = brandId,
                    BrandName = dataElement.GetProperty("Brand").GetString(),
                    BrandImg = dataElement.GetProperty("BrandImg").GetString(),
                };
                brand.BrandImg = base.UploadImgsByLink(brand.BrandImg).FirstOrDefault();
                this.BrandList.Add(brand);
            }
            var product = new ProductModel
            {
                Id = dataElement.GetProperty("ID").GetInt32(),
                BrandName = brand.BrandName,
                BrandImg = brand.BrandImg,
                MainImgs = this.ArrayToJson(dataElement, "MainImgs"),
                DetailImgs = this.ArrayToJson(dataElement, "DetailImgs"),
                ProductCode = dataElement.GetProperty("ProductCode").GetString(),
                Name = dataElement.TryGetProperty("Name", out var nameElement) ? nameElement.GetString() : string.Empty,
                Style = dataElement.TryGetProperty("Pattern", out var patternElement) ? patternElement.GetString() : string.Empty,
                SalePrice = dataElement.GetProperty("SalePrice").GetDecimal(),
            };
            this.Logger.Info($"商品主要数据:[编号：{product.Id}],[名称：{product.Name}],[品牌：{product.BrandName}],[价格：{product.SalePrice}]");
            var allProductIds = new List<int>();
            if (dataElement.TryGetProperty("Paras", out var specificationsElement) && specificationsElement.ValueKind == JsonValueKind.Array)
            {
                product.Specifications = GetSpecifications(specificationsElement.EnumerateArray());
            }
            if (dataElement.TryGetProperty("Specification", out var featuresElement) && featuresElement.ValueKind == JsonValueKind.Array && dataElement.TryGetProperty("ProductSpecifications", out var psElement) && psElement.ValueKind == JsonValueKind.Array)
            {
                var features = GetFeatures(featuresElement.EnumerateArray());
                allProductIds = GetAllProducts(psElement.EnumerateArray(), features, product);
            }
            return new Tuple<ProductModel, List<int>>(product, allProductIds);
        }
        /// <summary>
        /// 获取商品规格参数
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        string GetSpecifications(JsonElement.ArrayEnumerator elements)
        {
            var dic = new Dictionary<string, string>();
            try
            {
                foreach (var item in elements)
                {
                    if (item.TryGetProperty("Paras", out var parasElement) && parasElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var para in parasElement.EnumerateArray())
                        {
                            string value = para.GetProperty("ParameterValue").GetString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                dic.Add(para.GetProperty("Name").GetString(), value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error("解析商品规格异常", ex);
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(dic);
        }

        List<int> GetAllProducts(JsonElement.ArrayEnumerator elements, Dictionary<string, Dictionary<int, string>> features, ProductModel product)
        {
            var list = new List<int>();
            foreach (var item in elements)
            {
                int productId = item.GetProperty("ProductId").GetInt32();
                try
                {
                    if (productId == product.Id)
                    {
                        var featureDic = new Dictionary<string, string>();
                        if (item.TryGetProperty("SpecificationValueIds", out var svIdElement) && svIdElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var value in svIdElement.EnumerateArray())
                            {
                                int id = value.GetInt32();
                                var feature = features.FirstOrDefault(f => f.Value.Keys.Any(k => k == id));
                                if (featureDic.Keys.Any(k => k == feature.Key)) continue;
                                featureDic.Add(feature.Key, feature.Value[id]);
                            }
                        }
                        product.Thumbnails = item.TryGetProperty("Thumbnails", out var thumbnailsElement) ? thumbnailsElement.GetString() : product.MainImgs;
                        product.Features = Newtonsoft.Json.JsonConvert.SerializeObject(featureDic);
                        product.CostPrice = item.GetProperty("Price1").GetDecimal();
                        product.CustomPrice = item.GetProperty("Price2").GetDecimal();
                    }
                    list.Add(productId);
                }
                catch (Exception ex)
                {
                    this.Logger.Error($"解析商品{productId}规格异常", ex);
                }
            }
            return list;
        }
        /// <summary>
        /// 获取商品特性
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        Dictionary<string, Dictionary<int, string>> GetFeatures(JsonElement.ArrayEnumerator elements)
        {
            var dic = new Dictionary<string, Dictionary<int, string>>();
            try
            {
                foreach (var item in elements)
                {
                    if (item.TryGetProperty("Values", out var valuesElement) && valuesElement.ValueKind == JsonValueKind.Array)
                    {
                        var values = new Dictionary<int, string>();
                        foreach (var value in valuesElement.EnumerateArray())
                        {
                            values.Add(value.GetProperty("Id").GetInt32(), value.GetProperty("Name").GetString());
                        }
                        dic.Add(item.GetProperty("Name").GetString(), values);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error("解析商品特性异常", ex);
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

        /// <summary>
        /// 提交
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        bool Submit(ProductModel product)
        {
            this.Logger.Info($"开始提交{product.Id}-{product.Name}");
            var childCategory = this.CategoryList.FirstOrDefault(c => c.Id == product.CategoryId);
            var category = this.CategoryList.FirstOrDefault(c => c.Id == childCategory.ParentId);
            string cover = GetImgStr(product.ProductCode, "缩略图").FirstOrDefault();
            if (string.IsNullOrEmpty(cover) && !string.IsNullOrEmpty(product.Thumbnails))
            {
                cover = base.UploadImgsByLink(product.Thumbnails).FirstOrDefault();
            }
            product.Thumbnails = cover;
            var pics = GetImgStr(product.ProductCode, "主图");
            if (pics.Count == 0)
            {
                var mainImgs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(product.MainImgs);
                pics = base.UploadImgsByLink(mainImgs.ToArray());
            }
            product.MainImgs = Newtonsoft.Json.JsonConvert.SerializeObject(pics);
            //product.Id = Convert.ToInt32($"{childCategory.ParentId.Value}{ childCategory.Id}{product.Id}");
            var dataDic = new Dictionary<string, string>
                {
                    {"platformType","1"},
                    {"materialTypeID","5"},
                    {"typeID","3"},//固定
                    {"productCode",product.ProductCode},
                    {"productID", product.Id.ToString()},
                    {"materialName",product.Name},
                    {"categoryName",category.CategoryName},
                    {"categoryCoverPath",category.CategoryImg},
                    {"mincategoryName",childCategory.CategoryName},
                    {"mincategoryCoverPath",childCategory.CategoryImg},
                    {"brandName",product.BrandName},
                    {"brandCoverPath",product.BrandImg},
                    {"marketPrice",product.SalePrice.ToString()},//市场价格
                    {"floorPrice",product.CostPrice.ToString()},//供货价
                    {"discountPrice",product.CustomPrice.ToString()},//折扣价
                    {"Attribute",GetProductAttributeJson(product.Features)},//属性json
                    {"coverPath",product.Thumbnails??pics.FirstOrDefault()},
                    {"materialPicture",product.MainImgs},//产品多图json
                    {"materialDetails",GetProductDetails(product)}//产品介绍
                };
            return base.SubmitProduct(dataDic);
        }
        string GetProductDetails(Model.Shiweijia.ProductModel product)
        {
            var details = new StringBuilder();
            #region 文字描述
            try
            {
                var dic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(product.Specifications);
                foreach (var item in dic)
                {
                    details.Append($"<p>{item.Key}:{item.Value}</p>");
                }
            }
            catch (Exception) { }
            #endregion
            var detailImgs = GetImgStr(product.ProductCode, "详情图");
            if (detailImgs.Count == 0)
            {
                var imgs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(product.DetailImgs);
                detailImgs = base.UploadImgsByLink(imgs.ToArray());
            }
            product.DetailImgs = Newtonsoft.Json.JsonConvert.SerializeObject(detailImgs);
            foreach (var item in detailImgs)
            {
                details.Append($"<img src=\"{item}\" />");
            }
            return details.ToString();
        }
        List<string> GetImgStr(string productCode, string folderName)
        {
            //TODO:指定文件夹(可以配置)
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductPic", productCode, folderName);
            //读取文件--->上传
            if (!Directory.Exists(path))
            {
                return new List<string>();
            }
            return base.UploadImgsByFile(FileHelper.GetAllFiles(path).ToArray());
        }
        string GetProductAttributeJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }
            try
            {
                var dic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                var attributes = dic.Where(a => !string.IsNullOrEmpty(a.Value)).Select(a => new
                {
                    AttributeName = a.Key,
                    AttributeValue = a.Value
                });
                return Newtonsoft.Json.JsonConvert.SerializeObject(attributes);
            }
            catch (Exception) { }
            return string.Empty;
        }
    }
}
