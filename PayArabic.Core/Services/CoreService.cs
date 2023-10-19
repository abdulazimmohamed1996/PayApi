using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PayArabic.Core.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
namespace PayArabic.Core.Services;

public class CoreService : ICoreService
{
    private readonly ICoreDao _dao;
    public CoreService(ICoreDao dao)
    {
        _dao = dao;
    }
    public string TokenGenerate(UserLightDTO user, bool isRefreshToken = false, bool isAccessToken = false)
    {
        var claims = new List<Claim>();
        claims.Add(new Claim("Id", user.Id.ToString()));
        claims.Add(new Claim("Parent_Id", user.Parent_Id.ToString()));
        claims.Add(new Claim("UserType", user.UserType.ToString()));
        claims.Add(new Claim("Reviewed", user.Reviewed.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, user.Name));
        claims.Add(new Claim(ClaimTypes.MobilePhone, user.Mobile));
        claims.Add(new Claim(ClaimTypes.Email, user.Email));
        if (isRefreshToken)
            claims.Add(new Claim("IsRefreshToken", "1"));
        else if (isAccessToken)
            claims.Add(new Claim("IsAccessToken", "1"));

        if (isRefreshToken || isAccessToken)
            claims.Add(new Claim("Api", "true"));
        else claims.Add(new Claim("Api", "false"));

        int validTokenMinutes = Convert.ToInt32(AppSettings.Instance.ValidTokenMinutes);
        string settingKey = AppSettings.Instance.Jwt.IssuerSigningKey;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settingKey));
        var jwtToken = new JwtSecurityToken(claims: claims,
                                         expires: DateTime.UtcNow.AddMinutes(validTokenMinutes),
                                         notBefore: DateTime.UtcNow,
                                         audience: AppSettings.Instance.Jwt.ValidAudience,
                                         issuer: AppSettings.Instance.Jwt.ValidIssuer,
                                         signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        string tokenText = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        TokenSave(tokenText, user.Id, isRefreshToken, isAccessToken);
        return tokenText;
    }
    public void TokenSave(string tokenText, long userId, bool isRefreshToken = false, bool isAccessToken = false)
    {
        _dao.TokenSave(tokenText, userId);
    }
    public void TokenDelete(long userId)
    {
        _dao.TokenDelete(userId);
    }

    public static void InitSystem()
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        var classes = assembly.GetTypes().Where(x => x.Name.EndsWith("Controller")).ToList();
        var list = GetSystemModules(classes);

        // write these modules to database
                
    }
    private static List<SysModuleDTO> GetSystemModules(List<Type> classes)
    {
        
        List<SysModuleDTO> list = new List<SysModuleDTO>();
        foreach (var type in classes)
        {
            bool moduleNotPermitted = type.GetCustomAttributes(typeof(NotPermissionableAttribute), true).Any();
            if (moduleNotPermitted)
                continue;

            RouteAttribute moduleRouteName = (RouteAttribute)Attribute.GetCustomAttribute(type, typeof(RouteAttribute),false);
            string moduleName = type.Name;
            if (moduleRouteName != null && !string.IsNullOrEmpty(moduleRouteName.Name))
                moduleName = moduleRouteName.Name;


            bool moduleNotAuditable = type.GetCustomAttributes(typeof(NotAuditableAttribute), true).Any();

            SysModuleDTO module = new SysModuleDTO()
            {
                Name = type.Name.Replace("Controller", ""),
                DisplayName = moduleName,
                Permission = "Edit",
                Auditable = !moduleNotAuditable,
                Functions = new List<SysFunctionDTO>()
            };

            var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                bool functionNotPermitted = m.GetCustomAttributes(typeof(NotPermissionableAttribute), true).Any();
                if (functionNotPermitted)
                    continue;

                RouteAttribute functionRouteName = (RouteAttribute)Attribute.GetCustomAttribute(m, typeof(RouteAttribute));
                string functionName = m.Name;
                if (functionRouteName != null && !string.IsNullOrEmpty(functionRouteName.Name))
                    functionName = functionRouteName.Name;

                bool functionNotAuditable = type.GetCustomAttributes(typeof(NotAuditableAttribute), true).Any();

                SysFunctionDTO function = new SysFunctionDTO()
                {
                    Name = m.Name,
                    DisplayName = functionName,
                    Permission = "Edit",
                    Auditable = !functionNotAuditable
                };
                module.Functions.Add(function);
            }

            list.Add(module);
        }
        return list;
    }
}