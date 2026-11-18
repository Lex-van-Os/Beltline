using Beltline.BagTracking.Domain.Enums;
using Beltline.BagTracking.Domain.Facts;
using Beltline.BagTracking.Domain.Models;

namespace Beltline.BagTracking.Domain;

public static class BagLegStateMachine
{
    private static readonly BagStatus[] HappyPath =
    [
        BagStatus.BagRegistered,
        BagStatus.DroppedOff,
        BagStatus.Inducted,
        BagStatus.ScreeningPending,
        BagStatus.ScreeningInProgress,
        BagStatus.ScreeningCleared,
        BagStatus.Sorting,
        BagStatus.AwaitingLoading,
        BagStatus.LoadAuthorized,
        BagStatus.Loading,
        BagStatus.Loaded
    ];
    
    public static BagLegHistoryEntry Apply(FlightHandling flightHandling, BagLeg bagLeg, BagScanned scannedFact)
    {
        if (flightHandling.FlightId != bagLeg.FlightId)
            throw new ArgumentException("Flight information flight id does not correspond to the bag flight id");
        
        BagStatus targetStatus = scannedFact.Checkpoint switch
        {
            Checkpoint.BagDrop => BagStatus.DroppedOff,
            Checkpoint.Induction => BagStatus.Inducted,
            Checkpoint.ScreeningEntry => BagStatus.ScreeningPending,
            Checkpoint.SorterEntry => BagStatus.Sorting,
            Checkpoint.MakeUp when !flightHandling.IsLoadingOpen => BagStatus.AwaitingLoading,
            Checkpoint.MakeUp when flightHandling.IsLoadingOpen => BagStatus.LoadAuthorized,
            Checkpoint.LoadingPoint => BagStatus.Loading,
            _ => throw new ArgumentOutOfRangeException(nameof(scannedFact))
        };

        var previousStatus = bagLeg.Status;
        var result = TryAdvance(bagLeg, targetStatus);
        return RecordHistoryEntry(bagLeg, previousStatus, scannedFact, DomainFact.BagScanned, result.Reason, checkpoint: scannedFact.Checkpoint);
    }

    public static BagLegHistoryEntry Apply(BagLeg bagLeg, ScreeningStarted screeningStartedFact)
    {
        var previousStatus = bagLeg.Status;
        var result = TryAdvance(bagLeg, BagStatus.ScreeningInProgress);
        return RecordHistoryEntry(bagLeg, previousStatus, screeningStartedFact, DomainFact.ScreeningStarted, result.Reason);
    }

    public static BagLegHistoryEntry Apply(BagLeg bagLeg, ScreeningCompleted screeningCompletedFact)
    {
        // Security hold is a later issue: a failed screening is recorded, but does not change the bag yet.
        if (screeningCompletedFact.Result == ScreeningResult.Failed)
        {
            return RecordHistoryEntry(bagLeg, bagLeg.Status, screeningCompletedFact, DomainFact.ScreeningCompleted,
                OutcomeReason.NotSupportedYet, screeningResult: screeningCompletedFact.Result);
        }

        var previousStatus = bagLeg.Status;
        var result = TryAdvance(bagLeg, BagStatus.ScreeningCleared);
        return RecordHistoryEntry(bagLeg, previousStatus, screeningCompletedFact, DomainFact.ScreeningCompleted,
            result.Reason, screeningResult: screeningCompletedFact.Result);
    }

    public static IReadOnlyCollection<BagLegHistoryEntry> Apply(FlightHandling flightHandling, LoadingOpened loadingOpenedFact, IReadOnlyCollection<BagLeg> flightBags)
    {
        var modifiedEntries = new List<BagLegHistoryEntry>();
        
        // Case: Prevent opening loading for a flight that cannot be properly validated.
        if (loadingOpenedFact.FlightId != flightHandling.FlightId)
            throw new ArgumentException("Flight information flight id does not correspond to the domain fact flight id");

        // Case: Prevent duplicate message arrivals overwriting previously set opened loading time.
        if (flightHandling.IsLoadingOpen)
            return modifiedEntries;
        
        if (flightBags.Any(flightBag => flightBag.FlightId != flightHandling.FlightId))
            throw new ArgumentException("Flight information flight id does not correspond to the bag flight id");

        foreach (var flightBag in flightBags.Where(flightBag => flightBag.Status == BagStatus.AwaitingLoading))
        {
            var previousStatus = flightBag.Status;
            flightBag.ChangeStatus(BagStatus.LoadAuthorized);
            modifiedEntries.Add(RecordHistoryEntry(flightBag, previousStatus, loadingOpenedFact, DomainFact.LoadingOpened, OutcomeReason.None));
        }
        
        flightHandling.SetLoadingOpened(loadingOpenedFact.OccurredAt);

        return modifiedEntries;
    }

    private static ScanOutcomeResult TryAdvance(BagLeg bagLeg, BagStatus status)
    {
        // Case: The current status of the bag is the final status.
        if (bagLeg.Status.IsFinal())
            return new ScanOutcomeResult(OutcomeReason.CurrentStatusFinal);
        
        // Case: Incoming status is earlier than current status.
        if (HappyPath.IndexOf(bagLeg.Status) > HappyPath.IndexOf(status))
            return new ScanOutcomeResult(OutcomeReason.EarlierThanCurrentStatus);
        
        // Case: Incoming status is the same as the current status.
        if (bagLeg.Status == status)
            return new ScanOutcomeResult(OutcomeReason.AlreadyInStatus);

        // Case: Current bag status is not in list of statuses to change.
        if (HappyPath.IndexOf(bagLeg.Status) == -1)
            return new ScanOutcomeResult(OutcomeReason.CurrentStatusNotOnHappyPath);

        // Case: A bag can only reach Loading from LoadAuthorized (IATA 753: no unauthorized bag on board).
        if (status == BagStatus.Loading && bagLeg.Status != BagStatus.LoadAuthorized)
            return new ScanOutcomeResult(OutcomeReason.NotAuthorizedForLoading);

        bagLeg.ChangeStatus(status);

        return new ScanOutcomeResult(OutcomeReason.None);
    }

    private static BagLegHistoryEntry RecordHistoryEntry(
        BagLeg bagLeg, BagStatus previousStatus, FactBase fact, DomainFact factType, OutcomeReason reason,
        Checkpoint? checkpoint = null, ScreeningResult? screeningResult = null)
    {
        var historyEntry = new BagLegHistoryEntry(bagLeg.Id, factType, fact.OccurredAt, previousStatus,
            bagLeg.Status, bagLeg.Version, reason.ToOutcome(), reason, checkpoint, screeningResult);

        bagLeg.AddBagLegHistoryEntry(historyEntry);

        return historyEntry;
    }
}