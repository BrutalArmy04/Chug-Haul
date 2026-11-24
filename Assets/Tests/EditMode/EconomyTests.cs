using NUnit.Framework;
using ChugHaul.Core;
using System;
using System.Collections.Generic;

namespace ChugHaul.Core.Tests
{
    public class EconomyTests
    {
        private GameState gameState;
        private Node nodeA;
        private Node nodeB;
        private Line line;
        private Network network;
        private Economy economy;
        private EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            gameState = new GameState(100);

            nodeA = new Node(1, "City A", 0f, 0f);
            nodeB = new Node(2, "City B", 3f, 4f);

            line = new Line(1, new[] { nodeA, nodeB }, 50);

            network = new Network(
                new[] { nodeA, nodeB },
                new[] { line }
            );

            config = new EconomyConfig();
            economy = new Economy(gameState, network, config);
        }

        // ---------- Lines ----------

        [Test]
        public void TryBuyLine_Affordable_ActivatesAndCharges()
        {
            BuyResult result = economy.TryBuyLine(1);

            Assert.That(result, Is.EqualTo(BuyResult.Ok));
            Assert.That(gameState.Coins, Is.EqualTo(50));
            Assert.That(line.IsActive, Is.True);
        }

        [Test]
        public void TryBuyLine_NotEnoughCoins_ChangesNothing()
        {
            gameState = new GameState(30);
            economy = new Economy(gameState, network);

            BuyResult result = economy.TryBuyLine(1);

            Assert.That(result, Is.EqualTo(BuyResult.NotEnoughCoins));
            Assert.That(gameState.Coins, Is.EqualTo(30));
            Assert.That(line.IsActive, Is.False);
        }

        [Test]
        public void TryBuyLine_AlreadyActive_DoesNotChargeAgain()
        {
            economy.TryBuyLine(1);

            BuyResult secondResult = economy.TryBuyLine(1);

            Assert.That(secondResult, Is.EqualTo(BuyResult.AlreadyActive));
            Assert.That(gameState.Coins, Is.EqualTo(50));
        }

        [Test]
        public void TryBuyLine_UnknownId_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => economy.TryBuyLine(-1)
            );
        }

        [Test]
        public void TryBuyLine_DoesNotCreateTrain()
        {
            economy.TryBuyLine(1);

            Assert.That(economy.GetTrain(1), Is.Null);
        }

        [Test]
        public void LineActivated_FiresOnceWithActivatedLine()
        {
            int eventCount = 0;
            Line receivedLine = null;

            economy.LineActivated += activatedLine =>
            {
                eventCount++;
                receivedLine = activatedLine;
            };

            economy.TryBuyLine(1);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(receivedLine, Is.SameAs(line));
        }

        // ---------- Buying trains ----------

        [Test]
        public void TryBuyTrain_InactiveLine_ReturnsLineNotActive()
        {
            BuyResult result = economy.TryBuyTrain(1);

            Assert.That(result, Is.EqualTo(BuyResult.LineNotActive));
            Assert.That(gameState.Coins, Is.EqualTo(100));
            Assert.That(economy.GetTrain(1), Is.Null);
        }

        [Test]
        public void TryBuyTrain_Twice_ReturnsAlreadyHasTrainAndChargesOnce()
        {
            economy.TryBuyLine(1);
            economy.TryBuyTrain(1);
            int coinsAfterFirst = gameState.Coins;

            BuyResult second = economy.TryBuyTrain(1);

            Assert.That(second, Is.EqualTo(BuyResult.AlreadyHasTrain));
            Assert.That(gameState.Coins, Is.EqualTo(coinsAfterFirst));
        }

        [Test]
        public void TryBuyTrain_Affordable_CreatesTrainAtFirstStopAndCharges()
        {
            economy.TryBuyLine(1);

            BuyResult result = economy.TryBuyTrain(1);

            Train train = economy.GetTrain(1);
            Assert.That(result, Is.EqualTo(BuyResult.Ok));
            Assert.That(gameState.Coins, Is.EqualTo(50 - config.TrainPrice));
            Assert.That(train, Is.Not.Null);
            Assert.That(train.Line, Is.SameAs(line));
            Assert.That(train.State, Is.EqualTo(TrainState.AtStop));
            Assert.That(train.CurrentStop, Is.SameAs(nodeA));
            Assert.That(train.Destination, Is.Null);
            Assert.That(train.Capacity, Is.EqualTo(config.TrainCapacity));
            Assert.That(train.Cargo, Is.Not.Null);
        }

        [Test]
        public void TryBuyTrain_NotEnoughCoins_ChangesNothing()
        {
            gameState = new GameState(50);
            economy = new Economy(gameState, network);
            economy.TryBuyLine(1); // leaves 0 coins

            BuyResult result = economy.TryBuyTrain(1);

            Assert.That(result, Is.EqualTo(BuyResult.NotEnoughCoins));
            Assert.That(gameState.Coins, Is.EqualTo(0));
            Assert.That(economy.GetTrain(1), Is.Null);
        }

        [Test]
        public void TryBuyTrain_UnknownId_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => economy.TryBuyTrain(-1)
            );
        }

        [Test]
        public void TrainBought_FiresOnceWithTrain()
        {
            economy.TryBuyLine(1);
            int eventCount = 0;
            Train received = null;
            economy.TrainBought += t => { eventCount++; received = t; };

            economy.TryBuyTrain(1);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(received, Is.SameAs(economy.GetTrain(1)));
        }

        // ---------- Dispatch ----------

        private void BuyLineAndTrain()
        {
            economy.TryBuyLine(1);
            economy.TryBuyTrain(1);
        }

        [Test]
        public void TryDispatch_NoTrain_ReturnsNoTrain()
        {
            economy.TryBuyLine(1);

            Assert.That(economy.TryDispatch(1, nodeB), Is.EqualTo(DispatchResult.NoTrain));
        }

        [Test]
        public void TryDispatch_NodeNotOnLine_ReturnsNotOnLine()
        {
            BuyLineAndTrain();
            Node stranger = new Node(99, "Elsewhere", 50f, 50f);

            Assert.That(economy.TryDispatch(1, stranger), Is.EqualTo(DispatchResult.NotOnLine));
            Assert.That(economy.GetTrain(1).State, Is.EqualTo(TrainState.AtStop));
        }

        [Test]
        public void TryDispatch_OwnStop_ReturnsSameStop()
        {
            BuyLineAndTrain();

            Assert.That(economy.TryDispatch(1, nodeA), Is.EqualTo(DispatchResult.SameStop));
            Assert.That(economy.GetTrain(1).State, Is.EqualTo(TrainState.AtStop));
        }

        [Test]
        public void TryDispatch_WhileTravelling_ReturnsNotAtStop()
        {
            BuyLineAndTrain();
            economy.TryDispatch(1, nodeB);

            Assert.That(economy.TryDispatch(1, nodeA), Is.EqualTo(DispatchResult.NotAtStop));
        }

        [Test]
        public void TryDispatch_NotAtStopIsCheckedBeforeNotOnLine()
        {
            BuyLineAndTrain();
            economy.TryDispatch(1, nodeB);
            Node stranger = new Node(99, "Elsewhere", 50f, 50f);

            Assert.That(economy.TryDispatch(1, stranger), Is.EqualTo(DispatchResult.NotAtStop));
        }

        [Test]
        public void TryDispatch_Valid_SetsTravellingAndDestinationAndFiresEvent()
        {
            BuyLineAndTrain();
            int eventCount = 0;
            Train received = null;
            economy.TrainDeparted += t => { eventCount++; received = t; };

            DispatchResult result = economy.TryDispatch(1, nodeB);

            Train train = economy.GetTrain(1);
            Assert.That(result, Is.EqualTo(DispatchResult.Ok));
            Assert.That(train.State, Is.EqualTo(TrainState.Travelling));
            Assert.That(train.Destination, Is.SameAs(nodeB));
            Assert.That(train.CurrentStop, Is.SameAs(nodeA));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(received, Is.SameAs(train));
        }

        [Test]
        public void TryDispatch_UnknownLine_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => economy.TryDispatch(-1, nodeB)
            );
        }

        [Test]
        public void TryDispatch_CanSkipMiddleStops()
        {
            Node a = new Node(1, "A", 0f, 0f);
            Node b = new Node(2, "B", 5f, 0f);
            Node c = new Node(3, "C", 5f, 3f);
            Line longLine = new Line(1, new[] { a, b, c }, 10);
            var net = new Network(new[] { a, b, c }, new[] { longLine });
            var eco = new Economy(new GameState(100), net);
            eco.TryBuyLine(1);
            eco.TryBuyTrain(1);

            Assert.That(eco.TryDispatch(1, c), Is.EqualTo(DispatchResult.Ok));
            Assert.That(eco.GetTrain(1).Destination, Is.SameAs(c));
        }

        // ---------- Completing trips ----------

        [Test]
        public void CompleteTrip_PutsTrainAtDestinationAndFiresArrivedOnce()
        {
            BuyLineAndTrain();
            economy.TryDispatch(1, nodeB);
            int eventCount = 0;
            Train received = null;
            economy.TrainArrived += (t, r) => { eventCount++; received = t; };

            economy.CompleteTrip(1);

            Train train = economy.GetTrain(1);
            Assert.That(train.State, Is.EqualTo(TrainState.AtStop));
            Assert.That(train.CurrentStop, Is.SameAs(nodeB));
            Assert.That(train.Destination, Is.Null);
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(received, Is.SameAs(train));
        }

        [Test]
        public void CompleteTrip_ThenDispatchBack_Works()
        {
            BuyLineAndTrain();
            economy.TryDispatch(1, nodeB);
            economy.CompleteTrip(1);

            Assert.That(economy.TryDispatch(1, nodeA), Is.EqualTo(DispatchResult.Ok));
            Assert.That(economy.TryDispatch(1, nodeB), Is.EqualTo(DispatchResult.NotAtStop));
        }

        [Test]
        public void CompleteTrip_NotTravelling_Throws()
        {
            BuyLineAndTrain();

            Assert.Throws<InvalidOperationException>(() => economy.CompleteTrip(1));
        }

        [Test]
        public void CompleteTrip_NoTrain_Throws()
        {
            economy.TryBuyLine(1);

            Assert.Throws<InvalidOperationException>(() => economy.CompleteTrip(1));
        }

        // ---------- Loading ----------

        private CargoType Kind(string name) => economy.Cargos.GetByName(name);

        private static Cargo Load(params CargoType[] wagons) => new Cargo(wagons);

        private void Stock(Node node, CargoType cargo, int amount)
        {
            for (int i = 0; i < amount; i++)
                Assert.That(economy.Depot.Add(node, cargo), Is.True);
        }

        [Test]
        public void GetOffer_ShowsWhatTheNodesDepotHolds()
        {
            CargoType food = Kind("Food");
            Stock(nodeA, food, 3);

            var offer = economy.GetOffer(nodeA);

            Assert.That(offer.Count, Is.EqualTo(1));
            Assert.That(offer[0].Cargo, Is.SameAs(food));
            Assert.That(offer[0].Count, Is.EqualTo(3));
            Assert.That(economy.GetOffer(nodeB), Is.Empty);
        }

        [Test]
        public void TryDispatch_OverCapacity_TakesNothing()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            Stock(nodeA, food, 5);
            Assert.That(config.TrainCapacity, Is.EqualTo(4));

            DispatchResult result = economy.TryDispatch(
                1, nodeB, Load(food, food, food, food, food));

            Assert.That(result, Is.EqualTo(DispatchResult.OverCapacity));
            Assert.That(economy.Depot.Count(nodeA, food), Is.EqualTo(5));
            Assert.That(economy.GetTrain(1).State, Is.EqualTo(TrainState.AtStop));
            Assert.That(economy.GetTrain(1).Cargo.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryDispatch_NotInStock_TakesNothing()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            Stock(nodeA, food, 3);

            DispatchResult result = economy.TryDispatch(1, nodeB, Load(food, food, food, food));

            Assert.That(result, Is.EqualTo(DispatchResult.NotInStock));
            Assert.That(economy.Depot.Count(nodeA, food), Is.EqualTo(3));
            Assert.That(economy.GetTrain(1).State, Is.EqualTo(TrainState.AtStop));
            Assert.That(economy.GetTrain(1).Cargo.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryDispatch_OverCapacityIsCheckedBeforeStock()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            Stock(nodeA, food, 3); // short on stock AND over capacity

            DispatchResult result = economy.TryDispatch(
                1, nodeB, Load(food, food, food, food, food));

            Assert.That(result, Is.EqualTo(DispatchResult.OverCapacity));
        }

        [Test]
        public void TryDispatch_MixedLoadMissingOneCargo_TakesNothing()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            CargoType ore = Kind("Ore");
            Stock(nodeA, food, 2); // no ore

            DispatchResult result = economy.TryDispatch(1, nodeB, Load(food, ore));

            Assert.That(result, Is.EqualTo(DispatchResult.NotInStock));
            Assert.That(economy.Depot.Count(nodeA, food), Is.EqualTo(2));
        }

        [Test]
        public void TryDispatch_LoadsOnlyFromTheTrainsCurrentStop()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            Stock(nodeB, food, 3); // train is at A

            DispatchResult result = economy.TryDispatch(1, nodeB, Load(food));

            Assert.That(result, Is.EqualTo(DispatchResult.NotInStock));
            Assert.That(economy.Depot.Count(nodeB, food), Is.EqualTo(3));
        }

        [Test]
        public void TryDispatch_FailedLoad_DoesNotRaiseDeparted()
        {
            BuyLineAndTrain();
            int departed = 0;
            economy.TrainDeparted += t => departed++;

            economy.TryDispatch(1, nodeB, Load(Kind("Food")));

            Assert.That(departed, Is.EqualTo(0));
        }

        [Test]
        public void TryDispatch_ValidLoad_ReducesDepotSetsCargoAndFiresDepotChanged()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            Stock(nodeA, food, 3);

            var seen = new List<(CargoType cargo, int count, TrainState state, int cargoCount)>();
            economy.Depot.DepotChanged += (n, c, count) =>
            {
                Train t = economy.GetTrain(1);
                seen.Add((c, count, t.State, t.Cargo.Count));
            };

            DispatchResult result = economy.TryDispatch(1, nodeB, Load(food, food));

            Assert.That(result, Is.EqualTo(DispatchResult.Ok));
            Assert.That(economy.Depot.Count(nodeA, food), Is.EqualTo(1));
            Assert.That(economy.GetTrain(1).Cargo.Count, Is.EqualTo(2));
            Assert.That(economy.GetTrain(1).Cargo[0], Is.SameAs(food));
            Assert.That(seen.Count, Is.EqualTo(1));
            Assert.That(seen[0].cargo, Is.SameAs(food));
            Assert.That(seen[0].count, Is.EqualTo(1));
            // The train was already updated when the event fired.
            Assert.That(seen[0].state, Is.EqualTo(TrainState.Travelling));
            Assert.That(seen[0].cargoCount, Is.EqualTo(2));
        }

        [Test]
        public void TryDispatch_MixedLoad_FiresOneEventPerCargoType()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            CargoType ore = Kind("Ore");
            Stock(nodeA, food, 3);
            Stock(nodeA, ore, 1);

            var seen = new List<(CargoType, int)>();
            economy.Depot.DepotChanged += (n, c, count) => seen.Add((c, count));

            economy.TryDispatch(1, nodeB, Load(food, ore, food));

            Assert.That(seen.Count, Is.EqualTo(2));
            Assert.That(seen[0].Item1, Is.SameAs(food));
            Assert.That(seen[0].Item2, Is.EqualTo(1));
            Assert.That(seen[1].Item1, Is.SameAs(ore));
            Assert.That(seen[1].Item2, Is.EqualTo(0));
        }

        [Test]
        public void TryDispatch_EmptyLoad_StillWorks()
        {
            BuyLineAndTrain();

            Assert.That(economy.TryDispatch(1, nodeB, new Cargo()), Is.EqualTo(DispatchResult.Ok));
            Assert.That(economy.GetTrain(1).Cargo.Count, Is.EqualTo(0));
        }

        // ---------- Mass ----------

        private int MassOnTrip(Node from, Node to, params CargoType[] wagons)
        {
            foreach (CargoType wagon in wagons)
                Stock(from, wagon, 1);

            Assert.That(economy.TryDispatch(1, to, Load(wagons)), Is.EqualTo(DispatchResult.Ok));
            int mass = economy.GetTrain(1).TotalMass;
            economy.CompleteTrip(1);
            return mass;
        }

        [Test]
        public void TotalMass_EmptyTrain_IsLocomotiveMass()
        {
            BuyLineAndTrain();

            Assert.That(economy.GetTrain(1).TotalMass, Is.EqualTo(config.LocomotiveMass));
        }

        [Test]
        public void TotalMass_GrowsWithEachWagon()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");

            int one = MassOnTrip(nodeA, nodeB, food);
            int two = MassOnTrip(nodeB, nodeA, food, food);
            int three = MassOnTrip(nodeA, nodeB, food, food, food);

            Assert.That(one, Is.EqualTo(config.LocomotiveMass + food.WagonMass));
            Assert.That(two, Is.EqualTo(config.LocomotiveMass + 2 * food.WagonMass));
            Assert.That(three, Is.EqualTo(config.LocomotiveMass + 3 * food.WagonMass));
            Assert.That(one, Is.LessThan(two));
            Assert.That(two, Is.LessThan(three));
        }

        [Test]
        public void TotalMass_DependsOnTheCargo()
        {
            BuyLineAndTrain();
            CargoType food = Kind("Food");
            CargoType ore = Kind("Ore");
            Assert.That(ore.WagonMass, Is.Not.EqualTo(food.WagonMass));

            int foodMass = MassOnTrip(nodeA, nodeB, food, food);
            int oreMass = MassOnTrip(nodeB, nodeA, ore, ore);

            Assert.That(foodMass, Is.EqualTo(config.LocomotiveMass + 2 * food.WagonMass));
            Assert.That(oreMass, Is.EqualTo(config.LocomotiveMass + 2 * ore.WagonMass));
        }

        [Test]
        public void TotalMass_FollowsTheConfiguredLocomotiveMass()
        {
            var custom = new EconomyConfig { LocomotiveMass = 20 };
            var eco = new Economy(new GameState(200), network, custom);
            eco.TryBuyLine(1);
            eco.TryBuyTrain(1);

            Assert.That(eco.GetTrain(1).TotalMass, Is.EqualTo(20));
        }

        // ---------- Delivery ----------

        private sealed class Scene
        {
            public Economy Eco;
            public GameState State;
            public EconomyConfig Config;
            public Node Farm;
            public Node Town;
            public CargoType Food;
            public CargoType Ore;
            public CargoType Passengers;
        }

        // Farm produces food, ore and passengers. Town wants food and passengers,
        // and does not want ore. One active line with a train, at the farm.
        private static Scene BuildScene(EconomyConfig cfg = null)
        {
            cfg = cfg ?? new EconomyConfig();
            var catalog = new CargoCatalog(cfg.Cargos);
            CargoType food = catalog.GetByName("Food");
            CargoType ore = catalog.GetByName("Ore");
            CargoType mail = catalog.GetByName("Mail");
            CargoType passengers = catalog.GetByName("Passengers");

            var farm = new Node(1, "Farm", 0f, 0f,
                produces: new[] { food, ore, passengers },
                demands: new[] { mail });
            var town = new Node(2, "Town", 3f, 4f,
                produces: new[] { mail },
                demands: new[] { food, passengers });

            var deliveryLine = new Line(1, new[] { farm, town }, 50);
            var net = new Network(new[] { farm, town }, new[] { deliveryLine });
            var state = new GameState(500);
            var eco = new Economy(state, net, cfg);

            Assert.That(eco.TryBuyLine(1), Is.EqualTo(BuyResult.Ok));
            Assert.That(eco.TryBuyTrain(1), Is.EqualTo(BuyResult.Ok));

            return new Scene
            {
                Eco = eco,
                State = state,
                Config = cfg,
                Farm = farm,
                Town = town,
                Food = food,
                Ore = ore,
                Passengers = passengers
            };
        }

        // Stocks the farm's depot with the wagons and sends the train to the town.
        private static void Dispatch(Scene s, params CargoType[] wagons)
        {
            foreach (CargoType wagon in wagons)
                Assert.That(s.Eco.Depot.Add(s.Farm, wagon), Is.True);

            Assert.That(
                s.Eco.TryDispatch(1, s.Town, new Cargo(wagons)),
                Is.EqualTo(DispatchResult.Ok));
        }

        private static void AssertSameDelivery(DeliveryResult expected, DeliveryResult actual)
        {
            Assert.That(actual.Total, Is.EqualTo(expected.Total));
            Assert.That(actual.Wagons.Count, Is.EqualTo(expected.Wagons.Count));

            for (int i = 0; i < expected.Wagons.Count; i++)
            {
                Assert.That(actual.Wagons[i].Cargo, Is.SameAs(expected.Wagons[i].Cargo), $"wagon {i}");
                Assert.That(actual.Wagons[i].Coins, Is.EqualTo(expected.Wagons[i].Coins), $"wagon {i}");
                Assert.That(actual.Wagons[i].Outcome, Is.EqualTo(expected.Wagons[i].Outcome), $"wagon {i}");
                Assert.That(actual.Wagons[i].FreeSlotsBefore,
                    Is.EqualTo(expected.Wagons[i].FreeSlotsBefore), $"wagon {i}");
            }
        }

        [Test]
        public void Delivery_ThreeFoodIntoFreeTown_PaysDescendingSteps()
        {
            Scene s = BuildScene();
            Dispatch(s, s.Food, s.Food, s.Food);
            int coinsBefore = s.State.Coins;

            DeliveryResult result = s.Eco.CompleteTrip(1);

            // Free slots before each wagon are 5, 4, 3: 150%, 125%, 100% of Food's base value 10.
            // Default rounding (half to even): 12.5 becomes 12.
            Assert.That(result.Wagons.Count, Is.EqualTo(3));
            Assert.That(result.Wagons[0].Coins, Is.EqualTo(15));
            Assert.That(result.Wagons[1].Coins, Is.EqualTo(12));
            Assert.That(result.Wagons[2].Coins, Is.EqualTo(10));
            Assert.That(result.Wagons[0].FreeSlotsBefore, Is.EqualTo(5));
            Assert.That(result.Wagons[1].FreeSlotsBefore, Is.EqualTo(4));
            Assert.That(result.Wagons[2].FreeSlotsBefore, Is.EqualTo(3));
            Assert.That(result.Wagons[0].Outcome, Is.EqualTo(WagonOutcome.Sold));
            Assert.That(result.Total, Is.EqualTo(37));
            Assert.That(s.State.Coins, Is.EqualTo(coinsBefore + 37));
            Assert.That(s.Eco.Demand.FreeSlots(s.Town, s.Food), Is.EqualTo(2));
        }

        [Test]
        public void Delivery_WagonIntoFullTown_IsUnpaidAndStoredInDepot()
        {
            Scene s = BuildScene();
            for (int i = 0; i < s.Config.DemandSlots; i++)
                s.Eco.Demand.TryAccept(s.Town, s.Food);
            Dispatch(s, s.Food);
            int coinsBefore = s.State.Coins;

            DeliveryResult result = s.Eco.CompleteTrip(1);

            WagonDelivery wagon = result.Wagons[0];
            Assert.That(wagon.Coins, Is.EqualTo(0));
            Assert.That(wagon.StoredInDepot, Is.True);
            Assert.That(wagon.Outcome, Is.EqualTo(WagonOutcome.StoredInDepot));
            Assert.That(result.Total, Is.EqualTo(0));
            Assert.That(s.State.Coins, Is.EqualTo(coinsBefore));
            Assert.That(s.Eco.Depot.Count(s.Town, s.Food), Is.EqualTo(1));
            Assert.That(s.Eco.Demand.FreeSlots(s.Town, s.Food), Is.EqualTo(0));
        }

        [Test]
        public void Delivery_CargoTheNodeDoesNotWant_IsUnpaidAndLandsInDepot()
        {
            Scene s = BuildScene();
            Dispatch(s, s.Ore);

            DeliveryResult result = s.Eco.CompleteTrip(1);

            Assert.That(result.Wagons[0].Coins, Is.EqualTo(0));
            Assert.That(result.Wagons[0].StoredInDepot, Is.True);
            Assert.That(result.Total, Is.EqualTo(0));
            Assert.That(s.Eco.Depot.Count(s.Town, s.Ore), Is.EqualTo(1));
            // The cargo changed trains: it is now on offer at the town.
            Assert.That(s.Eco.GetOffer(s.Town)[0].Cargo, Is.SameAs(s.Ore));
        }

        [Test]
        public void Delivery_DepotFull_WagonIsLostWithoutException()
        {
            Scene s = BuildScene();
            for (int i = 0; i < s.Config.DepotCapacity; i++)
                s.Eco.Depot.Add(s.Town, s.Ore);
            Dispatch(s, s.Ore);
            int coinsBefore = s.State.Coins;

            DeliveryResult result = null;
            Assert.DoesNotThrow(() => result = s.Eco.CompleteTrip(1));

            Assert.That(result.Wagons[0].Outcome, Is.EqualTo(WagonOutcome.Lost));
            Assert.That(result.Wagons[0].StoredInDepot, Is.False);
            Assert.That(result.Wagons[0].Coins, Is.EqualTo(0));
            Assert.That(result.Total, Is.EqualTo(0));
            Assert.That(s.State.Coins, Is.EqualTo(coinsBefore));
            Assert.That(s.Eco.Depot.Count(s.Town, s.Ore), Is.EqualTo(s.Config.DepotCapacity));
        }

        [Test]
        public void Delivery_EmptyTrain_TotalIsZero()
        {
            Scene s = BuildScene();
            Assert.That(s.Eco.TryDispatch(1, s.Town), Is.EqualTo(DispatchResult.Ok));
            int coinsBefore = s.State.Coins;

            DeliveryResult result = s.Eco.CompleteTrip(1);

            Assert.That(result.Total, Is.EqualTo(0));
            Assert.That(result.Wagons.Count, Is.EqualTo(0));
            Assert.That(s.State.Coins, Is.EqualTo(coinsBefore));
        }

        [Test]
        public void Delivery_TownFillsUpMidLoad_LaterWagonsAreNotBought()
        {
            Scene s = BuildScene(new EconomyConfig { TrainCapacity = 8 });
            Dispatch(s, s.Food, s.Food, s.Food, s.Food, s.Food, s.Food);

            DeliveryResult result = s.Eco.CompleteTrip(1);

            // 150%, 125%, 100%, 75% (7.5 rounds to 8), 50%, then the town is full.
            Assert.That(result.Wagons[0].Coins, Is.EqualTo(15));
            Assert.That(result.Wagons[1].Coins, Is.EqualTo(12));
            Assert.That(result.Wagons[2].Coins, Is.EqualTo(10));
            Assert.That(result.Wagons[3].Coins, Is.EqualTo(8));
            Assert.That(result.Wagons[4].Coins, Is.EqualTo(5));
            Assert.That(result.Wagons[5].Outcome, Is.EqualTo(WagonOutcome.StoredInDepot));
            Assert.That(result.Wagons[5].Coins, Is.EqualTo(0));
            Assert.That(result.Total, Is.EqualTo(50));
            Assert.That(s.Eco.Demand.FreeSlots(s.Town, s.Food), Is.EqualTo(0));
            Assert.That(s.Eco.Depot.Count(s.Town, s.Food), Is.EqualTo(1));
        }

        [Test]
        public void Delivery_UsesThePriceTableFromConfig()
        {
            var cfg = new EconomyConfig { PriceBySlots = new[] { 0f, 1f, 1f, 1f, 1f, 3f } };
            Scene s = BuildScene(cfg);
            Dispatch(s, s.Food);

            DeliveryResult result = s.Eco.CompleteTrip(1);

            Assert.That(result.Wagons[0].Coins, Is.EqualTo(30));
        }

        [Test]
        public void Delivery_PassengerWagons_AreSkippedForNow()
        {
            Scene s = BuildScene();
            Dispatch(s, s.Passengers);

            DeliveryResult result = s.Eco.CompleteTrip(1);

            // Placeholder until the passengers task: no pay, nothing stored.
            Assert.That(result.Wagons[0].Coins, Is.EqualTo(0));
            Assert.That(result.Wagons[0].Outcome, Is.EqualTo(WagonOutcome.Lost));
            Assert.That(s.Eco.Depot.Count(s.Town, s.Passengers), Is.EqualTo(0));
        }

        [Test]
        public void Delivery_TrainArrivedEvent_CarriesResultAndSeesFinalState()
        {
            Scene s = BuildScene();
            Dispatch(s, s.Food);
            int coinsBefore = s.State.Coins;

            int count = 0;
            DeliveryResult resultSeen = null;
            TrainState stateSeen = TrainState.Travelling;
            Node stopSeen = null;
            int coinsSeen = -1;
            s.Eco.TrainArrived += (t, r) =>
            {
                count++;
                resultSeen = r;
                stateSeen = t.State;
                stopSeen = t.CurrentStop;
                coinsSeen = s.State.Coins;
            };

            DeliveryResult result = s.Eco.CompleteTrip(1);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(resultSeen, Is.SameAs(result));
            Assert.That(stateSeen, Is.EqualTo(TrainState.AtStop));
            Assert.That(stopSeen, Is.SameAs(s.Town));
            Assert.That(coinsSeen, Is.EqualTo(coinsBefore + result.Total));
            Assert.That(s.Eco.GetTrain(1).Cargo.Count, Is.EqualTo(0));
        }

        [Test]
        public void Delivery_RealDelivery_RaisesDemandChangedPerSoldWagon()
        {
            Scene s = BuildScene();
            Dispatch(s, s.Food, s.Food, s.Food);
            var freeValues = new List<int>();
            s.Eco.Demand.DemandChanged += (n, c, free) => freeValues.Add(free);

            s.Eco.CompleteTrip(1);

            Assert.That(freeValues, Is.EqualTo(new[] { 4, 3, 2 }));
        }

        // ---------- Preview ----------

        [Test]
        public void Preview_EqualsRealDelivery_AndChangesNothing()
        {
            Scene s = BuildScene();
            Dispatch(s, s.Food, s.Food, s.Ore);
            Train train = s.Eco.GetTrain(1);

            int demandEvents = 0, depotEvents = 0, arrivedEvents = 0;
            s.Eco.Demand.DemandChanged += (n, c, free) => demandEvents++;
            s.Eco.Depot.DepotChanged += (n, c, count) => depotEvents++;
            s.Eco.TrainArrived += (t, r) => arrivedEvents++;

            int coins = s.State.Coins;
            int freeSlots = s.Eco.Demand.FreeSlots(s.Town, s.Food);
            int foodInDepot = s.Eco.Depot.Count(s.Town, s.Food);
            int oreInDepot = s.Eco.Depot.Count(s.Town, s.Ore);

            DeliveryResult preview = s.Eco.PreviewDelivery(train, s.Town, train.Cargo);

            Assert.That(s.State.Coins, Is.EqualTo(coins));
            Assert.That(s.Eco.Demand.FreeSlots(s.Town, s.Food), Is.EqualTo(freeSlots));
            Assert.That(s.Eco.Depot.Count(s.Town, s.Food), Is.EqualTo(foodInDepot));
            Assert.That(s.Eco.Depot.Count(s.Town, s.Ore), Is.EqualTo(oreInDepot));
            Assert.That(demandEvents + depotEvents + arrivedEvents, Is.EqualTo(0));
            Assert.That(train.State, Is.EqualTo(TrainState.Travelling));
            Assert.That(train.Cargo.Count, Is.EqualTo(3));

            DeliveryResult real = s.Eco.CompleteTrip(1);

            AssertSameDelivery(preview, real);
            Assert.That(real.Total, Is.EqualTo(27)); // food 15 + 12, ore unpaid
        }

        [Test]
        public void Preview_OfAHypotheticalLoadBeforeDispatch_MatchesTheLaterRealDelivery()
        {
            Scene s = BuildScene();
            Train train = s.Eco.GetTrain(1); // still at the farm, empty

            DeliveryResult preview = s.Eco.PreviewDelivery(
                train, s.Town, new Cargo(new[] { s.Food, s.Food }));

            Assert.That(train.State, Is.EqualTo(TrainState.AtStop));
            Assert.That(train.Cargo.Count, Is.EqualTo(0));

            Dispatch(s, s.Food, s.Food);
            DeliveryResult real = s.Eco.CompleteTrip(1);

            AssertSameDelivery(preview, real);
        }

        [Test]
        public void Preview_CountsDepotRoomUsedByEarlierWagonsOfTheSameLoad()
        {
            Scene s = BuildScene(new EconomyConfig { DepotCapacity = 2 });
            s.Eco.Depot.Add(s.Town, s.Ore); // one of two places already used
            Dispatch(s, s.Ore, s.Ore);
            Train train = s.Eco.GetTrain(1);

            DeliveryResult preview = s.Eco.PreviewDelivery(train, s.Town, train.Cargo);

            Assert.That(preview.Wagons[0].Outcome, Is.EqualTo(WagonOutcome.StoredInDepot));
            Assert.That(preview.Wagons[1].Outcome, Is.EqualTo(WagonOutcome.Lost));

            DeliveryResult real = s.Eco.CompleteTrip(1);

            AssertSameDelivery(preview, real);
            Assert.That(s.Eco.Depot.Count(s.Town, s.Ore), Is.EqualTo(2));
        }

        [Test]
        public void Preview_NullArguments_Throw()
        {
            Scene s = BuildScene();
            Train train = s.Eco.GetTrain(1);

            Assert.Throws<ArgumentNullException>(
                () => s.Eco.PreviewDelivery(null, s.Town, Cargo.Empty));
            Assert.Throws<ArgumentNullException>(
                () => s.Eco.PreviewDelivery(train, null, Cargo.Empty));
        }

        // ---------- Config ----------

        [Test]
        public void Config_Defaults_PriceTableHasOneEntryPerFreeSlotCount()
        {
            var defaults = new EconomyConfig();

            Assert.That(defaults.PriceBySlots.Length, Is.EqualTo(defaults.DemandSlots + 1));
        }

        [Test]
        public void CustomConfig_TrainCapacityIsRespected()
        {
            var custom = new EconomyConfig { TrainCapacity = 2 };
            var eco = new Economy(new GameState(100), network, custom);
            eco.TryBuyLine(1);

            eco.TryBuyTrain(1);

            Assert.That(eco.GetTrain(1).Capacity, Is.EqualTo(2));
        }

        [Test]
        public void CustomConfig_TrainPriceIsCharged()
        {
            var state = new GameState(100);
            var eco = new Economy(state, network, new EconomyConfig { TrainPrice = 10 });
            eco.TryBuyLine(1);

            eco.TryBuyTrain(1);

            Assert.That(state.Coins, Is.EqualTo(100 - 50 - 10));
        }

        [Test]
        public void CustomConfig_LinePriceFactorScalesLineCost()
        {
            var state = new GameState(100);
            var eco = new Economy(state, network, new EconomyConfig { LinePriceFactor = 0.5f });

            eco.TryBuyLine(1);

            // Line cost 50 x 0.5 = 25
            Assert.That(state.Coins, Is.EqualTo(75));
        }

        [Test]
        public void CustomConfig_LinePriceFactorCanMakeLineUnaffordable()
        {
            var state = new GameState(100);
            var eco = new Economy(state, network, new EconomyConfig { LinePriceFactor = 3f });

            BuyResult result = eco.TryBuyLine(1);

            // 50 x 3 = 150 > 100
            Assert.That(result, Is.EqualTo(BuyResult.NotEnoughCoins));
            Assert.That(line.IsActive, Is.False);
        }
    }
}
