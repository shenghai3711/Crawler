using System.ComponentModel.DataAnnotations.Schema;

namespace HZ.Crawler.Model.Shiweijia
{
    public class CategoryModel : BaseModel
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.None)]//不自动增长
        //public new int Id { get; set; }
        public string CategoryName { get; set; }
        public string CategoryImg { get; set; }
        public int? ParentId { get; set; }
    }
}
