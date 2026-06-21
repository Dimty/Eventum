using Eventum.Application.DTO;
using Eventum.Application.Interfaces.Services;
using Eventum.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.WebApi.Controllers;

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
    public async Task<IActionResult> Get(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var events = await _eventService.GetAllAsync(title, from, to, page, pageSize);
        return Ok(events);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ev = (await _eventService.GetByIdAsync(id))!;

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
    public async Task<IActionResult> Post(CreateEventDto createdEvent)
    {
        var newEvent = await _eventService.CreateAsync(createdEvent);

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
    public async Task<IActionResult> PutAsync(Guid id, UpdateEventDto updatedEvent)
    {
        await _eventService.UpdateAsync(id, updatedEvent);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/book")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(Guid id)
    {
        var booking = await _bookingService.CreateBookingAsync(id);

        Response.Headers.Location = $"/bookings/{booking.Id}";

        var dto = new BookingResponseDto
        {
            Id = booking.Id,
            EventId =  booking.Event.Id,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt,
        };
        
        return Accepted(dto);
    }
}