using Eventum.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsControllers():ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        throw  new NotImplementedException();
    }
    
    [HttpPost]
    public IActionResult Post()
    {
        throw new NotImplementedException();
    }
    
    [HttpPut]
    public IActionResult Put()
    {
        throw new NotImplementedException();
    }

    [HttpDelete]
    public IActionResult Delete()
    {
        throw  new NotImplementedException();
    }
}