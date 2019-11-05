using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;

namespace HZ.Crawler.Data
{
    public class DataContext : DbContext
    {
        private string CSName { get; set; }

        private DBTypeEnum? _dbType;
        private DBTypeEnum DBType
        {
            get
            {
                if (!_dbType.HasValue)
                {
                    _dbType = DBTypeEnum.SQLite;
                }
                return _dbType.Value;
            }
            set
            {
                _dbType = value;
            }
        }
        public DataContext(IConfiguration configuration)
        {
            this.CSName = configuration.GetConnectionString(nameof(this.CSName));
            this._dbType = configuration.GetValue<DBTypeEnum>(nameof(this.DBType));
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelCreating(modelBuilder);
            foreach (var item in modelBuilder.Model.GetEntityTypes())
            {
                //modelBuilder.Entity(item.Name).Property("Id").ValueGeneratedNever();//不自动增长
                modelBuilder.Entity(item.Name).ToTable($"T_" + item.ClrType.Name);
            }
            base.OnModelCreating(modelBuilder);
        }
        protected virtual void ModelCreating(ModelBuilder modelBuilder)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (this.DBType)
            {
                case DBTypeEnum.SQLite:
                    optionsBuilder.UseSqlite(this.CSName);
                    break;
                case DBTypeEnum.SqlServer:
                    optionsBuilder.UseSqlServer(this.CSName);
                    break;
                default:
                    break;
            }
            optionsBuilder.UseLoggerFactory(LoggerFactory);//将EF生成的sql语句输出到debug输出
            base.OnConfiguring(optionsBuilder);
        }
        public static readonly LoggerFactory LoggerFactory = new LoggerFactory(new[] { new DebugLoggerProvider() });
    }
}
