using DiscountCodeServer.Services;

namespace DiscountCodeServer.Tests;

public class DiscountCodeGeneratorTests
{
    private readonly DiscountCodeGenerator _generator = new();

    [Fact]
    public void GenerateCodes_ValidInput_ReturnsCorrectCount()
    {
        var codes = _generator.GenerateCodes(10, 8, []);
        Assert.Equal(10, codes.Count);
        Assert.All(codes, c => Assert.Equal(8, c.Length));
        Assert.Equal(10, codes.Distinct().Count());
    }

    [Fact]
    public void GenerateCodes_ValidInputWithExistingCodes_NoDuplicates()
    {
        var existing = new List<string> { "ABCDEFGH", "12345678" };
        var codes = _generator.GenerateCodes(5, 8, existing);
        Assert.Equal(5, codes.Count);
        Assert.All(codes, c => Assert.DoesNotContain(c, existing));
        Assert.Equal(5, codes.Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2001)]
    public void GenerateCodes_InvalidCount_ThrowsArgumentException(ushort count)
    {
        var ex = Assert.Throws<ArgumentException>(() => _generator.GenerateCodes(count, 8, []));
        Assert.Contains("Count", ex.Message);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(9)]
    public void GenerateCodes_InvalidLength_ThrowsArgumentException(byte length)
    {
        var ex = Assert.Throws<ArgumentException>(() => _generator.GenerateCodes(1, length, []));
        Assert.Contains("Length", ex.Message);
    }

    [Fact(Skip = "This test is not valid for the current implementation. " +
        "In order to make it valid you need to remove the lower limit of " +
        "the code length in order to trigger collision issues - from 7 to 2.")]
    public void GenerateCodes_ExcessiveExistingCodes_ThrowsException()
    {
        // Fill up all possible 7-char codes (36^7 is huge, so we simulate a collision scenario)
        var generator = new DiscountCodeGenerator();

        // Simulate by making the generator always return the same code
        var codes = new List<string>();
        for (int i = 0; i < 100; i++)
            codes.Add("ABCDEFG");

        // Should throw after too many attempts
        Assert.Throws<Exception>(() => generator.GenerateCodes(2000, 2, codes));
    }

    [Fact]
    public void GenerateCodes_AllCodesAreAlphanumeric()
    {
        var codes = _generator.GenerateCodes(20, 7, []);
        foreach (var code in codes)
        {
            Assert.Matches("^[A-Z0-9]{7}$", code);
        }
    }

    [Fact]
    public void GenerateCodes_ProducesUniqueCodesAcrossMultipleCalls()
    {
        var codes1 = _generator.GenerateCodes(10, 8, []);
        var codes2 = _generator.GenerateCodes(10, 8, codes1);
        Assert.Empty(codes1.Intersect(codes2));
    }
}