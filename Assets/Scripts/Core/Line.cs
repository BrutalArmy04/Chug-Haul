using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    public class Line
    {
        private readonly Node[] _stops;
        private readonly Segment[] _segments;

        public int Id { get; }
        public int Cost { get; }
        public bool IsActive { get; private set; }

        /// <summary>Ordered stops, first to last.</summary>
        public IReadOnlyList<Node> Stops => _stops;

        /// <summary>One segment per consecutive pair of stops, in line order.</summary>
        public IReadOnlyList<Segment> Segments => _segments;

        /// <summary>Total length: sum of all segments.</summary>
        public float Length { get; }

        [Obsolete("Use Stops[0]. Kept so M1 View code compiles until the View task replaces it.")]
        public Node NodeA => _stops[0];

        [Obsolete("Use Stops[Stops.Count - 1]. Kept so M1 View code compiles until the View task replaces it.")]
        public Node NodeB => _stops[_stops.Length - 1];

        public Line(int id, IReadOnlyList<Node> stops, int cost)
        {
            if (stops == null) throw new ArgumentNullException(nameof(stops));
            if (stops.Count < 2)
                throw new ArgumentException("A line needs at least 2 stops.", nameof(stops));

            var seen = new HashSet<Node>();
            _stops = new Node[stops.Count];
            for (int i = 0; i < stops.Count; i++)
            {
                Node stop = stops[i];
                if (stop == null)
                    throw new ArgumentException("A line cannot contain a null stop.", nameof(stops));
                if (!seen.Add(stop))
                    throw new ArgumentException("A node cannot appear twice on the same line.", nameof(stops));
                _stops[i] = stop;
            }

            Id = id;
            Cost = cost;
            IsActive = false;

            _segments = new Segment[_stops.Length - 1];
            float total = 0f;
            for (int i = 0; i < _segments.Length; i++)
            {
                _segments[i] = new Segment(_stops[i], _stops[i + 1]);
                total += _segments[i].Length;
            }
            Length = total;
        }

        /// <summary>M1-style two-stop constructor; delegates to the new one.</summary>
        [Obsolete("Use Line(int, IReadOnlyList<Node>, int).")]
        public Line(int id, Node nodeA, Node nodeB, int cost)
            : this(id, new[] { nodeA, nodeB }, cost)
        {
        }

        /// <summary>Index of the node on this line, or -1 if it is not a stop.</summary>
        public int IndexOf(Node node)
        {
            for (int i = 0; i < _stops.Length; i++)
                if (_stops[i].Equals(node)) return i;
            return -1;
        }

        public bool Contains(Node node) => IndexOf(node) >= 0;

        /// <summary>Distance along the line between two stops, same in both directions.</summary>
        public float DistanceBetween(Node a, Node b)
        {
            int i = RequireIndex(a, nameof(a));
            int j = RequireIndex(b, nameof(b));

            int lo = Math.Min(i, j);
            int hi = Math.Max(i, j);

            float sum = 0f;
            for (int k = lo; k < hi; k++)
                sum += _segments[k].Length;
            return sum;
        }

        /// <summary>
        /// Segments in travel order from <paramref name="from"/> to <paramref name="to"/>.
        /// Travelling backwards returns reversed segments with From/To swapped,
        /// so a caller can walk the result blindly. from == to gives an empty list.
        /// </summary>
        public IReadOnlyList<Segment> PathBetween(Node from, Node to)
        {
            int i = RequireIndex(from, nameof(from));
            int j = RequireIndex(to, nameof(to));

            var path = new List<Segment>(Math.Abs(j - i));
            if (i < j)
            {
                for (int k = i; k < j; k++)
                    path.Add(_segments[k]);
            }
            else
            {
                for (int k = i - 1; k >= j; k--)
                    path.Add(_segments[k].Reversed());
            }
            return path;
        }

        private int RequireIndex(Node node, string paramName)
        {
            if (node == null) throw new ArgumentNullException(paramName);
            int index = IndexOf(node);
            if (index < 0)
                throw new ArgumentException("Node is not a stop on this line.", paramName);
            return index;
        }

        internal void Activate()
        {
            IsActive = true;
        }
    }
}
