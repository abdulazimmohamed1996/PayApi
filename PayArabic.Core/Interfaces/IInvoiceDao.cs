using PayArabic.Core.Model;

namespace PayArabic.Core.Interfaces;
public interface IInvoiceDao
{
    ResponseDTO GetAll(
        string currentUserType,
        long currentUserId,
        long vendorId,
        string code,
        string refNumber,
        string status,
        string createDateFrom,
        string createDateTo,
        string expiryDateFrom,
        string expiryDateTo,
        float amountFrom,
        float amountTo,
        string customerName,
        string customerMobile,
        string customerEmail,
        string paymentMethod = "",
        string invoiceType = "Invoice",
        string listOptions = null);
    ResponseDTO GetById(string currentUserType, long currentUserId, long vendorId, long id);
    ResponseDTO Insert(string currentUserType, long currentUserId, long vendorId, InvoiceDTO.Composite entity);
    ResponseDTO Update(string currentUserType, long currentUserId, long vendorId, InvoiceDTO.InvoiceUpdate entity);
    ResponseDTO Delete(string currentUserType, long currentUserId, long vendorId, long id);
    ResponseDTO GetForPaymentByKey(long key);
    ResponseDTO CreatePaymentLinkInvoice(InvoiceDTO.ForPaymentLink entity);
    ResponseDTO CreateProductLinkInvoice(InvoiceDTO.ForProductLink entity);

    ResponseDTO GetInfo(long id, string paymentMethodCode = "", bool ignore_expire_status = false);
}
