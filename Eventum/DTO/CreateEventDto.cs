using System.ComponentModel.DataAnnotations;

namespace Eventum.DTO;

public class CreateEventDto
{
    [Required] public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Required] public DateTime StartAt { get; set; }

    [Required] public DateTime EndAt { get; set; }
    
    [Required] public int? TotalSeats { get; set; }
    
}