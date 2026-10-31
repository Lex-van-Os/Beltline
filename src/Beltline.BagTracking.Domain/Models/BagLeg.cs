namespace Beltline.BagTracking.Domain.Models;

public class BagLeg
{
    public BagLeg(TagNumber tagNumber, int flightId)
    {
        ArgumentNullException.ThrowIfNull(tagNumber);
        
        Id = Guid.CreateVersion7();
        TagNumber = tagNumber;
        FlightId = flightId;
        Status = BagStatus.BagRegistered;
        Version = 1;
    }

    public Guid Id { get; }

    public TagNumber TagNumber { get; }

    public int FlightId { get; }

    public BagStatus Status { get; private set; }

    public int Version { get; internal set; }
}