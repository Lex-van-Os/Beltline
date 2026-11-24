using NServiceBus;

namespace Beltline.Contracts.Commands;

// Registers a bag for a flight at check-in, before the bag is physically dropped off.
public record RegisterBag : ICommand
{
    public required string TagNumber { get; init; }

    public required int FlightId { get; init; }
}
