using PayArabic.Core.Model;

namespace PayArabic.Core.DTO;
public class DepositDTO
{
    public class DepositList : BaseListDTO
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Number { get; set; }
        public string Status { get; set; }
        public float Amount { get; set; }
        public string Date { get; set; }
    }
    public class DepositGetById : DepositList
    {
        public string Note { get; set; }
        public string CurrencyCode { get; set; }
        public List<Vendor> Vendors { get; set; }
        public List<AttachmentDTOLight> Attachments { get; set; }
    }
    public class DepositInsert
    {
        public string Type { get; set; }
        public string Status { get; set; }
        public string Number { get; set; }
        public string Date { get; set; }
        public string Note { get; set; }
        public List<LightVendor> Vendors { get; set; }
        public List<AttachmentDTO> Attachments { get; set; }
    }
    public class DepositUpdate : DepositInsert
    {
        public long Id { get; set; }
    }
    public class Vendor
    {
        public long VendorId { get; set; }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public string VendorCompany { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IBAN { get; set; }
        public string BankNameAr { get; set; }
        public string BankNameEn { get; set; }
        public string BankSwift { get; set; }
        public int InvoiceCount { get; set; }
        public float InvoiceTotal { get; set; }
        public IEnumerable<InvoiceDTO.ForVendor> Invoices { get; set; }
    }
    public class LightVendor
    {
        public long VendorId { get; set; }
        public List<long> DepositInvoiceIds { get; set; }
    }
}