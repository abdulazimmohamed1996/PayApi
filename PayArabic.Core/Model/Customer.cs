namespace PayArabic.Core.Model;
public class Customer : BaseEntity
{
    public long? Vendor_Id { get; set; }
    public string Name { get; set; }
    public string Ref_Number { get; set; }
    public string Mobile { get; set; }
    public string Email { get; set; }
    public int? Bank_Id { get; set; }
    public string Account_Holder_Name { get; set; }
    public string Account_Number { get; set; }
    public string IBAN { get; set; }
    public string Other { get; set; }
}