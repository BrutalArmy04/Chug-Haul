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

            line = new Line(1, nodeA, nodeB, 50);

            network = new Network(
                new[] { nodeA, nodeB },
                new[] { line }
            );

            economy = new Economy(gameState, network);
        }

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
            var customEconomy = new Economy(gameState, network, coinRate: 2f);
            
            Line testLine = new Line(2, nodeA, nodeB, 50);
            var networkWithLine = new Network(
                new[] { nodeA, nodeB },
                new[] { line, testLine }
            );
            customEconomy = new Economy(gameState, networkWithLine, coinRate: 2f);
            customEconomy.TryBuyLine(2);

            int reward = customEconomy.OnTrainArrived(2);

            // Line length is 5, coin rate is 2
            // Reward = (int)Math.Round(5 * 2) = 10
            Assert.That(reward, Is.EqualTo(10));
        }
    }
}