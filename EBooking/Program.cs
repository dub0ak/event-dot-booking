using System.Reflection;
using EBooking.Handlers;
using EBooking.Interfaces;
using EBooking.Middleware;
using EBooking.Services;
using EBooking.DataStore;
using EBooking.BackgroundServices;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEventsService, EventsService>();
builder.Services.AddSingleton<BookingStore>();
builder.Services.AddSingleton<IBookingService, BookingService>();
builder.Services.AddHostedService<BookingProcessingBackgroundService>();
builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value is not null && x.Value.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToList();

        var message = errors.Count > 0
            ? string.Join("; ", errors)
            : "Validation failed";

        return new BadRequestObjectResult(new ErrorResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Message = message
        });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.Run();