using Eventum.Background;
using Eventum.Middleware;
using Eventum.Services;
using Eventum.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Singleton was used because the event list is stored in memory
builder.Services.AddSingleton<IEventService, EventService>();

builder.Services.AddSingleton<BookingService>();

builder.Services.AddSingleton<IBookingService>(sp =>
    sp.GetRequiredService<BookingService>());

builder.Services.AddSingleton<IBookingProcessingService>(sp =>
    sp.GetRequiredService<BookingService>());

builder.Services.AddHostedService<BookingProcessingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();