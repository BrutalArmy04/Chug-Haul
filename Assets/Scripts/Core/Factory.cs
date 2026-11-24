using System;

namespace ChugHaul.Core
{
    /// <summary>
    /// What a city's factory does: turns one input cargo into one output cargo.
    /// The input is a normal entry in the city's Demands, so deliveries and payment use the
    /// existing demand and payout code. The speed comes from EconomyConfig.FactoryInterval.
    /// Immutable.
    /// </summary>
    public sealed class Factory
    {
        public CargoType Input { get; }
        public CargoType Output { get; }

        public Factory(CargoType input, CargoType output)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));

            if (input.Id == output.Id)
                throw new ArgumentException("A factory must turn one cargo into a different one.");

            // Passengers have no demand stock and no depot goods, so a factory can't use them.
            if (input.IsPassengers || output.IsPassengers)
                throw new ArgumentException("A factory cannot use passengers.");
        }
    }
}
