namespace PayArabic.Core.Interfaces;
public interface IProductLinkDao
{
    ResponseDTO GetAll(long currentUserId, string currentUserType, string name, string listOptions = null);
    ResponseDTO GetById(long currentUserId, string currentUserType, long id);
    ResponseDTO GetCategory(long currentUserId, string currentUserType, long productLinkkey = 0, bool isPayment = false, long vendorId = 0);
    ResponseDTO GetForPayment(long key);
    ResponseDTO Insert(long currentUserId, string currentUserType, ProductLinkDTO.ProductLinkInsert entity);
    ResponseDTO Update(long currentUserId, string currentUserType, ProductLinkDTO.ProductLinkUpdate entity);
    ResponseDTO Delete(long currentUserId, string currentUserType, long id);
}
