using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[Route("api/productcategory", Name = "ProductCategory")]
public class ProductCategoryController : BaseController
{
    private readonly IProductCategoryDao _dao;
    public ProductCategoryController(IProductCategoryDao dao)
    {
        _dao = dao;
    }

    [Route("getall")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetAll(string listOptions = null)
    {
        var result = _dao.GetAll(CurrentUser.Id, CurrentUser.UserType, listOptions);
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

    [Route("insert")]
    [HttpPost]
    public IActionResult Insert([FromBody] ProductCategoryDTO.ProductCategoryInsert entity)
    {
        var result = _dao.Insert(CurrentUser.Id, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] ProductCategoryDTO.ProductCategoryUpdate entity)
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