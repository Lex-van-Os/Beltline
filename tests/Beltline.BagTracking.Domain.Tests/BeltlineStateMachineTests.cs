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
    private static readonly DateTimeOffset ScannedTime = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

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
        var historyEntry = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(checkpoint));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Applied);
        historyEntry.Reason.ShouldBe(OutcomeReason.None);
        bagLeg.Status.ShouldBe(expectedStatus);
        bagLeg.Version.ShouldBe(previousVersion + 1);
    }

    [Theory]
    [InlineData(Checkpoint.Induction, BagStatus.Inducted, OutcomeReason.AlreadyInStatus)] // Same status (duplicate scan)
    [InlineData(Checkpoint.BagDrop, BagStatus.Sorting, OutcomeReason.EarlierThanCurrentStatus)]
    [InlineData(Checkpoint.MakeUp, BagStatus.Loading, OutcomeReason.EarlierThanCurrentStatus)]
    public void Apply_BagScannedWithCheckpointNotLater_LeavesBagLegUnchanged(
        Checkpoint checkpoint, BagStatus currentStatus, OutcomeReason expectedReason)
    {
        // Arrange
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;

        // Act
        var historyEntry = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(checkpoint));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Ignored);
        historyEntry.Reason.ShouldBe(expectedReason);
        bagLeg.Status.ShouldBe(currentStatus);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    [Theory]
    [InlineData(BagStatus.SecurityHold, OutcomeReason.CurrentStatusNotOnHappyPath)]
    [InlineData(BagStatus.Offloading, OutcomeReason.CurrentStatusNotOnHappyPath)]
    [InlineData(BagStatus.Departed, OutcomeReason.CurrentStatusFinal)]
    [InlineData(BagStatus.Offloaded, OutcomeReason.CurrentStatusFinal)]
    [InlineData(BagStatus.Missed, OutcomeReason.CurrentStatusFinal)]
    [InlineData(BagStatus.Missing, OutcomeReason.CurrentStatusFinal)]
    public void Apply_BagScannedWithStatusOutsideHappyPath_LeavesBagLegUnchanged(
        BagStatus currentStatus, OutcomeReason expectedReason)
    {
        // Arrange
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;

        // Act
        var historyEntry = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(Checkpoint.BagDrop));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Refused);
        historyEntry.Reason.ShouldBe(expectedReason);
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
        var historyEntry = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(Checkpoint.SorterEntry));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Applied);
        historyEntry.Reason.ShouldBe(OutcomeReason.None);
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
        var historyEntry = BagLegStateMachine.Apply(bagLeg, fact);

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Applied);
        historyEntry.Reason.ShouldBe(OutcomeReason.None);
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
        var historyEntry = BagLegStateMachine.Apply(bagLeg, CreateScreeningCompleted(ScreeningResult.Cleared));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Applied);
        historyEntry.Reason.ShouldBe(OutcomeReason.None);
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
        var historyEntry = BagLegStateMachine.Apply(bagLeg, CreateScreeningCompleted(ScreeningResult.Failed));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Ignored);
        historyEntry.Reason.ShouldBe(OutcomeReason.NotSupportedYet);
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
        var historyEntry = BagLegStateMachine.Apply(flightHandling, bagLeg, CreateBagScanned(Checkpoint.MakeUp));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Applied);
        historyEntry.Reason.ShouldBe(OutcomeReason.None);
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
        var historyEntry = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(Checkpoint.LoadingPoint));

        // Assert
        historyEntry.Outcome.ShouldBe(ScanOutcome.Refused);
        historyEntry.Reason.ShouldBe(OutcomeReason.NotAuthorizedForLoading);
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
        bagLeg.BagLegHistory.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_LoadingOpened_OpensLoadingAtFactTime()
    {
        // Arrange
        var flightHandling = CreateFlightHandling();

        // Act
        var historyEntries = BagLegStateMachine.Apply(flightHandling, CreateLoadingOpened(FlightId), []);

        // Assert
        historyEntries.ShouldBeEmpty();
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
        var historyEntries = BagLegStateMachine.Apply(flightHandling, duplicateFact, [bagLeg]);

        // Assert
        historyEntries.ShouldBeEmpty();
        flightHandling.LoadingOpenedAt.ShouldBe(LoadingOpenedTime);
        bagLeg.Status.ShouldBe(BagStatus.AwaitingLoading);
        bagLeg.Version.ShouldBe(previousVersion);
        bagLeg.BagLegHistory.ShouldBeEmpty();
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
        bagLeg.BagLegHistory.ShouldBeEmpty();
        otherFlightBagLeg.BagLegHistory.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(BagStatus.ScreeningInProgress, ScanOutcome.Ignored, OutcomeReason.AlreadyInStatus)]
    [InlineData(BagStatus.Sorting, ScanOutcome.Ignored, OutcomeReason.EarlierThanCurrentStatus)]
    [InlineData(BagStatus.Departed, ScanOutcome.Refused, OutcomeReason.CurrentStatusFinal)]
    public void Apply_ScreeningStartedNotApplicable_LeavesBagLegUnchanged(
        BagStatus currentStatus, ScanOutcome expectedOutcome, OutcomeReason expectedReason)
    {
        // Arrange: the forward-only rule is shared, so it also applies to facts other than scans
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;
        var fact = new ScreeningStarted { TagNumber = TagNumberValue, FlightId = FlightId };

        // Act
        var historyEntry = BagLegStateMachine.Apply(bagLeg, fact);

        // Assert
        historyEntry.Outcome.ShouldBe(expectedOutcome);
        historyEntry.Reason.ShouldBe(expectedReason);
        bagLeg.Status.ShouldBe(currentStatus);
        bagLeg.Version.ShouldBe(previousVersion);
    }

    [Fact]
    public void Apply_BagScanned_RecordsHistoryEntryWithFactDetails()
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.Inducted);
        var fact = CreateBagScanned(Checkpoint.SorterEntry);

        // Act
        var historyEntry = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, fact);

        // Assert
        bagLeg.BagLegHistory.ShouldHaveSingleItem().ShouldBeSameAs(historyEntry);
        historyEntry.BagLegId.ShouldBe(bagLeg.Id);
        historyEntry.FactType.ShouldBe(DomainFact.BagScanned);
        historyEntry.OccurredAt.ShouldBe(ScannedTime);
        historyEntry.PreviousStatus.ShouldBe(BagStatus.Inducted);
        historyEntry.CurrentStatus.ShouldBe(BagStatus.Sorting);
        historyEntry.Version.ShouldBe(bagLeg.Version);
        historyEntry.Checkpoint.ShouldBe(Checkpoint.SorterEntry);
        historyEntry.ScreeningResult.ShouldBeNull();
    }

    [Fact]
    public void Apply_ScreeningStarted_RecordsHistoryEntryWithoutCheckpointOrResult()
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.ScreeningPending);
        var fact = new ScreeningStarted { TagNumber = TagNumberValue, FlightId = FlightId };

        // Act
        var historyEntry = BagLegStateMachine.Apply(bagLeg, fact);

        // Assert
        bagLeg.BagLegHistory.ShouldHaveSingleItem().ShouldBeSameAs(historyEntry);
        historyEntry.FactType.ShouldBe(DomainFact.ScreeningStarted);
        historyEntry.Checkpoint.ShouldBeNull();
        historyEntry.ScreeningResult.ShouldBeNull();
    }

    [Theory]
    [InlineData(ScreeningResult.Cleared, BagStatus.ScreeningCleared)]
    [InlineData(ScreeningResult.Failed, BagStatus.ScreeningInProgress)] // Recorded, but does not change the bag yet
    public void Apply_ScreeningCompleted_RecordsHistoryEntryWithScreeningResult(
        ScreeningResult result, BagStatus expectedStatus)
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.ScreeningInProgress);

        // Act
        var historyEntry = BagLegStateMachine.Apply(bagLeg, CreateScreeningCompleted(result));

        // Assert
        bagLeg.BagLegHistory.ShouldHaveSingleItem().ShouldBeSameAs(historyEntry);
        historyEntry.FactType.ShouldBe(DomainFact.ScreeningCompleted);
        historyEntry.PreviousStatus.ShouldBe(BagStatus.ScreeningInProgress);
        historyEntry.CurrentStatus.ShouldBe(expectedStatus);
        historyEntry.ScreeningResult.ShouldBe(result);
        historyEntry.Checkpoint.ShouldBeNull();
    }

    [Theory]
    [InlineData(Checkpoint.Induction, BagStatus.Inducted)] // Ignored
    [InlineData(Checkpoint.LoadingPoint, BagStatus.Sorting)] // Refused
    [InlineData(Checkpoint.BagDrop, BagStatus.Departed)] // Refused
    public void Apply_BagScannedNotApplied_StillRecordsHistoryEntry(Checkpoint checkpoint, BagStatus currentStatus)
    {
        // Arrange
        var bagLeg = CreateBagLeg(currentStatus);
        var previousVersion = bagLeg.Version;

        // Act
        var historyEntry = BagLegStateMachine.Apply(CreateFlightHandling(), bagLeg, CreateBagScanned(checkpoint));

        // Assert
        bagLeg.BagLegHistory.ShouldHaveSingleItem().ShouldBeSameAs(historyEntry);
        historyEntry.PreviousStatus.ShouldBe(currentStatus);
        historyEntry.CurrentStatus.ShouldBe(currentStatus);
        historyEntry.Version.ShouldBe(previousVersion);
    }

    [Fact]
    public void Apply_SeveralFacts_AppendsHistoryEntriesInOrder()
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.BagRegistered);
        var flightHandling = CreateFlightHandling();

        // Act
        BagLegStateMachine.Apply(flightHandling, bagLeg, CreateBagScanned(Checkpoint.BagDrop, ScannedTime));
        BagLegStateMachine.Apply(flightHandling, bagLeg, CreateBagScanned(Checkpoint.Induction, ScannedTime.AddMinutes(5)));
        var lastHistoryEntry = BagLegStateMachine.Apply(flightHandling, bagLeg,
            CreateBagScanned(Checkpoint.ScreeningEntry, ScannedTime.AddMinutes(10)));

        // Assert
        bagLeg.BagLegHistory.Select(historyEntry => historyEntry.Checkpoint)
            .ShouldBe([Checkpoint.BagDrop, Checkpoint.Induction, Checkpoint.ScreeningEntry]);
        bagLeg.BagLegHistory.Select(historyEntry => historyEntry.OccurredAt)
            .ShouldBe([ScannedTime, ScannedTime.AddMinutes(5), ScannedTime.AddMinutes(10)]);
        bagLeg.BagLegHistory.Select(historyEntry => historyEntry.Version).ShouldBe([2, 3, 4]);
        bagLeg.BagLegHistory[^1].ShouldBeSameAs(lastHistoryEntry);
    }

    [Fact]
    public void Apply_SameBagScannedTwice_RecordsBothHistoryEntries()
    {
        // Arrange
        var bagLeg = CreateBagLeg(BagStatus.DroppedOff);
        var flightHandling = CreateFlightHandling();

        // Act
        var firstHistoryEntry = BagLegStateMachine.Apply(flightHandling, bagLeg, CreateBagScanned(Checkpoint.Induction));
        var secondHistoryEntry = BagLegStateMachine.Apply(flightHandling, bagLeg, CreateBagScanned(Checkpoint.Induction));

        // Assert
        bagLeg.BagLegHistory.ShouldBe([firstHistoryEntry, secondHistoryEntry]);
        firstHistoryEntry.Outcome.ShouldBe(ScanOutcome.Applied);
        secondHistoryEntry.Outcome.ShouldBe(ScanOutcome.Ignored);
        secondHistoryEntry.Reason.ShouldBe(OutcomeReason.AlreadyInStatus);
        secondHistoryEntry.Version.ShouldBe(firstHistoryEntry.Version);
    }

    [Fact]
    public void Apply_LoadingOpened_RecordsHistoryEntryOnlyForAuthorizedBags()
    {
        // Arrange
        var awaitingBagLeg = CreateBagLeg(BagStatus.AwaitingLoading);
        var sortingBagLeg = CreateBagLeg(BagStatus.Sorting);

        // Act
        var historyEntries = BagLegStateMachine.Apply(CreateFlightHandling(), CreateLoadingOpened(FlightId),
            [awaitingBagLeg, sortingBagLeg]);

        // Assert
        var historyEntry = historyEntries.ShouldHaveSingleItem();
        awaitingBagLeg.BagLegHistory.ShouldHaveSingleItem().ShouldBeSameAs(historyEntry);
        sortingBagLeg.BagLegHistory.ShouldBeEmpty();
        historyEntry.BagLegId.ShouldBe(awaitingBagLeg.Id);
        historyEntry.FactType.ShouldBe(DomainFact.LoadingOpened);
        historyEntry.OccurredAt.ShouldBe(LoadingOpenedTime);
        historyEntry.PreviousStatus.ShouldBe(BagStatus.AwaitingLoading);
        historyEntry.CurrentStatus.ShouldBe(BagStatus.LoadAuthorized);
        historyEntry.Outcome.ShouldBe(ScanOutcome.Applied);
        historyEntry.Reason.ShouldBe(OutcomeReason.None);
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

    private static BagScanned CreateBagScanned(Checkpoint checkpoint, DateTimeOffset? occurredAt = null) =>
        new() { TagNumber = TagNumberValue, FlightId = FlightId, Checkpoint = checkpoint, OccurredAt = occurredAt ?? ScannedTime };

    private static ScreeningCompleted CreateScreeningCompleted(ScreeningResult result) =>
        new() { TagNumber = TagNumberValue, FlightId = FlightId, Result = result };
}
