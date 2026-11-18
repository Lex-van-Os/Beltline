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
        var previousBagLegStatus = bagLeg.Status;
        
        if (flightHandling.FlightId != bagLeg.FlightId)
            throw new ArgumentException("Flight information flight id does not correspond to the bag flight id");
        
        BagStatus bagStatus = scannedFact.Checkpoint switch
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

        var result = TryAdvance(bagLeg, bagStatus);

        var scanOutcomeReason = result.Reason;
        var scanOutcome = scanOutcomeReason.ToOutcome();
        var historyEntry = new BagLegHistoryEntry(bagLeg.Id, DomainFact.BagScanned, scannedFact.OccurredAt, previousBagLegStatus, bagLeg.Status, bagLeg.Version, scanOutcome, scanOutcomeReason, checkpoint: scannedFact.Checkpoint);

        bagLeg.AddBagLegHistoryEntry(historyEntry);

        return historyEntry;
    }

    public static BagLegHistoryEntry Apply(BagLeg bagLeg, ScreeningStarted screeningStartedFact)
    {
        var previousBagLegStatus = bagLeg.Status;
        const BagStatus bagStatus = BagStatus.ScreeningInProgress;
        
        var result = TryAdvance(bagLeg, bagStatus);
        
        var scanOutcomeReason = result.Reason;
        var scanOutcome = scanOutcomeReason.ToOutcome();
        var historyEntry = new BagLegHistoryEntry(bagLeg.Id, DomainFact.ScreeningStarted, screeningStartedFact.OccurredAt, previousBagLegStatus, bagLeg.Status, bagLeg.Version, scanOutcome, scanOutcomeReason);

        bagLeg.AddBagLegHistoryEntry(historyEntry);
        
        return historyEntry;
    }
    
    public static BagLegHistoryEntry Apply(BagLeg bagLeg, ScreeningCompleted screeningCompletedFact)
    {
        var previousBagLegStatus = bagLeg.Status;
        
        // Currently return nothing. A later version will include logic for the SecurityHold functionality.
        if (screeningCompletedFact.Result == ScreeningResult.Failed)
        {
            var failedHistoryEntry =  new BagLegHistoryEntry(bagLeg.Id, DomainFact.ScreeningCompleted, screeningCompletedFact.OccurredAt, previousBagLegStatus, bagLeg.Status, bagLeg.Version, ScanOutcome.Ignored, OutcomeReason.NotSupportedYet, screeningResult: screeningCompletedFact.Result);
            
            bagLeg.AddBagLegHistoryEntry(failedHistoryEntry);
            
            return failedHistoryEntry;
        }
        
        var result = TryAdvance(bagLeg, BagStatus.ScreeningCleared);
        
        var scanOutcomeReason = result.Reason;
        var scanOutcome = scanOutcomeReason.ToOutcome();
        var historyEntry = new BagLegHistoryEntry(bagLeg.Id, DomainFact.ScreeningCompleted, screeningCompletedFact.OccurredAt, previousBagLegStatus, bagLeg.Status, bagLeg.Version, scanOutcome, scanOutcomeReason, screeningResult: screeningCompletedFact.Result);

        bagLeg.AddBagLegHistoryEntry(historyEntry);
        
        return historyEntry;
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
            flightBag.ChangeStatus(BagStatus.LoadAuthorized);
            
            var historyEntry = new BagLegHistoryEntry(flightBag.Id, DomainFact.LoadingOpened, loadingOpenedFact.OccurredAt, BagStatus.AwaitingLoading, BagStatus.LoadAuthorized, flightBag.Version, ScanOutcome.Applied, OutcomeReason.None);
            
            flightBag.AddBagLegHistoryEntry(historyEntry);
            modifiedEntries.Add(historyEntry);
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
}