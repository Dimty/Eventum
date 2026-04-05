using Eventum.Models;
using Eventum.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Eventum.Controllers;

[ApiController]
[Route("bookings")]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService): Controller
{
    private readonly  IBookingService _bookingService = bookingService;
    
    [HttpGet("{id}", Name = "GetBookingById")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        return Ok(booking);
    }
}