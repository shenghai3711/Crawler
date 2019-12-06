using HZ.Crawler.Model;
using HZ.Crawler.Model.Shiweijia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HZ.Crawler.Data
{
    public class ShiweijiaContext : DataContext
    {
        public ShiweijiaContext(IConfiguration configuration)
            : base(configuration)
        {
            //this.Database.EnsureDeleted();//存在就删除
            this.Database.EnsureCreated();//不存在就创建
        }
        public DbSet<CategoryModel> CategoryModels { get; set; }
        public DbSet<ProductModel> ProductModels { get; set; }
        public DbSet<BrandModel> BrandModels { get; set; }

        protected override void ModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CategoryModel>()
                        .Property(t => t.Id)
                        .ValueGeneratedNever();//不自动增长

            modelBuilder.Entity<ProductModel>()
                        .Property(t => t.Id)
                        .ValueGeneratedNever();//不自动增长

            modelBuilder.Entity<BrandModel>()
                        .Property(t => t.Id)
                        .ValueGeneratedNever();//不自动增长
        }
    }
}
