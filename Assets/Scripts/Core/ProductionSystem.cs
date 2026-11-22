using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    /// <summary>
    /// Industries (farm, forest, mine) make one unit of their cargo every IndustryInterval
    /// seconds and put it in their depot. When the depot is full they stop, and the timer
    /// does not run while stopped, so a pickup never causes an instant burst.
    /// Cities make nothing for now (mail and passengers come with their own tasks).
    /// </summary>
    public class ProductionSystem
    {
        private sealed class Source
        {
            public readonly Node Node;
            public readonly CargoType Cargo;
            public float Timer;
            public bool Stalled;

            public Source(Node node, CargoType cargo)
            {
                Node = node;
                Cargo = cargo;
            }
        }

        private readonly DepotSystem depot;
        private readonly EconomyConfig config;
        private readonly List<Source> sources = new List<Source>();

        /// <summary>Raised after a unit has been stored in the depot.</summary>
        public event Action<Node, CargoType> Produced;

        /// <summary>Raised once when production stops because the depot is full.</summary>
        public event Action<Node> ProductionStalled;

        /// <summary>Raised once when a stalled industry has room again.</summary>
        public event Action<Node> ProductionResumed;

        public ProductionSystem(Network network, DepotSystem depot, EconomyConfig config)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            this.depot = depot ?? throw new ArgumentNullException(nameof(depot));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            if (config.IndustryInterval <= 0f)
                throw new ArgumentException("IndustryInterval must be positive.", nameof(config));

            foreach (Node node in network.Nodes)
            {
                if (node.Kind == NodeKind.Industry)
                    sources.Add(new Source(node, node.Produces[0]));
            }
        }

        /// <summary>
        /// Advances production by dt seconds. Leftover time is carried over, so a large dt
        /// produces several units. A stalled industry starts a fresh timer on the first
        /// tick that finds room again (that tick's dt counts towards it).
        /// </summary>
        public void Tick(float dt)
        {
            if (dt < 0f) throw new ArgumentOutOfRangeException(nameof(dt));
            if (dt == 0f) return;

            float interval = config.IndustryInterval;

            foreach (Source source in sources)
            {
                if (depot.Count(source.Node, source.Cargo) >= config.DepotCapacity)
                {
                    Stall(source);
                    continue;
                }

                if (source.Stalled)
                {
                    source.Stalled = false;
                    source.Timer = 0f;
                    ProductionResumed?.Invoke(source.Node);
                }

                source.Timer += dt;

                while (source.Timer >= interval)
                {
                    if (!depot.Add(source.Node, source.Cargo))
                    {
                        // The depot filled up during this tick: drop the leftover time.
                        Stall(source);
                        break;
                    }

                    source.Timer -= interval;
                    Produced?.Invoke(source.Node, source.Cargo);
                }
            }
        }

        private void Stall(Source source)
        {
            source.Timer = 0f;

            if (source.Stalled) return;

            source.Stalled = true;
            ProductionStalled?.Invoke(source.Node);
        }
    }
}
