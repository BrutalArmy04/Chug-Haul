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
            economy.TrainArrived += t => { eventCount++; received = t; };

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
            CargoType steel = Kind("Steel");
            Stock(nodeA, food, 2); // no steel

            DispatchResult result = economy.TryDispatch(1, nodeB, Load(food, steel));

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
            CargoType steel = Kind("Steel");
            Stock(nodeA, food, 3);
            Stock(nodeA, steel, 1);

            var seen = new List<(CargoType, int)>();
            economy.Depot.DepotChanged += (n, c, count) => seen.Add((c, count));

            economy.TryDispatch(1, nodeB, Load(food, steel, food));

            Assert.That(seen.Count, Is.EqualTo(2));
            Assert.That(seen[0].Item1, Is.SameAs(food));
            Assert.That(seen[0].Item2, Is.EqualTo(1));
            Assert.That(seen[1].Item1, Is.SameAs(steel));
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
            CargoType steel = Kind("Steel");
            Assert.That(steel.WagonMass, Is.Not.EqualTo(food.WagonMass));

            int foodMass = MassOnTrip(nodeA, nodeB, food, food);
            int steelMass = MassOnTrip(nodeB, nodeA, steel, steel);

            Assert.That(foodMass, Is.EqualTo(config.LocomotiveMass + 2 * food.WagonMass));
            Assert.That(steelMass, Is.EqualTo(config.LocomotiveMass + 2 * steel.WagonMass));
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

        // ---------- Old payout (removed with the payout task) ----------

        [Test]
        public void OnTrainArrived_InactiveLineReturnsZero()
        {
            int reward = economy.OnTrainArrived(1);

            Assert.That(reward, Is.EqualTo(0));
        }

        [Test]
        public void OnTrainArrived_ActiveLineReturnsReward()
        {
            economy.TryBuyLine(1);

            int reward = economy.OnTrainArrived(1);

            // Line length is 5 (distance from (0,0) to (3,4)), default coin rate is 1
            // Reward = (int)Math.Round(5 * 1) = 5
            Assert.That(reward, Is.EqualTo(5));
        }

        [Test]
        public void OnTrainArrived_AddsCoinsToGameState()
        {
            economy.TryBuyLine(1);
            int coinsBeforeArrival = gameState.Coins;

            int reward = economy.OnTrainArrived(1);

            Assert.That(gameState.Coins, Is.EqualTo(coinsBeforeArrival + reward));
        }

        [Test]
        public void OnTrainArrived_UnknownIdThrows()
        {
            Assert.Throws<ArgumentException>(
                () => economy.OnTrainArrived(-1)
            );
        }

        [Test]
        public void OnTrainArrived_WithCustomCoinRate()
        {
            Line testLine = new Line(2, new[] { nodeA, nodeB }, 50);
            var networkWithLine = new Network(
                new[] { nodeA, nodeB },
                new[] { line, testLine }
            );
            var customEconomy = new Economy(
                gameState, networkWithLine, new EconomyConfig { LegacyCoinRate = 2f });
            customEconomy.TryBuyLine(2);

            int reward = customEconomy.OnTrainArrived(2);

            // Line length is 5, coin rate is 2
            // Reward = (int)Math.Round(5 * 2) = 10
            Assert.That(reward, Is.EqualTo(10));
        }
    }
}
