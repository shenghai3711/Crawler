using System;
using HZ.Crawler.Data;
using HZ.Crawler.Common;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Common.Extensions;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using HZ.Crawler.Model.Shiweijia;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HZ.Crawler.DataSpider
{
    public class ShiweijiaCategory : BaseSpider
    {
        private IHttpClient Client { get; set; }
        ShiweijiaContext Context { get; }
        public ShiweijiaCategory(IConfiguration configuration, DataContext context)
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
        }

        private string _Nonce = Guid.NewGuid().ToString("N").Substring(0, 11);
        protected override string LoadHTML(string url, string param = null)
        {
            try
            {
                string reqTime = DateTime.Now.GetMilliseconds().ToString();
                string sign = Encrypt.ToMd5($"AppId=9900&Nonce={this._Nonce}&ReqTime={reqTime}&TerminalType=web&TerminalVersion=lenovo", Encoding.UTF8).ToUpper();
                string data = JsonConvert.SerializeObject(new
                {
                    AppId = 9900,
                    ReqTime = reqTime,
                    Nonce = this._Nonce,
                    Signature = sign,
                    TerminalType = "web",
                    TerminalVersion = "lenovo"
                });
                string result = this.Client.Request(url, HttpMethod.POST, data, Encoding.UTF8, "application/json;charset=UTF-8");
                return result;
            }
            catch (Exception)
            {//TODO:加载异常
                return string.Empty;
            }
        }

        protected override string ParseSave(string html, string param = null)
        {
            try
            {
                var root = JToken.Parse(html);
                if (!root.Value<bool>("IsSuccess"))
                {//TODO:请求失败
                    Logger.Warn(root.Value<string>("Message"));
                    return string.Empty;
                }
                ParseItem(root["Data"], null).GetAwaiter().GetResult();
                this.Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {//TODO:解析异常
                Logger.Error(ex: ex);
                base.SaveFile(html);
            }
            return string.Empty;
        }

        async Task ParseItem(JToken elements, int? parentId)
        {
            foreach (var item in elements)
            {
                int id = item.Value<int>("ID");
                string name = item.Value<string>("CategoryName");
                var model = new Model.Shiweijia.CategoryModel
                {
                    Id = id,
                    CategoryName = name,
                    CategoryImg = item.Value<string>("CategoryImg"),
                    ParentId = parentId
                };
                if (!await this.Context.CategoryModels.AnyAsync(c => c.Id == model.Id))
                {
                    model.CategoryImg = !string.IsNullOrEmpty(model.CategoryImg) ? base.UploadImgsByLink(model.CategoryImg).FirstOrDefault() : "";
                    await this.Context.AddAsync(model);
                    Logger.Info($"已添加更新 {model.CategoryName}");
                }
                if (item["Subs"] != null && item["Subs"].Count() > 0)
                {
                    await ParseItem(item["Subs"], id);
                }
            }
        }
    }
}
