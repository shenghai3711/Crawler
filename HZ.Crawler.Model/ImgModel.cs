using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HZ.Crawler.Model
{
    public class ImgModel : BaseModel
    {
        /// <summary>
        /// MD5值
        /// </summary>
        public string MD5Key { get; set; }
        /// <summary>
        /// 已上传的链接
        /// </summary>
        [Required]
        public string UploadedUrl { get; set; }
        [NotMapped]
        public string Base64 { get; set; }
    }
}
