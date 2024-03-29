﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HZ.Crawler.Model.Shiweijia
{
    public class ProductModel : BaseModel
    {
        /// <summary>
        /// 商品编号
        /// </summary>
        public string ProductCode { get; set; }
        /// <summary>
        /// 品牌名称
        /// </summary>
        public string BrandName { get; set; }
        /// <summary>
        /// 品牌封面
        /// </summary>
        public string BrandImg { get; set; }
        /// <summary>
        /// 类别
        /// </summary>
        /// <value></value>
        public int CategoryId { get; set; }
        /// <summary>
        /// 风格
        /// </summary>
        public string Style { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 建议销售价
        /// </summary>
        public decimal SalePrice { get; set; }
        /// <summary>
        /// 供货价
        /// </summary>
        public decimal CostPrice { get; set; }
        /// <summary>
        /// 自定义销售价
        /// </summary>
        public decimal CustomPrice { get; set; }
        /// <summary>
        /// 显示图片(List<string>)
        /// </summary>
        public string MainImgs { get; set; }
        /// <summary>
        /// 详情图片(List<string>)
        /// </summary>
        public string DetailImgs { get; set; }
        /// <summary>
        /// 规格参数(Dictionary<string, string>)
        /// </summary>
        public string Specifications { get; set; }
        public string Thumbnails { get; set; }
        /// <summary>
        /// 特性(Dictionary<string, string>)
        /// </summary>
        public string Features { get; set; }
    }
}
