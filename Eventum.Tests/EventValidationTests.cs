using System.ComponentModel.DataAnnotations;
using Eventum.Data.Interfaces;
using Eventum.Data.Repositories;
using Eventum.DataAccess.Contexts;
using Eventum.DTO;
using Eventum.Services;
using Eventum.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Eventum.Tests;

public class EventValidationTests
{
    private readonly ServiceProvider _provider;

    public EventValidationTests()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IEventRepository, EventRepository>();
        
        services.AddScoped<IEventService, EventService>();

        _provider = services.BuildServiceProvider();
    }
    
    [Fact]
    public async Task Validate_ShouldThrow_WhenEndAtBeforeStartAt()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var dto = new CreateEventDto
        {
            Title = "Test",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = 3
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
           await service.CreateAsync(dto));
    }
    
      
    [Fact]
    public void Validate_ShouldFail_WhenTitleIsMissing()
    {
        // Arrange
        var dto = new UpdateEventDto
        {
            Title = null!,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }
}