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
            string spiderName = this.GetType().Name.ToLower();
            var list = this.Configuration.GetValue<List<SpiderConfig>>(nameof(SpiderConfig));
            //开始
            foreach (var config in list.Where(s => s.Name.ToLower() == spiderName))
            {
                foreach (var host in config.Hosts)
                {
                    this.CrawleHost(host);
                }
            }
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
                System.Threading.Thread.Sleep(new Random().Next(1000, 5000));
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
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        void SaveData<T>(params T[] ts) where T : BaseModel, new()
        {
            foreach (var t in ts)
            {
                this.Context.AddAsync(t);
            }
            this.Context.SaveChanges();
        }
        /// <summary>
        /// 解析失败的保存
        /// </summary>
        /// <param name="html"></param>
        void SaveFile(string html)
        {
            var dirName = "error";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            FileHelper.Write($"{dirName}/{this.GetType().Name.ToLower()}-{DateTime.Now.ToString("MMddHHmmss")}.txt", html);
        }

    }
}
