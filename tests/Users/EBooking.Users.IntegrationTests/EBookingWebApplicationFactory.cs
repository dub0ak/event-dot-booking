namespace EBooking.Users.IntegrationTests;

using EBooking.Users.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class EBookingWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public EBookingWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<UserDbContext>>();
            services.RemoveAll<UserDbContext>();
            services.AddDbContext<UserDbContext>(
                options => options.UseNpgsql(_connectionString));
        });
    }
}