using Beltline.BagTracking.Domain.Enums;

namespace Beltline.BagTracking.Domain.Models;

public class BagLegHistoryEntry
{
    public BagLegHistoryEntry(Guid bagLegId, DomainFact factType, DateTimeOffset occuredAt, BagStatus previousStatus, BagStatus currentStatus, int version,  ScanOutcome outcome, OutcomeReason reason, Checkpoint? checkpoint = null, ScreeningResult? screeningResult = null)
    {
        BagLegId = bagLegId; // BagLeg id
        FactType = factType; // Apply method specific
        OccuredAt = occuredAt; // Fact occured at
        PreviousStatus = previousStatus; // input BagLeg status
        CurrentStatus = currentStatus; // output BagLeg status
        Version = version; // output BagLeg version
        Outcome = outcome; // Outcome of Apply method
        Reason = reason; // Apply method specific
        Checkpoint = checkpoint; // Only for BagScanned facts
        ScreeningResult = screeningResult; // Only for ScreeningCompleted facts
    }
    
    public Guid BagLegId { get; }
    public DomainFact FactType { get; } 
    public DateTimeOffset OccuredAt { get; }
    public BagStatus PreviousStatus { get; }
    public BagStatus CurrentStatus { get; }
    public ScanOutcome Outcome { get; }
    public OutcomeReason Reason { get; }
    public Checkpoint? Checkpoint { get; }
    public ScreeningResult? ScreeningResult { get; }
    public int Version { get; }
}