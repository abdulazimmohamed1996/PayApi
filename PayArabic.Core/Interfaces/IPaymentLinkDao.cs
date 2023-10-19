namespace PayArabic.Core.Interfaces;
public interface IPaymentLinkDao
{
    ResponseDTO GetAll(long currentUserId, string currentUserType, string title, string listOptions = null);
    ResponseDTO GetById(long currentUserId, string currentUserType, long id);
    ResponseDTO Insert(long currentUserId, string currentUserType, long vendorId, PaymentLinkDTO.PaymentLinkInsert entity);
    ResponseDTO Update(long currentUserId, string currentUserType, long vendorId, PaymentLinkDTO.PaymentLinkUpdate entity);
    ResponseDTO GetForPayment(long key);
    ResponseDTO Delete(long currentUserId, string currentUserType, long vendorId, long id);
}
