using Beltline.BagTracking.Domain.Enums;

namespace Beltline.BagTracking.Domain.Facts;

public class ScreeningCompleted : FactBase
{
    public ScreeningResult Result { get; set; }
}