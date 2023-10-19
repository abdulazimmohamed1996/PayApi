namespace PayArabic.Core.DTO;
public class ProductCategoryDTO
{
    public class ProductCategoryList : BaseListDTO
    {
        public long Id { get; set; }
        public string Code { get; set; } 
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public int Sort { get; set; }
        public bool InActive { get; set; }
        public string CreateDate { get; set; }
    }
    public class ProductCategoryGetById : ProductCategoryList
    {
        
    }
    public class ProductCategoryInsert
    {
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public int Sort { get; set; }
    }
    public class ProductCategoryUpdate : ProductCategoryInsert
    {
        public long Id { get; set; }
        public bool InActive { get; set; }
    }
}