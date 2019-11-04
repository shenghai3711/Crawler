using System;
using HZ.Crawler.Data;
using HZ.Crawler.Common;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Common.Extensions;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace HZ.Crawler.DataSpider
{
    public class ShiweijiaCategory : BaseSpider
    {
        private IHttpClient Client { get; set; }
        public ShiweijiaCategory(IConfiguration configuration, DataContext context)
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
        }

        private string _Nonce = Guid.NewGuid().ToString("N").Substring(0, 11);
        protected override string LoadHTML(string url)
        {
            try
            {
                string reqTime = DateTime.Now.GetMilliseconds().ToString();
                string sign = Encrypt.ToMd5($"Nonce={this._Nonce}&ReqTime={reqTime}&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8).ToUpper();
                string data = JsonSerializer.Serialize(new
                {
                    ReqTime = reqTime,
                    Nonce = this._Nonce,
                    Signature = sign,
                    TerminalType = "web",
                    TerminalVersion = "lenovo"
                });
                string result = this.Client.Request(url, HttpMethod.POST, data, Encoding.UTF8, "application/json;charset=UTF-8");
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
                    var resultList = ParseItem(jsonElement.GetProperty("Data").EnumerateArray());
                    base.SaveData(ts: resultList.ToArray());
                }
            }
            catch (System.Exception)
            {//TODO:解析异常
                base.SaveFile(html);
            }
            return string.Empty;
        }

        List<Model.Shiweijia.CategoryModel> ParseItem(JsonElement.ArrayEnumerator elements)
        {
            //TODO:判断是否为空
            var list = new List<Model.Shiweijia.CategoryModel>();
            foreach (var item in elements)
            {
                int id = item.GetProperty("ID").GetInt32();
                string name = item.GetProperty("CategoryName").GetString();
                list.Add(new Model.Shiweijia.CategoryModel
                {
                    Id = id,
                    CategoryName = name,
                    CategoryImg = item.GetProperty("CategoryImg").GetString()
                });
                var childs = item.GetProperty("Subs").EnumerateArray();
                list.AddRange(ParseItem(childs));
            }
            return list;
        }

    }
}
