using Eventum.DTO;
using Eventum.Models;
using Eventum.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Controllers;

[ApiController]
[Route("events")]
[Produces("application/json")]
public class EventsController(IEventService eventService,
    IBookingService bookingService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;
    private readonly IBookingService _bookingService = bookingService;
    
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<Event>), StatusCodes.Status200OK)]
    public IActionResult Get(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var events = _eventService.GetAll(title, from, to, page, pageSize);
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
            TotalSeats = ev.TotalSeats,
            AvailableSeats = ev.AvailableSeats
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
            TotalSeats = createdEvent.TotalSeats!.Value
        });

        var dto = new EventResponseDto
        {
            Id = newEvent.Id,
            Description = newEvent.Description,
            Title = newEvent.Title,
            StartAt = newEvent.StartAt,
            EndAt = newEvent.EndAt,
            TotalSeats = newEvent.TotalSeats,
            AvailableSeats = newEvent.AvailableSeats
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

    [HttpPost("{id}/book")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(Guid id)
    {
        var booking = await _bookingService.CreateBookingAsync(id);

        Response.Headers.Location = $"/bookings/{booking.Id}";
        return Accepted(booking);
    }
}