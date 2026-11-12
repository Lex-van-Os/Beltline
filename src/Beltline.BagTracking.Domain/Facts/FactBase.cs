namespace Beltline.BagTracking.Domain.Facts;

public class FactBase
{
    public int FlightId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}