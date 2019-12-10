using System;
using System.IO;
using System.Text;
using HZ.Crawler.Data;
using HZ.Crawler.DataSpider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HZ.Crawler.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //Common.Logger.LoggerHost = "http://localhost/";
            var logger = new Common.Logger(typeof(Program));
            //编码注册
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //添加Nugget包Microsoft.Extensions.Configuration(ConfigurationBuilder) 和 Microsoft.Extensions.Configuration.Json(AddJsonFile)
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) //指定配置文件所在的目录
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) //指定加载的配置文件
            .Build(); //编译成对象

            var service = new ServiceCollection()
            .AddOptions()
            .AddSingleton<IConfiguration>(config)//单例
            //.AddTransient<ILoggerFactory, LoggerFactory>()
            .AddTransient<DataContext, ShiweijiaContext>()
            .AddTransient<BaseSpider, ShiweijiaCategory>()
            .AddTransient<BaseSpider, ShiweijiaProduct>() //注入服务
            .BuildServiceProvider(); //编译

            logger.Info("开始抓取");
            foreach (var spider in service.GetServices<BaseSpider>())
            {
               spider.Run();
            }
            logger.Info("抓取完成");
            Console.Read();
        }
    }
}
