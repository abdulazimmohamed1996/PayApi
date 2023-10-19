namespace PayArabic.Core.Model;

public class UserPaymentMethodDTO
{
    public long Id { get; set; }
    public string NameEn { get; set; }
    public string NameAr { get; set; }
    public string Code { get; set; }
    public string PaymentMethodType { get; set; }
    public string FeesType { get; set; }
    public float FeesFixedAmount { get; set; }
    public float FeesPercentAmount { get; set; }
    public string PaidBy { get; set; }// Vendor, Customer, Split
    public bool InActive { get; set; }
}