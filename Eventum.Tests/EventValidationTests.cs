using System.ComponentModel.DataAnnotations;
using Eventum.DTO;

namespace Eventum.Tests;

public class EventValidationTests
{
    [Fact]
    public void Validate_ShouldReturnError_WhenEndAtBeforeStartAt()
    {
        var dto = new UpdateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1)
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("EndAt"));
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