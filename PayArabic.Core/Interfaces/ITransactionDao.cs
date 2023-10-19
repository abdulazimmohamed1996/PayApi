namespace PayArabic.Core.Interfaces;
public interface ITransactionDao
{
    object Insert(TransactionDTO.Info entity, string invoiceStatus, string paymentMethodCode, string refundQuery = "");
    object Update(TransactionDTO.Info entity, string invoiceStatus, string paymentMethodCode, string refundQuery = "");
    TransactionDTO.Info GetPendingByInvoiceId(long invoiceId, string paymentGatewayCode);
    List<TransactionDTO.Info> GetPendingTransactions(string paymentGatewayCode, int minutesFrom = 0);
}
