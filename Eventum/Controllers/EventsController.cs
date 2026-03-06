using Eventum.DTO;
using Eventum.Models;
using Eventum.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Controllers;

[ApiController]
[Route("events")]
[Produces("application/json")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Event>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(_eventService.GetAll());
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var ev = _eventService.GetById(id);

        if (ev is null) return NotFound();

        return Ok(ev);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post(CreateEventDto createdEvent)
    {
        if (createdEvent.StartAt > createdEvent.EndAt)
        {
            ModelState.AddModelError(nameof(createdEvent.EndAt), 
                "EndAt must be later than StartAt");

            return ValidationProblem(ModelState);
        }
        
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Put(Guid id, UpdateEventDto updatedEvent)
    {
        if (updatedEvent.StartAt > updatedEvent.EndAt)
        {
            ModelState.AddModelError(nameof(updatedEvent.EndAt), 
                "EndAt must be later than StartAt");

            return ValidationProblem(ModelState);
        }

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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        if (_eventService.Delete(id)) return NoContent();
        return NotFound();
    }
}