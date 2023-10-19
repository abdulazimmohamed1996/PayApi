namespace PayArabic.Core.Model;
public class User : BaseEntity
{
    public long Parent_Id { get; set; }
    public string UserType { get; set; }
    public float? Balance { get; set; }
    public string Name { get; set; }
    public string Mobile { get; set; }
    public string Email { get; set; }
    public long? SMS_Id { get; set; }
    public long? Email_Id { get; set; }
    public string Company_Name { get; set; }
    public string Logo { get; set; }
    public string WebSite_Url { get; set; }
    public string Work_Email { get; set; }
    public string Work_Phone { get; set; }
    public int? Invoice_Valid_Days { get; set; }
    public string Invoice_Lang { get; set; } // En or Ar
    public string Terms_Condition { get; set; }
    public long? Bank_Id { get; set; }
    public string Account_Holder_Name { get; set; }
    public string Account_Number { get; set; }
    public string IBAN { get; set; }
    public string Commercial_License { get; set; }
    public string Signature_Authorization { get; set; }
    public string Article_Association { get; set; }
    public string Commercial_Register { get; set; }
    public string Civil_Id_Owner { get; set; }
    public string Civil_Id_Manager { get; set; }
    public string Bank_Account_Letter { get; set; }
    public string Other { get; set; }
    public string Password_Hash { get; set; }
    public string Password_Salt { get; set; }
    public string EmailActivationKey { get; set; }
    public string MobileActivationKey { get; set; }
    public bool? ContactActivated { get; set; }
    public int? Login_AccessFailed { get; set; }
    public DateTime? Login_LockDate { get; set; }
    public bool? Login_LockEnabled { get; set; }
    public bool? Reviewed { get; set; }
    public string Json_SocialLinks { get; set; }
    public string Social_Links_Json { get; set; }
    public string Account_Type { get; set; }//'Personal','Business'
    public string Sales_Referral_Code { get; set; }
    public string BiometricsPublicKey { get; set; }
    public string SMS_Sender { get; set; }
    public string KYC_doc { get; set; }
    public string Code { get; set; }
}