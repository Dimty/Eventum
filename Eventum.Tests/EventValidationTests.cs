using System.ComponentModel.DataAnnotations;
using Eventum.DTO;
using Eventum.Exceptions;
using Eventum.Services;

namespace Eventum.Tests;

public class EventValidationTests
{
    private readonly EventService _service = new();
    
    [Fact]
    public void Validate_ShouldThrow_WhenEndAtBeforeStartAt()
    {
        var dto = new CreateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = 3
        };

        Assert.Throws<ValidationException>(() =>
            _service.Create(dto));
    }
    
      
    [Fact]
    public void Validate_ShouldFail_WhenTitleIsMissing()
    {
        var dto = new UpdateEventDto
        {
            Title = null!,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }
}