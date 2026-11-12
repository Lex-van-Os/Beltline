namespace Beltline.BagTracking.Domain.Facts;

public class ScreeningStarted : FactBase
{
    public required string TagNumber { get; set; }
}