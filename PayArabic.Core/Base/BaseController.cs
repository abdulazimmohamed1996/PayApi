using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PayArabic.Core.Base;

[ApiController]
[IPBlock]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    protected UserDTO.Light CurrentUser
    {
        get
        {
            if (HttpContext.User == null)
                return null;
            return new UserDTO.Light()
            {
                Id = Convert.ToInt64(HttpContext.User.FindFirstValue("Id")),
                Name = HttpContext.User.FindFirstValue("Name"),
                ParentId = Convert.ToInt64(HttpContext.User.FindFirstValue("ParentId")),
                UserType = HttpContext.User.FindFirstValue("UserType"),
                Reviewed = Convert.ToBoolean(HttpContext.User.FindFirstValue("Reviewed")),
            };
        }
    }
    protected UserDTO.RequestInfo RequestInfo
    {
        get
        {
            return (UserDTO.RequestInfo)HttpContext.Items["CurrentRequestInfo"];
        }
    }
}
