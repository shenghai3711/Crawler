using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HZ.Crawler.Model
{
    public class BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]//不自动增长
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]//自动增长(默认)
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]//添加时自动增长
        public int Id { get; set; }
    }
}
