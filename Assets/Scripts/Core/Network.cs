using System;
using System.Collections.Generic;
using System.Linq;

namespace ChugHaul.Core
{
    public class Network
    {
        private readonly List<Node> nodes;
        private readonly List<Line> lines;

        private readonly Dictionary<int, Node> nodesById;
        private readonly Dictionary<int, Line> linesById;

        public IReadOnlyList<Node> Nodes { get; }
        public IReadOnlyList<Line> Lines { get; }

        public Network(IEnumerable<Node> nodes, IEnumerable<Line> lines)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            this.nodes = new List<Node>(nodes);
            this.lines = new List<Line>(lines);

            nodesById = new Dictionary<int, Node>();
            linesById = new Dictionary<int, Line>();

            // Validate and index nodes.
            foreach (Node node in this.nodes)
            {
                if (node == null)
                    throw new ArgumentException("Network cannot contain null nodes.");

                if (!nodesById.TryAdd(node.Id, node))
                    throw new ArgumentException($"Duplicate node ID: {node.Id}");
            }

            // Validate and index lines.
            foreach (Line line in this.lines)
            {
                if (line == null)
                    throw new ArgumentException("Network cannot contain null lines.");

                if (!linesById.TryAdd(line.Id, line))
                    throw new ArgumentException($"Duplicate line ID: {line.Id}");

                if (ReferenceEquals(line.NodeA, line.NodeB))
                    throw new ArgumentException("A line cannot connect a node to itself.");

                if (!nodesById.ContainsValue(line.NodeA) ||
                    !nodesById.ContainsValue(line.NodeB))
                {
                    throw new ArgumentException(
                        $"Line {line.Id} references a node outside the network.");
                }
            }

            // Expose read-only views.
            Nodes = this.nodes.AsReadOnly();
            Lines = this.lines.AsReadOnly();
        }

        public Node GetNode(int id)
        {
            nodesById.TryGetValue(id, out Node node);
            return node;
        }

        public Line GetLine(int id)
        {
            linesById.TryGetValue(id, out Line line);
            return line;
        }

        public IReadOnlyList<Line> LinesAt(Node node)
        {
            List<Line> result = new List<Line>();

            foreach (Line line in lines)
            {
                if (ReferenceEquals(line.NodeA, node) ||
                    ReferenceEquals(line.NodeB, node))
                {
                    result.Add(line);
                }
            }

            return result.AsReadOnly();
        }
    }
}