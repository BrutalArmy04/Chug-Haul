namespace ChugHaul.Core
{
    public enum NodeKind
    {
        /// <summary>A town. Wants goods; makes nothing for now (mail and passengers come later).</summary>
        City,

        /// <summary>A raw-material producer (farm, forest, mine): one cargo, no demands.</summary>
        Industry
    }
}
