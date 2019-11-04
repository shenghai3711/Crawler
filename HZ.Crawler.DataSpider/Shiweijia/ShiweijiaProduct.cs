using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using HZ.Crawler.Common;
using HZ.Crawler.Common.Extensions;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Data;
using Microsoft.Extensions.Configuration;

namespace HZ.Crawler.DataSpider
{
    public class ShiweijiaProduct : BaseSpider
    {
        private IHttpClient Client { get; set; }
        public ShiweijiaProduct(IConfiguration configuration, DataContext context)
        : base(configuration, context)
        {
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
        private int _PageSize = 20;
        protected override string LoadHTML(string url)
        {
            try
            {
                int pageIndex = Convert.ToInt32(url);
                string reqTime = DateTime.Now.GetMilliseconds().ToString();
                string sign = Encrypt.ToMd5($"MaxPrice=0&MinPrice=0&Nonce={this._Nonce}&OrderType=0&PageIndex={pageIndex}&PageSize={this._PageSize}&ReqTime={reqTime}&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8).ToUpper();
                string data = JsonSerializer.Serialize(new
                {
                    ReqTime = reqTime,
                    Nonce = this._Nonce,
                    Signature = sign,
                    TerminalType = "web",
                    TerminalVersion = "lenovo",
                    Category = "",
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
                string result = this.Client.Request(this._ProductUrl, HttpMethod.POST, data, Encoding.UTF8, "application/json;charset=UTF-8");
                return result;
            }
            catch (System.Exception)
            {//TODO:加载异常
                return string.Empty;
            }
        }

        protected override string ParseSave(string html)
        {
            try
            {
                using (var jsonDoc = JsonDocument.Parse(html))
                {
                    var jsonElement = jsonDoc.RootElement;
                    bool isSuccess = jsonElement.GetProperty("IsSuccess").GetBoolean();
                    if (!isSuccess)
                    {//TODO:请求失败
                        return string.Empty;
                    }
                    var resultList = ParseItem(jsonElement.GetProperty("Data").GetProperty("Rows").EnumerateArray());
                    base.SaveData(ts: resultList.ToArray());
                    int pageIndex = jsonElement.GetProperty("Data").GetProperty("PageIndex").GetInt32();
                    int total = jsonElement.GetProperty("Data").GetProperty("Total").GetInt32();
                    int pageCount = Convert.ToInt32(Math.Ceiling(total / Convert.ToDouble(this._PageSize)));
                    return pageIndex > pageCount ? string.Empty : (pageIndex + 1).ToString();
                }
            }
            catch (System.Exception)
            {//TODO:解析异常
                base.SaveFile(html);
                return string.Empty;
            }
        }
        List<Model.Shiweijia.ProductModel> ParseItem(JsonElement.ArrayEnumerator elements)
        {
            //TODO:判断是否为空
            var list = new List<Model.Shiweijia.ProductModel>();
            foreach (var item in elements)
            {
                //还需要加载商品详情
                var product = new Model.Shiweijia.ProductModel
                {
                    Id = item.GetProperty("ID").GetInt32(),
                    ProductCode = item.GetProperty("ProductCode").GetString(),
                    Name = item.GetProperty("Name").GetString(),
                    Style = item.GetProperty("Pattern").GetString(),
                    SalePrice = item.GetProperty("SalePrice").GetInt32()
                };
                GetProductDetail(product);
                list.Add(product);
            }
            return list;
        }
        /// <summary>
        /// 获取商品详情
        /// </summary>
        /// <param name="product"></param>
        void GetProductDetail(Model.Shiweijia.ProductModel product)
        {
            string reqTime = DateTime.Now.GetMilliseconds().ToString();
            string sign = Encrypt.ToMd5($"Id={product.Id}&Nonce={this._Nonce}&ReqTime={reqTime}&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8);
            string data = JsonSerializer.Serialize(new
            {
                ReqTime = reqTime,
                Nonce = this._Nonce,
                Signature = sign,
                TerminalType = "web",
                TerminalVersion = "lenovo",
                Id = product.Id,
                UserId = ""
            });
            string result = this.Client.Request(this._ProductDetailUrl, HttpMethod.POST, data, Encoding.UTF8, "application/json;charset=UTF-8");
            using (var jsonDoc = JsonDocument.Parse(result))
            {
                bool isSuccess = jsonDoc.RootElement.GetProperty("IsSuccess").GetBoolean();
                if (!isSuccess)
                {//TODO:请求失败
                    return;
                }
                var dataElement = jsonDoc.RootElement.GetProperty("Data");
                product.Brand = new Model.Shiweijia.BrandModel
                {
                    Id = dataElement.GetProperty("BrandId").GetInt32(),
                    Name = dataElement.GetProperty("Brand").GetString(),
                    Logo = dataElement.GetProperty("BrandImg").GetString()
                };
                product.FullDescription = dataElement.GetProperty("FullDescription").GetString();
                product.MainImgs = dataElement.GetProperty("MainImgs").GetString();
                product.DetailImgs = dataElement.GetProperty("DetailImgs").GetString();
                product.Specifications = GetSpecifications(dataElement.GetProperty("Paras").EnumerateArray());
                var features = GetFeatures(dataElement.GetProperty("Specification").EnumerateArray());
                product.ProductSpecifications = GetSpecificationModel(dataElement.GetProperty("ProductSpecifications").EnumerateArray(), features);
            }
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
                foreach (var para in item.GetProperty("Paras").EnumerateArray())
                {
                    dic.Add(para.GetProperty("Name").GetString(), para.GetProperty("ParameterValue").GetString());
                }
            }
            return JsonSerializer.Serialize(dic);
        }
        /// <summary>
        /// 获取商品规格
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        List<Model.Shiweijia.ProductSpecificationModel> GetSpecificationModel(JsonElement.ArrayEnumerator elements, Dictionary<int, string> features)
        {
            var list = new List<Model.Shiweijia.ProductSpecificationModel>();
            foreach (var item in elements)
            {
                var featureList = new List<string>();
                foreach (var value in item.GetProperty("SpecificationValueIds").EnumerateArray())
                {
                    featureList.Add(features[value.GetInt32()]);
                }
                list.Add(new Model.Shiweijia.ProductSpecificationModel
                {
                    Id = item.GetProperty("ProductId").GetInt32(),
                    ProductName = item.GetProperty("ProductName").GetString(),
                    SalePrice = item.GetProperty("SalePrice").GetInt32(),
                    Thumbnails = item.GetProperty("Thumbnails").GetString(),
                    Features = JsonSerializer.Serialize(featureList)
                });
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
                foreach (var value in item.GetProperty("Values").EnumerateArray())
                {
                    dic.Add(value.GetProperty("Id").GetInt32(), value.GetProperty("Name").GetString());
                }
            }
            return dic;
        }

    }
}
