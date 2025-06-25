using DiscountCodeServer.DB;
using DiscountCodeServer.Redis;
using DiscountCodeServer.Repositories;
using DiscountCodeServer.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using Xunit;

namespace DiscountCodeServer.Tests;

public class DiscountCodeGenerationTestsReal
{
    private readonly DiscountCodeRepository _repository;
    private readonly DiscountCodeService _service;

    // Static Redis connection shared across all tests
    private static readonly ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");

    // Static constructor to initialize Redis and warm up the connection
    static DiscountCodeGenerationTestsReal()
    {
        _ = WarmUpRedisAsync(); // Fire-and-forget, or await in async context if possible
    }

    public DiscountCodeGenerationTestsReal()
    {
        // Initialize database context and services as before
        var options = new DbContextOptionsBuilder<DiscountCodeContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DiscountCodeDb;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options;
        var context = new DiscountCodeContext(options);

        var redisConfig = new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" },
            AbortOnConnectFail = false,
            ConnectTimeout = 10000,
            SyncTimeout = 10000
        };
        var redisInstance = ConnectionMultiplexer.Connect(redisConfig);
        var cacheService = new RedisCacheService(redisInstance);

        _repository = new DiscountCodeRepository(context, cacheService);
        _service = new DiscountCodeService(_repository, new DiscountCodeGenerator(), new UnitOfWork.UnitOfWork(context));
    }

    // Async method to warm up Redis connection
    private static async Task WarmUpRedisAsync()
    {
        var db = redis.GetDatabase();
        var pong = await db.PingAsync();
        Console.WriteLine($"Warm-up Redis response: {pong.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task GenerateCodes_Multithreaded_CreatesUniqueCodes()
    {
        int threadCount = 10;
        ushort codesPerThread = 1000;
        int totalGeneratedCodes = threadCount * codesPerThread;

        // Get count before generation
        var allCodesBefore = await _repository.GetAllCodesAsync();

        var tasks = new List<Task>();
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            { 
                await _service.GenerateAndAddCodesAsync(codesPerThread, 7);
            }));
        }
        await Task.WhenAll(tasks);

        // Get count after generation
        var allCodesAfter = await _repository.GetAllCodesAsync();

        int countBefore = allCodesBefore.Count;
        int countAfter = allCodesAfter.Count;

        // Assert the difference matches the number of generated codes
        Assert.Equal(totalGeneratedCodes, countAfter - countBefore);

        // Assert uniqueness of codes
        var distinctCodes = allCodesAfter.Distinct().ToList();
        Assert.Equal(countAfter, distinctCodes.Count);
    }

    [Fact]
    public async Task GenerateCodes_Multithreaded_CreatesUniqueCodes2()
    {
        int threadCount = 10;
        ushort codesPerThread = 1000;
        int totalGeneratedCodes = threadCount * codesPerThread;

        // Get count before generation
        var allCodesBefore = await _repository.GetAllCodesAsync();

        var tasks = new List<Task>();
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await _service.GenerateAndAddCodesAsync(codesPerThread, 7);
            }));
        }
        await Task.WhenAll(tasks);

        // Get count after generation
        var allCodesAfter = await _repository.GetAllCodesAsync();

        int countBefore = allCodesBefore.Count;
        int countAfter = allCodesAfter.Count;

        // Assert the difference matches the number of generated codes
        Assert.Equal(totalGeneratedCodes, countAfter - countBefore);

        // Assert uniqueness of codes
        var distinctCodes = allCodesAfter.Distinct().ToList();
        Assert.Equal(countAfter, distinctCodes.Count);
    }

    [Fact]
    public async Task RedisPingTest()
    {
        var db = redis.GetDatabase();
        var pong = await db.PingAsync();
        Console.WriteLine($"Redis responded in {pong.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task RedisPingTest2()
    {
        var db = redis.GetDatabase();
        var pong = await db.PingAsync();
        Console.WriteLine($"Redis responded in {pong.TotalMilliseconds} ms");
    }
}
