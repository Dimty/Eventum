using System.Security.Claims;
using Eventum.Application.Interfaces.Services;
using Eventum.Domain.Models;
using Eventum.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.WebApi.Controllers;

[ApiController]
[Route("bookings")]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService): Controller
{
    private readonly  IBookingService _bookingService = bookingService;
    [Authorize]
    [HttpGet("{id}", Name = "GetBookingById")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        return Ok(booking);
    }
    
    [Authorize]
    [HttpPut("{id}", Name = "CancelBookingById")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var booking = await _bookingService.CancelBookingAsync(id, User.GetUserId());
        return Ok();
    }
    
    [Authorize]
    [HttpDelete("{id}", Name = "DeleteBooking")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBooking(Guid id)
    {
        await _bookingService.DeleteBookingAsync(id, User.GetUserId());
        return Ok();
    }
}