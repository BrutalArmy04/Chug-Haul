using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    /// <summary>
    /// Every node has a depot holding units of cargo, capped at DepotCapacity per cargo.
    /// What a train can load at a stop is exactly what that stop's depot holds.
    /// Depots hold anything dropped there, not only what the node produces.
    /// </summary>
    public class DepotSystem
    {
        private readonly EconomyConfig config;
        private readonly CargoCatalog catalog;
        private readonly HashSet<int> nodeIds = new HashSet<int>();
        private readonly Dictionary<(int nodeId, int cargoId), int> counts =
            new Dictionary<(int nodeId, int cargoId), int>();

        /// <summary>Raised when a count changes: (node, cargo, new count).</summary>
        public event Action<Node, CargoType, int> DepotChanged;

        public DepotSystem(Network network, CargoCatalog catalog, EconomyConfig config)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            if (config.DepotCapacity < 0)
                throw new ArgumentException("DepotCapacity cannot be negative.", nameof(config));

            foreach (Node node in network.Nodes)
                nodeIds.Add(node.Id);
        }

        public int Count(Node node, CargoType cargo)
        {
            RequireNode(node);
            RequireCargo(cargo);
            return Raw(node, cargo);
        }

        /// <summary>Stores one unit. Returns false, changing nothing, when that cargo is full.</summary>
        public bool Add(Node node, CargoType cargo)
        {
            RequireNode(node);
            RequireCargo(cargo);

            int count = Raw(node, cargo);
            if (count >= config.DepotCapacity) return false;

            counts[(node.Id, cargo.Id)] = count + 1;
            DepotChanged?.Invoke(node, cargo, count + 1);
            return true;
        }

        /// <summary>Removes n units. Returns false, changing nothing, if fewer are stored.</summary>
        public bool Take(Node node, CargoType cargo, int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            return TryTakeMany(node, new[] { new OfferItem(cargo, n) });
        }

        /// <summary>One OfferItem per cargo with a count above 0, in catalog order.</summary>
        public IReadOnlyList<OfferItem> GetOffer(Node node)
        {
            RequireNode(node);

            var offer = new List<OfferItem>();
            foreach (CargoType cargo in catalog.Cargos)
            {
                int count = Raw(node, cargo);
                if (count > 0)
                    offer.Add(new OfferItem(cargo, count));
            }
            return offer;
        }

        /// <summary>True if every item (repeats are summed) is fully in stock. Changes nothing.</summary>
        public bool CanTake(Node node, IReadOnlyList<OfferItem> items)
        {
            return TryPlan(node, items, out _);
        }

        /// <summary>
        /// Takes everything or nothing. All counts are updated first, then DepotChanged
        /// is raised once per cargo type, so listeners never see a half-taken state.
        /// </summary>
        public bool TryTakeMany(Node node, IReadOnlyList<OfferItem> items)
        {
            if (!TryPlan(node, items, out List<OfferItem> plan))
                return false;

            var newCounts = new List<int>(plan.Count);
            foreach (OfferItem item in plan)
            {
                int remaining = Raw(node, item.Cargo) - item.Count;
                counts[(node.Id, item.Cargo.Id)] = remaining;
                newCounts.Add(remaining);
            }

            for (int i = 0; i < plan.Count; i++)
                DepotChanged?.Invoke(node, plan[i].Cargo, newCounts[i]);

            return true;
        }

        private bool TryPlan(Node node, IReadOnlyList<OfferItem> items, out List<OfferItem> plan)
        {
            RequireNode(node);
            if (items == null) throw new ArgumentNullException(nameof(items));

            plan = new List<OfferItem>();
            var position = new Dictionary<int, int>();

            foreach (OfferItem item in items)
            {
                if (item == null) throw new ArgumentException("Items cannot contain null.", nameof(items));
                if (item.Count <= 0) throw new ArgumentOutOfRangeException(nameof(items));

                if (catalog.GetById(item.Cargo.Id) == null)
                    return false; // unknown cargo can't be in stock

                if (position.TryGetValue(item.Cargo.Id, out int index))
                    plan[index] = new OfferItem(plan[index].Cargo, plan[index].Count + item.Count);
                else
                {
                    position.Add(item.Cargo.Id, plan.Count);
                    plan.Add(item);
                }
            }

            foreach (OfferItem item in plan)
            {
                if (Raw(node, item.Cargo) < item.Count)
                    return false;
            }

            return true;
        }

        private int Raw(Node node, CargoType cargo)
        {
            counts.TryGetValue((node.Id, cargo.Id), out int count);
            return count;
        }

        private void RequireNode(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (!nodeIds.Contains(node.Id))
                throw new ArgumentException($"Node {node.Id} is not in the network.", nameof(node));
        }

        private void RequireCargo(CargoType cargo)
        {
            if (cargo == null) throw new ArgumentNullException(nameof(cargo));
            if (catalog.GetById(cargo.Id) == null)
                throw new ArgumentException($"Cargo {cargo.Id} is not in the catalog.", nameof(cargo));
        }
    }
}
