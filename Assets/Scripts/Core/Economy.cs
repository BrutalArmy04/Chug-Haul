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
        public event Action<Train, DeliveryResult> TrainArrived;

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
            Production = new ProductionSystem(this.network, Depot, this.config);
            Factories = new FactorySystem(this.network, Demand, Depot, this.config);
        }

        /// <summary>Every cargo kind in the game.</summary>
        public CargoCatalog Cargos { get; }

        /// <summary>What each node has in stock for trains to load.</summary>
        public DepotSystem Depot { get; }

        /// <summary>Industries making cargo into their depots.</summary>
        public ProductionSystem Production { get; }

        /// <summary>City factories turning raw material into products.</summary>
        public FactorySystem Factories { get; }

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
            Production.Tick(dt);
            Factories.Tick(dt);
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
        public DeliveryResult CompleteTrip(int lineId)
        {
            RequireLine(lineId);

            if (!trains.TryGetValue(lineId, out Train train))
                throw new InvalidOperationException($"Line {lineId} has no train.");

            if (train.State != TrainState.Travelling)
                throw new InvalidOperationException(
                    $"Train on line {lineId} is not travelling.");

            Node destination = train.Destination;
            DeliveryResult result = ComputeDelivery(train, destination, train.Cargo);

            // Move the train first, so every listener below sees it at its destination.
            train.Cargo = Cargo.Empty;
            train.CurrentStop = destination;
            train.Destination = null;
            train.State = TrainState.AtStop;

            ApplyDelivery(destination, result);

            if (result.Total > 0)
                gameState.Earn(result.Total);

            TrainArrived?.Invoke(train, result);

            return result;
        }

        /// <summary>
        /// What delivering this load at this destination would earn, without changing anything.
        /// Returns exactly what CompleteTrip would produce for the same state.
        /// </summary>
        public DeliveryResult PreviewDelivery(Train train, Node destination, Cargo load)
        {
            if (train == null) throw new ArgumentNullException(nameof(train));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            return ComputeDelivery(train, destination, load ?? Cargo.Empty);
        }

        /// <summary>
        /// The single place that decides every wagon's outcome. Pure: it reads demand and
        /// depot state, tracks the effect of earlier wagons in local counters, and changes
        /// nothing. Both CompleteTrip and PreviewDelivery use it.
        /// </summary>
        private DeliveryResult ComputeDelivery(Train train, Node destination, Cargo load)
        {
            var wagons = new List<WagonDelivery>(load.Count);
            var accepted = new Dictionary<int, int>(); // cargo id -> wagons sold so far
            var stored = new Dictionary<int, int>();   // cargo id -> wagons stored so far

            foreach (CargoType cargo in load)
            {
                if (cargo.IsPassengers)
                {
                    // The passengers task fills this in (paid only at their own destination).
                    wagons.Add(new WagonDelivery(cargo, 0, WagonOutcome.Lost, 0));
                    continue;
                }

                accepted.TryGetValue(cargo.Id, out int soldSoFar);
                stored.TryGetValue(cargo.Id, out int storedSoFar);

                if (Demand.Wants(destination, cargo))
                {
                    int free = Demand.FreeSlots(destination, cargo) - soldSoFar;
                    if (free > 0)
                    {
                        float price = ApplyMultipliers(
                            cargo.BaseValue * config.PriceBySlots[free],
                            train, destination, cargo);

                        wagons.Add(new WagonDelivery(
                            cargo, (int)Math.Round(price), WagonOutcome.Sold, free));
                        accepted[cargo.Id] = soldSoFar + 1;
                        continue;
                    }
                }

                // Not wanted, or no free slots: unpaid. Depot if there is room, else lost.
                if (Depot.Count(destination, cargo) + storedSoFar < config.DepotCapacity)
                {
                    wagons.Add(new WagonDelivery(cargo, 0, WagonOutcome.StoredInDepot, 0));
                    stored[cargo.Id] = storedSoFar + 1;
                }
                else
                {
                    wagons.Add(new WagonDelivery(cargo, 0, WagonOutcome.Lost, 0));
                }
            }

            return new DeliveryResult(wagons);
        }

        /// <summary>
        /// The one place to add price multipliers (variety bonus, first-delivery bonus, ...).
        /// Must stay pure: it is also called by PreviewDelivery. Returns the price unchanged for now.
        /// </summary>
        private float ApplyMultipliers(float price, Train train, Node destination, CargoType cargo)
        {
            return price;
        }

        /// <summary>Makes a computed delivery real: fills demand slots and depots.</summary>
        private void ApplyDelivery(Node destination, DeliveryResult result)
        {
            foreach (WagonDelivery wagon in result.Wagons)
            {
                switch (wagon.Outcome)
                {
                    case WagonOutcome.Sold:
                    {
                        int freeBefore = Demand.TryAccept(destination, wagon.Cargo);
                        if (freeBefore != wagon.FreeSlotsBefore)
                            throw new InvalidOperationException(
                                "Delivery computation and demand state disagree.");
                        break;
                    }
                    case WagonOutcome.StoredInDepot:
                    {
                        if (!Depot.Add(destination, wagon.Cargo))
                            throw new InvalidOperationException(
                                "Delivery computation and depot state disagree.");
                        break;
                    }
                }
            }
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
