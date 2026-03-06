using Eventum.DTO;
using Eventum.Models;
using Eventum.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_eventService.GetAll());
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var ev = _eventService.GetById(id);

        if (ev is null) return NotFound();

        return Ok(ev);
    }

    [HttpPost]
    public IActionResult Post(CreateEventDto createdEvent)
    {
        if (createdEvent.StartAt > createdEvent.EndAt) return BadRequest("EndAt must be later than StartAt");

        var newEvent = _eventService.Create(new Event
        {
            Description = createdEvent.Description,
            Title = createdEvent.Title,
            StartAt = createdEvent.StartAt,
            EndAt = createdEvent.EndAt,
        });

        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, newEvent);
    }

    [HttpPut("{id}")]
    public IActionResult Put(Guid id, UpdateEventDto updatedEvent)
    {
        if (updatedEvent.StartAt > updatedEvent.EndAt) return BadRequest("EndAt must be later than StartAt");

        var updated = _eventService.Update(id, new Event
        {
            Description = updatedEvent.Description,
            Title = updatedEvent.Title,
            StartAt = updatedEvent.StartAt,
            EndAt = updatedEvent.EndAt,
        });

        if (!updated) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        if (_eventService.Delete(id)) return NoContent();
        return NotFound();
    }
}