using System;

namespace ChugHaul.Core
{
    // One straight piece of a line, between two consecutive stops. Immutable.
    public class Segment
    {
        public Node From { get; }
        public Node To { get; }

        // Straight distance between From and To. Always > 0.
        public float Length { get; }

        // TODO (elevation data task): add CrossesWater, Climb(Node from) and Gradient here.
        // Length is guaranteed > 0, so Gradient can divide by it.
        public Segment(Node from, Node to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));

            double dx = to.X - from.X;
            double dz = to.Z - from.Z;
            Length = (float)Math.Sqrt(dx * dx + dz * dz);

            if (Length <= 0f)
                throw new ArgumentException(
                    $"Nodes {from.Id} and {to.Id} are at the same position, so the segment between them has no length.");
        }

        // The same segment travelled the other way (From and To swapped).
        // TODO (elevation data task): pass CrossesWater (and any other per-segment data) through here,
        // or it is lost on backward trips. Line.PathBetween uses this for every backward path.
        public Segment Reversed() => new Segment(To, From);
    }
}
