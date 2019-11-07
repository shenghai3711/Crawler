namespace HZ.Crawler.Model.Shiweijia
{
    public class CategoryModel : BaseModel
    {
        public string CategoryName { get; set; }
        public string CategoryImg { get; set; }
        public int? ParentId { get; set; }
    }
}
