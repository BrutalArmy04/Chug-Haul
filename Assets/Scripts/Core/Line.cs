using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    // A predetermined route through several nodes (e.g. 1-2-3-4), made of straight segments.
    public class Line
    {
        public int Id { get; }
        public IReadOnlyList<Node> Stops { get; }

        // One per consecutive pair of stops, in route order: Segments[i] goes from Stops[i] to Stops[i + 1].
        public IReadOnlyList<Segment> Segments { get; }

        public int Cost { get; }
        public bool IsActive { get; private set; }

        // Sum of all segment lengths.
        public float Length { get; }

        [Obsolete("M1 only. Use Stops[0]. Removed once the View handles multi-node lines.")]
        public Node NodeA => Stops[0];

        [Obsolete("M1 only. Use Stops[Stops.Count - 1]. Removed once the View handles multi-node lines.")]
        public Node NodeB => Stops[Stops.Count - 1];

        // TODO (elevation data task): the optional bool[] crossesWater (one flag per segment) goes on this
        // constructor and into the segment loop below. Throw if its length isn't Stops.Count - 1.
        public Line(int id, IReadOnlyList<Node> stops, int cost)
        {
            if (stops == null)
                throw new ArgumentNullException(nameof(stops));

            if (stops.Count < 2)
                throw new ArgumentException($"Line {id} needs at least 2 stops.", nameof(stops));

            // Copy, so the caller can't change the route afterwards.
            var stopList = new List<Node>(stops.Count);
            var seen = new HashSet<Node>();

            foreach (Node stop in stops)
            {
                if (stop == null)
                    throw new ArgumentException($"Line {id} has a null stop.", nameof(stops));

                if (!seen.Add(stop))
                    throw new ArgumentException($"Line {id} visits node {stop.Id} more than once.", nameof(stops));

                stopList.Add(stop);
            }

            var segments = new List<Segment>(stopList.Count - 1);
            float length = 0f;

            for (int i = 0; i < stopList.Count - 1; i++)
            {
                var segment = new Segment(stopList[i], stopList[i + 1]);
                segments.Add(segment);
                length += segment.Length;
            }

            Id = id;
            Stops = stopList.AsReadOnly();
            Segments = segments.AsReadOnly();
            Cost = cost;
            Length = length;
            IsActive = false;
        }

        // A line between two nodes (the M1 shape).
        public Line(int id, Node nodeA, Node nodeB, int cost)
            : this(id, new[] { nodeA, nodeB }, cost)
        {
        }

        // Position of the node in Stops, or -1 if it isn't a stop of this line.
        public int IndexOf(Node node)
        {
            for (int i = 0; i < Stops.Count; i++)
            {
                if (ReferenceEquals(Stops[i], node))
                    return i;
            }

            return -1;
        }

        // Track distance between two stops: the sum of the segments between them. Same in both directions.
        public float DistanceBetween(Node a, Node b)
        {
            int i = RequireStop(a, nameof(a));
            int j = RequireStop(b, nameof(b));

            float distance = 0f;

            for (int k = Math.Min(i, j); k < Math.Max(i, j); k++)
                distance += Segments[k].Length;

            return distance;
        }

        // The segments to travel from one stop to another, in travel order.
        // Travelling backwards, each segment is reversed (From/To swapped), so the View can walk the list as is.
        // Same stop -> empty list.
        public IReadOnlyList<Segment> PathBetween(Node from, Node to)
        {
            int i = RequireStop(from, nameof(from));
            int j = RequireStop(to, nameof(to));

            var path = new List<Segment>(Math.Abs(j - i));

            if (i < j)
            {
                for (int k = i; k < j; k++)
                    path.Add(Segments[k]);
            }
            else
            {
                for (int k = i - 1; k >= j; k--)
                    path.Add(Segments[k].Reversed());
            }

            return path.AsReadOnly();
        }

        internal void Activate()
        {
            IsActive = true;
        }

        int RequireStop(Node node, string paramName)
        {
            if (node == null)
                throw new ArgumentNullException(paramName);

            int index = IndexOf(node);

            if (index < 0)
                throw new ArgumentException($"Node {node.Id} is not a stop of line {Id}.", paramName);

            return index;
        }
    }
}
