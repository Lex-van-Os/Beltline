namespace Beltline.BagTracking.Domain.Enums;

public enum BagStatus
{
    // Happy path (IATA 753: acceptance through loading)
    BagRegistered,
    DroppedOff,
    Inducted,
    ScreeningPending,
    ScreeningInProgress,
    ScreeningCleared,
    Sorting,
    AwaitingLoading,
    LoadAuthorized,
    Loading,
    Loaded,

    // Alternative paths
    SecurityHold,
    Offloading,

    // Final states
    Departed,
    Offloaded,
    Missed,
    Missing
}

public static class BagStatusExtensions
{
    public static bool IsFinal(this BagStatus bagStatus)
    {
        if (bagStatus is BagStatus.Departed or BagStatus.Offloaded or BagStatus.Missed or BagStatus.Missing)
            return true;

        return false;
    }
}
