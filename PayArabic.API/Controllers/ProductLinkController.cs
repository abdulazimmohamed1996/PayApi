using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[Route("api/productlink", Name = "ProductLink")]
public class ProductLinkController : BaseController
{
    private readonly IProductLinkDao _dao;
    public ProductLinkController(IProductLinkDao dao)
    {
        _dao = dao;
    }

    [Route("getall")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetAll(string name, string listOptions = null)
    {
        var result = _dao.GetAll(CurrentUser.Id, CurrentUser.UserType, name, listOptions);
        return Ok(result);
    }

    [Route("getbyid")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetById(long id)
    {
        var result = _dao.GetById(CurrentUser.Id, CurrentUser.UserType, id);
        return Ok(result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("getcategory")]
    [HttpGet]
    public IActionResult GetCategory()
    {
        var result = _dao.GetCategory(CurrentUser.Id, CurrentUser.UserType);
        return Ok(result);
    }

    [Route("getforpayment")]
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetForPayment(long key)
    {
        var result = _dao.GetForPayment(key);
        return Ok(result); 
    }

    [Route("insert")]
    [HttpPost]
    public IActionResult Insert([FromBody] ProductLinkDTO.ProductLinkInsert entity)
    {
        var result = _dao.Insert(CurrentUser.Id, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] ProductLinkDTO.ProductLinkUpdate entity)
    {
        var result = _dao.Update(CurrentUser.Id, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [Route("delete")]
    [HttpDelete]
    public IActionResult Delete(long id)
    {
        var result = _dao.Delete(CurrentUser.Id, CurrentUser.UserType, id);
        return Ok(result);
    }

}