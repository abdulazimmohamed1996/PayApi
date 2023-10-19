namespace PayArabic.Core.Interfaces;
public interface IProductCategoryDao
{
    ResponseDTO GetAll(long currentUserId, string currentUserType, string listOptions = null);
    ResponseDTO GetById(long currentUserId, string currentUserType, long id);
    ResponseDTO Insert(long currentUserId, string currentUserType, ProductCategoryDTO.ProductCategoryInsert entity);
    ResponseDTO Update(long currentUserId, string currentUserType, ProductCategoryDTO.ProductCategoryUpdate entity);
    ResponseDTO Delete(long currentUserId, string currentUserType, long id);
}
