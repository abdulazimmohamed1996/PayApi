namespace PayArabic.Core.Interfaces;

public interface ICoreService
{
    string TokenGenerate(UserDTO.Light userBasicInfo, bool isRefreshToken = false, bool isAccessToken = false);
}
