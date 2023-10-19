namespace PayArabic.Core.Model;
public class Token : BaseEntity
{
    public long UserId { get; set; }
    public string Text { get; set; }
    public bool IsRefreshToken { get; set; }
    public bool IsAccessToken { get; set; }
}
