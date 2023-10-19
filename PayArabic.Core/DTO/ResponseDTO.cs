namespace PayArabic.Core.DTO;

public class ResponseDTO
{
    public bool IsValid { get; set; }
    public string ErrorKey { get; set; } = string.Empty;
    public dynamic Response { get; set; }
}
