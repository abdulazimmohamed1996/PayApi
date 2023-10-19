using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[Route("api/product", Name = "Product")]
public class ProductController : BaseController
{
    private readonly IProductDao _dao;
    public ProductController(IProductDao dao)
    {
        _dao = dao;
    }

    [Route("getall")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetAll(long categoryId, string name, string desc, string listOptions = null)
    {
        var result = _dao.GetAll(CurrentUser.Id, CurrentUser.UserType, categoryId, name, desc, listOptions);
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

    [Route("autocomplete")]
    [HttpGet]
    [NotAuditable]
    public IActionResult AutoComplete(string name = "")
    {
        var result = _dao.AutoComplete(CurrentUser.Id, CurrentUser.UserType, name);
        return Ok(result);
    }

    [Route("insert")]
    [HttpPost]
    public IActionResult Insert([FromBody] ProductDTO.ProductInsert entity)
    {
        var result = _dao.Insert(CurrentUser.Id, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] ProductDTO.ProductUpdate entity)
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