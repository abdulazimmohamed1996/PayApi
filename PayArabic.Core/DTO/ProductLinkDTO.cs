namespace PayArabic.Core.DTO;
public class ProductLinkDTO
{
    public class ProductLinkList : BaseListDTO
    {
        public long Id { get; set; }
        public long Key { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public bool InActive { get; set; }
    }
    public class ProductLinkGetById : ProductLinkList
    {
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public List<ProductLinkCategory> Categories { get; set; }
    }
    public class ProductLinkInsert
    {
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public List<long> Products { get; set; }
    }
    public class ProductLinkUpdate : ProductLinkInsert
    {
        public long Id { get; set; }
        public bool InActive { get; set; }
    }
    public class ProductLinkGetForPayment
    {
        public long Key { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public long VendorId { get; set; }
        public string VendorCompany { get; set; }
        public string VendorLogo { get; set; }
        public string VendorLogoPath { get; set; }
        public List<ProductLinkCategory> Categories { get; set; }
    }
    public class ProductLinkCategory
    {
        public long Id { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public List<ProductLinkProduct> Products { get; set; }
    }
    public class ProductLinkProduct
    {
        public long Id { get; set; }
        public long CategoryId { get; set; }
        public long ProductLinkId { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string DescEn { get; set; }
        public string DescAr { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public bool Stockable { get; set; }
        public bool StockableShow { get; set; }
        public List<AttachmentDTOLight> Attachments { get; set; }
    }
}