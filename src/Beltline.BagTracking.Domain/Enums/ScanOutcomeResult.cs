namespace Beltline.BagTracking.Domain.Enums;

public class ScanOutcomeResult
{
    public ScanOutcomeResult(OutcomeReason reason)
    {
        Reason = reason;
        ScanOutcome = reason.ToOutcome();
    }
    
    public ScanOutcome ScanOutcome { get; }
    public OutcomeReason Reason { get; }
}