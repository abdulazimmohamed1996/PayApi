namespace PayArabic.Core.DTO;

public class NBKMasterCardAmexResponse
{
    public string Merchant { get; set; }
    public string Result { get; set; }
    public string SuccessIndicator { get; set; }
    public Session session { get; set; }
    public class Session
    {
        public string Id { get; set; }
        public string UpdateStatus { get; set; }
        public string Version { get; set; }
    }
}