using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[Route("api/Integration", Name = "Integration")]
public class IntegrationController : BaseController
{
    private readonly IIntegrationDao _dao;
    private readonly IInvoiceDao _invoiceDao;
    public IntegrationController(IIntegrationDao dao, IInvoiceDao invoiceDao)
    {
        _dao = dao;
        _invoiceDao = invoiceDao;
    }

    [Route("getall")]
    [NotAuditable]
    [HttpGet]
    public IActionResult GetAll()
    {
        var result = _dao.GetAll();

        if (result == null || result.Count() <= 0)
            return Ok(new ResponseDTO { IsValid = true, ErrorKey = "EmptyResult" });

        return Ok(new ResponseDTO { IsValid = true, ErrorKey = "", Response = result });
    }

    #region Knet
    [Route("Knet_GetInvoice_PaymentLink")]
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Knet_GetInvoice_PaymentLink(long id)
    {
        var info = _invoiceDao.GetInfo(id, IntegrationCode.Knet.ToString());
        if(!info.IsValid)
            return Ok(info);
        var result = _dao.KentInit(info.Response);
        return Ok(new ResponseDTO { IsValid = true, ErrorKey = "", Response = result });
    }

    [Route("Knet_ProcessInvoice_Payment")]
    [HttpPost]
    [AllowAnonymous]
    public IActionResult Knet_Process(string ErrorText, string paymentid, string trackid
        , string Error, string result, string postdate, string tranid, string auth, string avr
        , string Ref, string amt, string udf1, string udf2, string udf3, string udf4, string udf5, string trandata)
    {
        //knetInput = "CC1464F1001FAE899CB832D1F6AF7429E2652BFCABE981E2FB4DDD65E169B60798752D1A041E74626D6D90F37DDE4EDDD49689ED68317DB78897CA83E1D6B71EF8A0D7E68E33EE2AC7BD58506547C65DF82F54E2472E518780E6E6C7C2F9304F37E826B50823A285BC744F2D658C36BDB574380E36EB048B7423E399E9AFC7B8DC1F7DB1EDB62B9041451F99A7B26EC084DF093697FBD98414CAD3A4840F4CD41E14BC7EAD00881F72A931233A4D4AC54C944D119CFCC830F54B24102C19EDE607A0F7BD876AE3FD58255F8C020357022EA2D07EF0555F2083D658F378F5A4D1664561C484435792315E77FA29BDC7D3219A4C99D082EEE7D925196318CEE145";
        string ipAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
        var res = _dao.KnetProcess(ErrorText, paymentid, trackid
        , Error, result, postdate, tranid, auth, avr
        , Ref, amt, udf1, udf2, udf3, udf4, udf5, trandata, ipAddress);
        return Redirect(res);
    }
    #endregion
}