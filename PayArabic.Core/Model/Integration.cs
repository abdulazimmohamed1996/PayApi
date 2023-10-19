namespace PayArabic.Core.Model;
public class Integration : BaseEntity
{
    public string Name_En { get; set; }
    public string Name_Ar { get; set; }
    public string Code { get; set; }
    public string Type { get; set; }//PaymentMethod, SMS, Email
    public string PaymentMethodType { get; set; } // Normal, Pos
    public string CC_Types { get; set; }
    public int? Sort_No { get; set; }
}