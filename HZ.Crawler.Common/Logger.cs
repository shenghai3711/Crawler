using System;
using System.IO;
using log4net;
using log4net.Config;

namespace HZ.Crawler.Common
{
    public class Logger
    {
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
        }

        public void Warn(string msg)
        {
            Console.WriteLine(msg);
            loger.Warn(msg);
        }

        public void Info(string msg)
        {
            Console.WriteLine(msg);
            loger.Info(msg);
        }

        public void Debug(string msg)
        {
            Console.WriteLine(msg);
            loger.Debug(msg);
        }
    }
}
