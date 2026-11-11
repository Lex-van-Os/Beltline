using Beltline.BagTracking.Domain.Enums;
using Beltline.BagTracking.Domain.Facts;
using Beltline.BagTracking.Domain.Models;

namespace Beltline.BagTracking.Domain;

public static class BagLegStateMachine
{
    private static bool _isLoadingOpen = false;
    
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
    
    public static bool Apply(BagLeg bagLeg, BagScanned scannedFact)
    {
        // TODO: Implement logic -> loading point scan only goes to loading if status is load authorized. Previous status means unchanged
        
        BagStatus bagStatus = scannedFact.Checkpoint switch
        {
            Checkpoint.BagDrop => BagStatus.DroppedOff,
            Checkpoint.Induction => BagStatus.Inducted,
            Checkpoint.ScreeningEntry => BagStatus.ScreeningPending,
            Checkpoint.SorterEntry => BagStatus.Sorting,
            Checkpoint.MakeUp when !_isLoadingOpen => BagStatus.AwaitingLoading,
            Checkpoint.MakeUp when _isLoadingOpen => BagStatus.LoadAuthorized,
            Checkpoint.LoadingPoint when bagLeg.Status == BagStatus.LoadAuthorized => BagStatus.Loading,
            Checkpoint.LoadingPoint when bagLeg.Status != BagStatus.LoadAuthorized => bagLeg.Status, // Status does not change
            _ => throw new ArgumentOutOfRangeException(nameof(scannedFact))
        };

        return TryAdvance(bagLeg, bagStatus);
    }
    
    public static bool Apply(BagLeg bagLeg, ScreeningStarted screeningStartedFact)
    {
        const BagStatus bagStatus = BagStatus.ScreeningInProgress;
        
        return TryAdvance(bagLeg, bagStatus);
    }
    
    public static bool Apply(BagLeg bagLeg, ScreeningCompleted screeningCompletedFact)
    {
        // Currently return nothing. A later version will include logic for the SecurityHold functionality.
        return screeningCompletedFact.Result == ScreeningResult.Cleared && TryAdvance(bagLeg, BagStatus.ScreeningCleared);
    }

    public static bool Apply(FlightHandling flightHandling, LoadingOpened loadingOpenedFact)
    {
        flightHandling.LoadingOpenedAt = loadingOpenedFact.OccurredAt;
        _isLoadingOpen  = true;

        return true;
    }

    private static bool TryAdvance(BagLeg bagLeg, BagStatus status)
    {
        // TODO: When bag is in awaiting loading and IsLoadingOpen -> Automatically go to load authorized -> Else unchanged (enforce here?)
        
        // Case: Incoming status is not later than current status; do nothing.
        if (HappyPath.IndexOf(bagLeg.Status) >= HappyPath.IndexOf(status))
            return false;
        // Case: Current bag status is not in list of statuses to change; do nothing.
        else if (HappyPath.IndexOf(bagLeg.Status) == -1)
            return false;
            
        bagLeg.ChangeStatus(status);

        return true;
    }
}