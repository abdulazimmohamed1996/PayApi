using Microsoft.AspNetCore.Mvc;

namespace PayArabic.API.Controllers;

[Route("api/currency", Name = "Currency")]
public class CurrencyController : BaseController
{
    private readonly ICurrencyDao _dao;
    public CurrencyController(ICurrencyDao dao)
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

    [Route("getrate")]
    [HttpGet]
    [NotAuditable]
    public IActionResult GetRate(string currencyCode, float amount)
    {
        var result = _dao.GetRate(currencyCode, amount);
        return Ok(result);
    }

    [Route("insert")]
    [HttpPost]
    public IActionResult Insert([FromBody] CurrencyDTO.CurrencyInsert entity)
    {
        var result = _dao.Insert(CurrentUser.Id, CurrentUser.UserType, entity);
        return Ok(result);
    }

    [Route("update")]
    [HttpPut]
    public IActionResult Update([FromBody] CurrencyDTO.CurrencyUpdate entity)
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