using HZ.Crawler.Common;
using HZ.Crawler.Data;
using HZ.Crawler.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HZ.Crawler.DataSpider
{
    public abstract class BaseSpider
    {
        private DataContext Context { get; }
        private IConfiguration Configuration { get; }
        public BaseSpider(IConfiguration configuration, DataContext context)
        {
            this.Configuration = configuration;
            this.Context = context;
        }

        public void Run()
        {
            ClaenData();
            string spiderName = this.GetType().Name.ToLower();
            var config = new SpiderConfig();
            this.Configuration.GetSection(this.GetType().Name).Bind(config);
            //this.Configuration.GetValue<SpiderConfig>(this.GetType().Name);//反射不到数组
            //开始
            foreach (var host in config.Hosts)
            {
                this.CrawleHost(host);
            }
            ClaenData();
            //结束
        }
        private void CrawleHost(string host)
        {
            string url = host;
            do
            {
                string html = this.LoadHTML(url);
                if (string.IsNullOrEmpty(html))
                {
                    break;
                }
                url = this.ParseSave(html);
                if (string.IsNullOrEmpty(url))
                {
                    break;
                }
                System.Threading.Thread.Sleep(new Random().Next(30000, 60000));
            } while (true);
        }
        protected abstract string LoadHTML(string url);
        /// <summary>
        /// 解析保存并返回下一页链接
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected abstract string ParseSave(string html);
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
        protected void ClaenData()
        {
            this.Context.CleanData();
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

    }
}
