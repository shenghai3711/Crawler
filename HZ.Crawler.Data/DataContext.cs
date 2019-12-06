using HZ.Crawler.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel.DataAnnotations.Schema;

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
        public DbSet<ImgModel> ImgModels { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var item in modelBuilder.Model.GetEntityTypes())
            {
                //modelBuilder.Entity(item.Name).Property("Id").ValueGeneratedNever();//不自动增长
                modelBuilder.Entity(item.Name).ToTable($"T_" + item.ClrType.Name);
            }
            ModelCreating(modelBuilder);
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
            base.OnConfiguring(optionsBuilder);
        }
    }
}
