using Beltline.BagTracking.Domain.Enums;

namespace Beltline.BagTracking.Domain.Facts;

public class ScreeningCompleted : FactBase
{
    public required string TagNumber { get; set; }
    public ScreeningResult Result { get; set; }
}