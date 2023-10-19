namespace PayArabic.Core.Interfaces;

public interface IUserDao
{
    IEnumerable<UserDTO.UserList> GetAllUser(
        long currentUserId,
        string currentUserType,
        string name,
        string mobile,
        string email,
        string userType,
        bool? inactive,
        string listOptions = null);
    ResponseDTO Register(UserDTO.Register entity);
    ResponseDTO Login(UserDTO.Login entity);
    ResponseDTO Activate(long currentUserId, string currentUserType, long id, bool activate);
    ResponseDTO ActivateEmail(string emailActivationKey, string mobileActivationKey);
    ResponseDTO ForgetPassword(string email, string mobile, string recaptchaToken);
    ResponseDTO ForgetPasswordRecovery(UserDTO.RestPassword entity);
    ResponseDTO GetById(string currentUserType, long id);
    UserDTO.LightInfo GetLightInfo(long userId);
    ResponseDTO GetPaymentMethodByUserId(long userId);
    ResponseDTO Insert(long currentUserId, string currentUserType, UserDTO.UserInsert entity);
    ResponseDTO Update(long currentUserId, string currentUserType, UserDTO.UserUpdate entity);
    ResponseDTO Delete(long currentUserId, string currentUserType, long id);
    ResponseDTO GetUserPermission(long currentUserId, string currentUserType, long userId);
    ResponseDTO SaveUserPermission(long currentUserId, long parentUserId, List<SaveUserPermissionDTO> permissions);
    Task<UserDTO.RequestInfo> GetUserInfoPerRequest(long userId, string token, string moduleName, string functionName);
}



