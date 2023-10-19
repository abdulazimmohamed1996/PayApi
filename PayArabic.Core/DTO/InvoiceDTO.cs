namespace PayArabic.Core.DTO;
public class InvoiceDTO
{
    public class Composite
    {
        public List<InvoiceInsert> Invoices { get; set; }
    }
    public class InvoiceUpdate:InvoiceInsert
    {
        public long Id { get; set; }
    }
    public class InvoiceInsert
    {
        //public long Key { get; set; }
        public long OriginalInvoiceId { get; set; }
        public long VendorId { get; set; }
        public string Type { get; set; } // Invoice, Refund
        public string Status { get; set; }
        public string CurrencyCode { get; set; }
        public string ExpiryDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
        public string VendorName { get; set; }
        public string VendorCompany { get; set; }
        public string VendorEmail { get; set; }
        public string Lang { get; set; }
        public float Amount { get; set; }
        //public float Subtotal { get; set; }
        public float Fees { get; set; }
        //public float Total { get; set; }
        public string SMSSender { get; set; }
        public string SendType { get; set; }
        public string DiscountType { get; set; }
        public float DiscountAmount { get; set; }
        public string RefNumber { get; set; }
        public string Comment { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public int RemindAfter { get; set; }
        public List<InvoiceItemDTO> Items { get; set; }
        public List<AttachmentDTO> Attachments { get; set; }
    }
    public class InvoiceList : BaseListDTO
    {
        public long Id { get; set; }
        public long VendorId { get; set; }
        public string Key { get; set; }
        public string Code { get; set; }
        public string Status { get; set; } // 1=Unpaid, 2= Paid, 3= Canceled, 4= Deposited, 5= Refunded
        public float Amount { get; set; }
        public float Total { get; set; }
        public float DiscountAmount { get; set; }
        
        public float Subtotal { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
        public string RefNumber { get; set; }
        public string CurrencyCode { get; set; } //ISO_4217
        public string ExpiryDate { get; set; }
        public int ViewsNo { get; set; }
        public string VendorName { get; set; }
        public float Fees { get; set; }
        public string PaymentDate { get; set; }
        public string CreateDate { get; set; }
    }
    public class InvoiceGetById : InvoiceList
    {
        public float AlternateAmount { get; set; }
        public string SendType { get; set; }
        public string Lang { get; set; }
        public int RemindAfter { get; set; }
        public string Comment { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public string URL { get; set; }
        public string QR { get; set; }
        public List<InvoiceItemDTO> Items { get; set; }
        public List<AttachmentDTOLight> Attachments { get; set; }
    }
    public class Info
    {
        public long Id { get; set; }
        public long Key { get; set; }
        public long OriginalInvoiceId { get; set; }
        public long VendorId { get; set; }
        public string Type { get; set; } // Invoice, Refund
        public string Status { get; set; }
        public string CurrencyCode { get; set; }
        public string ExpiryDate { get; set; }
        public string Code { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
        public string VendorName { get; set; }
        public string VendorCompany { get; set; }
        public string VendorEmail { get; set; }
        public string Lang { get; set; }
        public double Amount { get; set; }
        public double Subtotal { get; set; }
        public double Fees { get; set; }
        public double Total { get; set; }
        public string SmsSender { get; }
        public long CreatedBy { get; set; }
    }
    public class InvoicePayment
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public long VendorId { get; set; }
        public string Code { get; set; }
        public string RefNumber { get; set; }
        public string Status { get; set; } // 1=Unpaid, 2= Paid, 3= Canceled, 4= Deposited, 5= Refunded
        public float Amount { get; set; }
        public string SendType { get; set; } // 1=SMS, 2=Email
        public string Lang { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
        public string CurrencyCode { get; set; } //ISO_4217
        public string DiscountType { get; set; }
        public float DiscountAmount { get; set; }
        public string ExpiryDate { get; set; }
        public int RemindAfter { get; set; }
        public string Comment { get; set; }
        public bool TermsConditionEnabled { get; set; }
        public string TermsCondition { get; set; }
        public float Subtotal { get; set; }
        public float Fees { get; set; }
        public float Total { get; set; }
        public string VendorLogo { get; set; }
        public string VendorWebSiteUrl { get; set; }
        public string VendorSocialLinksJson { get; set; }
        public string VendorCompany { get; set; }
        public List<InvoiceItemDTO> Items { get; set; }
        public List<AttachmentDTOLight> Attachments { get; set; }
    }
    public class ForDeposit
    {
        public long Id { get; set; }
        public long Key { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
        public string Status { get; set; } // Unpaid, Paid, Canceled, Deposited, Refunded
        public float Amount { get; set; }
        public float Subtotal { get; set; }
        public float Fees { get; set; }
        public float Total { get; set; }
        public string CreateDate { get; set; }
        public string PaymentDate { get; set; }
        public string CurrencyCode { get; set; }
        public string CustomerName { get; set; }

        public long VendorId { get; set; }
        public long VendorDepositId { get; set; }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public string VendorCompany { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IBAN { get; set; }
        public string BankNameAr { get; set; }
        public string BankNameEn { get; set; }
        public string BankSwift { get; set; }
    }
    public class ForVendor
    {
        public long Id { get; set; }
        public long Key { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public float Amount { get; set; }
        public float Subtotal { get; set; }
        public float Fees { get; set; }
        public float Total { get; set; }
        public string CreateDate { get; set; }
        public string PaymentDate { get; set; }
        public string CurrencyCode { get; set; }
        public string CustomerName { get; set; }
    }
    public class ForPaymentLink
    {
        public long PaymentLinkKey { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
        public string Comment { get; set; }
        public float Amount { get; set; }
    }
    public class ForProductLink
    {
        public long ProductLinkKey { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
        public string Comment { get; set; }
        public string Lang { get; set; }
        public double TotalAmount { get; set; }
        public List<ForProductLinkItem> Products { get; set; }
    }
    public class ForProductLinkItem
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public float Amount { get; set; }
    }
}