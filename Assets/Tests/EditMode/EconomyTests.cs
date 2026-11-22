using NUnit.Framework;
using ChugHaul.Core;
using System;

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

            economy = new Economy(gameState, network);
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
            Assert.That(gameState.Coins, Is.EqualTo(50 - EconomyConfig.TrainPrice));
            Assert.That(train, Is.Not.Null);
            Assert.That(train.Line, Is.SameAs(line));
            Assert.That(train.State, Is.EqualTo(TrainState.AtStop));
            Assert.That(train.CurrentStop, Is.SameAs(nodeA));
            Assert.That(train.Destination, Is.Null);
            Assert.That(train.Capacity, Is.EqualTo(EconomyConfig.TrainCapacity));
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
            var customEconomy = new Economy(gameState, networkWithLine, coinRate: 2f);
            customEconomy.TryBuyLine(2);

            int reward = customEconomy.OnTrainArrived(2);

            // Line length is 5, coin rate is 2
            // Reward = (int)Math.Round(5 * 2) = 10
            Assert.That(reward, Is.EqualTo(10));
        }
    }
}
