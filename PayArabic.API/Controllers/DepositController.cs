using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[Route("api/deposit", Name = "Deposit")]
public class DepositController : BaseController
{
    private readonly IDepositDao _dao;
    public DepositController(IDepositDao dao)
    {
        _dao = dao;
    }

    [Route("getall")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetAll(long vendorId, string vendor,
        string code,
        string number,
        string status,
        string dateFrom,
        string dateTo,
        float amountFrom,
        float amountTo,
        string depositType,
        string listOptions = null)
    {

        if (CurrentUser.UserType == UserType.Vendor.ToString())
            vendorId = CurrentUser.Id;
        else if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;
        else
            vendorId = 0;

        depositType = String.IsNullOrEmpty(depositType) ? "Normal" : depositType;

        var result = _dao.GetAll(
            CurrentUser.Id,
            CurrentUser.UserType,
            vendorId,
            vendor,
            code,
            number,
            status,
            dateFrom,
            dateTo,
            amountFrom,
            amountTo,
            depositType,
            listOptions);

        return Ok(result );
    }

    [Route("getbyid")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetById(long id, string type)
    {
        if (id <= 0) return Ok(new ResponseDTO { IsValid = false, ErrorKey = "DepositIdRequired" });
        var result = _dao.GetById(CurrentUser.Id, CurrentUser.UserType, type, id);
        return Ok(result);
    }

    [Route("insert")]
    [HttpPost]
    public IActionResult Insert([FromBody] DepositDTO.DepositInsert entity)
    {
        if (entity.Vendors == null || !entity.Vendors.Any())
            return Ok(new ResponseDTO { IsValid = false, ErrorKey = "VendorIdRequired" });

        long currentUserId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            currentUserId = CurrentUser.ParentId;

        var result = _dao.Insert(currentUserId, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] DepositDTO.DepositUpdate entity)
    {
        if (entity.Id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "DepositIdRequired" });
        
        if (entity.Vendors == null || !entity.Vendors.Any())
            return Ok(new ResponseDTO { IsValid = true, ErrorKey = "VendorIdRequired" });


        var result = _dao.Update(CurrentUser.Id, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [Route("delete")]
    [HttpDelete]
    public IActionResult Delete(long id)
    {
        if (id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "DepositIdRequired" });

        var result = _dao.Delete(CurrentUser.Id, CurrentUser.UserType, id);
        return Ok(result);
    }

    [Route("GetVendorInvoicesReadyForDeposit")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetVendorInvoicesReadyForDeposit(
        string depositType,
        string list,
        string vendorCode,
        string vendorName,
        string invoiceCode,
        string invoiceKey,
        string invoiceRefNumber,
        string customerName,
        string customerMobile,
        string customerEmail,
        string invoiceDateFrom,
        string invoiceDateTo,
        float invoiceAmountFrom,
        float invoiceAmountTo
        )
    {
        var result = _dao.GetVendorInvoicesReadyForDeposit(
            CurrentUser.Id, 
            CurrentUser.UserType,
            depositType,
            list,
            vendorCode,
            vendorName,
            invoiceCode,
            invoiceKey,
            invoiceRefNumber,
            customerName,
            customerMobile,
            customerEmail,
            invoiceDateFrom,
            invoiceDateTo,
            invoiceAmountFrom,
            invoiceAmountTo);
        return Ok(result);
    }
}