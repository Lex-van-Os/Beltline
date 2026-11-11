using Beltline.BagTracking.Domain.Enums;

namespace Beltline.BagTracking.Domain.Facts;

public class BagScanned : FactBase
{
    public Checkpoint Checkpoint { get; set; }
}