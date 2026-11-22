namespace ChugHaul.Core
{
    /// <summary>
    /// Every economy number lives here. Plain class with public fields and defaults,
    /// so tests can build their own: new EconomyConfig { DepotCapacity = 2 }.
    /// Pass one instance to everything that needs numbers.
    /// Values are first guesses; they get tuned in the balance task.
    /// Later Core tasks add their numbers here too.
    /// </summary>
    public class EconomyConfig
    {
        // ---- Money ----
        public int StartingCoins = 100;
        public int TrainPrice = 40;
        /// <summary>Multiplies Line.Cost when buying a line.</summary>
        public float LinePriceFactor = 1f;

        // ---- Cargo / trains ----
        // Per-cargo base value and wagon mass arrive with the cargo task.
        public int LocomotiveMass = 5;
        public int TrainCapacity = 4;

        // ---- Demand ----
        public int DemandSlots = 5;
        /// <summary>Price multiplier, indexed by number of free slots (0..DemandSlots).</summary>
        public float[] PriceBySlots = { 0f, 0.50f, 0.75f, 1.00f, 1.25f, 1.50f };
        /// <summary>Seconds between a city consuming goods.</summary>
        public float CityConsumeInterval = 45f;

        // ---- Supply ----
        public float IndustryInterval = 20f;
        public int DepotCapacity = 8;
        public float FactoryInterval = 30f;

        // ---- Passengers ----
        public int WaitingRoomSize = 6;
        public float PassengerInterval = 25f;
        public float PassengerRatePerUnit = 1f;

        // ---- Time and costs ----
        /// <summary>Seconds per in-game month.</summary>
        public float MonthLength = 60f;
        public int TrainMaintenance = 5;
        public float LineMaintenancePerUnit = 0.5f;
        public int RepossessAfterMonths = 3;
        /// <summary>Fraction of the price refunded when a train is repossessed.</summary>
        public float RepossessRefund = 0.5f;

        // ---- Terrain ----
        public float UphillPenalty = 0.25f;
        public float MinSpeedFactor = 0.3f;
        public float GradientCostFactor = 0.5f;
        public float WaterCostFactor = 2f;

        // ---- Legacy (removed with the payout task) ----
        /// <summary>Reward per unit of line length in the old Length x rate payout.</summary>
        public float LegacyCoinRate = 1f;
    }
}
