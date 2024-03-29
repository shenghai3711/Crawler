using System;
using ServiceStack.Redis;

namespace HZ.Crawler.RedisService.Init
{
    public class RedisManager
    {
        /// <summary>
        /// redis配置文件信息
        /// </summary>
        private static RedisConfigInfo RedisConfigInfo = new RedisConfigInfo();

        private static PooledRedisClientManager prcManager;

        /// <summary>
        /// 静态构造方法，初始化链接池管理对象
        /// </summary>
        static RedisManager()
        {
            CreateManager();
        }

        /// <summary>
        /// 创建链接池管理对象
        /// </summary>
        private static void CreateManager()
        {
            string[] WriteServerConStr = RedisConfigInfo.WriteServerList.Split(',');
            string[] ReadServerConStr = RedisConfigInfo.ReadServerList.Split(',');
            prcManager = new PooledRedisClientManager(ReadServerConStr, WriteServerConStr,
                             new RedisClientManagerConfig
                             {
                                 MaxWritePoolSize = RedisConfigInfo.MaxWritePoolSize,
                                 MaxReadPoolSize = RedisConfigInfo.MaxReadPoolSize,
                                 AutoStart = RedisConfigInfo.AutoStart,
                             });
        }

        /// <summary>
        /// 客户端缓存操作对象
        /// </summary>
        public static IRedisClient GetClient()
        {
            return prcManager.GetClient();
        }
    }
}
