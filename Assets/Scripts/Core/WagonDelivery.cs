using System;

namespace ChugHaul.Core
{
    /// <summary>The outcome for a single wagon in a delivery. Immutable.</summary>
    public sealed class WagonDelivery
    {
        public CargoType Cargo { get; }

        /// <summary>Coins earned for this wagon. 0 unless Outcome is Sold.</summary>
        public int Coins { get; }

        public WagonOutcome Outcome { get; }

        /// <summary>
        /// Free demand slots the destination had before this wagon (what set its price).
        /// 0 unless Outcome is Sold.
        /// </summary>
        public int FreeSlotsBefore { get; }

        public bool StoredInDepot => Outcome == WagonOutcome.StoredInDepot;

        internal WagonDelivery(CargoType cargo, int coins, WagonOutcome outcome, int freeSlotsBefore)
        {
            Cargo = cargo ?? throw new ArgumentNullException(nameof(cargo));
            Coins = coins;
            Outcome = outcome;
            FreeSlotsBefore = freeSlotsBefore;
        }
    }
}
