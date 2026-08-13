using HakwadagAssassinGame.Infrastructure.Services;

namespace HakwadagAssassinGame.Tests.Integration.Redis;

public sealed class RedisTokenStoreTests : RedisTestBase
{
    private RedisTokenStore CreateStore() => new(Multiplexer);

    [Fact]
    public async Task StoreAndGetPlayerId()
    {
        var store = CreateStore();
        var token = "test-token-123";
        var playerId = Guid.NewGuid();

        await store.StoreAsync(token, playerId);
        var retrieved = await store.GetPlayerIdAsync(token);

        Assert.NotNull(retrieved);
        Assert.Equal(playerId, retrieved.Value);
    }

    [Fact]
    public async Task GetPlayerId_NonExistentToken_ReturnsNull()
    {
        var store = CreateStore();

        var retrieved = await store.GetPlayerIdAsync("nonexistent");

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Remove_RemovesToken()
    {
        var store = CreateStore();
        var token = "test-token-456";
        var playerId = Guid.NewGuid();
        await store.StoreAsync(token, playerId);

        await store.RemoveAsync(token);
        var retrieved = await store.GetPlayerIdAsync(token);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Remove_NonExistentToken_DoesNotThrow()
    {
        var store = CreateStore();

        var exception = await Record.ExceptionAsync(() => store.RemoveAsync("nonexistent"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Store_TokenHasExpiry()
    {
        var store = CreateStore();
        var token = "expiry-test";
        await store.StoreAsync(token, Guid.NewGuid());

        // Verify the TTL is set on the key (should be ~7 days)
        var ttl = await Database.KeyTimeToLiveAsync($"token:{token}");

        Assert.NotNull(ttl);
        Assert.True(ttl.Value.TotalDays > 0);
    }

    [Fact]
    public async Task MultipleTokens_DifferentPlayers()
    {
        var store = CreateStore();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        await store.StoreAsync("token1", p1);
        await store.StoreAsync("token2", p2);

        Assert.Equal(p1, (await store.GetPlayerIdAsync("token1"))!.Value);
        Assert.Equal(p2, (await store.GetPlayerIdAsync("token2"))!.Value);
    }
}
