using System.Collections.Generic;

namespace ChugHaul.Core
{
    /// <summary>What a train's load earns on arrival, wagon by wagon. Immutable.</summary>
    public sealed class DeliveryResult
    {
        /// <summary>One entry per wagon, in load order.</summary>
        public IReadOnlyList<WagonDelivery> Wagons { get; }

        /// <summary>Sum of all wagon coins.</summary>
        public int Total { get; }

        internal DeliveryResult(List<WagonDelivery> wagons)
        {
            Wagons = wagons.AsReadOnly();

            int total = 0;
            foreach (WagonDelivery wagon in wagons)
                total += wagon.Coins;
            Total = total;
        }
    }
}
