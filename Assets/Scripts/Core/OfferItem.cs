using System;

namespace ChugHaul.Core
{
    /// <summary>One line of what a stop offers: a cargo and how many units. Immutable.</summary>
    public sealed class OfferItem
    {
        public CargoType Cargo { get; }
        public int Count { get; }

        public OfferItem(CargoType cargo, int count)
        {
            Cargo = cargo ?? throw new ArgumentNullException(nameof(cargo));
            Count = count;
        }
    }
}
