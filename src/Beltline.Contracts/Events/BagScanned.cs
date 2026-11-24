using NServiceBus;

namespace Beltline.Contracts.Events;

// A scanner in the simulated world saw a bag at a checkpoint.
public record BagScanned : IEvent
{
    public required string TagNumber { get; init; }

    public required int FlightId { get; init; }

    public required Checkpoint Checkpoint { get; init; }

    // When the scan happened in simulated time.
    public required DateTimeOffset OccurredAt { get; init; }
}
