using HZ.Crawler.Common;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Data;
using HZ.Crawler.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HZ.Crawler.DataSpider
{
    public abstract class BaseSpider
    {
        private DataContext Context { get; }
        private IConfiguration Configuration { get; }
        private string ImportMaterialHost { get; }
        public BaseSpider(IConfiguration configuration, DataContext context)
        {
            this.Configuration = configuration;
            this.Context = context;
            this.ImportMaterialHost = configuration.GetValue(nameof(this.ImportMaterialHost), string.Empty);
        }

        public void Run()
        {
            var config = new SpiderConfig();
            this.Configuration.GetSection(this.GetType().Name).Bind(config);
            //this.Configuration.GetValue<SpiderConfig>(this.GetType().Name);//反射不到数组
            //开始
            foreach (var host in config.Hosts)
            {
                this.CrawleHost(host);
            }
            //结束
        }
        private void CrawleHost(string host)
        {
            string url = host;
            foreach (var item in this.InitSpider())
            {
                do
                {
                    string html = this.LoadHTML(url, item);
                    if (string.IsNullOrEmpty(html))
                    {
                        break;
                    }
                    url = this.ParseSave(html, item);
                    if (string.IsNullOrEmpty(url))
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(new Random().Next(3000, 6000));
                } while (true);
            }
        }
        protected virtual List<string> InitSpider()
        {
            return new List<string> { string.Empty };
        }
        protected abstract string LoadHTML(string url, string param = null);
        /// <summary>
        /// 解析保存并返回下一页链接
        /// </summary>
        /// <param name="html"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected abstract string ParseSave(string html, string param = null);
        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="isSave"></param>
        /// <param name="ts"></param>
        /// <typeparam name="T"></typeparam>
        protected void SaveData<T>(bool isSave = true, params T[] ts) where T : BaseModel, new()
        {
            foreach (var t in ts)
            {
                this.Context.AddAsync(t);
            }
            if (isSave)
            {
                this.Context.SaveChangesAsync();
            }
        }
        protected bool Exists<T>(T t) where T : BaseModel, new()
        {
            return this.Context.Find<T>(t.Id) != null;
        }
        /// <summary>
        /// 解析失败的保存
        /// </summary>
        /// <param name="html"></param>
        protected void SaveFile(string html)
        {
            var dirName = "error";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            FileHelper.Write($"{dirName}/{this.GetType().Name.ToLower()}-{DateTime.Now.ToString("MMddHHmmssfff")}-{Guid.NewGuid().ToString("N").Substring(0, 4)}.txt", html);
        }

        protected bool SubmitProduct(Dictionary<string, string> dataDic)
        {
            var client = HttpClientFactory.Create();
            dataDic.Add("action", "addPlatformMaterial");
            string data = string.Join("&", dataDic.Select(d => $"{d.Key}={d.Value}"));
            string result = client.Request(this.ImportMaterialHost, HttpMethod.POST, data, Encoding.UTF8);
            var json = JsonDocument.Parse(result);
            return json.RootElement.GetProperty("OK").GetBoolean();
        }
    }
}
