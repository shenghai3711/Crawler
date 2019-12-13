using HZ.Crawler.Common;
using HZ.Crawler.Common.Extensions;
using HZ.Crawler.Common.Net;
using HZ.Crawler.Data;
using HZ.Crawler.Model;
using HZ.Crawler.RedisService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private IConfiguration Configuration { get; }
        private string ImportMaterialHost { get; }
        private string MerchantID { get; }
        private int ThreadCount { get; }
        private DataContext Context { get; }
        private bool _Sign = true;
        protected readonly Common.Logger Logger = null;
        public static RedisHashService RedisClient { get; }
        private static ConcurrentBag<ImgModel> ImgList { get; set; }
        static BaseSpider()
        {
            RedisClient = new RedisHashService();
            RedisClient.FlushAll();
        }
        public BaseSpider(IConfiguration configuration, DataContext context)
        {
            this.Context = context;
            this.Logger = new Logger(this.GetType());
            this.Configuration = configuration;
            this.ImportMaterialHost = configuration.GetValue(nameof(this.ImportMaterialHost), string.Empty);
            this.MerchantID = configuration.GetValue(nameof(this.MerchantID), string.Empty);
            this.ThreadCount = configuration.GetValue(nameof(this.ThreadCount), 5);
            this.Logger.Info($"{this.GetType().Name}初始化成功，设置线程数量：{this.ThreadCount}");
        }
        public static async Task Init(DataContext context)
        {
            var imgList = await context.ImgModels.ToListAsync();
            ImgList = new ConcurrentBag<ImgModel>(imgList);
            // foreach (var item in imgList)
            // {
            //     RedisClient.SetEntryInHash(nameof(ImgModel), item.MD5Key, item.UploadedUrl);
            // }
            // context.ImgModels.RemoveRange(imgList);
        }
        public static async Task Finish(DataContext context)
        {
            // var imgList = RedisClient.GetAllEntriesFromHash(nameof(ImgModel)).Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => new ImgModel
            // {
            //     MD5Key = kv.Key,
            //     UploadedUrl = kv.Value
            // });

            await context.ImgModels.AddRangeAsync(ImgList.Where(i => i.Id == 0));
            await context.SaveChangesAsync();
        }
        public void Run()
        {
            var config = new SpiderConfig();
            Stopwatch watch = new Stopwatch();
            this.Configuration.GetSection(this.GetType().Name).Bind(config);
            //this.Configuration.GetValue<SpiderConfig>(this.GetType().Name);//反射不到数组
            this.Logger.Info($"{config.Name}开始采集");
            watch.Start();
            OnBegin();
            foreach (var host in config.Hosts)
            {
                this.Logger.Info($"开始采集{config.Name}-{host}");
                this.CrawleHost(host);
                this.Logger.Info($"采集结束{config.Name}-{host}");
            }
            OnEnd();
            watch.Stop();
            this.Logger.Info($"{config.Name}采集结束---用时：{watch.ElapsedMilliseconds / (1000 * 60.0)}/分");
        }
        private void CrawleHost(string host)
        {
            string url = host;
            var taskList = new List<Task>();
            foreach (var item in this.InitSpider())
            {
                taskList = taskList.Where(t => !t.IsCanceled && !t.IsCompleted && !t.IsFaulted).ToList();
                if (taskList.Count >= this.ThreadCount)//线程数量达到
                {
                    Task.WaitAny(taskList.ToArray());//等待完成一个
                }
                else
                    Task.Delay(500);
                string temp = item;
                taskList.Add(Task.Run(() =>
                {
                    this.Logger.Info($"***************************************************************************");
                    this.Logger.Info($"开启线程，线程ID：【{System.Threading.Thread.CurrentThread.ManagedThreadId}】");
                    TaskRun(url, temp);
                    this.Logger.Info($"结束线程，线程ID：【{System.Threading.Thread.CurrentThread.ManagedThreadId}】");
                    this.Logger.Info($"***************************************************************************");
                }));
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
                catch (Exception ex)
                {
                    this.Logger.Error($"加载数据异常", ex);
                }
                if (string.IsNullOrEmpty(html)) break;
                try
                {
                    url = this.ParseSave(html, param);
                }
                catch (Exception ex)
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
        protected void OnBegin()
        {
            Begin();
            Task.Run(() =>
            {
                do
                {
                    if (ImgList.Where(i => i.Id == 0).Count() > 10)
                    {
                        this.Context.ImgModels.AddRangeAsync(ImgList.Where(i => i.Id == 0));
                        this.Context.SaveChangesAsync();
                    }
                    Task.Delay(3000);
                } while (_Sign);
            });

        }
        protected void OnEnd()
        {
            End();
            _Sign = false;
        }
        protected virtual void Begin() { }
        protected virtual void End() { }
        protected abstract string LoadHTML(string url, string param = null);
        /// <summary>
        /// 解析保存并返回下一页链接
        /// </summary>
        /// <param name="html"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected abstract string ParseSave(string html, string param = null);

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
            try
            {
                this.Logger.Debug(data);
                string result = client.Request(this.ImportMaterialHost, HttpMethod.POST, data, Encoding.UTF8, "application/x-www-form-urlencoded");
                this.Logger.Debug(result);
                var json = JsonDocument.Parse(result);
                if (json.RootElement.GetProperty("OK").GetBoolean())
                {
                    this.Logger.Info($"{dataDic["productID"]}-{dataDic["materialName"]} 提交成功！");
                    return true;
                }
                this.Logger.Info($"{dataDic["productID"]}-{dataDic["materialName"]} 提交失败！");
            }
            catch (Exception ex)
            {
                this.Logger.Error($"{dataDic["productID"]}-{dataDic["materialName"]} 提交异常！", ex);
            }
            this.SaveFile(data);
            return false;
        }
        protected List<string> UploadImgsByLink(params string[] links)
        {
            var base64List = new List<string>();
            foreach (var item in links.Where(l => !string.IsNullOrEmpty(l)))
            {//图片转base64
                string ext = item.Substring(item.LastIndexOf(".") + 1);
                if (ext.Contains("-"))
                {
                    ext = ext.Substring(0, ext.LastIndexOf('-'));
                }
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        string base64Str = Convert.ToBase64String(new WebClient().DownloadData(item));
                        base64List.Add($"data:image/{ext};base64,{base64Str}");
                        break;
                    }
                    catch (Exception)
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                }
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
                return imgList;
            var imgModels = new ImgModel[base64Array.Length];
            for (int i = 0; i < base64Array.Length; i++)
            {
                string md5 = Encrypt.ToMd5(base64Array[i], Encoding.UTF8);
                //string uploadedUrl = RedisClient.GetValueFromHash(nameof(ImgModel), md5);
                var model = ImgList.FirstOrDefault(i => i.MD5Key == md5);//await this.Context.ImgModels.FirstOrDefaultAsync(i => i.MD5Key == md5);
                imgModels[i] = model ?? new ImgModel
                {
                    MD5Key = md5,
                    Base64 = base64Array[i]
                };
            }
            var uploadList = imgModels.Where(i => string.IsNullOrEmpty(i.UploadedUrl)).ToList();
            if (uploadList.Count == 0)
            {
                return imgModels.Select(i => i.UploadedUrl).ToList();
            }
            var client = HttpClientFactory.Create();
            var dataDic = new Dictionary<string, string>
            {
                {"action","upfileImages"},
                {"merchantID",this.MerchantID},
                {"imgDataJson",JsonConvert.SerializeObject(uploadList.Select(u=>u.Base64))}
            };
            string postData = string.Join("&", dataDic.Select(d => $"{d.Key}={d.Value.ToUrlEncode()}"));
            string result = client.Request(this.ImportMaterialHost, HttpMethod.POST, postData, Encoding.UTF8, "application/x-www-form-urlencoded");
            var root = JToken.Parse(result);
            if (root.Value<bool>("OK"))
            {
                int count = 0;
                foreach (var item in root["Message"].Values<string>())
                {
                    uploadList[count].UploadedUrl = item;
                    ImgList.Add(new ImgModel
                    {
                        UploadedUrl = item,
                        MD5Key = uploadList[count].MD5Key
                    });
                    //RedisClient.SetEntryInHash(nameof(ImgModel), uploadList[count].MD5Key, uploadList[count].UploadedUrl);
                    count++;
                }
                imgList.AddRange(imgModels.Select(i => i.UploadedUrl));
            }
            return imgList;
        }
    }
}
