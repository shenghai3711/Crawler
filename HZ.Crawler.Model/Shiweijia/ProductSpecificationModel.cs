using System;
using System.Collections.Generic;
using System.Text;

namespace HZ.Crawler.Model.Shiweijia
{
    public class ProductSpecificationModel : BaseModel
    {
        public string ProductName { get; set; }
        public decimal SalePrice { get; set; }
        public string Thumbnails { get; set; }
        /// <summary>
        /// 特性(List<string>)
        /// </summary>
        public string Features { get; set; }
        public int ProductId { get; set; }
    }
}
