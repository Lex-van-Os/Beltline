using Beltline.BagTracking.Domain.Enums;
using Beltline.BagTracking.Domain.Facts;
using Beltline.BagTracking.Domain.Models;
using Shouldly;

namespace Beltline.BagTracking.Domain.Tests;

public class BeltlineStateMachineTests
{
    private const string TagNumberValue = "0771927081";
    private const int FlightId = 42;
    private const int OtherFlightId = 43;
    private static readonly DateTimeOffset LoadingOpenedTime = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

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

    [Fact]
    public void Apply_BagScannedAtMakeUpWithLoadingOpen_AdvancesStatusToLoadAuthorized()
    {
        // Arrange
        var flightHandling = CreateOpenFlightHandling();
        var bagLeg = CreateBagLeg(BagStatus.Sorting);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(flightHandling, bagLeg, CreateBagScanned(Checkpoint.MakeUp));

        // Assert
        changed.ShouldBeTrue();
        bagLeg.Status.ShouldBe(BagStatus.LoadAuthorized);
        bagLeg.Version.ShouldBe(previousVersion + 1);
    }

    [Theory]
    [InlineData(BagStatus.Inducted)]
    [InlineData(BagStatus.Sorting)]
    [InlineData(BagStatus.AwaitingLoading)]
    public void Apply_BagScannedAtLoadingPointWithoutLoadAuthorized_LeavesBagLegUnchanged(BagStatus currentStatus)
    {
        // Arrange
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;

        // Act
        var changed = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(Checkpoint.LoadingPoint));

        // Assert
        changed.ShouldBeFalse();
        bagLeg.Status.ShouldBe(currentStatus);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    [Fact]
    public void Apply_BagScannedWithFlightHandlingOfOtherFlight_ThrowsArgumentException()
    {
        // Arrange
        var flightHandling = new FlightHandling(OtherFlightId);
        var bagLeg = CreateBagLeg(BagStatus.Sorting);

        // Act and assert
        Should.Throw<ArgumentException>(() =>
            BagLegStateMachine.Apply(flightHandling, bagLeg, CreateBagScanned(Checkpoint.MakeUp)));
        bagLeg.Status.ShouldBe(BagStatus.Sorting);
    }

    [Fact]
    public void Apply_LoadingOpened_OpensLoadingAtFactTime()
    {
        // Arrange
        var flightHandling = CreateFlightHandling();

        // Act
        var changed = BagLegStateMachine.Apply(flightHandling, CreateLoadingOpened(FlightId), []);

        // Assert
        changed.ShouldBeTrue();
        flightHandling.IsLoadingOpen.ShouldBeTrue();
        flightHandling.LoadingOpenedAt.ShouldBe(LoadingOpenedTime);
    }

    [Fact]
    public void Apply_LoadingOpened_AuthorizesOnlyAwaitingLoadingBags()
    {
        // Arrange
        var awaitingBagLegs = new[] { CreateBagLeg(BagStatus.AwaitingLoading), CreateBagLeg(BagStatus.AwaitingLoading) };
        var otherBagLegs = new[] { CreateBagLeg(BagStatus.Sorting), CreateBagLeg(BagStatus.Loading) };
        var awaitingVersions = awaitingBagLegs.Select(bagLeg => bagLeg.Version).ToArray();
        var otherVersions = otherBagLegs.Select(bagLeg => bagLeg.Version).ToArray();

        // Act
        BagLegStateMachine.Apply(CreateFlightHandling(), CreateLoadingOpened(FlightId), [..awaitingBagLegs, ..otherBagLegs]);

        // Assert
        awaitingBagLegs.Select(bagLeg => bagLeg.Status).ShouldAllBe(status => status == BagStatus.LoadAuthorized);
        awaitingBagLegs.Select(bagLeg => bagLeg.Version).ShouldBe(awaitingVersions.Select(version => version + 1));
        otherBagLegs.Select(bagLeg => bagLeg.Status).ShouldBe([BagStatus.Sorting, BagStatus.Loading]);
        otherBagLegs.Select(bagLeg => bagLeg.Version).ShouldBe(otherVersions);
    }

    [Fact]
    public void Apply_LoadingOpenedTwice_LeavesFlightAndBagLegsUnchanged()
    {
        // Arrange
        var flightHandling = CreateOpenFlightHandling();
        var bagLeg = CreateBagLeg(BagStatus.AwaitingLoading);
        var previousVersion = bagLeg.Version;
        var duplicateFact = CreateLoadingOpened(FlightId, LoadingOpenedTime.AddHours(1));

        // Act
        var changed = BagLegStateMachine.Apply(flightHandling, duplicateFact, [bagLeg]);

        // Assert
        changed.ShouldBeFalse();
        flightHandling.LoadingOpenedAt.ShouldBe(LoadingOpenedTime);
        bagLeg.Status.ShouldBe(BagStatus.AwaitingLoading);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    [Fact]
    public void Apply_LoadingOpenedForOtherFlight_ThrowsArgumentException()
    {
        // Arrange
        var flightHandling = CreateFlightHandling();

        // Act and assert
        Should.Throw<ArgumentException>(() =>
            BagLegStateMachine.Apply(flightHandling, CreateLoadingOpened(OtherFlightId), []));
        flightHandling.IsLoadingOpen.ShouldBeFalse();
    }

    [Fact]
    public void Apply_LoadingOpenedWithBagLegOfOtherFlight_ThrowsAndLeavesEverythingUnchanged()
    {
        // Arrange: the bag of this flight comes first, so a half-finished loop would already have changed it
        var flightHandling = CreateFlightHandling();
        var bagLeg = CreateBagLeg(BagStatus.AwaitingLoading);
        var otherFlightBagLeg = CreateBagLeg(BagStatus.AwaitingLoading, OtherFlightId);
        var previousVersion = bagLeg.Version;

        // Act and assert
        Should.Throw<ArgumentException>(() =>
            BagLegStateMachine.Apply(flightHandling, CreateLoadingOpened(FlightId), [bagLeg, otherFlightBagLeg]));
        flightHandling.IsLoadingOpen.ShouldBeFalse();
        bagLeg.Status.ShouldBe(BagStatus.AwaitingLoading);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    // Puts the BagLeg directly in the given status, so each test starts from a known point
    private static BagLeg CreateBagLeg(BagStatus status, int flightId = FlightId)
    {
        var bagLeg = new BagLeg(new TagNumber(TagNumberValue), flightId);

        if (status != BagStatus.BagRegistered)
            bagLeg.ChangeStatus(status);

        return bagLeg;
    }

    // A flight for which loading has not opened yet
    private static FlightHandling CreateFlightHandling() => new(FlightId);

    // A flight for which loading opened at LoadingOpenedTime
    private static FlightHandling CreateOpenFlightHandling()
    {
        var flightHandling = new FlightHandling(FlightId);
        flightHandling.SetLoadingOpened(LoadingOpenedTime);

        return flightHandling;
    }

    private static LoadingOpened CreateLoadingOpened(int flightId, DateTimeOffset? occurredAt = null) =>
        new() { FlightId = flightId, OccurredAt = occurredAt ?? LoadingOpenedTime };

    private static BagScanned CreateBagScanned(Checkpoint checkpoint) =>
        new() { TagNumber = TagNumberValue, FlightId = FlightId, Checkpoint = checkpoint };

    private static ScreeningCompleted CreateScreeningCompleted(ScreeningResult result) =>
        new() { TagNumber = TagNumberValue, FlightId = FlightId, Result = result };
}
