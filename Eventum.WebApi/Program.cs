using Eventum.Application.Common;
using Eventum.Application.Interfaces.Repositories;
using Eventum.Application.Interfaces.Services;
using Eventum.Application.Services;
using Eventum.Infrastructure.Background;
using Eventum.Infrastructure.Data.Contexts;
using Eventum.Infrastructure.Data.Repositories;
using Eventum.Infrastructure.Logging;
using Eventum.WebApi.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<IBookingService>(sp => sp.GetRequiredService<BookingService>());
builder.Services.AddScoped<IBookingProcessingService>(sp => sp.GetRequiredService<BookingService>());
builder.Services.AddHostedService<BookingProcessingService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(typeof(IAppLogger<>), typeof(AppLoggerAdapter<>));


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
