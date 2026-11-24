using NServiceBus;

namespace Beltline.Contracts.Commands;

// Registers a flight, so that bags can be registered for it.
public record RegisterFlight : ICommand
{
    public required int FlightId { get; init; }
}
