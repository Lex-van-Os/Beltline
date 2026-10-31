using Beltline.BagTracking.Domain.Models;
using Shouldly;

namespace Beltline.BagTracking.Domain.Tests;

public class BeltlineDomainTests
{
    [Theory]
    [InlineData("0771927081")]
    [InlineData("0123456789")] // Leading zero must be preserved
    [InlineData("9999999999")]
    public void TagNumber_WithValidNumber_ConstructsSuccessfully(string validNumber)
    {
        // Act
        var tagNumber = new TagNumber(validNumber);

        // Assert
        tagNumber.Number.ShouldBe(validNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("          ")] // Whitespace only
    [InlineData("077192708")] // Too short
    [InlineData("07719270811")] // Too long
    [InlineData("07a927481b")] // Letters
    [InlineData(" 771927081")] // Leading whitespace
    [InlineData("077192708\n")] // Trailing newline
    [InlineData("٠١٢٣٤٥٦٧٨٩")] // Non-ASCII (Arabic-Indic) digits
    public void TagNumber_WithInvalidFormat_ThrowsException(string? invalidNumber)
    {
        // Act and assert
        Should.Throw<ArgumentException>(() => new TagNumber(invalidNumber!));
    }

    [Fact]
    public void ConstructedBagLeg_WithExpectedValues_ConstructsSuccessfully()
    {
        // Arrange
        var tagNumber = new TagNumber("0771927081");
        var flightId = 42;

        // Act
        var bagLeg = new BagLeg(tagNumber, flightId);

        // Assert
        bagLeg.Status.ShouldBe(BagStatus.BagRegistered);
        bagLeg.Version.ShouldBe(1);
        bagLeg.Id.ShouldNotBe(Guid.Empty);
        bagLeg.Id.Version.ShouldBe(7);
        bagLeg.TagNumber.ShouldBe(tagNumber);
        bagLeg.FlightId.ShouldBe(flightId);
    }

    // The only statuses that end a bag's journey; every other status is derived from this list
    private static readonly BagStatus[] FinalStatuses =
        [BagStatus.Departed, BagStatus.Offloaded, BagStatus.Missed, BagStatus.Missing];

    public static TheoryData<BagStatus> FinalStatusData => new(FinalStatuses);

    public static TheoryData<BagStatus> NonFinalStatusData => new(Enum.GetValues<BagStatus>().Except(FinalStatuses));

    [Theory]
    [MemberData(nameof(FinalStatusData))]
    public void BagStatus_WithFinalStatus_ReturnsTrue(BagStatus finalStatus)
    {
        // Act and assert
        finalStatus.IsFinal().ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(NonFinalStatusData))]
    public void BagStatus_WithoutFinalStatus_ReturnsFalse(BagStatus nonFinalStatus)
    {
        // Act and assert
        nonFinalStatus.IsFinal().ShouldBeFalse();
    }
}
