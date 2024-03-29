﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HZ.Crawler.Model
{
    public class BaseModel
    {
        [Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.None)]//不自动增长
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]///自动增长(默认)
        public int Id { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]//在添加或更新时生成值
        public DateTime UpdateDate { get; set; } = DateTime.Now;
    }
}
