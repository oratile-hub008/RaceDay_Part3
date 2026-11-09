using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Tests;

/// <summary>Boots the real API in memory, swapping SQL Server for an in-memory database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SeedDemoData", "false");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Never talk to real Azure during tests.
            var storage = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorage));
            if (storage != null) services.Remove(storage);
            services.AddSingleton<IBlobStorage, FakeBlobStorage>();
        });
    }
}

/// <summary>Pretends to upload an image and returns a fake URL.</summary>
public class FakeBlobStorage : IBlobStorage
{
    public Task<string> UploadImageAsync(IFormFile file, string folder) =>
        Task.FromResult($"https://fake-storage.test/{folder}/{file.FileName}");
}
