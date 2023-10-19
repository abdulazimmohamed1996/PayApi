using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/audit", Name = "Audit")]
public class AuditController : BaseController
{
    private readonly IAuditDao _dao;

    public AuditController(IAuditDao dao)
    {
        _dao = dao;
    }

    [Route("getall")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetAll(string module, string function, string listOptions = null)
    {
        if (CurrentUser.UserType != UserType.SuperAdmin.ToString() && CurrentUser.UserType != UserType.Admin.ToString())
            return Ok(new ResponseDTO { IsValid = false, ErrorKey = "UnauthorizedAccess" });

        var result = _dao.GetAll(module, function, listOptions);
        if (result == null || result.Count() <= 0)
            return Ok(new ResponseDTO { IsValid = false, ErrorKey = "EmptyResult" });
        return Ok(new ResponseDTO { IsValid = true, Response = result });
    }

    [Route("getbyid")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetById(long id)
    {
        if (CurrentUser.UserType != UserType.SuperAdmin.ToString() && CurrentUser.UserType != UserType.Admin.ToString())
            return Ok(new ResponseDTO { IsValid = false, ErrorKey = "UnauthorizedAccess" });

        if (id <= 0) return Ok(new ResponseDTO { IsValid = false, ErrorKey = "AuditIdRequired" });

        var result = _dao.GetById(id);
        if (result == null)
            return Ok(new ResponseDTO { IsValid = false, ErrorKey = "EmptyResult" });
        return Ok(new ResponseDTO { IsValid = true, Response = result });
    }
}