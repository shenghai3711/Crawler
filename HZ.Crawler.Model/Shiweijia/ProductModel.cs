using System.Collections.Generic;

namespace HZ.Crawler.Model.Shiweijia
{
    public class ProductModel : BaseModel
    {
        /// <summary>
        /// 商品编号
        /// </summary>
        public string ProductCode { get; set; }
        /// <summary>
        /// 品牌
        /// </summary>
        public string Brand { get; set; }
        /// <summary>
        /// 风格
        /// </summary>
        public string Style { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string FullDescription { get; set; }
        /// <summary>
        /// 价格
        /// </summary>
        public int SalePrice { get; set; }
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
        /// <summary>
        /// 产品规格
        /// </summary>
        public List<ProductSpecificationModel> ProductSpecifications { get; set; }

    }
}
