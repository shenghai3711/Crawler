using HZ.Crawler.Model.Shiweijia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
        public DbSet<BrandModel> BrandModels { get; set; }
        public DbSet<CategoryModel> CategoryModels { get; set; }
        public DbSet<ProductModel> ProductModels { get; set; }
        public DbSet<ProductSpecificationModel> ProductSpecificationModels { get; set; }
        public DbSet<StyleModel> StyleModels { get; set; }
    }
}
