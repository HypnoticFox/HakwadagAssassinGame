using StackExchange.Redis;

namespace HakwadagAssassinGame.Tests.Integration.Redis;

/// <summary>
/// Base class for Redis integration tests. Tries to connect to a local Redis instance;
/// tests are skipped when Redis is unavailable.
/// </summary>
public abstract class RedisTestBase : IAsyncLifetime
{
    private const string RedisConnectionString = "localhost:6379";

    /// <summary>Gets the Redis database used by all repository tests.</summary>
    protected IDatabase Database { get; private set; } = null!;

    /// <summary>Gets the connection multiplexer used by all repository tests.</summary>
    protected IConnectionMultiplexer Multiplexer { get; private set; } = null!;

    /// <summary>
    /// When true, Redis is available and tests can proceed.
    /// When false, tests should be skipped.
    /// </summary>
    protected bool RedisAvailable { get; private set; }

    /// <summary>Flushes the selected database before each test.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            Multiplexer = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);
            Database = Multiplexer.GetDatabase();
            RedisAvailable = true;
            await Database.ExecuteAsync("FLUSHDB");
        }
        catch
        {
            RedisAvailable = false;
        }
    }

    /// <summary>Disposes the Redis connection.</summary>
    public async Task DisposeAsync()
    {
        if (RedisAvailable && Multiplexer is not null)
        {
            await Database.ExecuteAsync("FLUSHDB");
            Multiplexer.Dispose();
        }
    }

    /// <summary>Skips the test when Redis is unavailable.</summary>
    protected void SkipIfRedisUnavailable()
    {
        if (!RedisAvailable)
        {
            throw new SkipException("Redis is not available on localhost:6379. Skipping integration test.");
        }
    }
}
