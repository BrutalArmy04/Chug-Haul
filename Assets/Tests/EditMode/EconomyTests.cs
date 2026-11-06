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
    }
}