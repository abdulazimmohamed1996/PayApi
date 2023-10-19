using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace PayArabic.Core.Filters;

public class IPBlockAttribute : ActionFilterAttribute
{
    private short _numberOfSeconds;

    public IPBlockAttribute()
    {
        _numberOfSeconds = Convert.ToInt16(AppSettings.Instance.ValidPeriodBetweenRequestsInSeconds);
    }
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        IPDTO model = new IPDTO();
        var requesterIp = context.HttpContext.Connection.RemoteIpAddress.ToString();
        var remoteIpModel = context.HttpContext.Session.GetString(requesterIp);
        if (remoteIpModel == null)
        {
            model.Address = requesterIp;
            model.Time = DateTime.Now;
            model.EndPoint = context.HttpContext.GetEndpoint().ToString();
            context.HttpContext.Session.SetString(requesterIp, JsonConvert.SerializeObject(model));
        }
        else
        {
            var _record = JsonConvert.DeserializeObject<IPDTO>(remoteIpModel);
            if (DateTime.Now.Subtract(_record.Time).TotalSeconds <= _numberOfSeconds
                && _record.EndPoint == context.HttpContext.GetEndpoint().ToString()
                && _record.Address == requesterIp)
            {
                context.Result = new UnauthorizedObjectResult(new ResponseDTO { IsValid = false, ErrorKey = "PermissionDenied" });
            }
            else
            {
                _record.Time = DateTime.Now;
                if (_record.EndPoint != context.HttpContext.GetEndpoint().ToString())
                    _record.EndPoint = context.HttpContext.GetEndpoint().ToString();
                context.HttpContext.Session.Remove(requesterIp);
                context.HttpContext.Session.SetString(requesterIp, JsonConvert.SerializeObject(_record));
            }
        }
    }
}
