using System.Text.Json.Serialization;

using EBooking.Bookings.Api;
using EBooking.Bookings.Application;
using EBooking.Bookings.Infrastructure;

using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["OpenTelemetry:ServiceName"];

if (string.IsNullOrWhiteSpace(serviceName))
{
    throw new InvalidOperationException("OpenTelemetry service name is not configured.");
}

var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];

if (string.IsNullOrWhiteSpace(otlpEndpoint))
{
    throw new InvalidOperationException("OTLP endpoint is not configured.");
}

builder.Services.AddApplicationServices();

builder.Services.AddInfrastructureServices(
    builder.Configuration);

builder.Services.AddJwtAuthentication(
    builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    const string bearerScheme = "bearer";

    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "EBooking Bookings API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        bearerScheme,
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = bearerScheme,
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "JWT Authorization header using the Bearer scheme."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                bearerScheme,
                document)] = []
        });
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(serviceName))
    .WithTracing(tracing =>
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(options =>
                options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter());

builder.Services.AddHostedService<BookingProcessingBackgroundService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<BookingsDbContext>();

    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

await app.RunAsync();

public partial class Program;