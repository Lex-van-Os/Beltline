using Beltline.BagTracking.Domain.Enums;

namespace Beltline.BagTracking.Domain.Models;

public class BagLegHistoryEntry
{
    public BagLegHistoryEntry(Guid bagLegId, DomainFact factType, DateTimeOffset occurredAt, BagStatus previousStatus, BagStatus currentStatus, int version,  ScanOutcome outcome, OutcomeReason reason, Checkpoint? checkpoint = null, ScreeningResult? screeningResult = null)
    {
        BagLegId = bagLegId;
        FactType = factType;
        OccurredAt = occurredAt;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Version = version;
        Outcome = outcome;
        Reason = reason;
        Checkpoint = checkpoint;
        ScreeningResult = screeningResult;
    }
    
    public Guid BagLegId { get; }
    public DomainFact FactType { get; } 
    public DateTimeOffset OccurredAt { get; }
    public BagStatus PreviousStatus { get; }
    public BagStatus CurrentStatus { get; }
    public ScanOutcome Outcome { get; }
    public OutcomeReason Reason { get; }
    public Checkpoint? Checkpoint { get; }
    public ScreeningResult? ScreeningResult { get; }
    public int Version { get; }
}