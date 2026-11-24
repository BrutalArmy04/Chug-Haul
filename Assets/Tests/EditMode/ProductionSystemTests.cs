using NUnit.Framework;
using ChugHaul.Core;
using System;
using System.Collections.Generic;

namespace ChugHaul.Core.Tests
{
    public class ProductionSystemTests
    {
        private CargoType food;
        private CargoType wood;
        private CargoType steel;
        private CargoCatalog catalog;
        private Node farm;
        private Node forest;
        private Node city;
        private Network network;
        private EconomyConfig config;
        private DepotSystem depot;
        private ProductionSystem production;

        [SetUp]
        public void SetUp()
        {
            food = new CargoType(1, "Food", 10, 2);
            wood = new CargoType(2, "Wood", 6, 3);
            steel = new CargoType(3, "Steel", 15, 4);
            catalog = new CargoCatalog(new[] { food, wood, steel });

            farm = new Node(1, "Farm", 0, 0, produces: new[] { food }, kind: NodeKind.Industry);
            forest = new Node(2, "Forest", 5, 0, produces: new[] { wood }, kind: NodeKind.Industry);
            city = new Node(3, "City", 10, 0, demands: new[] { food, wood });

            network = new Network(new[] { farm, forest, city }, new Line[0]);
            config = new EconomyConfig();
            depot = new DepotSystem(network, catalog, config);
            production = new ProductionSystem(network, depot, config);
        }

        private static int CountFor(List<Node> nodes, Node node)
        {
            return nodes.FindAll(n => ReferenceEquals(n, node)).Count;
        }

        private void FillFarmDepot()
        {
            for (int i = 0; i < config.DepotCapacity; i++)
                Assert.That(depot.Add(farm, food), Is.True);
        }

        // ---------- Node kinds ----------

        [Test]
        public void Node_DefaultKindIsCity()
        {
            Assert.That(new Node(1, "A", 0, 0).Kind, Is.EqualTo(NodeKind.City));
        }

        [Test]
        public void Industry_WithOneProductAndNoDemands_IsValid()
        {
            var node = new Node(1, "Mine", 0, 0, produces: new[] { steel }, kind: NodeKind.Industry);

            Assert.That(node.Kind, Is.EqualTo(NodeKind.Industry));
            Assert.That(node.Produces.Count, Is.EqualTo(1));
        }

        [Test]
        public void Industry_WithTwoProducts_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "X", 0, 0,
                produces: new[] { food, wood }, kind: NodeKind.Industry));
        }

        [Test]
        public void Industry_WithNoProduct_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "X", 0, 0,
                kind: NodeKind.Industry));
        }

        [Test]
        public void Industry_WithDemands_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "X", 0, 0,
                produces: new[] { food },
                demands: new[] { wood },
                kind: NodeKind.Industry));
        }

        // ---------- Producing ----------

        [Test]
        public void AfterOneInterval_DepotGainsOneUnitAndProducedFires()
        {
            var seen = new List<(Node, CargoType)>();
            production.Produced += (n, c) => { if (ReferenceEquals(n, farm)) seen.Add((n, c)); };

            production.Tick(config.IndustryInterval);

            Assert.That(depot.Count(farm, food), Is.EqualTo(1));
            Assert.That(seen.Count, Is.EqualTo(1));
            Assert.That(seen[0].Item1, Is.SameAs(farm));
            Assert.That(seen[0].Item2, Is.SameAs(food));
        }

        [Test]
        public void BeforeTheInterval_NothingIsProduced()
        {
            production.Tick(config.IndustryInterval - 0.5f);

            Assert.That(depot.Count(farm, food), Is.EqualTo(0));
        }

        [Test]
        public void EachIndustryMakesItsOwnCargo()
        {
            production.Tick(config.IndustryInterval);

            Assert.That(depot.Count(farm, food), Is.EqualTo(1));
            Assert.That(depot.Count(forest, wood), Is.EqualTo(1));
            Assert.That(depot.Count(farm, wood), Is.EqualTo(0));
        }

        [Test]
        public void SmallTicks_AccumulateToOneUnit()
        {
            for (int i = 0; i < 19; i++)
                production.Tick(1f);
            Assert.That(depot.Count(farm, food), Is.EqualTo(0));

            production.Tick(1f);
            Assert.That(depot.Count(farm, food), Is.EqualTo(1));
        }

        [Test]
        public void OneBigTick_ProducesSeveralUnits()
        {
            int events = 0;
            production.Produced += (n, c) => { if (ReferenceEquals(n, farm)) events++; };

            production.Tick(3 * config.IndustryInterval);

            Assert.That(depot.Count(farm, food), Is.EqualTo(3));
            Assert.That(events, Is.EqualTo(3));
        }

        [Test]
        public void LeftoverTime_IsCarriedOver()
        {
            production.Tick(config.IndustryInterval * 1.5f);
            Assert.That(depot.Count(farm, food), Is.EqualTo(1));

            production.Tick(config.IndustryInterval * 0.5f);
            Assert.That(depot.Count(farm, food), Is.EqualTo(2));
        }

        [Test]
        public void Produced_FiresAfterTheDepotIsUpdated()
        {
            int countAtEvent = -1;
            production.Produced += (n, c) =>
            {
                if (ReferenceEquals(n, farm)) countAtEvent = depot.Count(farm, food);
            };

            production.Tick(config.IndustryInterval);

            Assert.That(countAtEvent, Is.EqualTo(1));
        }

        [Test]
        public void CustomConfig_IndustryIntervalIsRespected()
        {
            var custom = new EconomyConfig { IndustryInterval = 10f };
            var d = new DepotSystem(network, catalog, custom);
            var system = new ProductionSystem(network, d, custom);

            system.Tick(10f);

            Assert.That(d.Count(farm, food), Is.EqualTo(1));
        }

        [Test]
        public void NonPositiveInterval_Throws()
        {
            var bad = new EconomyConfig { IndustryInterval = 0f };

            Assert.Throws<ArgumentException>(() => new ProductionSystem(network, depot, bad));
        }

        [Test]
        public void NegativeTick_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => production.Tick(-1f));
        }

        // ---------- Cities ----------

        [Test]
        public void Cities_ProduceNothing_EvenIfTheyListCargo()
        {
            Node producerCity = new Node(9, "Odd city", 0, 0, produces: new[] { food });
            var net = new Network(new[] { producerCity, farm }, new Line[0]);
            var d = new DepotSystem(net, catalog, config);
            var system = new ProductionSystem(net, d, config);
            var produced = new List<Node>();
            system.Produced += (n, c) => produced.Add(n);

            system.Tick(10 * config.IndustryInterval);

            Assert.That(d.Count(producerCity, food), Is.EqualTo(0));
            Assert.That(CountFor(produced, producerCity), Is.EqualTo(0));
            Assert.That(CountFor(produced, farm), Is.GreaterThan(0)); // the industry still does
        }

        // ---------- Stalling ----------

        [Test]
        public void FullDepot_NoProduction_StalledFiresOnce()
        {
            FillFarmDepot();
            var stalled = new List<Node>();
            var produced = new List<Node>();
            production.ProductionStalled += n => stalled.Add(n);
            production.Produced += (n, c) => produced.Add(n);

            for (int i = 0; i < 5; i++)
                production.Tick(config.IndustryInterval);

            Assert.That(depot.Count(farm, food), Is.EqualTo(config.DepotCapacity));
            Assert.That(CountFor(produced, farm), Is.EqualTo(0));
            Assert.That(CountFor(stalled, farm), Is.EqualTo(1)); // once, not every tick
        }

        [Test]
        public void ProductionThatFillsTheDepot_StopsAtCapacityAndStallsOnce()
        {
            var small = new EconomyConfig { DepotCapacity = 2 };
            var d = new DepotSystem(network, catalog, small);
            var system = new ProductionSystem(network, d, small);
            var stalled = new List<Node>();
            system.ProductionStalled += n => stalled.Add(n);

            system.Tick(10 * small.IndustryInterval);
            system.Tick(small.IndustryInterval);

            Assert.That(d.Count(farm, food), Is.EqualTo(2));
            Assert.That(CountFor(stalled, farm), Is.EqualTo(1));
        }

        [Test]
        public void StalledIndustry_DoesNotAffectOthers()
        {
            FillFarmDepot();

            production.Tick(config.IndustryInterval);

            Assert.That(depot.Count(forest, wood), Is.EqualTo(1));
        }

        [Test]
        public void NothingStalled_WhileThereIsRoom()
        {
            var stalled = new List<Node>();
            production.ProductionStalled += n => stalled.Add(n);

            production.Tick(config.IndustryInterval);

            Assert.That(stalled, Is.Empty);
        }

        // ---------- Resuming ----------

        [Test]
        public void AfterAPickup_ProductionResumesOnlyAfterAFullInterval()
        {
            FillFarmDepot();
            production.Tick(1f);                       // stalls
            depot.Take(farm, food, 1);                 // pickup: one place free

            production.Tick(config.IndustryInterval - 1f);
            Assert.That(depot.Count(farm, food), Is.EqualTo(config.DepotCapacity - 1));

            production.Tick(1f);
            Assert.That(depot.Count(farm, food), Is.EqualTo(config.DepotCapacity));
        }

        [Test]
        public void TheTickThatFindsRoom_CountsTowardsTheNewInterval()
        {
            FillFarmDepot();
            production.Tick(1f);
            depot.Take(farm, food, 1);

            production.Tick(config.IndustryInterval);

            Assert.That(depot.Count(farm, food), Is.EqualTo(config.DepotCapacity));
        }

        [Test]
        public void AfterALongStall_APickupDoesNotCauseABurst()
        {
            FillFarmDepot();
            production.Tick(100 * config.IndustryInterval); // stalled for a long time
            depot.Take(farm, food, 3);

            production.Tick(config.IndustryInterval);

            // One unit, not three: stalled time is not banked.
            Assert.That(depot.Count(farm, food), Is.EqualTo(config.DepotCapacity - 2));
        }

        [Test]
        public void Resumed_FiresOnceWhenRoomAppears_NotWhileStillFull()
        {
            FillFarmDepot();
            var resumed = new List<Node>();
            production.ProductionResumed += n => resumed.Add(n);

            production.Tick(1f);
            production.Tick(1f);
            Assert.That(CountFor(resumed, farm), Is.EqualTo(0));

            depot.Take(farm, food, 1);
            production.Tick(1f);
            production.Tick(1f);

            Assert.That(CountFor(resumed, farm), Is.EqualTo(1));
        }

        [Test]
        public void StallsAgain_AfterResumingAndFillingUp()
        {
            FillFarmDepot();
            var stalled = new List<Node>();
            production.ProductionStalled += n => stalled.Add(n);

            production.Tick(1f);                              // stall 1
            depot.Take(farm, food, 1);
            production.Tick(config.IndustryInterval);         // resumes, refills
            production.Tick(1f);                              // stall 2

            Assert.That(CountFor(stalled, farm), Is.EqualTo(2));
        }

        // ---------- Economy ----------

        [Test]
        public void EconomyTick_DrivesProduction()
        {
            var cfg = new EconomyConfig();
            var all = new CargoCatalog(cfg.Cargos);
            CargoType grain = all.GetByName("Grain");
            Node ownFarm = new Node(1, "Farm", 0, 0, produces: new[] { grain }, kind: NodeKind.Industry);
            var net = new Network(new[] { ownFarm }, new Line[0]);
            var economy = new Economy(new GameState(100), net, cfg);

            economy.Tick(cfg.IndustryInterval);

            Assert.That(economy.Depot.Count(ownFarm, grain), Is.EqualTo(1));
            Assert.That(economy.GetOffer(ownFarm).Count, Is.EqualTo(1));
        }
    }
}
