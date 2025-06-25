using Moq;
using StackExchange.Redis;
using DiscountCodeServer.Redis;
using System.Text.Json;

namespace DiscountCodeServer.Tests;
public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockConnection;
    private readonly Mock<IDatabase> _mockDb;
    private readonly RedisCacheService _service;

    public RedisCacheServiceTests()
    {
        _mockConnection = new Mock<IConnectionMultiplexer>();
        _mockDb = new Mock<IDatabase>();
        _mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDb.Object);
        _service = new RedisCacheService(_mockConnection.Object);
    }

    [Fact]
    public async Task GetAsync_CacheHit_ReturnsDeserializedObject()
    {
        var key = "test-key";
        var expected = new TestObj { Value = 42 };
        var json = JsonSerializer.Serialize(expected);

        _mockDb.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        var result = await _service.GetAsync<TestObj>(key);

        Assert.NotNull(result);
        Assert.Equal(expected.Value, result.Value);
    }

    [Fact]
    public async Task GetAsync_CacheMiss_ThrowsKeyNotFoundException()
    {
        var key = "missing-key";
        _mockDb.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)string.Empty);

        try
        {
            var result = await _service.GetAsync<TestObj>(key);
            Assert.True(false, "Expected KeyNotFoundException was not thrown.");
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"Caught expected exception: {ex.GetType().Name} - {ex.Message}");
            Assert.Contains(key, ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Caught unexpected exception: {ex.GetType().Name} - {ex.Message}");
            Assert.True(false, $"Unexpected exception type: {ex.GetType().Name}");
        }
    }

    [Fact]
    public async Task RemoveAsync_KeyExists_LogsAndRemoves()
    {
        var key = "remove-key";
        _mockDb.Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true)
            .Verifiable();

        await _service.RemoveAsync(key);

        _mockDb.Verify();
    }

    [Fact]
    public async Task RemoveAsync_KeyDoesNotExist_LogsWarning()
    {
        var key = "remove-missing-key";
        _mockDb.Setup(db => db.KeyDeleteAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false)
            .Verifiable();

        await _service.RemoveAsync(key);

        _mockDb.Verify();
    }

    public class TestObj
    {
        public int Value { get; set; }
    }
}