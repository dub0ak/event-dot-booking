using System.Text;
using System.Text.Json.Serialization;

using EBooking.Users.Api;
using EBooking.Users.Application;
using EBooking.Users.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

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
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

var jwtSecret = jwtSection["Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JWT secret is not configured.");
}

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("JWT secret must contain at least 32 bytes.");
}

var jwtIssuer = jwtSection["Issuer"];

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException("JWT issuer is not configured.");
}

var jwtAudience = jwtSection["Audience"];

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT audience is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)
                ),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
await ApplyMigrationsAsync(app);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<UserDbContext>();

    await dbContext.Database.MigrateAsync();
}

public partial class Program;