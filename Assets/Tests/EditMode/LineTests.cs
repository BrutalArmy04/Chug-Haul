using System;
using NUnit.Framework;

namespace ChugHaul.Core.Tests
{
    public class LineTests
    {
        private static Node N(int id, float x, float z) => new Node(id, "N" + id, x, z);

        // (0,0) -> (5,0) -> (5,3): segments of length 5 and 3
        private static Line ThreeStopLine(out Node a, out Node b, out Node c)
        {
            a = N(1, 0, 0);
            b = N(2, 5, 0);
            c = N(3, 5, 3);
            return new Line(1, new[] { a, b, c }, 10);
        }

        [Test]
        public void ThreeStopLine_LengthIsSumOfSegments()
        {
            var line = ThreeStopLine(out var a, out _, out var c);

            Assert.AreEqual(2, line.Segments.Count);
            Assert.AreEqual(5f, line.Segments[0].Length, 1e-4f);
            Assert.AreEqual(3f, line.Segments[1].Length, 1e-4f);
            Assert.AreEqual(8f, line.Length, 1e-4f);
            Assert.AreEqual(8f, line.DistanceBetween(a, c), 1e-4f);
        }

        [Test]
        public void DistanceBetween_IsSymmetric()
        {
            var line = ThreeStopLine(out var a, out var b, out var c);

            Assert.AreEqual(line.DistanceBetween(a, c), line.DistanceBetween(c, a), 1e-4f);
            Assert.AreEqual(line.DistanceBetween(a, b), line.DistanceBetween(b, a), 1e-4f);
            Assert.AreEqual(3f, line.DistanceBetween(b, c), 1e-4f);
        }

        [Test]
        public void PathBetween_Backwards_IsReversedAndFlipped()
        {
            var line = ThreeStopLine(out var a, out var b, out var c);

            var path = line.PathBetween(c, a);

            Assert.AreEqual(2, path.Count);
            Assert.AreSame(c, path[0].From);
            Assert.AreSame(b, path[0].To);
            Assert.AreSame(b, path[1].From);
            Assert.AreSame(a, path[1].To);
            Assert.AreEqual(3f, path[0].Length, 1e-4f);
            Assert.AreEqual(5f, path[1].Length, 1e-4f);
        }

        [Test]
        public void PathBetween_Forwards_IsInLineOrder()
        {
            var line = ThreeStopLine(out var a, out var b, out var c);

            var path = line.PathBetween(a, c);

            Assert.AreEqual(2, path.Count);
            Assert.AreSame(a, path[0].From);
            Assert.AreSame(b, path[0].To);
            Assert.AreSame(b, path[1].From);
            Assert.AreSame(c, path[1].To);
        }

        [Test]
        public void PathBetween_SameNode_IsEmpty()
        {
            var line = ThreeStopLine(out var a, out _, out _);
            Assert.AreEqual(0, line.PathBetween(a, a).Count);
        }

        [Test]
        public void TwoStopLine_BehavesLikeM1()
        {
            var a = N(1, 0, 0);
            var b = N(2, 3, 4);
            var line = new Line(1, new[] { a, b }, 5);

            Assert.AreEqual(5f, line.Length, 1e-4f);
            Assert.AreEqual(1, line.Segments.Count);
            Assert.AreEqual(5f, line.DistanceBetween(a, b), 1e-4f);
            Assert.IsFalse(line.IsActive);
#pragma warning disable CS0618
            Assert.AreSame(a, line.NodeA);
            Assert.AreSame(b, line.NodeB);
#pragma warning restore CS0618
        }

        [Test]
        public void OneStop_Throws()
        {
            var a = N(1, 0, 0);
            Assert.Throws<ArgumentException>(() => new Line(1, new[] { a }, 5));
        }

        [Test]
        public void RepeatedStop_Throws()
        {
            var a = N(1, 0, 0);
            var b = N(2, 1, 0);
            Assert.Throws<ArgumentException>(() => new Line(1, new[] { a, b, a }, 5));
        }

        [Test]
        public void NodeNotOnLine_Throws()
        {
            var line = ThreeStopLine(out var a, out _, out _);
            var stranger = N(99, 50, 50);

            Assert.AreEqual(-1, line.IndexOf(stranger));
            Assert.Throws<ArgumentException>(() => line.DistanceBetween(a, stranger));
            Assert.Throws<ArgumentException>(() => line.PathBetween(stranger, a));
        }
    }
}