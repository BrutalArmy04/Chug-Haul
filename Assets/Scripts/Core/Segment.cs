using System;

namespace ChugHaul.Core
{
    /// <summary>One straight leg between two consecutive stops. Immutable.</summary>
    public sealed class Segment
    {
        public Node From { get; }
        public Node To { get; }
        public float Length { get; }

        public Segment(Node from, Node to)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            From = from;
            To = to;

            double dx = to.X - from.X;
            double dz = to.Z - from.Z;
            Length = (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>The same leg walked the other way (From/To swapped).</summary>
        public Segment Reversed() => new Segment(To, From);
    }
}
