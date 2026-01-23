using Hive.Application;
using Hive.Application.Interfaces;
using Hive.Infrastructure;
using Hive.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Tests.Integration;

/// <summary>
/// Base class for integration tests that use real services and repositories
/// with an in-memory database context.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly InMemoryDbContext DbContext;

    protected IntegrationTestBase(bool seedData = false)
    {
        var services = new ServiceCollection();

        // Add infrastructure services with in-memory storage
        services.AddInfrastructureServices(seedData: seedData);

        // Add application services
        services.AddApplicationServices();

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<InMemoryDbContext>();
    }

    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
