namespace PayArabic.Core.Interfaces;
public interface ICurrencyDao
{
    ResponseDTO GetAll(long currentUserId, string currentUserType, string name, string listOptions = null);
    ResponseDTO GetById(long currentUserId, string currentUserType, long id);
    ResponseDTO GetRate(string currencyCode, float amount); 
    ResponseDTO Insert(long currentUserId, string currentUserType, CurrencyDTO.CurrencyInsert entity);
    ResponseDTO Update(long currentUserId, string currentUserType, CurrencyDTO.CurrencyUpdate entity);
    ResponseDTO Delete(long currentUserId, string currentUserType, long id);
}
