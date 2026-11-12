using Beltline.BagTracking.Domain.Enums;

namespace Beltline.BagTracking.Domain.Facts;

public class BagScanned : FactBase
{
    public required string TagNumber { get; set; }
    public Checkpoint Checkpoint { get; set; }
}