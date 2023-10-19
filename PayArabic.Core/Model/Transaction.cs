namespace PayArabic.Core.Model;
public class Transaction : BaseEntity
{
    public string Request_Type { get; set; } // Authorize, Capture, Refund
    public string Type { get; set; } // Invoice, Refund
    public long Invoice_Id { get; set; }
    public string Status { get; set; } // Success, Failed
    public string Response { get; set; }
    public string PaymentGateway { get; set; }
    public string PaymentGatewayCode { get; set; }
    public string TransactionId { get; set; }
    public string TrackId { get; set; }
    public string ReferenceId { get; set; }
    public string PaymentId { get; set; }
    public float Amount { get; set; }
    public string IpAddress { get; set; }
    public string CurrencyCode { get; set; }
}