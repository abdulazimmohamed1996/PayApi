using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PayArabic.Core.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
namespace PayArabic.API.Services;

public class CoreService : ICoreService
{
    private readonly ICoreDao _dao;
    public CoreService(ICoreDao dao)
    {
        _dao = dao;
    }
  
    public string TokenGenerate(UserDTO.Light user, bool isRefreshToken = false, bool isAccessToken = false)
    {
        var claims = new List<Claim>();
        claims.Add(new Claim("Id", user.Id.ToString()));
        claims.Add(new Claim("ParentId", user.ParentId.ToString()));
        claims.Add(new Claim("UserType", user.UserType.ToString()));
        claims.Add(new Claim("Reviewed", user.Reviewed.ToString()));
        claims.Add(new Claim("Name", user.Name));
        claims.Add(new Claim("Email", user.Email));
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
        //TokenSave(tokenText, user.Id, isRefreshToken, isAccessToken);
        _dao.TokenSave(tokenText, user.Id);
        return tokenText;
    }
    //public void TokenSave(string tokenText, long userId, bool isRefreshToken = false, bool isAccessToken = false)
    //{
    //    _dao.TokenSave(tokenText, userId);
    //}
    //public void TokenDelete(long userId)
    //{
    //    _dao.TokenDelete(userId);
    //}

    public static void InitSystem()
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        var classes = assembly.GetTypes().Where(x => x.Name.EndsWith("Controller")).ToList();
        var list = GetSystemModules(classes);
         
        // write these modules to database
        CoreDao dao = new CoreDao();
        dao.InitSystem(list);
    }
    private static List<SysPermissionDTO.Module> GetSystemModules(List<Type> classes)
    {

        List<SysPermissionDTO.Module> list = new List<SysPermissionDTO.Module>();
        foreach (var type in classes)
        {
            bool moduleAnonymous = type.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();
            if (moduleAnonymous)
                continue;

            bool moduleNotPermitted = type.GetCustomAttributes(typeof(NotPermissionableAttribute), true).Any();
            if (moduleNotPermitted)
                continue;

            RouteAttribute moduleRouteName = (RouteAttribute)Attribute.GetCustomAttribute(type, typeof(RouteAttribute), false);
            string moduleName = type.Name.Replace("Controller", "");
            if (moduleRouteName != null && !string.IsNullOrEmpty(moduleRouteName.Name))
                moduleName = moduleRouteName.Name;


            bool moduleNotAuditable = type.GetCustomAttributes(typeof(NotAuditableAttribute), true).Any();

            SysPermissionDTO.Module module = new SysPermissionDTO.Module()
            {
                Name = type.Name.Replace("Controller", ""),
                DisplayName = moduleName,
                Permission = "Edit",
                Auditable = !moduleNotAuditable,
                Functions = new List<SysPermissionDTO.Function>()
            };

            var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                bool functionAnonymous = m.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();
                if (functionAnonymous)
                    continue;

                bool functionNotPermitted = m.GetCustomAttributes(typeof(NotPermissionableAttribute), true).Any();
                if (functionNotPermitted)
                    continue;

                RouteAttribute functionRouteName = (RouteAttribute)Attribute.GetCustomAttribute(m, typeof(RouteAttribute));
                string functionName = m.Name;
                if (functionRouteName != null && !string.IsNullOrEmpty(functionRouteName.Name))
                    functionName = functionRouteName.Name;

                bool functionNotAuditable = m.GetCustomAttributes(typeof(NotAuditableAttribute), true).Any();

                SysPermissionDTO.Function function = new SysPermissionDTO.Function()
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