using System.ComponentModel.DataAnnotations;

namespace Eventum.Models;

public class Event
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    private Event() { }

    private Event(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (startAt > endAt)
            throw new ValidationException("EndAt must be later than StartAt");

        if (totalSeats <= 0)
            throw new ValidationException("TotalSeats must be greater than zero");

        return new Event(title, description, startAt, endAt, totalSeats);
    }
    
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats - count < 0) return false;
        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        if (AvailableSeats + count > TotalSeats)
            AvailableSeats = TotalSeats;
        else
            AvailableSeats += count;
    }
}