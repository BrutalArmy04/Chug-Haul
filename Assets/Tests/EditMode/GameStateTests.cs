
using NUnit.Framework;
using ChugHaul.Core;
using System;

namespace ChugHaul.Core.Tests
{
    public class GameStateTests
    {
        [Test]
        public void TrySpend_SufficientCoins_DeductsAmount()
        {
            GameState state = new GameState(100);

            bool result = state.TrySpend(30);

            Assert.That(result, Is.True);
            Assert.That(state.Coins, Is.EqualTo(70));
        }

        [Test]
        public void TrySpend_InsufficientCoins_ChangesNothing()
        {
            GameState state = new GameState(100);

            bool result = state.TrySpend(150);

            Assert.That(result, Is.False);
            Assert.That(state.Coins, Is.EqualTo(100));
        }

        [Test]
        public void Earn_AddsAmount()
        {
            GameState state = new GameState(100);

            state.Earn(25);

            Assert.That(state.Coins, Is.EqualTo(125));
        }

        [Test]
        public void MoneyChanged_FiresOnceWithNewBalance()
        {
            GameState state = new GameState(100);

            int eventCount = 0;
            int receivedAmount = -1;

            state.MoneyChanged += amount =>
            {
                eventCount++;
                receivedAmount = amount;
            };

            state.TrySpend(30);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(receivedAmount, Is.EqualTo(70));
        }

        [Test]
        public void MoneyChanged_DoesNotFireOnFailedSpend()
        {
            GameState state = new GameState(100);

            int eventCount = 0;

            state.MoneyChanged += _ => eventCount++;

            state.TrySpend(150);

            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void TrySpend_NegativeAmount_Throws()
        {
            GameState state = new GameState(100);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => state.TrySpend(-10)
            );
        }

        [Test]
        public void Earn_NegativeAmount_Throws()
        {
            GameState state = new GameState(100);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => state.Earn(-10)
            );
        }
    }
}
