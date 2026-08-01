using System.ComponentModel.DataAnnotations;

namespace Eventum.Events.Domain;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }

    private Event()
    {
    }

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
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Title is required");

        if (startAt >= endAt)
            throw new ValidationException("StartAt must be before EndAt");

        if (totalSeats <= 0)
            throw new ValidationException("TotalSeats must be greater than zero");

        return new Event(title, description, startAt, endAt, totalSeats);
    }

    public static Event Restore(
        Guid id,
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        int availableSeats)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Id is required");

        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Title is required");

        if (startAt >= endAt)
            throw new ValidationException("StartAt must be before EndAt");

        if (totalSeats <= 0)
            throw new ValidationException("TotalSeats must be greater than zero");

        if (availableSeats < 0 || availableSeats > totalSeats)
            throw new ValidationException("AvailableSeats must be between zero and TotalSeats");

        return new Event
        {
            Id = id,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats
        };
    }

    public void Update(string title, string? description, DateTime startAt, DateTime endAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Title is required");

        if (startAt >= endAt)
            throw new ValidationException("StartAt must be before EndAt");

        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }

    public bool TryDecreaseAvailableSeats(int seats)
    {
        if (seats <= 0)
            throw new ValidationException("Seats must be greater than zero");

        if (AvailableSeats < seats)
            return false;

        AvailableSeats -= seats;
        return true;
    }
}
