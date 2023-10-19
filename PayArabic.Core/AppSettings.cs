namespace PayArabic.Core;
public sealed class AppSettings
{
    private static AppSettings _instance;
    private static readonly object padlock = new object();
    private AppSettings()
    { }

    public static AppSettings Instance
    {
        get
        {
            lock (padlock)
            {
                if (_instance == null)
                {
                    _instance = new AppSettings();
                }
                return _instance;
            }
        }
    }
    public string ProviderName { get; set; }
    public string DBConnection { get; set; }
    public string ValidPeriodBetweenRequestsInSeconds { get; set; }
    public string PayArabicURL { get; set; }
    public string PayArabicUI { get; set; }
    public string ValidTokenMinutes { get; set; }
    public string RecaptchaSecretKey { get; set; }
    public string RecaptchaPassword { get; set; }
    public string UserPasswordKey { get; set; }
    public string DefaultCurrency { get; set; }
    public string InvoicePublicUrl { get; set; }
    public string AttachmentUrl { get; set; }
    public JwtSettings Jwt { get; set; }
    public KnetPaymentSettings KnetPaymentSettings { get; set; }

    #region Services Project Setting
    public string ConfirmEmailUrl { get; set; }
    public string ResetPasswordUrl { get; set; }
    #endregion
}
public class JwtSettings
{
    public bool ValidateIssuerSigningKey { get; set; }
    public string IssuerSigningKey { get; set; }
    public bool ValidateIssuer { get; set; } = true;
    public string ValidIssuer { get; set; }
    public bool ValidateAudience { get; set; } = true;
    public string ValidAudience { get; set; }
    public bool RequireExpirationTime { get; set; }
    public bool ValidateLifetime { get; set; } = true;
}
public class KnetPaymentSettings
{
    public string Mode { get; set; }
    public string ApiGatewayUrl { get; set; }
    public string ApiTestGatewayUrl { get; set; }
}


