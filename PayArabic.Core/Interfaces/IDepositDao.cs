namespace PayArabic.Core.Interfaces;
public interface IDepositDao
{
    ResponseDTO GetAll(
        long currentUserId,
        string currentUserType,
        long vendorId,
        string vendor,
        string code,
        string number,
        string status,
        string dateFrom,
        string dateTo,
        float amountFrom,
        float amountTo,
        string depositType,
        string listOptions = null);
    ResponseDTO GetById(long currentUserId, string currentUserType, string type, long id);
    ResponseDTO Insert(long currentUserId, string currentUserType, DepositDTO.DepositInsert entity);
    ResponseDTO Update(long currentUserId, string currentUserType, DepositDTO.DepositUpdate entity);
    ResponseDTO Delete(long currentUserId, string currentUserType, long id);
    public ResponseDTO GetVendorInvoicesReadyForDeposit(
        long currentUserId, 
        string currentUserType,
        string depositType,
        string list,
        string vendorCode,
        string vendorName,
        string invoiceCode,
        string invoiceKey,
        string invoiceRefNumber,
        string customerName,
        string customerMobile,
        string customerEmail,
        string invoiceDateFrom,
        string invoiceDateTo,
        float invoiceAmountFrom,
        float invoiceAmountTo);
}
