using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayArabic.Core.Model;
using System.Collections.Generic;

namespace PayArabic.API.Controllers;

[Route("api/user", Name = "Manage User")]
public class UserController : BaseController
{
    private readonly IUserDao _dao;
    private readonly ICoreDao _coreDao;
    private readonly ICoreService _coreService;
    public UserController(IUserDao dao, ICoreService coreService, ICoreDao coreDao)
    {
        _dao = dao;
        _coreService = coreService;
        _coreDao = coreDao;
    }

    [Route("register")]
    [HttpPost]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Register([FromBody] UserDTO.Register entity)
    {
        var result = _dao.Register(entity);
        return Ok(result);
    }

    [Route("login")]
    [HttpPost]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Login([FromBody] UserDTO.Login entity)
    {
        // #1: Try to login
        var result = _dao.Login(entity);
        if (!result.IsValid) return Ok(result);

        // #2: Generate Jwt token  
        var tokenText = _coreService.TokenGenerate(result.Response);

        // #3: Return sucess with user and token
        return Ok(new ResponseDTO { IsValid = true, Response = new { Token = tokenText, User= result.Response } });
    }

    [HttpDelete("logout")]
    [NotAuditable]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Logout()
    {
        Request.HttpContext.Session.Clear();
        _coreDao.TokenDelete(CurrentUser.Id);
        return NoContent();
    }

    [Route("GetUserPermission")]
    [NotAuditable]
    [HttpGet]
    public IActionResult GetUserPermission(long userId)
    {
        if (userId == -1 && userId != CurrentUser.Id) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "UnauthorizedAccess" });
        long currentUserId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            currentUserId = CurrentUser.ParentId;
        var result = _dao.GetUserPermission(currentUserId, CurrentUser.UserType, userId);
        return Ok(result);
    }

    [Route("SaveUserPermission")]
    [HttpPost]
    public IActionResult SaveUserPermission([FromBody] List<SaveUserPermissionDTO> permissions)
    {
        long parentUserId = CurrentUser.ParentId;
        if (parentUserId == 0)
            parentUserId = CurrentUser.Id;
        //if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
        //    currentUserId = CurrentUser.ParentId;
        var result = _dao.SaveUserPermission(CurrentUser.Id, CurrentUser.ParentId, permissions);
        return Ok(result);
    }

    [Route("getalluser")]
    [HttpGet]
    [NotAuditable]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetAllUser(
        string name,
        string mobile,
        string email,
        string userType,
        bool? inactive,
        string listOptions = null)
    {
        long currentUserId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            currentUserId = CurrentUser.ParentId;

        var result = _dao.GetAllUser(currentUserId, CurrentUser.UserType, name, mobile, email, userType, inactive, listOptions);
        if (result == null || result.Count() <= 0)
            return Ok(new ResponseDTO { IsValid = true, ErrorKey = "EmptyResult" });

        return Ok(new ResponseDTO { IsValid = true, ErrorKey = "", Response = result });
    }
    

    [Route("activate")]
    [HttpPut]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Activate(long id, bool activate)
    {
        if (id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "UserIdRequired" });
        var result = _dao.Activate(CurrentUser.Id, CurrentUser.UserType, id, activate);
        return Ok(result);
    }

    [Route("delete")]
    [HttpDelete]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Delete(long id)
    {
        if (id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "UserIdRequired" });

        var result = _dao.Delete(CurrentUser.Id, CurrentUser.UserType, id);
        return Ok(result);
    }

    [Route("ActivateEmail")]
    [HttpPut]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult ActivateEmail(string emailActivationKey, string mobileActivationKey)
    {
        var result = _dao.ActivateEmail(emailActivationKey, mobileActivationKey);
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("forget")]
    [HttpPost]
    [AllowAnonymous]
    public IActionResult ForgetPassword(string email, string mobile, string recaptchaToken)
    {
        var result = _dao.ForgetPassword(email, mobile, recaptchaToken);
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("forgetrecovery")]
    [HttpPost]
    [AllowAnonymous]
    public IActionResult ForgetPasswordRecovery([FromBody] UserDTO.RestPassword entity)
    {
        var result = _dao.ForgetPasswordRecovery(entity);
        return Ok(result);
    }

    [Route("getbyid")]
    [HttpGet]
    [NotAuditable]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetById(long id)
    {
        if (id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "UserIdRequired" });
        var result = _dao.GetById(CurrentUser.UserType, id);
        return Ok(result);
    }

    [Route("GetPaymentMethod")]
    [HttpGet]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetPaymentMethodByUserId(long userId)
    {
        if (userId <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "UserIdRequired" });
        var result = _dao.GetPaymentMethodByUserId(userId);
        return Ok(result);
    }

    [Route("insert")]
    [HttpPost]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Insert([FromBody] UserDTO.UserInsert entity)
    {
        long currentUserId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            currentUserId = CurrentUser.ParentId;
        var result = _dao.Insert(currentUserId, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] UserDTO.UserUpdate entity)
    {
        long currentUserId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            currentUserId = CurrentUser.ParentId;

        var result = _dao.Update(currentUserId, CurrentUser.UserType, entity);
        return Ok(result);
    }
}