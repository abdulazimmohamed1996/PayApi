namespace PayArabic.Core.Model;

public class UserPaymentMethod : BaseEntity
{
    public long UserId { get; set; }
    public long PaymentMethod_Id { get; set; } //Refere to Integration Id with Type = PaymentMethod
    public string FeesType { get; set; }
    public float? FeesFixed_Amount { get; set; }
    public float? FeesPercent_Amount { get; set; }
    public string PaidBy { get; set; } // Vendor, Customer, Split
}