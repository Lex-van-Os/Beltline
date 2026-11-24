namespace Beltline.Contracts;

// The contracts have their own copy of the checkpoints, so they don't depend on the domain.
// The BagTracking host translates it to the domain's Checkpoint.
// Explicit values: they are sent over the wire, so reordering must not change their meaning.
public enum Checkpoint
{
    BagDrop = 0,
    Induction = 1,
    ScreeningEntry = 2,
    SorterEntry = 3,
    MakeUp = 4,
    LoadingPoint = 5
}
