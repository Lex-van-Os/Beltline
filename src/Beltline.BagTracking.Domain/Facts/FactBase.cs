namespace Beltline.BagTracking.Domain.Facts;

public class FactBase
{
    public required string TagNumber { get; set; }
    public int FlightId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}