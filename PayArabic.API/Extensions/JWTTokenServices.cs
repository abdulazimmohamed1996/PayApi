using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PayArabic.API.Extensions;
public static class JWTTokenServices
{
    public static void AddJWTTokenServices(this IServiceCollection services, IConfiguration Configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = AppSettings.Instance.Jwt.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(AppSettings.Instance.Jwt.IssuerSigningKey)),
                ValidateIssuer = AppSettings.Instance.Jwt.ValidateIssuer,
                ValidIssuer = AppSettings.Instance.Jwt.ValidIssuer,
                ValidateAudience = AppSettings.Instance.Jwt.ValidateAudience,
                ValidAudience = AppSettings.Instance.Jwt.ValidAudience,
                RequireExpirationTime = AppSettings.Instance.Jwt.RequireExpirationTime,
                ValidateLifetime = AppSettings.Instance.Jwt.RequireExpirationTime,
                ClockSkew = TimeSpan.FromDays(1),
            };
        });

        // Add Jwt Setings
        //var bindJwtSettings = new AppSettings.JwtSettings();
        //Configuration.Bind("AppSettings:Jwt", bindJwtSettings);
        //Services.AddSingleton(bindJwtSettings);
        //services.AddAuthentication(options =>
        //{
        //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //}).AddJwtBearer(options =>
        //{
        //    options.RequireHttpsMetadata = false;
        //    options.SaveToken = true;
        //    options.TokenValidationParameters = new TokenValidationParameters()
        //    {
        //        ValidateIssuerSigningKey = bindJwtSettings.ValidateIssuerSigningKey,
        //        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(bindJwtSettings.IssuerSigningKey)),
        //        ValidateIssuer = bindJwtSettings.ValidateIssuer,
        //        ValidIssuer = bindJwtSettings.ValidIssuer,
        //        ValidateAudience = bindJwtSettings.ValidateAudience,
        //        ValidAudience = bindJwtSettings.ValidAudience,
        //        RequireExpirationTime = bindJwtSettings.RequireExpirationTime,
        //        ValidateLifetime = bindJwtSettings.RequireExpirationTime,
        //        ClockSkew = TimeSpan.FromDays(1),
        //    };
        //});
    }
}