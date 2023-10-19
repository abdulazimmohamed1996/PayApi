namespace PayArabic.Core.Interfaces;
public interface IProductDao
{
    ResponseDTO GetAll(long currentUserId, string currentUserType, long categoryId, string name, string desc, string listOptions = null);
    ResponseDTO GetById(long currentUserId, string currentUserType, long id);
    ResponseDTO Insert(long currentUserId, string currentUserType, ProductDTO.ProductInsert entity);
    ResponseDTO Update(long currentUserId, string currentUserType, ProductDTO.ProductUpdate entity);
    ResponseDTO AutoComplete(long currentUserId, string currentUserType, string name);
    ResponseDTO Delete(long currentUserId, string currentUserType, long id);
}
