using Beltline.BagTracking.Domain.Enums;

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

    public int Version { get; private set; }

    private readonly List<BagLegHistoryEntry> _bagLegHistory = [];
    public IReadOnlyList<BagLegHistoryEntry> BagLegHistory => _bagLegHistory;

    internal void ChangeStatus(BagStatus bagStatus)
    {
        this.Status = bagStatus;
        this.Version++;
    }

    internal void AddBagLegHistoryEntry(BagLegHistoryEntry bagLegHistoryEntry)
    {
        _bagLegHistory.Add(bagLegHistoryEntry);
    }
}