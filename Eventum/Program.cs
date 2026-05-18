using Eventum.Background;
using Eventum.Data.Interfaces;
using Eventum.Data.Repositories;
using Eventum.DataAccess.Contexts;
using Eventum.Middleware;
using Eventum.Services;
using Eventum.Services.Interfaces;
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

builder.Services.AddScoped<IBookingService>(sp =>
    sp.GetRequiredService<BookingService>());

builder.Services.AddScoped<IBookingProcessingService>(sp =>
    sp.GetRequiredService<BookingService>());

builder.Services.AddHostedService<BookingProcessingService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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