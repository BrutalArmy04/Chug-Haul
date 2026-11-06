
using System;

namespace ChugHaul.Core
{
    public class GameState
    {
        public int Coins { get; private set; }

        public event Action<int> MoneyChanged;

        public GameState(int startingCoins)
        {
            if (startingCoins < 0)
                throw new ArgumentOutOfRangeException(nameof(startingCoins));

            Coins = startingCoins;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (amount > Coins)
                return false;

            if (amount == 0)
                return true;

            Coins -= amount;
            MoneyChanged?.Invoke(Coins);

            return true;
        }

        public void Earn(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (amount == 0)
                return;

            Coins = checked(Coins + amount);
            MoneyChanged?.Invoke(Coins);
        }
    }
}
