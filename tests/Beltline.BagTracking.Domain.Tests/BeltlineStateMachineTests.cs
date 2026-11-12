using Beltline.BagTracking.Domain.Enums;
using Beltline.BagTracking.Domain.Facts;
using Beltline.BagTracking.Domain.Models;
using Shouldly;

namespace Beltline.BagTracking.Domain.Tests;

public class BeltlineStateMachineTests
{
    // Tests to add for this issue:
    // 1. 
    
    private const string TagNumberValue = "0771927081";
    private const int FlightId = 42;

    [Theory]
    [InlineData(Checkpoint.BagDrop, BagStatus.BagRegistered, BagStatus.DroppedOff)]
    [InlineData(Checkpoint.Induction, BagStatus.DroppedOff, BagStatus.Inducted)]
    [InlineData(Checkpoint.ScreeningEntry, BagStatus.Inducted, BagStatus.ScreeningPending)]
    [InlineData(Checkpoint.SorterEntry, BagStatus.ScreeningCleared, BagStatus.Sorting)]
    [InlineData(Checkpoint.MakeUp, BagStatus.Sorting, BagStatus.AwaitingLoading)]
    [InlineData(Checkpoint.LoadingPoint, BagStatus.LoadAuthorized, BagStatus.Loading)]
    public void Apply_BagScannedWithLaterCheckpoint_AdvancesStatus(
        Checkpoint checkpoint, BagStatus currentStatus, BagStatus expectedStatus)
    {
        // Arrange
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(checkpoint));

        // Assert
        changed.ShouldBeTrue();
        bagLeg.Status.ShouldBe(expectedStatus);
        bagLeg.Version.ShouldBe(previousVersion + 1);
    }

    [Theory]
    [InlineData(Checkpoint.Induction, BagStatus.Inducted)] // Same status (duplicate scan)
    [InlineData(Checkpoint.BagDrop, BagStatus.Sorting)] // Earlier status
    [InlineData(Checkpoint.MakeUp, BagStatus.Loading)] // Earlier status
    public void Apply_BagScannedWithCheckpointNotLater_LeavesBagLegUnchanged(
        Checkpoint checkpoint, BagStatus currentStatus)
    {
        // Arrange
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(checkpoint));

        // Assert
        changed.ShouldBeFalse();
        bagLeg.Status.ShouldBe(currentStatus);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    [Theory]
    [InlineData(BagStatus.SecurityHold)]
    [InlineData(BagStatus.Offloading)]
    [InlineData(BagStatus.Departed)]
    [InlineData(BagStatus.Offloaded)]
    [InlineData(BagStatus.Missed)]
    [InlineData(BagStatus.Missing)]
    public void Apply_BagScannedWithStatusOutsideHappyPath_LeavesBagLegUnchanged(BagStatus currentStatus)
    {
        // Arrange
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(Checkpoint.BagDrop));

        // Assert
        changed.ShouldBeFalse();
        bagLeg.Status.ShouldBe(currentStatus);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    [Fact]
    public void Apply_BagScannedSkippingStatuses_AdvancesStatus()
    {
        // Arrange: the screening scans were missed, the bag is seen again at the sorter
        var bagLeg = CreateBagLeg(BagStatus.Inducted);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(Checkpoint.SorterEntry));

        // Assert
        changed.ShouldBeTrue();
        bagLeg.Status.ShouldBe(BagStatus.Sorting);
        bagLeg.Version.ShouldBe(previousVersion + 1);
    }

    [Fact]
    public void Apply_ScreeningStarted_AdvancesStatusToScreeningInProgress()
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.ScreeningPending);
        var previousVersion = bagLeg.Version;
        var fact = new ScreeningStarted { TagNumber = TagNumberValue, FlightId = FlightId };

        // Act
        var changed = BagLegStateMachine.Apply(bagLeg, fact);

        // Assert
        changed.ShouldBeTrue();
        bagLeg.Status.ShouldBe(BagStatus.ScreeningInProgress);
        bagLeg.Version.ShouldBe(previousVersion + 1);
    }

    [Fact]
    public void Apply_ScreeningCompletedCleared_AdvancesStatusToScreeningCleared()
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.ScreeningInProgress);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(bagLeg, CreateScreeningCompleted(ScreeningResult.Cleared));

        // Assert
        changed.ShouldBeTrue();
        bagLeg.Status.ShouldBe(BagStatus.ScreeningCleared);
        bagLeg.Version.ShouldBe(previousVersion + 1);
    }

    [Fact]
    public void Apply_ScreeningCompletedFailed_LeavesBagLegUnchanged()
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.ScreeningInProgress);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(bagLeg, CreateScreeningCompleted(ScreeningResult.Failed));

        // Assert
        changed.ShouldBeFalse();
        bagLeg.Status.ShouldBe(BagStatus.ScreeningInProgress);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    // Puts the BagLeg directly in the given status, so each test starts from a known point
    private static BagLeg CreateBagLeg(BagStatus status)
    {
        var bagLeg = new BagLeg(new TagNumber(TagNumberValue), FlightId);

        if (status != BagStatus.BagRegistered)
            bagLeg.ChangeStatus(status);

        return bagLeg;
    }

    // A flight for which loading has not opened yet
    private static FlightHandling CreateFlightHandling() => new(FlightId);

    private static BagScanned CreateBagScanned(Checkpoint checkpoint) =>
        new() { TagNumber = TagNumberValue, FlightId = FlightId, Checkpoint = checkpoint };

    private static ScreeningCompleted CreateScreeningCompleted(ScreeningResult result) =>
        new() { TagNumber = TagNumberValue, FlightId = FlightId, Result = result };
}
