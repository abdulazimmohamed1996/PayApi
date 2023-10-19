using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PayArabic.Core.Filters;

public class ValidateModelStateFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ResponseDTO { IsValid = false, ErrorKey = "WrongObject", Response = context.ModelState });
        }
    }
}

