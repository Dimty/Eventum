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
    [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var events = _eventService.GetAll()
            .Select(e => new EventResponseDto
            {
                Id = e.Id,
                Description = e.Description,
                Title = e.Title,
                StartAt = e.StartAt,
                EndAt = e.EndAt,
            });
        return Ok(events);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var ev = _eventService.GetById(id);

        var dto = new EventResponseDto
        {
            Id = ev.Id,
            Description = ev.Description,
            Title = ev.Title,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
        };

        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post(CreateEventDto createdEvent)
    {
        var newEvent = _eventService.Create(new Event
        {
            Description = createdEvent.Description,
            Title = createdEvent.Title,
            StartAt = createdEvent.StartAt,
            EndAt = createdEvent.EndAt,
        });

        var dto = new EventResponseDto
        {
            Id = newEvent.Id,
            Description = newEvent.Description,
            Title = newEvent.Title,
            StartAt = newEvent.StartAt,
            EndAt = newEvent.EndAt,
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Put(Guid id, UpdateEventDto updatedEvent)
    {
        var updated = _eventService.Update(id, new Event
        {
            Description = updatedEvent.Description,
            Title = updatedEvent.Title,
            StartAt = updatedEvent.StartAt,
            EndAt = updatedEvent.EndAt,
        });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        _eventService.Delete(id);
        return NoContent();
    }
}