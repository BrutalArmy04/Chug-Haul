namespace ChugHaul.Core
{
    /// <summary>What happened to one wagon at its destination.</summary>
    public enum WagonOutcome
    {
        /// <summary>The destination wanted it and had a free slot: paid.</summary>
        Sold,

        /// <summary>Unpaid, but there was room in the destination's depot.</summary>
        StoredInDepot,

        /// <summary>Unpaid and no room: the cargo is gone.</summary>
        Lost
    }
}
