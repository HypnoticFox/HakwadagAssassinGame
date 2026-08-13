using System.Collections.Concurrent;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace HakwadagAssassinGame.Tests.Integration.Redis;

/// <summary>
/// Base class for Redis integration tests. Starts a shared Redis container with Testcontainers;
/// the container is started once and reused across all test classes.
/// Each test class is assigned its own Redis database index so classes can run in parallel
/// without flushing each other's data.
/// </summary>
public abstract class RedisTestBase : IAsyncLifetime
{
    private static readonly Lazy<Task<RedisContainer>> SharedContainer = new(StartContainerAsync);
    private static readonly ConcurrentDictionary<string, int> DatabaseIndexByClass = new();
    private static int nextDatabaseIndex;

    private static async Task<RedisContainer> StartContainerAsync()
    {
        var container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
        await container.StartAsync();
        return container;
    }

    /// <summary>Gets the Redis database used by all repository tests.</summary>
    protected IDatabase Database { get; private set; } = null!;

    /// <summary>Gets the connection multiplexer used by all repository tests.</summary>
    protected IConnectionMultiplexer Multiplexer { get; private set; } = null!;

    /// <summary>Connects to the shared Redis container and flushes the class database before each test.</summary>
    public async ValueTask InitializeAsync()
    {
        var container = await SharedContainer.Value;
        var databaseIndex = DatabaseIndexByClass.GetOrAdd(
            GetType().FullName ?? GetType().Name,
            static _ => Interlocked.Increment(ref nextDatabaseIndex) - 1);
        var options = ConfigurationOptions.Parse(container.GetConnectionString() + ",allowAdmin=true");
        options.DefaultDatabase = databaseIndex;
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(options);
        Database = Multiplexer.GetDatabase();
        await Database.ExecuteAsync("FLUSHDB");
    }

    /// <summary>Flushes the class database and disposes the Redis connection. The shared container is left running.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Multiplexer is not null)
        {
            await Database.ExecuteAsync("FLUSHDB");
            Multiplexer.Dispose();
        }
    }
}
