using HZ.Crawler.Common;
using HZ.Crawler.Common.Extensions;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Data;
using HZ.Crawler.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HZ.Crawler.DataSpider
{
    public abstract class BaseSpider
    {
        private DataContext Context { get; }
        private IConfiguration Configuration { get; }
        private string ImportMaterialHost { get; }
        private string MerchantID { get; }
        private int ThreadCount { get; }
        protected readonly Common.Logger Logger = null;
        public BaseSpider(IConfiguration configuration, DataContext context)
        {
            this.Logger = new Logger(this.GetType());
            this.Configuration = configuration;
            this.Context = context;
            this.ImportMaterialHost = configuration.GetValue(nameof(this.ImportMaterialHost), string.Empty);
            this.MerchantID = configuration.GetValue(nameof(this.MerchantID), string.Empty);
            this.ThreadCount = configuration.GetValue(nameof(this.ThreadCount), 5);
            this.Logger.Info($"初始化成功，线程数量：{this.ThreadCount}");
        }

        public void Run()
        {
            var config = new SpiderConfig();
            this.Configuration.GetSection(this.GetType().Name).Bind(config);
            //this.Configuration.GetValue<SpiderConfig>(this.GetType().Name);//反射不到数组
            this.Logger.Info($"{config.Name}开始采集");
            foreach (var host in config.Hosts)
            {
                this.Logger.Info($"开始采集{config.Name}-{host}");
                this.CrawleHost(host);
                this.Logger.Info($"采集结束{config.Name}-{host}");
            }
            this.Logger.Info($"{config.Name}采集结束");
        }
        private void CrawleHost(string host)
        {
            string url = host;
            var taskList = new List<Task>();
            foreach (var item in this.InitSpider())
            {
                this.Logger.Info($"开启线程【{System.Threading.Thread.CurrentThread.ManagedThreadId}】");
                if (taskList.Count >= this.ThreadCount)//线程数量达到
                {
                    taskList = taskList.Where(t => !t.IsCanceled && !t.IsCompleted && !t.IsFaulted).ToList();
                    Task.WaitAny(taskList.ToArray());//等待完成一个
                }
                else
                    Task.Delay(500);
                string temp = item;
                taskList.Add(Task.Factory.StartNew(() => TaskRun(url, temp)));
            }
            Task.WaitAll(taskList.ToArray());//等待所有完成
        }
        private void TaskRun(string url, string param)
        {
            do
            {
                this.Logger.Info($"加载【{url}】......");
                string html = string.Empty;
                try
                {
                    html = this.LoadHTML(url, param);
                }
                catch (System.Exception ex)
                {
                    this.Logger.Error($"加载数据异常", ex);
                }
                if (string.IsNullOrEmpty(html)) break;
                try
                {
                    url = this.ParseSave(html, param);
                }
                catch (System.Exception ex)
                {
                    this.SaveFile(html);
                    this.Logger.Error($"解析数据异常", ex);
                }
                System.Threading.Thread.Sleep(new Random().Next(3000, 6000));
            } while (!string.IsNullOrEmpty(url));
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
            string path = $"{dirName}/{this.GetType().Name.ToLower()}-{DateTime.Now.ToString("MMddHHmmssfff")}-{Guid.NewGuid().ToString("N").Substring(0, 4)}.txt";
            FileHelper.Write(path, html);
            this.Logger.Info($"已保存文件:{path}");
        }
        /// <summary>
        /// 提交产品
        /// </summary>
        /// <param name="dataDic"></param>
        /// <returns></returns>
        protected bool SubmitProduct(Dictionary<string, string> dataDic)
        {
            var client = HttpClientFactory.Create();
            dataDic.Add("action", "addPlatformMaterial");
            dataDic.Add("merchantID", this.MerchantID);
            string data = string.Join("&", dataDic.Select(d => $"{d.Key}={d.Value}"));
            string result = client.Request(this.ImportMaterialHost, HttpMethod.POST, data, Encoding.UTF8, "application/x-www-form-urlencoded");
            var json = JsonDocument.Parse(result);
            if (json.RootElement.GetProperty("OK").GetBoolean())
            {
                this.Logger.Info($"{dataDic["productID"]}-{dataDic["materialName"]} 提交成功！");
                return true;
            }
            this.SaveFile(data);
            this.Logger.Info($"{dataDic["productID"]}-{dataDic["materialName"]} 提交失败！");
            return false;
        }
        protected List<string> UploadImgsByLink(params string[] links)
        {
            var base64List = new List<string>();
            foreach (var item in links)
            {//图片转base64
                string ext = item.Substring(item.LastIndexOf(".") + 1);
                if (ext.Contains("-"))
                {
                    ext = ext.Substring(0, ext.LastIndexOf('-'));
                }
                string base64Str = Convert.ToBase64String(new WebClient().DownloadData(item));
                base64List.Add($"data:image/{ext};base64,{base64Str}");
            }
            return UploadImgs(base64List.ToArray());
        }
        protected List<string> UploadImgsByFile(params string[] files)
        {
            var base64List = new List<string>();
            foreach (var item in files)
            {//图片转base64
                string ext = item.Substring(item.LastIndexOf(".") + 1);
                string base64Str = Convert.ToBase64String(FileHelper.ReadToBytes(item));
                base64List.Add($"data:image/{ext};base64,{base64Str}");
            }
            return UploadImgs(base64List.ToArray());
        }
        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="base64Array"></param>
        /// <returns></returns>
        protected List<string> UploadImgs(params string[] base64Array)
        {
            var imgList = new List<string>();
            if (base64Array == null || base64Array.Length == 0)
            {
                return imgList;
            }
            var client = HttpClientFactory.Create();
            var dataDic = new Dictionary<string, string>
            {
                {"action","upfileImages"},
                {"merchantID",this.MerchantID},
                {"imgDataJson",Newtonsoft.Json.JsonConvert.SerializeObject(base64Array)}
            };
            string postData = string.Join("&", dataDic.Select(d => $"{d.Key}={d.Value.ToUrlEncode()}"));
            string result = client.Request(this.ImportMaterialHost, HttpMethod.POST, postData, Encoding.UTF8, "application/x-www-form-urlencoded");
            var root = JToken.Parse(result);
            if (root.Value<bool>("OK"))
            {
                this.Logger.Info($"上传图片成功！");
                imgList.AddRange(root["Message"].Values<string>());
            }
            return imgList;
        }
    }
}
