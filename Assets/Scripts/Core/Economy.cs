using System;

namespace ChugHaul.Core
{
    public class Economy
    {
        private readonly GameState gameState;
        private readonly Network network;
        private readonly float coinRate;

        public event Action<Line> LineActivated;

        public Economy(GameState gameState, Network network, float coinRate = 1f)
        {
            this.gameState = gameState
                ?? throw new ArgumentNullException(nameof(gameState));

            this.network = network
                ?? throw new ArgumentNullException(nameof(network));

            this.coinRate = coinRate;
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

            if (gameState.Coins < line.Cost)
                return BuyResult.NotEnoughCoins;

            // All checks have passed. Now perform the purchase.
            bool spent = gameState.TrySpend(line.Cost);

            if (!spent)
                return BuyResult.NotEnoughCoins;

            line.Activate();

            LineActivated?.Invoke(line);

            return BuyResult.Ok;
        }

        // Called by the View when a train reaches the end of a line. Returns the coins earned.
        public int OnTrainArrived(int lineId)
        {
            Line line = network.GetLine(lineId);
            if (line == null) throw new ArgumentException($"Unknown line {lineId}");  // adjust if GetLine throws itself
            if (!line.IsActive) return 0;

            int reward = (int)Math.Round(line.Length * coinRate);
            gameState.Earn(reward);
            return reward;
        }
    }
}