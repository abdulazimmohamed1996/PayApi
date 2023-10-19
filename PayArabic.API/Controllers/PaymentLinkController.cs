using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[Route("api/paymentlink", Name = "PaymentLink")]
public class PaymentLinkController : BaseController
{
    private readonly IPaymentLinkDao _dao;
    public PaymentLinkController(IPaymentLinkDao dao)
    {
        _dao = dao;
    }

    [Route("getall")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetAll(string title, string listOptions = null)
    {
        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;

        var result = _dao.GetAll(vendorId, CurrentUser.UserType, title, listOptions);
        return Ok(result);
    }

    [Route("getbyid")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetById(long id)
    {
        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;

        var result = _dao.GetById(vendorId, CurrentUser.UserType, id);
        return Ok(result);
    }

    [Route("getforpayment")]
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetForPayment(long key)
    {
        var result = _dao.GetForPayment(key);
        if (result == null)
            return Ok(new ResponseDTO() { IsValid = false, ErrorKey = "EmptyResult", Response = null });
        return Ok(result); 
    }

    [Route("insert")]
    [HttpPost]
    public IActionResult Insert([FromBody] PaymentLinkDTO.PaymentLinkInsert entity)
    {
        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;

        var result = _dao.Insert(CurrentUser.Id, CurrentUser.UserType, vendorId, entity);
        return Ok(result);
    }

    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] PaymentLinkDTO.PaymentLinkUpdate entity)
    {
        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;

        var result = _dao.Update(CurrentUser.Id, CurrentUser.UserType, vendorId, entity);
        return Ok(result);
    }

    [Route("delete")]
    [HttpDelete]
    public IActionResult Delete(long id)
    {
        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;
        var result = _dao.Delete(CurrentUser.Id, CurrentUser.UserType, vendorId, id);
        return Ok(result);
    }

}