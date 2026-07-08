using Eventum.Bookings.Application.DTO;
using Eventum.Bookings.Application.Exceptions;
using Eventum.Bookings.Application.Interfaces;
using Eventum.Bookings.Domain;
using Eventum.Bookings.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Bookings.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost("events/{eventId:guid}/book")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(Guid eventId, CancellationToken token)
    {
        try
        {
            var booking = await bookingService.CreateBookingAsync(eventId, User.GetUserId(), token: token);
            Response.Headers.Location = $"/bookings/{booking.Id}";
            return Accepted(ToDto(booking));
        }
        catch (BusinessRuleViolationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = ex.RuleName, Detail = ex.Message });
        }
    }

    [HttpGet("bookings/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(Guid id, CancellationToken token)
    {
        try
        {
            var booking = await bookingService.GetBookingByIdAsync(id, token);
            return Ok(ToDto(booking));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = ex.Message });
        }
    }

    [HttpDelete("bookings/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(Guid id, CancellationToken token)
    {
        try
        {
            await bookingService.CancelBookingAsync(id, User.GetUserId(), token);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Detail = ex.Message
            });
        }
        catch (BookingAlreadyCancelledException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = ex.Message });
        }
    }

    private static BookingResponseDto ToDto(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        UserId = booking.UserId,
        Seats = booking.Seats,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}
