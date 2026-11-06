using NUnit.Framework;
using ChugHaul.Core;
using System;

namespace ChugHaul.Core.Tests
{
    public class NetworkTests
    {
        [Test]
        public void Line_CalculatesLengthCorrectly()
        {
            Node a = new Node(1, "A", 0, 0);
            Node b = new Node(2, "B", 3, 4);

            Line line = new Line(1, a, b, 10);

            Assert.That(line.Length, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void GetNode_ReturnsCorrectNode()
        {
            Network network = TestMap.Create();

            Assert.That(network.GetNode(2).Name, Is.EqualTo("Brasov"));
        }

        [Test]
        public void GetNode_UnknownId_ReturnsNull()
        {
            Network network = TestMap.Create();

            Assert.That(network.GetNode(999), Is.Null);
        }

        [Test]
        public void LinesAt_ReturnsOnlyConnectedLines()
        {
            Network network = TestMap.Create();
            Node brasov = network.GetNode(2);

            var result = network.LinesAt(brasov);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Does.Contain(network.GetLine(1)));
            Assert.That(result, Does.Contain(network.GetLine(2)));
        }

        [Test]
        public void Constructor_DuplicateNodeId_Throws()
        {
            Node a = new Node(1, "A", 0, 0);
            Node b = new Node(1, "B", 3, 4);

            Assert.Throws<ArgumentException>(() =>
                new Network(new[] { a, b }, Array.Empty<Line>())
            );
        }

        [Test]
        public void Constructor_DuplicateLineId_Throws()
        {
            Node a = new Node(1, "A", 0, 0);
            Node b = new Node(2, "B", 3, 4);
            Node c = new Node(3, "C", 6, 4);

            Line line1 = new Line(1, a, b, 10);
            Line line2 = new Line(1, b, c, 20);

            Assert.Throws<ArgumentException>(() =>
                new Network(new[] { a, b, c }, new[] { line1, line2 })
            );
        }

        [Test]
        public void Constructor_UnknownNode_Throws()
        {
            Node a = new Node(1, "A", 0, 0);
            Node b = new Node(2, "B", 3, 4);
            Node outside = new Node(3, "Outside", 6, 4);

            Line line = new Line(1, a, outside, 10);

            Assert.Throws<ArgumentException>(() =>
                new Network(new[] { a, b }, new[] { line })
            );
        }

        [Test]
        public void Constructor_SelfLoop_Throws()
        {
            Node a = new Node(1, "A", 0, 0);

            Line line = new Line(1, a, a, 10);

            Assert.Throws<ArgumentException>(() =>
                new Network(new[] { a }, new[] { line })
            );
        }
    }
}