using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    /// <summary>
    /// Cities want certain cargo. Each (city, cargo) has a stock with DemandSlots places.
    /// A free slot pays well; a full stock accepts nothing. Stocks drain over time,
    /// one unit per CityConsumeInterval, so demand comes back.
    /// Passengers have no stock (they are paid at their destination).
    /// </summary>
    public class DemandSystem
    {
        private sealed class Stock
        {
            public readonly Node Node;
            public readonly CargoType Cargo;
            public int Stored;
            public float Timer;

            public Stock(Node node, CargoType cargo)
            {
                Node = node;
                Cargo = cargo;
            }
        }

        private readonly EconomyConfig config;
        private readonly Dictionary<(int nodeId, int cargoId), Stock> stocksByKey =
            new Dictionary<(int nodeId, int cargoId), Stock>();
        private readonly List<Stock> stocks = new List<Stock>();

        /// <summary>Raised on every change: (node, cargo, free slots after the change).</summary>
        public event Action<Node, CargoType, int> DemandChanged;

        /// <summary>
        /// Cargo that a factory consumes itself; cities skip consuming it.
        /// Set by the factory task. Defaults to "never".
        /// </summary>
        public Func<Node, CargoType, bool> IsFactoryInput { get; set; }

        public DemandSystem(Network network, EconomyConfig config)
        {
            if (network == null) throw new ArgumentNullException(nameof(network));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            if (config.DemandSlots < 0)
                throw new ArgumentException("DemandSlots cannot be negative.", nameof(config));
            if (config.CityConsumeInterval <= 0f)
                throw new ArgumentException("CityConsumeInterval must be positive.", nameof(config));
            if (config.PriceBySlots == null || config.PriceBySlots.Length != config.DemandSlots + 1)
                throw new ArgumentException(
                    "PriceBySlots needs DemandSlots + 1 entries (index = free slots).",
                    nameof(config));

            foreach (Node node in network.Nodes)
            {
                foreach (CargoType cargo in node.Demands)
                {
                    if (cargo.IsPassengers) continue;

                    var stock = new Stock(node, cargo);
                    stocksByKey.Add((node.Id, cargo.Id), stock);
                    stocks.Add(stock);
                }
            }
        }

        /// <summary>True if the node has a demand stock for this cargo (never for passengers).</summary>
        public bool Wants(Node node, CargoType cargo)
        {
            return FindStock(node, cargo) != null;
        }

        /// <summary>
        /// Free slots for a wanted cargo. Throws if the node has no stock for it
        /// (not demanded, or passengers), since that is a caller bug.
        /// </summary>
        public int FreeSlots(Node node, CargoType cargo)
        {
            return config.DemandSlots - RequireStock(node, cargo).Stored;
        }

        /// <summary>
        /// Stores one unit if a slot is free and returns the free slots from before
        /// the delivery (the payout task prices by this). Returns 0 and stores nothing
        /// if the stock is full or the node has no stock for this cargo.
        /// </summary>
        public int TryAccept(Node node, CargoType cargo)
        {
            Stock stock = FindStock(node, cargo);
            if (stock == null) return 0;

            int freeBefore = config.DemandSlots - stock.Stored;
            if (freeBefore <= 0) return 0;

            if (stock.Stored == 0)
                stock.Timer = 0f;

            stock.Stored++;

            DemandChanged?.Invoke(node, cargo, config.DemandSlots - stock.Stored);

            return freeBefore;
        }

        /// <summary>
        /// Advances consumption by dt seconds. A stock's timer only runs while it holds
        /// something, and leftover time is kept, so large dt steps consume correctly.
        /// </summary>
        public void Tick(float dt)
        {
            if (dt < 0f) throw new ArgumentOutOfRangeException(nameof(dt));
            if (dt == 0f) return;

            float interval = config.CityConsumeInterval;

            foreach (Stock stock in stocks)
            {
                if (stock.Stored == 0) continue;
                if (IsFactoryInput != null && IsFactoryInput(stock.Node, stock.Cargo)) continue;

                stock.Timer += dt;

                while (stock.Timer >= interval && stock.Stored > 0)
                {
                    stock.Timer -= interval;
                    stock.Stored--;

                    DemandChanged?.Invoke(
                        stock.Node, stock.Cargo, config.DemandSlots - stock.Stored);
                }

                if (stock.Stored == 0)
                    stock.Timer = 0f;
            }
        }

        private Stock FindStock(Node node, CargoType cargo)
        {
            if (node == null || cargo == null) return null;
            stocksByKey.TryGetValue((node.Id, cargo.Id), out Stock stock);
            return stock;
        }

        private Stock RequireStock(Node node, CargoType cargo)
        {
            Stock stock = FindStock(node, cargo);
            if (stock == null)
                throw new ArgumentException(
                    "This node has no demand stock for that cargo " +
                    "(not demanded, or passengers).");
            return stock;
        }
    }
}
