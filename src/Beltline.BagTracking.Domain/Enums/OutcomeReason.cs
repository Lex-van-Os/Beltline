namespace Beltline.BagTracking.Domain.Enums;

public enum OutcomeReason
{
    None = 0, // The fact was applied

    // Ignored
    EarlierThanCurrentStatus = 1,
    AlreadyInStatus = 2,
    NotSupportedYet = 3,

    // Refused
    CurrentStatusFinal = 4,
    CurrentStatusNotOnHappyPath = 5,
    NotAuthorizedForLoading = 6
}

public static class ScanOutcomeReasonExtensions
{
    public static ScanOutcome ToOutcome(this OutcomeReason reason)
    {
        return reason switch
        {
            OutcomeReason.None => ScanOutcome.Applied,
            OutcomeReason.EarlierThanCurrentStatus or OutcomeReason.AlreadyInStatus
                or OutcomeReason.NotSupportedYet => ScanOutcome.Ignored,
            OutcomeReason.CurrentStatusFinal or OutcomeReason.CurrentStatusNotOnHappyPath
                or OutcomeReason.NotAuthorizedForLoading => ScanOutcome.Refused,
            _ => throw  new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }
}