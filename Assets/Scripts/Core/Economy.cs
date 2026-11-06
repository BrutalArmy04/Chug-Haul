using System;

namespace ChugHaul.Core
{
    public class Economy
    {
        private readonly GameState gameState;
        private readonly Network network;

        public event Action<Line> LineActivated;

        public Economy(GameState gameState, Network network)
        {
            this.gameState = gameState
                ?? throw new ArgumentNullException(nameof(gameState));

            this.network = network
                ?? throw new ArgumentNullException(nameof(network));
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
    }
}