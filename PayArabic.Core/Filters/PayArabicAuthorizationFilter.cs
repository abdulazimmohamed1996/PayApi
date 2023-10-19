using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace PayArabic.Core.Filters
{
    public class PayArabicAuthorizationFilter : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if ((context.ActionDescriptor as ControllerActionDescriptor).MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any())// if action marked as anonymous then do nothing
                return;
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var extractedAuthorization))// if request header does not contains "Authorization" key then return error
                context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null });
            else
            {
                var authHeader = AuthenticationHeaderValue.Parse(extractedAuthorization);
                if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))// if there's no authorization value in the header then return error
                    context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null });
                else
                {
                    string httpMethod = context.HttpContext.Request.Method.ToLower();
                    string controller = context.HttpContext.Request.RouteValues["controller"].ToString();
                    string action = context.HttpContext.Request.RouteValues["action"].ToString();
                    var _userDao = context.HttpContext.RequestServices.GetRequiredService<IUserDao>();
                    var userId = context.HttpContext.User.FindFirstValue("Id");
                    var userType = context.HttpContext.User.FindFirstValue("UserType");
                    var reviewed = context.HttpContext.User.FindFirstValue("Reviewed");

                    if (string.IsNullOrEmpty(userId))
                        context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null });
                    else if (string.IsNullOrEmpty(userType)
                        && userType != UserType.SystemAdmin.ToString()
                        && userType != UserType.SuperAdmin.ToString()
                        && userType != UserType.Admin.ToString()
                        && userType != UserType.Vendor.ToString()
                        && userType != UserType.User.ToString())
                        context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = "WrongUserType", Response = null });
                    else if (userType == UserType.Vendor.ToString() && reviewed == "False")
                        if(!((controller == "User" && action == "Update") || httpMethod == "get"))
                            context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = "UnderReview", Response = null });
                    else // JWT Access
                    {
                        var requestInfo = await _userDao.GetUserInfoPerRequest(Convert.ToInt64(userId), authHeader.Parameter, controller, action);
                        if (!string.IsNullOrEmpty(requestInfo.TokenValue)) //TokenExpired | InvalidToken
                            context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = requestInfo.TokenValue, Response = null });
                        else if (requestInfo.PermissionValue.Equals("AccessPermissionDenied"))//AccessPermissionDenied
                            context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = requestInfo.PermissionValue, Response = null });
                        else
                            context.HttpContext.Items["CurrentRequestInfo"] = requestInfo;
                    }
                }
            }
        }
    }
}
