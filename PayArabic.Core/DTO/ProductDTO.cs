namespace PayArabic.Core.DTO;
public class ProductDTO
{
    public class ProductList : BaseListDTO
    {
        public long Id { get; set; }
        public string CategoryNameEn { get; set; }
        public string CategoryNameAr { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public float Quantity { get; set; }
        public float Price { get; set; }
        public bool Stockable { get; set; }
    }
    public class ProductGetById : ProductList
    {
        public long CategoryId { get; set; }
        public long PaymentLinkKey { get; set; }
        public long ProductLinkKey { get; set; }
        public string DescEn { get; set; }
        public string DescAr { get; set; }
        public bool StockableShow { get; set; }
        public List<AttachmentDTOLight> Attachments { get; set; }
    }
    public class ProductInsert
    {
        public long CategoryId { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string DescEn { get; set; }
        public string DescAr { get; set; }
        public float Quantity { get; set; }
        public float Price { get; set; }
        public bool Stockable { get; set; }
        public bool StockableShow { get; set; }
        public List<AttachmentDTO> Attachments { get; set; }
    }
    public class ProductUpdate : ProductInsert
    {
        public long Id { get; set; }
    }
    public class ProductAutoComplete
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string CategoryNameEn { get; set; }
        public string CategoryNameAr { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public float Quantity { get; set; }
        public float Price { get; set; }
        public bool Stockable { get; set; }
    }
}