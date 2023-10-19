namespace PayArabic.Core.DTO;
public class PaymentLinkDTO
{
    public class PaymentLinkList : BaseListDTO
    {
        public long Id { get; set; }
        public long Key { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Lang { get; set; }
        public float Amount { get; set; }
        public bool IsOpenAmount { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public string CreateDate { get; set; }
        public bool InActive { get; set; }
    }
    public class PaymentLinkGetById : PaymentLinkList
    {
        public string Currency { get; set; }
        public string Comment { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
    }
    public class PaymentLinkInsert
    {
        public string Title { get; set; }
        public float Amount { get; set; }
        public string Currency { get; set; }
        public string Lang { get; set; }
        public bool IsOpenAmount { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public string Comment { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public bool InActive { get; set; }
    }
    public class PaymentLinkUpdate : PaymentLinkInsert
    {
        public long Id { get; set; }
    }
    public class PaymentLinkGetForPayment
    {
        public long Key { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public float Amount { get; set; }
        public bool IsOpenAmount { get; set; }
        public float MinAmount { get; set; }
        public float MaxAmount { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public string VendorCompany { get; set; }
        public string VendorLogo { get; set; }
        public string VendorLogoPath { get; set; }
    }
}