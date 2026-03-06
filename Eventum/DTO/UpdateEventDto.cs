using System.ComponentModel.DataAnnotations;

namespace Eventum.DTO;

public class UpdateEventDto:IValidatableObject
{
    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartAt > EndAt)
        {
            yield return new ValidationResult(
                "EndAt must be later than StartAt",
                new[] { nameof(EndAt) });
        }
    }
}