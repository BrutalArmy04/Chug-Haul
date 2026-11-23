using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    /// <summary>
    /// A city's factory takes 1 unit from its input demand stock and puts 1 unit of its output
    /// into the city's depot every FactoryInterval seconds. Taking input frees a demand slot,
    /// so the city buys more raw material. If there is no input, or the output depot is full,
    /// the factory stalls: its timer is reset and does not run, so there is never a burst
    /// afterwards (same rule as industries).
    /// </summary>
    public class FactorySystem
    {
        private sealed class Plant
        {
            public readonly Node Node;
            public readonly Factory Factory;
            public float Timer;
            public bool Stalled;

            public Plant(Node node)
            {
                Node = node;
                Factory = node.Factory;
            }
        }

        private readonly DemandSystem demand;
        private readonly DepotSystem depot;
        private readonly EconomyConfig config;
        private readonly List<Plant> plants = new List<Plant>();

        /// <summary>Raised after one unit of output has been stored in the depot.</summary>
        public event Action<Node> FactoryProduced;

        /// <summary>Raised once when a factory stops (no input, or output depot full).</summary>
        public event Action<Node> FactoryStalled;

        /// <summary>Raised once when a stalled factory has input and room again.</summary>
        public event Action<Node> FactoryResumed;

        public FactorySystem(
            Network network, DemandSystem demand, DepotSystem depot, EconomyConfig config)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            this.demand = demand ?? throw new ArgumentNullException(nameof(demand));
            this.depot = depot ?? throw new ArgumentNullException(nameof(depot));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            if (config.FactoryInterval <= 0f)
                throw new ArgumentException("FactoryInterval must be positive.", nameof(config));

            foreach (Node node in network.Nodes)
            {
                if (node.Factory != null)
                    plants.Add(new Plant(node));
            }
        }

        /// <summary>
        /// Advances all factories by dt seconds. Leftover time is carried over, so a large dt
        /// can make several units. A stalled factory starts a fresh timer on the first tick
        /// that finds input and room again (that tick's dt counts towards it).
        /// </summary>
        public void Tick(float dt)
        {
            if (dt < 0f) throw new ArgumentOutOfRangeException(nameof(dt));
            if (dt == 0f) return;

            float interval = config.FactoryInterval;

            foreach (Plant plant in plants)
            {
                if (!CanWork(plant))
                {
                    Stall(plant);
                    continue;
                }

                if (plant.Stalled)
                {
                    plant.Stalled = false;
                    plant.Timer = 0f;
                    FactoryResumed?.Invoke(plant.Node);
                }

                plant.Timer += dt;

                while (plant.Timer >= interval)
                {
                    // Input or room can run out in the middle of a big tick.
                    if (!CanWork(plant))
                    {
                        Stall(plant);
                        break;
                    }

                    demand.TryConsume(plant.Node, plant.Factory.Input);
                    depot.Add(plant.Node, plant.Factory.Output);

                    plant.Timer -= interval;
                    FactoryProduced?.Invoke(plant.Node);
                }
            }
        }

        private bool CanWork(Plant plant)
        {
            bool hasInput =
                demand.FreeSlots(plant.Node, plant.Factory.Input) < config.DemandSlots;
            bool hasRoom =
                depot.Count(plant.Node, plant.Factory.Output) < config.DepotCapacity;

            return hasInput && hasRoom;
        }

        private void Stall(Plant plant)
        {
            plant.Timer = 0f;

            if (plant.Stalled) return;

            plant.Stalled = true;
            FactoryStalled?.Invoke(plant.Node);
        }
    }
}
