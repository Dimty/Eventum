using Eventum.Events.Application.DTO;
using Eventum.Events.Application.Exceptions;
using Eventum.Events.Application.Interfaces;
using Eventum.Events.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Events.WebApi.Controllers;

[ApiController]
[Route("events")]
[Produces("application/json")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<Event>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        string? title,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken token = default)
    {
        var events = await eventService.GetAllAsync(title, from, to, page, pageSize, token);
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken token)
    {
        try
        {
            var ev = await eventService.GetByIdAsync(id, token);
            return Ok(ToDto(ev));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateEventDto request, CancellationToken token)
    {
        var ev = await eventService.CreateAsync(request, token);
        var dto = ToDto(ev);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateEventDto request, CancellationToken token)
    {
        try
        {
            await eventService.UpdateAsync(id, request, token);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        try
        {
            await eventService.DeleteAsync(id, token);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = ex.Message });
        }
    }

    private static EventResponseDto ToDto(Event ev) => new()
    {
        Id = ev.Id,
        Title = ev.Title,
        Description = ev.Description,
        StartAt = ev.StartAt,
        EndAt = ev.EndAt,
        TotalSeats = ev.TotalSeats,
        AvailableSeats = ev.AvailableSeats
    };
}
