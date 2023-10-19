namespace PayArabic.Core.Model;
public class Invoice : BaseEntity
{
    public long? OriginalInvoice_Id { get; set; }
    public long? VendorDeposit_Id { get; set; }
    public long Vendor_Id { get; set; }
    public string Type { get; set; }
    public string Code { get; set; }
    public string Status { get; set; } // Unpaid, Paid, Canceled, Deposited, Refunded
    public float? Amount { get; set; }
    public float? Fees { get; set; }
    public float? Subtotal { get; set; }
    public float? Total { get; set; }
    public string SendType { get; set; } // SMS, Email
    public string Lang { get; set; }
    public string Customer_Name { get; set; }
    public string Customer_Mobile { get; set; }
    public string Customer_Email { get; set; }
    public string Currency_Code { get; set; } //ISO_4217
    public string Discount_Type { get; set; } // Amount, Percent
    public float? Discount_Amount { get; set; }
    public DateTime? Expiry_Date { get; set; }
    public string Attachment { get; set; }
    public int? Remind_After { get; set; }
    public string Comment { get; set; }
    public bool? Terms_Condition_Enabled { get; set; }
    public string Terms_Condition { get; set; }
    public int? Views_No { get; set; }
    public string TransactionData { get; set; }
    public DateTime? Payment_Date { get; set; }
    public string Ref_Number { get; set; }
    public long? Key { get; set; }
}