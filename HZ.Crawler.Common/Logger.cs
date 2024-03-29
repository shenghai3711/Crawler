using NLog;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace HZ.Crawler.Common
{
    public class Logger
    {
        public static string LoggerHost;
        static Logger()
        {
            //var logRepository = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            //var fileInfo = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CfgFiles\\log4net.cfg.xml"));
            //XmlConfigurator.Configure(logRepository, fileInfo);
            //ILog Log = LogManager.GetLogger(typeof(Logger));
            //Log.Info("系统初始化Logger模块");
        }
        private NLog.Logger loger = null;
        public Logger(Type type)
        {
            loger = LogManager.GetCurrentClassLogger(type);
        }

        public void Error(string msg = "出现异常", Exception ex = null)
        {
            loger.Error(ex, msg);
            CommitLogInfo("ERROR", $"{msg}:{ex?.Message}");
        }

        public void Warn(string msg)
        {
            loger.Warn(msg);
            CommitLogInfo("WARNING", msg);
        }

        public void Info(string msg)
        {
            loger.Info(msg);
            CommitLogInfo("INFO", msg);
        }

        public void Debug(string msg)
        {
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
