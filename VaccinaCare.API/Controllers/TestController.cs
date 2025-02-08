using Microsoft.AspNetCore.Mvc;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{ 
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Test Controller is workiádasdasdasdasdsang!" });
    }
}