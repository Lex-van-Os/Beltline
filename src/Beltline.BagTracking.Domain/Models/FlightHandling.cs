namespace Beltline.BagTracking.Domain.Models;

public class FlightHandling
{
    public FlightHandling(int flightId)
    {
        FlightId = flightId;
    }
    
    public int FlightId { get; }
    public bool IsLoadingOpen => LoadingOpenedAt is not null;
    public DateTimeOffset? LoadingOpenedAt { get; private set; }

    internal void SetLoadingOpened(DateTimeOffset loadingOpenedAt)
    {
        this.LoadingOpenedAt = loadingOpenedAt;
    }
}