using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayArabic.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace PayArabic.API.Controllers;

[Route("api/invoice", Name = "Invoice")]
public class InvoiceController : BaseController
{
    private readonly IInvoiceDao _dao;
    public InvoiceController(IInvoiceDao dao)
    {
        _dao = dao;
    }

    [Route("getall")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetAll(long vendorId, string code,
        string refNumber,
        string status,
        string createDateFrom,
        string createDateTo,
        string expiryDateFrom,
        string expiryDateTo,
        float amountFrom,
        float amountTo,
        string customerName,
        string customerMobile,
        string customerEmail,
        string paymentMethod = "",
        string invoiceType = "Invoice",
        string listOptions = null)
    {
        if (CurrentUser.UserType == UserType.Vendor.ToString())
            vendorId = CurrentUser.Id;
        else if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;
        else 
            vendorId = 0;

        var result = _dao.GetAll(
            CurrentUser.UserType,
            CurrentUser.Id,
            vendorId,
            code,
            refNumber,
            status,
            createDateFrom,
            createDateTo,
            expiryDateFrom,
            expiryDateTo,
            amountFrom,
            amountTo,
            customerName,
            customerMobile,
            customerEmail,
            paymentMethod,
            invoiceType,
            listOptions);

        return Ok(result);
    }


    [Route("getbyid")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetById(long id)
    {
        if (id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "InvoiceIdRequired" });

        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;

        var result = _dao.GetById(CurrentUser.UserType, CurrentUser.Id, vendorId, id);
        return Ok(result);
    }

    [Route("insert")]
    [HttpPost]
    public IActionResult Insert([FromBody] InvoiceDTO.Composite entity)
    {
        if (entity == null || entity.Invoices == null || entity.Invoices.Count < 0)
            return Ok(new ResponseDTO { IsValid = false, ErrorKey = "ObjectIsEmpty" });

        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;

        var result = _dao.Insert(CurrentUser.UserType, CurrentUser.Id, vendorId, entity);
        return Ok(result);
    }

    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] InvoiceDTO.InvoiceUpdate entity)
    {
        if (entity.Id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "InvoiceIdRequired" });
        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;
        var result = _dao.Update(CurrentUser.UserType, CurrentUser.Id, vendorId, entity);
        return Ok(result);
    }

    [Route("delete")]
    [HttpDelete]
    public IActionResult Delete(long id)
    {
        if (id <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "InvoiceIdRequired" });

        long vendorId = CurrentUser.Id;
        if (CurrentUser.UserType == UserType.User.ToString() && CurrentUser.ParentId > 0)
            vendorId = CurrentUser.ParentId;

        var result = _dao.Delete(CurrentUser.UserType, CurrentUser.Id, vendorId, id);
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("getforpaymentbykey")]
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetForPaymentByKey(long key)
    {
        if (key <= 0) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "InvoiceKeyRequired" });
        var result = _dao.GetForPaymentByKey(key);
        return Ok(result);
    }

    [Route("createpaymentlinkinvoice")]
    [HttpPost]
    [AllowAnonymous]
    public IActionResult CreatePaymentLinkInvoice([FromBody] InvoiceDTO.ForPaymentLink entity)
    {
        if (entity == null) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "ObjectIsEmpty" });
        var result = _dao.CreatePaymentLinkInvoice(entity);
        return Ok(result);
    }

    [Route("createproductlinkinvoice")]
    [HttpPost]
    [AllowAnonymous]
    public IActionResult CreateProductLinkInvoice([FromBody] InvoiceDTO.ForProductLink entity)
    {
        if (entity == null) return Ok(new ResponseDTO { IsValid = true, ErrorKey = "ObjectIsEmpty" });
        var result = _dao.CreateProductLinkInvoice(entity);
        return Ok(result);
    }


}