using NUnit.Framework;
using ChugHaul.Core;
using System;

namespace ChugHaul.Core.Tests
{
    public class LineTests
    {
        // A(0,0) -> B(3,4) -> C(3,7): segments of length 5 and 3.
        private Node a;
        private Node b;
        private Node c;
        private Line line;

        [SetUp]
        public void SetUp()
        {
            a = new Node(1, "A", 0, 0);
            b = new Node(2, "B", 3, 4);
            c = new Node(3, "C", 3, 7);

            line = new Line(1, new[] { a, b, c }, 10);
        }

        // --- Building the line ---

        [Test]
        public void ThreeStopLine_LengthIsSumOfSegments()
        {
            Assert.That(line.Segments.Count, Is.EqualTo(2));
            Assert.That(line.Segments[0].Length, Is.EqualTo(5f).Within(0.001f));
            Assert.That(line.Segments[1].Length, Is.EqualTo(3f).Within(0.001f));
            Assert.That(line.Length, Is.EqualTo(8f).Within(0.001f));
            Assert.That(line.DistanceBetween(a, c), Is.EqualTo(8f).Within(0.001f));
        }

        [Test]
        public void Segments_ConnectConsecutiveStopsInRouteOrder()
        {
            Assert.That(line.Segments[0].From, Is.SameAs(a));
            Assert.That(line.Segments[0].To, Is.SameAs(b));
            Assert.That(line.Segments[1].From, Is.SameAs(b));
            Assert.That(line.Segments[1].To, Is.SameAs(c));
        }

        [Test]
        public void TwoStopLine_BehavesLikeM1()
        {
            Node x = new Node(10, "X", 0, 0);
            Node y = new Node(11, "Y", 3, 4);

            Line twoStop = new Line(1, x, y, 10);

            Assert.That(twoStop.Stops, Is.EqualTo(new[] { x, y }));
            Assert.That(twoStop.Segments.Count, Is.EqualTo(1));
            Assert.That(twoStop.Length, Is.EqualTo(5f).Within(0.001f));
            Assert.That(twoStop.DistanceBetween(x, y), Is.EqualTo(5f).Within(0.001f));
        }

#pragma warning disable CS0618 // NodeA/NodeB are obsolete on purpose; delete this test together with them.
        [Test]
        public void NodeAAndNodeB_AreFirstAndLastStop()
        {
            Assert.That(line.NodeA, Is.SameAs(a));
            Assert.That(line.NodeB, Is.SameAs(c));
        }
#pragma warning restore CS0618

        [Test]
        public void Stops_AreCopied_SoChangingTheInputDoesNotChangeTheLine()
        {
            Node[] stops = { a, b, c };
            Line copied = new Line(2, stops, 10);

            stops[1] = new Node(99, "Other", 1, 1);

            Assert.That(copied.Stops[1], Is.SameAs(b));
        }

        // --- Invalid lines ---

        [Test]
        public void Constructor_OneStop_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Line(1, new[] { a }, 10));
        }

        [Test]
        public void Constructor_NoStops_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Line(1, Array.Empty<Node>(), 10));
        }

        [Test]
        public void Constructor_RepeatedStop_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Line(1, new[] { a, b, a }, 10));
        }

        [Test]
        public void Constructor_SelfLoop_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Line(1, a, a, 10));
        }

        [Test]
        public void Constructor_NullStops_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Line(1, null, 10));
        }

        [Test]
        public void Constructor_NullStop_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Line(1, new[] { a, null, c }, 10));
        }

        [Test]
        public void Constructor_TwoStopsAtSamePosition_Throws()
        {
            Node twin = new Node(4, "Twin of A", 0, 0);

            Assert.Throws<ArgumentException>(() => new Line(1, new[] { a, twin }, 10));
        }

        // --- IndexOf ---

        [Test]
        public void IndexOf_ReturnsPositionInRoute()
        {
            Assert.That(line.IndexOf(a), Is.EqualTo(0));
            Assert.That(line.IndexOf(b), Is.EqualTo(1));
            Assert.That(line.IndexOf(c), Is.EqualTo(2));
        }

        [Test]
        public void IndexOf_NodeNotOnLine_ReturnsMinusOne()
        {
            Node outside = new Node(9, "Outside", 10, 10);

            Assert.That(line.IndexOf(outside), Is.EqualTo(-1));
            Assert.That(line.IndexOf(null), Is.EqualTo(-1));
        }

        // --- DistanceBetween ---

        [Test]
        public void DistanceBetween_IsTheSameInBothDirections()
        {
            Node d = new Node(4, "D", 8, 9);
            Line longer = new Line(2, new[] { a, b, c, d }, 10);
            Node[] stops = { a, b, c, d };

            foreach (Node from in stops)
            {
                foreach (Node to in stops)
                    Assert.That(longer.DistanceBetween(from, to), Is.EqualTo(longer.DistanceBetween(to, from)));
            }
        }

        [Test]
        public void DistanceBetween_NeighbouringStops_IsThatSegment()
        {
            Assert.That(line.DistanceBetween(b, c), Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void DistanceBetween_SameStop_IsZero()
        {
            Assert.That(line.DistanceBetween(b, b), Is.EqualTo(0f));
        }

        [Test]
        public void DistanceBetween_NodeNotOnLine_Throws()
        {
            Node outside = new Node(9, "Outside", 10, 10);

            Assert.Throws<ArgumentException>(() => line.DistanceBetween(a, outside));
        }

        // --- PathBetween ---

        [Test]
        public void PathBetween_Forward_ReturnsSegmentsInRouteOrder()
        {
            var path = line.PathBetween(a, c);

            Assert.That(path, Is.EqualTo(new[] { line.Segments[0], line.Segments[1] }));
        }

        [Test]
        public void PathBetween_Backward_ReturnsSegmentsReversedAndFlipped()
        {
            var path = line.PathBetween(c, a);

            Assert.That(path.Count, Is.EqualTo(2));

            Assert.That(path[0].From, Is.SameAs(c));
            Assert.That(path[0].To, Is.SameAs(b));
            Assert.That(path[0].Length, Is.EqualTo(3f).Within(0.001f));

            Assert.That(path[1].From, Is.SameAs(b));
            Assert.That(path[1].To, Is.SameAs(a));
            Assert.That(path[1].Length, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void PathBetween_FromMiddleStop_ContainsOnlyTheSegmentsBetween()
        {
            var forward = line.PathBetween(b, c);
            var backward = line.PathBetween(b, a);

            Assert.That(forward, Is.EqualTo(new[] { line.Segments[1] }));
            Assert.That(backward.Count, Is.EqualTo(1));
            Assert.That(backward[0].From, Is.SameAs(b));
            Assert.That(backward[0].To, Is.SameAs(a));
        }

        [Test]
        public void PathBetween_SameStop_IsEmpty()
        {
            Assert.That(line.PathBetween(b, b), Is.Empty);
        }

        [Test]
        public void PathBetween_NodeNotOnLine_Throws()
        {
            Node outside = new Node(9, "Outside", 10, 10);

            Assert.Throws<ArgumentException>(() => line.PathBetween(outside, a));
        }

        [Test]
        public void PathBetween_ConsecutiveSegmentsMeet()
        {
            // The View walks the path blindly: each segment must start where the previous one ended.
            Node d = new Node(4, "D", 8, 9);
            Line longer = new Line(2, new[] { a, b, c, d }, 10);

            foreach (var path in new[] { longer.PathBetween(a, d), longer.PathBetween(d, a) })
            {
                for (int i = 1; i < path.Count; i++)
                    Assert.That(path[i].From, Is.SameAs(path[i - 1].To));
            }
        }
    }
}
