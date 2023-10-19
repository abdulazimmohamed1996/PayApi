namespace PayArabic.Core.DTO;
public class TransactionDTO
{
    public class TransactionList : BaseListDTO
    {
    }
    public class Info
    {
        public long Id { get; set; }
        public string RequestType { get; set; }
        public string Type { get; set; }
        public long InvoiceId { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public string PaymentGateway { get; set; }
        public string PaymentGatewayCode { get; set; }
        public string TransactionId { get; set; }
        public double Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string TrackId { get; set; }
        public string ReferenceId { get; set; }
        public string PaymentId { get; set; }
        public string IpAddress { get; set; }
        public string CreateDate { get; set; }
    }
}