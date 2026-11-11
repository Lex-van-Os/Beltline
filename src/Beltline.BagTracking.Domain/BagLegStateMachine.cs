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
    
    public static bool Apply(BagLeg bagLeg, BagScanned scannedFact)
    {
        BagStatus bagStatus = scannedFact.Checkpoint switch
        {
            Checkpoint.BagDrop => BagStatus.DroppedOff,
            Checkpoint.Induction => BagStatus.Inducted,
            Checkpoint.ScreeningEntry => BagStatus.ScreeningPending,
            Checkpoint.SorterEntry => BagStatus.Sorting,
            Checkpoint.MakeUp => BagStatus.AwaitingLoading,
            Checkpoint.LoadingPoint => BagStatus.Loading,
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

    private static bool TryAdvance(BagLeg bagLeg, BagStatus status)
    {
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