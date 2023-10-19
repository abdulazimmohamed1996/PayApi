using PayArabic.Core.Model;

namespace PayArabic.Core.DTO;
public class UserDTO
{
    public class Login
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool isMobile { get; set; }
        public string recaptchaToken { get; set; }

    }
    public class Light
    {
        public long Id { get; set; }
        public long ParentId { get; set; } //used for employee user type

        public string UserType { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public bool Reviewed { get; set; }
    }
    public class LightInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string EmailActivationKey { get; set; }
        public string MobileActivationKey { get; set; }
    }
    public class Info : Light
    {
        public string Error { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public int LoginAccessFailed { get; set; } = 0;
        public DateTime LoginLockDate { get; set; } = DateTime.Now;
        public bool LoginLockEnabled { get; set; } = false;
        public bool ContactActivated { get; set; } = false;
        public bool InActive { get; set; } = true;
    }
    public class RequestInfo
    {
        public long UserId { get; set; }
        public string TokenValue { get; set; } // TokenExpired | InvalidToken | ''
        public string PermissionValue { get; set; } // AccessPermissionDenied | RealPermissionValue
        public bool AuditValue { get; set; } // 1 | 0
        public string Controller { get; set; }
        public string Action { get; set; }
    }
    public class Register
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Mobile { get; set; }
        public string CompanyName { get; set; }
        public string WebSiteUrl { get; set; }
        public string WorkPhone { get; set; }
        public string AccountType { get; set; }
        public string recaptchaToken { get; set; }
    }
    public class RestPassword
    {
        public string EmailActivationKey { get; set; }
        public string MobileActivationCode { get; set; }
        public string Password { get; set; }
    }
    public class UserList : BaseListDTO
    {
        public long Id { get; set; }
        public string UserType { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string WorkEmail { get; set; }
        public string WorkPhone { get; set; }
        public string WebSiteUrl { get; set; }
        public bool InActive { get; set; }
        public string CreateDate { get; set; }
        
        
        
    }
    public class UserGetById : UserList
    {
        public string AccountType { get; set; }

        public int InvoiceValidDays { get; set; }
        public string InvoiceLang { get; set; }
        public string TermsCondition { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IBAN { get; set; }
        public string CivilIdManager { get; set; }
        public string SalesReferralCode { get; set; }
        public string SocialLinksJson { get; set; }
        public long SMSId { get; set; }
        public string SMSSender { get; set; }
        public int BankId { get; set; }
        public string BankNameEn { get; set; }
        public string BankNameAr { get; set; }


        //public string Password { get; set; }
        //public long EmailId { get; set; }
        //public string Logo { get; set; }
        //public string CommercialLicense { get; set; }
        //public string SignatureAuthorization { get; set; }
        //public string ArticleAssociation { get; set; }
        //public string CommercialRegister { get; set; }
        //public string CivilIdOwner { get; set; }
        //public string BankAccountLetter { get; set; }
        //public string KYCdoc { get; set; }
        //public string Other { get; set; }
        public List<Light> Users { get; set; }
        public List<UserPaymentMethodDTO> PaymentMethods { get; set; }
        public List<AttachmentDTOLight> Attachments { get; set; }
    }
    //public class GetAllLight
    //{
    //    public long Id { get; set; }
    //    public string Name { get; set; }
    //}
    public class UserInsert
    {
        public string UserType { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string AccountType { get; set; }
        public long EmailId { get; set; }
        public long SMSId { get; set; }
        public string SMSSender { get; set; }
        public string CompanyName { get; set; }
        public string WorkEmail { get; set; }
        public string WorkPhone { get; set; }
        public string WebSiteUrl { get; set; }
        public int InvoiceValidDays { get; set; }
        public string InvoiceLang { get; set; }
        public string TermsCondition { get; set; }
        public int BankId { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IBAN { get; set; }

        public bool InActive { get; set; }
        public bool Reviewed { get; set; }
        public string Password { get; set; }
        public string SocialLinksJson { get; set; }
        public string SalesReferralCode { get; set; }
        
        //public string Logo { get; set; }
        //public string CommercialLicense { get; set; }
        //public string SignatureAuthorization { get; set; }
        //public string ArticleAssociation { get; set; }
        //public string CommercialRegister { get; set; }
        //public string CivilIdOwner { get; set; }
        //public string CivilIdManager { get; set; }
        //public string BankAccountLetter { get; set; }
        //public string KYCdoc { get; set; }

        //public string Other { get; set; }
        //public string BankNameEn { get; set; }
        //public string BankNameAr { get; set; }
        public List<UserPaymentMethodDTO> PaymentMethods { get; set; }
        public List<AttachmentDTO> Attachments { get; set; }

    }
    public class UserUpdate:UserInsert
    {
        public long Id { get; set; }
    }
    public class Complete
    {
        public long Id { get; set; }
        public string UserType { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Mobile { get; set; }
        public string AccountType { get; set; }
        public long EmailId { get; set; }
        public long SMSId { get; set; }
        public string SMSSender { get; set; }
        public string CompanyName { get; set; }
        public string WorkEmail { get; set; }
        public string WorkPhone { get; set; }
        public string Logo { get; set; }
        public string WebSiteUrl { get; set; }
        public int InvoiceValidDays { get; set; }
        public string InvoiceLang { get; set; }
        public string TermsCondition { get; set; }
        public string CommercialLicense { get; set; }
        public string SignatureAuthorization { get; set; }
        public string ArticleAssociation { get; set; }
        public string CommercialRegister { get; set; }
        public string CivilIdOwner { get; set; }
        public string CivilIdManager { get; set; }
        public string BankAccountLetter { get; set; }
        public string KYCdoc { get; set; }
        public string SalesReferralCode { get; set; }
        public string SocialLinksJson { get; set; }
        public string Other { get; set; }
        public bool InActive { get; set; }
        public int BankId { get; set; }
        public string BankNameEn { get; set; }
        public string BankNameAr { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IBAN { get; set; }
        public List<Light> Users { get; set; }
        public List<UserPaymentMethodDTO> PaymentMethods { get; set; }

    }
}