using NUnit.Framework;
using ChugHaul.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChugHaul.Core.Tests
{
    public class FactorySystemTests
    {
        private CargoType wood;
        private CargoType planks;
        private CargoType food;
        private CargoType passengers;
        private CargoCatalog catalog;
        private Node town;      // has a factory: wood -> planks
        private Node plainCity; // wants wood, no factory
        private Network network;
        private EconomyConfig config;
        private DemandSystem demand;
        private DepotSystem depot;
        private FactorySystem factories;

        [SetUp]
        public void SetUp()
        {
            wood = new CargoType(1, "Wood", 6, 3);
            planks = new CargoType(2, "Planks", 12, 3);
            food = new CargoType(3, "Food", 10, 2);
            passengers = new CargoType(4, "Passengers", 5, 1, isPassengers: true);
            catalog = new CargoCatalog(new[] { wood, planks, food, passengers });

            town = new Node(1, "Town", 0, 0,
                demands: new[] { wood },
                factory: new Factory(wood, planks));
            plainCity = new Node(2, "City", 5, 0, demands: new[] { wood });

            network = new Network(new[] { town, plainCity }, new Line[0]);
            config = new EconomyConfig();
            demand = new DemandSystem(network, config);
            depot = new DepotSystem(network, catalog, config);
            factories = new FactorySystem(network, demand, depot, config);
        }

        private static int CountOf(List<Node> nodes, Node node)
        {
            return nodes.FindAll(n => ReferenceEquals(n, node)).Count;
        }

        private void DeliverWood(int amount)
        {
            for (int i = 0; i < amount; i++)
                Assert.That(demand.TryAccept(town, wood), Is.GreaterThan(0));
        }

        private void FillOutputDepot()
        {
            for (int i = 0; i < config.DepotCapacity; i++)
                Assert.That(depot.Add(town, planks), Is.True);
        }

        private int InputStored() => config.DemandSlots - demand.FreeSlots(town, wood);

        // ---------- Factory and Node rules ----------

        [Test]
        public void Factory_StoresInputAndOutput()
        {
            var f = new Factory(wood, planks);

            Assert.That(f.Input, Is.SameAs(wood));
            Assert.That(f.Output, Is.SameAs(planks));
        }

        [Test]
        public void Factory_NullCargo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Factory(null, planks));
            Assert.Throws<ArgumentNullException>(() => new Factory(wood, null));
        }

        [Test]
        public void Factory_SameInputAndOutput_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Factory(wood, wood));
        }

        [Test]
        public void Factory_WithPassengers_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Factory(passengers, planks));
            Assert.Throws<ArgumentException>(() => new Factory(wood, passengers));
        }

        [Test]
        public void Node_WithoutFactory_HasNone()
        {
            Assert.That(new Node(1, "A", 0, 0).Factory, Is.Null);
            Assert.That(plainCity.Factory, Is.Null);
            Assert.That(town.Factory, Is.Not.Null);
        }

        [Test]
        public void Node_FactoryInputNotInDemands_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "X", 0, 0,
                demands: new[] { food },
                factory: new Factory(wood, planks)));
        }

        [Test]
        public void Node_FactoryOutputInDemands_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "X", 0, 0,
                demands: new[] { wood, planks },
                factory: new Factory(wood, planks)));
        }

        [Test]
        public void Node_FactoryOnAnIndustry_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "X", 0, 0,
                produces: new[] { food },
                kind: NodeKind.Industry,
                factory: new Factory(wood, planks)));
        }

        // ---------- Making product ----------

        [Test]
        public void NoInput_NoOutput_AndStalledFiresOnce()
        {
            var stalled = new List<Node>();
            var produced = new List<Node>();
            factories.FactoryStalled += n => stalled.Add(n);
            factories.FactoryProduced += n => produced.Add(n);

            for (int i = 0; i < 5; i++)
                factories.Tick(config.FactoryInterval);

            Assert.That(depot.Count(town, planks), Is.EqualTo(0));
            Assert.That(CountOf(produced, town), Is.EqualTo(0));
            Assert.That(CountOf(stalled, town), Is.EqualTo(1)); // once, not every tick
        }

        [Test]
        public void TwoInput_TwoIntervals_TwoOutput_AndInputStockBackToZero()
        {
            DeliverWood(2);
            var produced = new List<Node>();
            factories.FactoryProduced += n => produced.Add(n);

            factories.Tick(2 * config.FactoryInterval);

            Assert.That(depot.Count(town, planks), Is.EqualTo(2));
            Assert.That(InputStored(), Is.EqualTo(0));
            Assert.That(demand.FreeSlots(town, wood), Is.EqualTo(config.DemandSlots));
            Assert.That(CountOf(produced, town), Is.EqualTo(2));
        }

        [Test]
        public void TwoInput_TwoSeparateIntervals_GiveTheSameResult()
        {
            DeliverWood(2);

            factories.Tick(config.FactoryInterval);
            Assert.That(depot.Count(town, planks), Is.EqualTo(1));
            Assert.That(InputStored(), Is.EqualTo(1));

            factories.Tick(config.FactoryInterval);
            Assert.That(depot.Count(town, planks), Is.EqualTo(2));
            Assert.That(InputStored(), Is.EqualTo(0));
        }

        [Test]
        public void BeforeTheInterval_NothingIsMade()
        {
            DeliverWood(1);

            factories.Tick(config.FactoryInterval - 0.5f);

            Assert.That(depot.Count(town, planks), Is.EqualTo(0));
            Assert.That(InputStored(), Is.EqualTo(1));
        }

        [Test]
        public void SmallTicks_AccumulateToOneUnit()
        {
            DeliverWood(1);

            for (int i = 0; i < 29; i++)
                factories.Tick(1f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(0));

            factories.Tick(1f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(1));
        }

        [Test]
        public void LeftoverTime_IsCarriedOver()
        {
            DeliverWood(2);

            factories.Tick(config.FactoryInterval * 1.5f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(1));

            factories.Tick(config.FactoryInterval * 0.5f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(2));
        }

        [Test]
        public void FactoryProduced_FiresAfterTheDepotIsUpdated()
        {
            DeliverWood(1);
            int countAtEvent = -1;
            factories.FactoryProduced += n => countAtEvent = depot.Count(town, planks);

            factories.Tick(config.FactoryInterval);

            Assert.That(countAtEvent, Is.EqualTo(1));
        }

        [Test]
        public void TakingInput_FreesADemandSlot_AndRaisesDemandChanged()
        {
            DeliverWood(2);
            var freeValues = new List<int>();
            demand.DemandChanged += (n, c, free) => freeValues.Add(free);

            factories.Tick(2 * config.FactoryInterval);

            Assert.That(freeValues, Is.EqualTo(new[] { 4, 5 }));
        }

        [Test]
        public void InputRunsOutInTheMiddleOfABigTick_StallsOnce()
        {
            DeliverWood(1);
            var stalled = new List<Node>();
            factories.FactoryStalled += n => stalled.Add(n);

            factories.Tick(3 * config.FactoryInterval);

            Assert.That(depot.Count(town, planks), Is.EqualTo(1));
            Assert.That(CountOf(stalled, town), Is.EqualTo(1));
        }

        // ---------- Output depot full ----------

        [Test]
        public void OutputDepotFull_InputIsNotConsumed_AndFactoryStallsOnce()
        {
            FillOutputDepot();
            DeliverWood(1);
            var stalled = new List<Node>();
            var produced = new List<Node>();
            factories.FactoryStalled += n => stalled.Add(n);
            factories.FactoryProduced += n => produced.Add(n);

            for (int i = 0; i < 5; i++)
                factories.Tick(config.FactoryInterval);

            Assert.That(InputStored(), Is.EqualTo(1));
            Assert.That(depot.Count(town, planks), Is.EqualTo(config.DepotCapacity));
            Assert.That(CountOf(produced, town), Is.EqualTo(0));
            Assert.That(CountOf(stalled, town), Is.EqualTo(1));
        }

        [Test]
        public void OutputDepotFillsInTheMiddleOfABigTick_StopsAtCapacityAndStalls()
        {
            DeliverWood(5);
            var small = new EconomyConfig { DepotCapacity = 2 };
            var d = new DepotSystem(network, catalog, small);
            var system = new FactorySystem(network, demand, d, small);
            var stalled = new List<Node>();
            system.FactoryStalled += n => stalled.Add(n);

            system.Tick(10 * small.FactoryInterval);

            Assert.That(d.Count(town, planks), Is.EqualTo(2));
            Assert.That(InputStored(), Is.EqualTo(3));
            Assert.That(CountOf(stalled, town), Is.EqualTo(1));
        }

        [Test]
        public void PickingUpOutput_FactoryResumesOnlyAfterAFullInterval()
        {
            FillOutputDepot();
            DeliverWood(1);
            factories.Tick(1f);                 // stalls: output depot is full
            depot.Take(town, planks, 1);        // pickup: one place free

            factories.Tick(config.FactoryInterval - 1f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(config.DepotCapacity - 1));
            Assert.That(InputStored(), Is.EqualTo(1));

            factories.Tick(1f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(config.DepotCapacity));
            Assert.That(InputStored(), Is.EqualTo(0));
        }

        [Test]
        public void AfterALongStall_APickupDoesNotCauseABurst()
        {
            FillOutputDepot();
            DeliverWood(5);
            factories.Tick(100 * config.FactoryInterval); // stalled for a long time
            depot.Take(town, planks, 3);

            factories.Tick(config.FactoryInterval);

            // One unit, not three: stalled time is not banked.
            Assert.That(depot.Count(town, planks), Is.EqualTo(config.DepotCapacity - 2));
            Assert.That(InputStored(), Is.EqualTo(4));
        }

        // ---------- Waiting for input ----------

        [Test]
        public void InputArrivingLater_ResumesWithAFreshTimer_AndResumedFiresOnce()
        {
            var resumed = new List<Node>();
            factories.FactoryResumed += n => resumed.Add(n);

            factories.Tick(1f);                 // no input: stalls
            DeliverWood(1);

            factories.Tick(config.FactoryInterval - 1f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(0));

            factories.Tick(1f);
            Assert.That(depot.Count(town, planks), Is.EqualTo(1));
            Assert.That(CountOf(resumed, town), Is.EqualTo(1));
        }

        [Test]
        public void Resumed_DoesNotFireWhileStillStalled()
        {
            var resumed = new List<Node>();
            factories.FactoryResumed += n => resumed.Add(n);

            factories.Tick(1f);
            factories.Tick(1f);

            Assert.That(resumed, Is.Empty);
        }

        [Test]
        public void StallsAgain_AfterResumingAndRunningOut()
        {
            var stalled = new List<Node>();
            factories.FactoryStalled += n => stalled.Add(n);

            factories.Tick(1f);                           // stall 1: no input
            DeliverWood(1);
            factories.Tick(config.FactoryInterval);       // resumes, makes 1
            factories.Tick(1f);                           // stall 2: input used up

            Assert.That(CountOf(stalled, town), Is.EqualTo(2));
        }

        // ---------- Demand does not eat the factory's input ----------

        [Test]
        public void CityDemandDoesNotConsumeTheFactoryInput()
        {
            DeliverWood(1);

            demand.Tick(10 * config.CityConsumeInterval);

            Assert.That(InputStored(), Is.EqualTo(1));
        }

        [Test]
        public void CityWithoutAFactory_StillConsumesItsDemandOverTime()
        {
            demand.TryAccept(plainCity, wood);

            demand.Tick(config.CityConsumeInterval);

            Assert.That(demand.FreeSlots(plainCity, wood), Is.EqualTo(config.DemandSlots));
        }

        [Test]
        public void CityWithoutAFactory_IsNotTouchedByFactories()
        {
            demand.TryAccept(plainCity, wood);

            factories.Tick(10 * config.FactoryInterval);

            Assert.That(demand.FreeSlots(plainCity, wood), Is.EqualTo(config.DemandSlots - 1));
            Assert.That(depot.Count(plainCity, planks), Is.EqualTo(0));
        }

        // ---------- Config and guards ----------

        [Test]
        public void CustomConfig_FactoryIntervalIsRespected()
        {
            var custom = new EconomyConfig { FactoryInterval = 10f };
            var d = new DepotSystem(network, catalog, custom);
            var system = new FactorySystem(network, demand, d, custom);
            DeliverWood(1);

            system.Tick(10f);

            Assert.That(d.Count(town, planks), Is.EqualTo(1));
        }

        [Test]
        public void NonPositiveInterval_Throws()
        {
            var bad = new EconomyConfig { FactoryInterval = 0f };

            Assert.Throws<ArgumentException>(
                () => new FactorySystem(network, demand, depot, bad));
        }

        [Test]
        public void NegativeTick_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => factories.Tick(-1f));
        }

        // ---------- Through Economy ----------

        private static (Economy eco, Node forest, Node city, CargoType w, CargoType p) BuildChain(
            GameState state)
        {
            var cfg = new EconomyConfig();
            var all = new CargoCatalog(cfg.Cargos);
            CargoType w = all.GetByName("Wood");
            CargoType p = all.GetByName("Planks");

            var forest = new Node(1, "Forest", 0, 0,
                produces: new[] { w }, kind: NodeKind.Industry);
            var city = new Node(2, "Sawmill town", 3, 4,
                demands: new[] { w },
                factory: new Factory(w, p));
            var chainLine = new Line(1, new[] { forest, city }, 50);
            var net = new Network(new[] { forest, city }, new[] { chainLine });

            return (new Economy(state, net, cfg), forest, city, w, p);
        }

        [Test]
        public void Economy_FactoryOutputShowsUpInTheOffer()
        {
            var (eco, forest, city, w, p) = BuildChain(new GameState(500));
            eco.Demand.TryAccept(city, w);

            eco.Tick(new EconomyConfig().FactoryInterval);

            var offer = eco.GetOffer(city);
            Assert.That(offer.Count, Is.EqualTo(1));
            Assert.That(offer[0].Cargo, Is.SameAs(p));
            Assert.That(offer[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void Chain_DeliveredRawMaterialIsPaid_ThenBecomesAProduct()
        {
            var state = new GameState(500);
            var (eco, forest, city, w, p) = BuildChain(state);
            eco.TryBuyLine(1);
            eco.TryBuyTrain(1);
            eco.Depot.Add(forest, w);
            Assert.That(
                eco.TryDispatch(1, city, new Cargo(new[] { w })),
                Is.EqualTo(DispatchResult.Ok));
            int coinsBefore = state.Coins;

            DeliveryResult result = eco.CompleteTrip(1);

            // The factory pays like any buyer: Wood is worth 6, first slot pays 150%.
            Assert.That(result.Total, Is.EqualTo(9));
            Assert.That(state.Coins, Is.EqualTo(coinsBefore + 9));
            Assert.That(eco.Demand.FreeSlots(city, w), Is.EqualTo(new EconomyConfig().DemandSlots - 1));

            eco.Tick(new EconomyConfig().FactoryInterval);

            Assert.That(eco.Depot.Count(city, p), Is.EqualTo(1));
            Assert.That(eco.Demand.FreeSlots(city, w), Is.EqualTo(new EconomyConfig().DemandSlots));
        }

        // ---------- TestMap ----------

        [Test]
        public void TestMap_HasFactoriesThatFormChains()
        {
            Network map = TestMap.Create();
            var withFactory = map.Nodes.Where(n => n.Factory != null).ToList();

            Assert.That(withFactory.Count, Is.GreaterThan(0));

            foreach (Node node in withFactory)
            {
                Factory f = node.Factory;
                Assert.That(node.Kind, Is.EqualTo(NodeKind.City), node.Name);
                Assert.That(node.Demands.Any(c => c.Id == f.Input.Id), Is.True, node.Name);

                // The raw material comes from an industry, and some other city wants the product.
                Assert.That(
                    map.Nodes.Any(n => n.Kind == NodeKind.Industry && n.Produces[0].Id == f.Input.Id),
                    Is.True, $"{f.Input.Name} has no industry");
                Assert.That(
                    map.Nodes.Any(n => !ReferenceEquals(n, node) &&
                                       n.Demands.Any(c => c.Id == f.Output.Id)),
                    Is.True, $"{f.Output.Name} is wanted by no other city");
            }
        }

        [Test]
        public void TestMap_FactoryChainRunsEndToEnd()
        {
            var state = new GameState(1000);
            Network map = TestMap.Create();
            var cfg = new EconomyConfig();
            var eco = new Economy(state, map, cfg);
            Node brasov = map.GetNode(2);
            CargoType wood = eco.Cargos.GetByName("Wood");
            CargoType planksCargo = eco.Cargos.GetByName("Planks");

            eco.Demand.TryAccept(brasov, wood);
            eco.Tick(cfg.FactoryInterval);

            Assert.That(eco.Depot.Count(brasov, planksCargo), Is.EqualTo(1));
        }
    }
}
