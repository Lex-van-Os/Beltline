namespace Beltline.BagTracking.Domain.Tests;

public class BeltlineDomainTests
{
    private static int Add(int x, int y) => x + y;

    [Fact]
    public void PlaceholderAdditionEquals() => Assert.Equal(4, Add(2, 2));
    
    [Fact]
    public void PlaceholderAdditionAdjective() => Assert.NotEqual(2, Add(2, 2));
}