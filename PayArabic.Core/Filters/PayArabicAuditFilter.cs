using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace PayArabic.Core.Filters
{
    public class PayArabicAuditFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            UserDTO.RequestInfo requestInfo = (UserDTO.RequestInfo)context.HttpContext.Items["CurrentRequestInfo"];
            if (context.HttpContext.Items["ActionParameters"] == null)
                return;

            //Dictionary<string, object> result = new Dictionary<string, object>();
            var arguments = JsonConvert.SerializeObject(context.HttpContext.Items["ActionParameters"]);
            
            AuditDTO.AuditInsert auditDTO = new AuditDTO.AuditInsert()
            {
                UserId = requestInfo.UserId,
                IPAddress = context.HttpContext.Connection.RemoteIpAddress.ToString(),
                Module = requestInfo.Controller,
                Function = requestInfo.Action,
                Notes = arguments
            };
            var _auditDao = context.HttpContext.RequestServices.GetRequiredService<IAuditDao>();
            _auditDao.Insert(auditDTO);
            return;
        }
        
        public void OnActionExecuting(ActionExecutingContext context)
        {
            UserDTO.RequestInfo requestInfo = (UserDTO.RequestInfo)context.HttpContext.Items["CurrentRequestInfo"];
            if (requestInfo == null || !requestInfo.AuditValue)
                return;

            context.HttpContext.Items["ActionParameters"] = context.ActionArguments;
        }
    }
}