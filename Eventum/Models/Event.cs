namespace Eventum.Models;

public class Event
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int TotalSeats { get; set; }

    public int? AvailableSeats { get; set; }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats - count < 0) return false;
        AvailableSeats -= count;
        return true;
    }

    //not used yet 
    public void ReleaseSeats(int count = 1)
    {
        if (AvailableSeats + count > TotalSeats)
            AvailableSeats = TotalSeats;
        else
            AvailableSeats += count;
    }
}