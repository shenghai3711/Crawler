using System;
using System.IO;
using System.Net;
using System.Text;
using log4net;
using log4net.Config;

namespace HZ.Crawler.Common
{
    public class Logger
    {
        public static string LoggerHost;
        private readonly static Net.IHttpClient Client = Net.HttpClientFactory.Create();
        static Logger()
        {
            var Repository = LogManager.CreateRepository("NETCoreRepository");
            var fileInfo = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CfgFiles\\log4net.cfg.xml"));
            XmlConfigurator.Configure(Repository, fileInfo);
            ILog Log = LogManager.GetLogger(typeof(Logger));
            Log.Info("系统初始化Logger模块");
        }

        private ILog loger = null;
        public Logger(Type type)
        {
            loger = LogManager.GetLogger(type);
        }

        public void Error(string msg = "出现异常", Exception ex = null)
        {
            Console.WriteLine(msg);
            loger.Error(msg, ex);
            CommitLogInfo("ERROR", $"{msg}:{ex?.Message}");
        }

        public void Warn(string msg)
        {
            Console.WriteLine(msg);
            loger.Warn(msg);
            CommitLogInfo("WARNING", msg);
        }

        public void Info(string msg)
        {
            Console.WriteLine(msg);
            loger.Info(msg);
            CommitLogInfo("INFO", msg);
        }

        public void Debug(string msg)
        {
            Console.WriteLine(msg);
            loger.Debug(msg);
            CommitLogInfo("DEBUG", msg);
        }
        private void CommitLogInfo(string type, string msg)
        {
            if (string.IsNullOrEmpty(LoggerHost)) return;
            System.Threading.Tasks.Task.Run(() =>
            {
                Request($"{LoggerHost}logger/postlogmessage", new
                {
                    Category = type,
                    Message = msg
                });
            });
        }

        private void Request(string url, object json)
        {
            if (string.IsNullOrEmpty(url) || json == null)
            {
                return;
            }
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";
            string data = System.Text.Json.JsonSerializer.Serialize(json);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            req.ContentLength = bytes.Length;
            Stream rs = req.GetRequestStream();
            rs.Write(bytes, 0, bytes.Length);
            rs.Flush();
            rs.Close();
            try
            {
                using (WebResponse res = req.GetResponse())
                {
                    try
                    {
                        string result = string.Empty;
                        HttpWebResponse response = (HttpWebResponse)res;
                        if (response != null)
                        {
                            System.Console.Write($"{response.StatusCode}:");
                        }
                        using (var sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                        {
                            System.Console.WriteLine(sr.ReadToEnd());
                        }
                    }
                    finally
                    {
                        res.Close();
                    }
                }
            }
            catch (WebException e)
            {
                if (null == e.Response)
                {
                    throw e;
                }
                using (StreamReader sr = new StreamReader(e.Response.GetResponseStream(), Encoding.UTF8))
                {
                    System.Console.WriteLine(sr.ReadToEnd());
                }
            }
        }

    }
}
