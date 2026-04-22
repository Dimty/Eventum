namespace Eventum.DTO;

public class EventResponseDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string? Description { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }
    
    public int TotalSeats { get; set; }
    
    public int AvailableSeats { get; set; }
}