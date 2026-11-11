namespace Beltline.BagTracking.Domain.Models;

public class FlightHandling
{
    public int FlightId { get; set; }
    public DateTimeOffset? LoadingOpenedAt { get; set; }
}