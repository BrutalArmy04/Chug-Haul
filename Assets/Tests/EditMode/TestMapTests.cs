using NUnit.Framework;
using ChugHaul.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChugHaul.Core.Tests
{
    public class TestMapTests
    {
        private Network map;
        private CargoCatalog catalog;

        // The sketch posted in #core. If the map changes, update the sketch and this table.
        private static readonly (int id, string name, NodeKind kind, float x, float z)[] SketchNodes =
        {
            (1, "Bucharest", NodeKind.City, 0f, 0f),
            (2, "Brasov", NodeKind.City, 2f, 14f),
            (3, "Sibiu", NodeKind.City, 16f, 14f),
            (4, "Cluj", NodeKind.City, 18f, 0f),
            (5, "Farm", NodeKind.Industry, -6f, -4f),
            (6, "Forest", NodeKind.Industry, 24f, 6f),
            (7, "Mine", NodeKind.Industry, 22f, 18f),
        };

        private static readonly (int id, int[] stops, int cost)[] SketchLines =
        {
            (3, new[] { 3, 4 }, 25),
            (4, new[] { 1, 4 }, 40),
            (5, new[] { 5, 1, 2, 3 }, 70),
            (6, new[] { 6, 4 }, 20),
            (7, new[] { 7, 3 }, 20),
        };

        [SetUp]
        public void SetUp()
        {
            map = TestMap.Create();
            catalog = new CargoCatalog(new EconomyConfig().Cargos);
        }

        // How many lines you have to ride to get from one node to another (0 = same node,
        // 1 = one line serves both, 2 = one transfer, ...). -1 if the nodes are not connected.
        private int LinesNeeded(Node from, Node to)
        {
            if (ReferenceEquals(from, to)) return 0;

            var distance = new Dictionary<int, int> { { from.Id, 0 } };
            var queue = new Queue<Node>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();

                foreach (Line line in map.LinesAt(current))
                {
                    foreach (Node next in line.Stops)
                    {
                        if (distance.ContainsKey(next.Id)) continue;

                        distance[next.Id] = distance[current.Id] + 1;
                        if (ReferenceEquals(next, to)) return distance[next.Id];
                        queue.Enqueue(next);
                    }
                }
            }

            return -1;
        }

        private IEnumerable<Node> Industries => map.Nodes.Where(n => n.Kind == NodeKind.Industry);
        private IEnumerable<Node> Cities => map.Nodes.Where(n => n.Kind == NodeKind.City);

        private bool Wants(Node node, CargoType cargo) => node.Demands.Any(c => c.Id == cargo.Id);

        // ---------- The map is valid and matches the sketch ----------

        [Test]
        public void Network_AcceptsTheMap()
        {
            Assert.DoesNotThrow(() => TestMap.Create());
        }

        [Test]
        public void Nodes_MatchTheSketch()
        {
            Assert.That(map.Nodes.Count, Is.EqualTo(SketchNodes.Length));

            foreach (var expected in SketchNodes)
            {
                Node node = map.GetNode(expected.id);

                Assert.That(node, Is.Not.Null, $"node {expected.id}");
                Assert.That(node.Name, Is.EqualTo(expected.name));
                Assert.That(node.Kind, Is.EqualTo(expected.kind), expected.name);
                Assert.That(node.X, Is.EqualTo(expected.x), expected.name);
                Assert.That(node.Z, Is.EqualTo(expected.z), expected.name);
            }
        }

        [Test]
        public void Lines_MatchTheSketch()
        {
            Assert.That(map.Lines.Count, Is.EqualTo(SketchLines.Length));

            foreach (var expected in SketchLines)
            {
                Line line = map.GetLine(expected.id);

                Assert.That(line, Is.Not.Null, $"line {expected.id}");
                Assert.That(line.Stops.Select(n => n.Id).ToArray(), Is.EqualTo(expected.stops),
                    $"line {expected.id}");
                Assert.That(line.Cost, Is.EqualTo(expected.cost), $"line {expected.id}");
            }
        }

        [Test]
        public void NoTwoLinesShareASegment()
        {
            // Two lines on the same track overlap on screen and can't be told apart when clicked.
            var seen = new HashSet<(int, int)>();

            foreach (Line line in map.Lines)
            {
                foreach (Segment segment in line.Segments)
                {
                    int a = Math.Min(segment.From.Id, segment.To.Id);
                    int b = Math.Max(segment.From.Id, segment.To.Id);
                    Assert.That(seen.Add((a, b)), Is.True, $"segment {a}-{b} is on more than one line");
                }
            }
        }

        [Test]
        public void EverySegmentIsLongEnoughToClick()
        {
            // A node's building is about 3.2 units wide, so on a shorter segment the two
            // buildings' colliders cover the whole line and it can't be clicked.
            const float MinLength = 7f;

            foreach (Line line in map.Lines)
            {
                foreach (Segment segment in line.Segments)
                {
                    Assert.That(segment.Length, Is.GreaterThanOrEqualTo(MinLength),
                        $"line {line.Id}: {segment.From.Name} - {segment.To.Name}");
                }
            }
        }

        [Test]
        public void Map_HasFourCitiesThreeIndustriesAndTwoFactories()
        {
            Assert.That(Cities.Count(), Is.EqualTo(4));
            Assert.That(Industries.Count(), Is.EqualTo(3));
            Assert.That(map.Nodes.Count(n => n.Factory != null), Is.EqualTo(2));
        }

        [Test]
        public void Industries_MakeGrainLogsAndOre()
        {
            var made = Industries.Select(n => n.Produces[0].Name).OrderBy(name => name).ToArray();

            Assert.That(made, Is.EqualTo(new[] { "Grain", "Logs", "Ore" }));
        }

        [Test]
        public void Factories_TurnGrainIntoFoodAndLogsIntoLumber()
        {
            var recipes = map.Nodes
                .Where(n => n.Factory != null)
                .Select(n => n.Factory.Input.Name + " -> " + n.Factory.Output.Name)
                .OrderBy(text => text)
                .ToArray();

            Assert.That(recipes, Is.EqualTo(new[] { "Grain -> Food", "Logs -> Lumber" }));
        }

        [Test]
        public void EveryNodeIsOnALine_AndTheMapIsOneConnectedNetwork()
        {
            Node start = map.GetNode(1);

            foreach (Node node in map.Nodes)
            {
                Assert.That(map.LinesAt(node).Count, Is.GreaterThan(0), node.Name);
                Assert.That(LinesNeeded(start, node), Is.GreaterThanOrEqualTo(0), node.Name);
            }
        }

        [Test]
        public void Map_HasALineWithFourOrMoreStops()
        {
            Assert.That(map.Lines.Any(l => l.Stops.Count >= 4), Is.True);
        }

        [Test]
        public void SomeNodeIsSharedByTwoLines()
        {
            Assert.That(map.Nodes.Any(n => map.LinesAt(n).Count >= 2), Is.True);
        }

        // ---------- Cargo ----------

        [Test]
        public void EveryCargoIsProducedSomewhereAndWantedSomewhere()
        {
            foreach (CargoType cargo in catalog.Cargos)
            {
                // Passengers and mail have their own tasks, and nothing makes them yet.
                if (cargo.IsPassengers || cargo.Name == "Mail") continue;

                bool produced = map.Nodes.Any(n =>
                    n.Produces.Any(c => c.Id == cargo.Id) ||
                    (n.Factory != null && n.Factory.Output.Id == cargo.Id));
                bool wanted = map.Nodes.Any(n => Wants(n, cargo));

                Assert.That(produced, Is.True, $"{cargo.Name} is never produced");
                Assert.That(wanted, Is.True, $"{cargo.Name} is never wanted");
            }
        }

        // ---------- Chains ----------

        [Test]
        public void FoodChain_FarmToFoodPlantToATownThatWantsFood_IsReachable()
        {
            CargoType grain = catalog.GetByName("Grain");
            CargoType food = catalog.GetByName("Food");

            Node farm = Industries.Single(n => n.Produces[0].Id == grain.Id);
            Node plant = map.Nodes.Single(n => n.Factory != null && n.Factory.Input.Id == grain.Id);
            var towns = Cities.Where(n => Wants(n, food)).ToList();

            Assert.That(plant.Factory.Output.Id, Is.EqualTo(food.Id));
            Assert.That(LinesNeeded(farm, plant), Is.GreaterThanOrEqualTo(1));
            Assert.That(towns, Is.Not.Empty);
            Assert.That(towns.Any(town => LinesNeeded(plant, town) >= 1), Is.True);
        }

        [Test]
        public void EveryIndustry_HasACompleteChainToATownThatWantsTheEndProduct()
        {
            foreach (Node industry in Industries)
            {
                CargoType raw = industry.Produces[0];

                // Either a town wants the raw material itself, or a factory turns it into
                // something a town wants. Either way every step must be reachable.
                bool complete = false;

                foreach (Node town in Cities.Where(n => Wants(n, raw)))
                {
                    if (town.Factory == null)
                    {
                        complete |= LinesNeeded(industry, town) >= 1;
                        continue;
                    }

                    // The town is a factory: the product has to reach some buyer.
                    CargoType product = town.Factory.Output;
                    foreach (Node buyer in Cities.Where(n => !ReferenceEquals(n, town) && Wants(n, product)))
                    {
                        complete |= LinesNeeded(industry, town) >= 1 &&
                                    LinesNeeded(town, buyer) >= 1;
                    }
                }

                Assert.That(complete, Is.True, $"{industry.Name} ({raw.Name}) has no complete chain");
            }
        }

        [Test]
        public void FoodFromBrasovToCluj_NeedsATransfer()
        {
            CargoType food = catalog.GetByName("Food");
            Node brasov = map.GetNode(2);
            Node cluj = map.GetNode(4);

            // No single line serves both, but they are connected through another line.
            Assert.That(map.LinesAt(brasov).Intersect(map.LinesAt(cluj)), Is.Empty);
            Assert.That(LinesNeeded(brasov, cluj), Is.EqualTo(2));
            Assert.That(Wants(cluj, food), Is.True);

            // A shared node where the Food can be dropped: it sits on a line with Brasov and on
            // a line with Cluj, and does not buy Food itself (so the Food lands in its depot).
            var transferNodes = map.Nodes.Where(n =>
                !ReferenceEquals(n, brasov) && !ReferenceEquals(n, cluj) &&
                map.LinesAt(n).Intersect(map.LinesAt(brasov)).Any() &&
                map.LinesAt(n).Intersect(map.LinesAt(cluj)).Any() &&
                !Wants(n, food)).ToList();

            Assert.That(transferNodes, Is.Not.Empty);
            Assert.That(transferNodes.Select(n => n.Name), Does.Contain("Bucharest"));
        }

        [Test]
        public void OreFromTheMineToBucharest_NeedsATransferAtSibiu()
        {
            Node mine = map.GetNode(7);
            Node sibiu = map.GetNode(3);
            Node bucharest = map.GetNode(1);
            CargoType ore = catalog.GetByName("Ore");

            Assert.That(Wants(bucharest, ore), Is.True);
            Assert.That(Wants(sibiu, ore), Is.False); // so Ore dropped at Sibiu waits in its depot
            Assert.That(map.LinesAt(mine).Intersect(map.LinesAt(bucharest)), Is.Empty);
            Assert.That(LinesNeeded(mine, bucharest), Is.EqualTo(2));
        }

        // ---------- Playing it ----------

        [Test]
        public void Playthrough_FarmToFactoryToTransferToTown()
        {
            var state = new GameState(1000);
            var cfg = new EconomyConfig();
            var eco = new Economy(state, map, cfg);

            Node farm = map.GetNode(5);
            Node brasov = map.GetNode(2);
            Node bucharest = map.GetNode(1);
            Node cluj = map.GetNode(4);
            CargoType grain = eco.Cargos.GetByName("Grain");
            CargoType food = eco.Cargos.GetByName("Food");

            Assert.That(eco.TryBuyLine(5), Is.EqualTo(BuyResult.Ok));
            Assert.That(eco.TryBuyTrain(5), Is.EqualTo(BuyResult.Ok));
            Assert.That(eco.TryBuyLine(4), Is.EqualTo(BuyResult.Ok));
            Assert.That(eco.TryBuyTrain(4), Is.EqualTo(BuyResult.Ok));

            // 1. The farm makes Grain.
            eco.Tick(cfg.IndustryInterval);
            Assert.That(eco.Depot.Count(farm, grain), Is.EqualTo(1));

            // 2. Line 5 takes it to Brasov. The factory pays for it like any buyer.
            Assert.That(eco.TryDispatch(5, brasov, new Cargo(new[] { grain })),
                Is.EqualTo(DispatchResult.Ok));
            int coins = state.Coins;
            DeliveryResult grainSale = eco.CompleteTrip(5);
            Assert.That(grainSale.Total, Is.EqualTo(9)); // Grain is worth 6, first slot pays 150%
            Assert.That(state.Coins, Is.EqualTo(coins + 9));

            // 3. The factory turns the Grain into Food and frees the demand slot.
            eco.Tick(cfg.FactoryInterval);
            Assert.That(eco.Depot.Count(brasov, food), Is.EqualTo(1));
            Assert.That(eco.Demand.FreeSlots(brasov, grain), Is.EqualTo(cfg.DemandSlots));

            // 4. Line 5 carries the Food to Bucharest. Bucharest does not want Food:
            //    it is unpaid and waits in Bucharest's depot (the transfer point).
            Assert.That(eco.TryDispatch(5, bucharest, new Cargo(new[] { food })),
                Is.EqualTo(DispatchResult.Ok));
            DeliveryResult drop = eco.CompleteTrip(5);
            Assert.That(drop.Total, Is.EqualTo(0));
            Assert.That(drop.Wagons[0].StoredInDepot, Is.True);
            Assert.That(eco.Depot.Count(bucharest, food), Is.EqualTo(1));
            Assert.That(eco.Depot.Count(brasov, food), Is.EqualTo(0));

            // 5. Line 4 picks it up there and delivers it to Cluj, which wants Food.
            Assert.That(eco.TryDispatch(4, cluj, new Cargo(new[] { food })),
                Is.EqualTo(DispatchResult.Ok));
            coins = state.Coins;
            DeliveryResult sale = eco.CompleteTrip(4);
            Assert.That(sale.Total, Is.EqualTo(15)); // Food is worth 10, first slot pays 150%
            Assert.That(state.Coins, Is.EqualTo(coins + 15));
            Assert.That(eco.Depot.Count(bucharest, food), Is.EqualTo(0));
            Assert.That(eco.Demand.FreeSlots(cluj, food), Is.EqualTo(cfg.DemandSlots - 1));
        }
    }
}
