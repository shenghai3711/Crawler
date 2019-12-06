using System.ComponentModel.DataAnnotations.Schema;

namespace HZ.Crawler.Model.Shiweijia
{
    public class BrandModel : BaseModel
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.None)]//不自动增长
        //public new int Id { get; set; }
        public string BrandName { get; set; }
        public string BrandImg { get; set; }
    }
}
