namespace PayArabic.Core.Interfaces;

public interface ICoreDao
{
    void TokenSave(string tokenText, long userId, bool isRefreshToken = false, bool isAccessToken = false);
    void TokenDelete(long userId);
    //Task<bool> TokenValidate(long userId, string token, int validTokenMinutes);
    public UserDTO.Light TokenGetUser(string tokenText);

    void InitSystem(List<SysPermissionDTO.Module> list);
}

