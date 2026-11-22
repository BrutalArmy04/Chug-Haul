namespace ChugHaul.Core
{
    /// <summary>
    /// Every economy number lives here. Hardcoded this sprint; ScriptableObjects come later.
    /// Plain C#, no UnityEngine.
    /// </summary>
    public static class EconomyConfig
    {
        // Placeholder: check against what SandboxBootstrap uses today.
        public const int StartingCoins = 100;

        // Reward per unit of line length in the old Length x coinRate payout.
        // Goes away with the payout task.
        public const float CoinRate = 1f;

        // One train model this sprint. Placeholder values.
        public const int TrainPrice = 30;
        public const int TrainCapacity = 10;
    }
}
