using System.Linq;
using NUnit.Framework;

namespace ChugHaul.Core.Tests
{
    public class NetworkLinesTests
    {
        [Test]
        public void LinesAt_FindsLineWhereNodeIsMiddleStop()
        {
            var a = new Node(1, "A", 0, 0);
            var b = new Node(2, "B", 5, 0);
            var c = new Node(3, "C", 5, 3);
            var line = new Line(1, new[] { a, b, c }, 10);
            var network = new Network(new[] { a, b, c }, new[] { line });

            Assert.AreEqual(1, network.LinesAt(b).Count);
            Assert.AreSame(line, network.LinesAt(b)[0]);
        }

        [Test]
        public void LinesAt_FindsEveryLineThroughASharedNode()
        {
            var network = TestMap.Create();
            var brasov = network.GetNode(2);

            // Brasov: end of line 1, start of line 2, middle of line 5.
            var ids = network.LinesAt(brasov).Select(l => l.Id).OrderBy(i => i).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 2, 5 }, ids);
        }

        [Test]
        public void TestMap_HasAFourStopLine()
        {
            var network = TestMap.Create();
            Assert.IsTrue(network.Lines.Any(l => l.Stops.Count == 4));
        }

        [Test]
        public void Network_RejectsLineWithStopOutsideNetwork()
        {
            var a = new Node(1, "A", 0, 0);
            var b = new Node(2, "B", 1, 0);
            var stranger = new Node(3, "C", 2, 0);
            var line = new Line(1, new[] { a, b, stranger }, 5);

            Assert.Throws<System.ArgumentException>(
                () => new Network(new[] { a, b }, new[] { line }));
        }
    }
}
