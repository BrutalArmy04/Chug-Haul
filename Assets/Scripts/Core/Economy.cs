using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    public class Economy
    {
        private readonly GameState gameState;
        private readonly Network network;
        private readonly EconomyConfig config;
        private readonly Dictionary<int, Train> trains = new Dictionary<int, Train>();

        public event Action<Line> LineActivated;
        public event Action<Train> TrainBought;
        public event Action<Train> TrainDeparted;
        public event Action<Train> TrainArrived;

        public Economy(GameState gameState, Network network, EconomyConfig config = null)
        {
            this.gameState = gameState
                ?? throw new ArgumentNullException(nameof(gameState));

            this.network = network
                ?? throw new ArgumentNullException(nameof(network));

            this.config = config ?? new EconomyConfig();

            Cargos = new CargoCatalog(this.config.Cargos);
            Demand = new DemandSystem(this.network, this.config);
            Depot = new DepotSystem(this.network, Cargos, this.config);
        }

        /// <summary>Every cargo kind in the game.</summary>
        public CargoCatalog Cargos { get; }

        /// <summary>What each node has in stock for trains to load.</summary>
        public DepotSystem Depot { get; }

        /// <summary>What a train could load at this node right now.</summary>
        public IReadOnlyList<OfferItem> GetOffer(Node node)
        {
            return Depot.GetOffer(node);
        }

        /// <summary>What cities want and how much room they have left for it.</summary>
        public DemandSystem Demand { get; }

        /// <summary>Advances all time-based systems by dt seconds.</summary>
        public void Tick(float dt)
        {
            Demand.Tick(dt);
        }

        public BuyResult TryBuyLine(int lineId)
        {
            Line line = network.GetLine(lineId);

            if (line == null)
                throw new ArgumentException(
                    $"Unknown line ID: {lineId}",
                    nameof(lineId)
                );

            if (line.IsActive)
                return BuyResult.AlreadyActive;

            int price = (int)Math.Round(line.Cost * config.LinePriceFactor);

            if (gameState.Coins < price)
                return BuyResult.NotEnoughCoins;

            // All checks have passed. Now perform the purchase.
            bool spent = gameState.TrySpend(price);

            if (!spent)
                return BuyResult.NotEnoughCoins;

            line.Activate();

            LineActivated?.Invoke(line);

            return BuyResult.Ok;
        }

        /// <summary>The train on this line, or null if none has been bought.</summary>
        public Train GetTrain(int lineId)
        {
            RequireLine(lineId);
            trains.TryGetValue(lineId, out Train train);
            return train;
        }

        public BuyResult TryBuyTrain(int lineId)
        {
            Line line = RequireLine(lineId);

            if (!line.IsActive)
                return BuyResult.LineNotActive;

            if (trains.ContainsKey(lineId))
                return BuyResult.AlreadyHasTrain;

            // Strict spending for now; the debt task changes this later.
            if (!gameState.TrySpend(config.TrainPrice))
                return BuyResult.NotEnoughCoins;

            var train = new Train(line, config.TrainCapacity, config.LocomotiveMass);
            trains.Add(lineId, train);

            TrainBought?.Invoke(train);

            return BuyResult.Ok;
        }

        /// <summary>
        /// Sends the line's train to a stop on its line, non-stop.
        /// Checks run in order: NoTrain, NotAtStop, NotOnLine, SameStop, OverCapacity, NotInStock.
        /// The load is taken from the depot at the train's current stop. Everything is
        /// checked before anything is taken, so a train is never left half-loaded.
        /// </summary>
        public DispatchResult TryDispatch(int lineId, Node destination, Cargo load = null)
        {
            RequireLine(lineId);

            if (!trains.TryGetValue(lineId, out Train train))
                return DispatchResult.NoTrain;

            if (train.State != TrainState.AtStop)
                return DispatchResult.NotAtStop;

            if (train.Line.IndexOf(destination) < 0)
                return DispatchResult.NotOnLine;

            if (ReferenceEquals(destination, train.CurrentStop))
                return DispatchResult.SameStop;

            Cargo wagons = load ?? Cargo.Empty;

            if (wagons.Count > train.Capacity)
                return DispatchResult.OverCapacity;

            // One item per wagon; the depot sums repeats of the same cargo.
            var items = new List<OfferItem>(wagons.Count);
            foreach (CargoType wagon in wagons)
                items.Add(new OfferItem(wagon, 1));

            if (items.Count > 0 && !Depot.CanTake(train.CurrentStop, items))
                return DispatchResult.NotInStock;

            // Everything is checked. Update the train first, then take from the depot,
            // so DepotChanged listeners see a consistent state.
            train.Cargo = wagons;
            train.Destination = destination;
            train.State = TrainState.Travelling;

            if (items.Count > 0)
                Depot.TryTakeMany(train.CurrentStop, items);

            TrainDeparted?.Invoke(train);

            return DispatchResult.Ok;
        }

        /// <summary>
        /// Called by the View when the train reaches its destination.
        /// Throws if the line has no train or it is not Travelling (a View bug).
        /// </summary>
        public void CompleteTrip(int lineId)
        {
            RequireLine(lineId);

            if (!trains.TryGetValue(lineId, out Train train))
                throw new InvalidOperationException($"Line {lineId} has no train.");

            if (train.State != TrainState.Travelling)
                throw new InvalidOperationException(
                    $"Train on line {lineId} is not travelling.");

            // Unload everything. The payout task turns this into money.
            train.Cargo = new Cargo();

            train.CurrentStop = train.Destination;
            train.Destination = null;
            train.State = TrainState.AtStop;

            TrainArrived?.Invoke(train);
        }

        // Called by the View when a train reaches the end of a line. Returns the coins earned.
        // Remove together with the Length x coinRate reward once the payout task is merged.
        public int OnTrainArrived(int lineId)
        {
            Line line = network.GetLine(lineId);
            if (line == null) throw new ArgumentException($"Unknown line {lineId}");  // adjust if GetLine throws itself
            if (!line.IsActive) return 0;

            int reward = (int)Math.Round(line.Length * config.LegacyCoinRate);
            gameState.Earn(reward);
            return reward;
        }

        private Line RequireLine(int lineId)
        {
            Line line = network.GetLine(lineId);

            if (line == null)
                throw new ArgumentException(
                    $"Unknown line ID: {lineId}",
                    nameof(lineId)
                );

            return line;
        }
    }
}
