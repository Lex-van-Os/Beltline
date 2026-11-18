namespace Beltline.BagTracking.Domain.Enums;

// The type of fact that was reported to the domain, as stored in a BagLegHistoryEntry
public enum DomainFact
{
    // Bag-level facts
    BagScanned,
    ScreeningStarted,
    ScreeningCompleted,

    // Flight-level facts
    LoadingOpened
}
