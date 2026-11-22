using NUnit.Framework;
using ChugHaul.Core;
using System;
using System.Collections.Generic;

namespace ChugHaul.Core.Tests
{
    public class DepotSystemTests
    {
        private CargoType food;
        private CargoType mail;
        private CargoType steel;
        private CargoCatalog catalog;
        private Node nodeA;
        private Node nodeB;
        private Network network;
        private EconomyConfig config;
        private DepotSystem depot;

        [SetUp]
        public void SetUp()
        {
            food = new CargoType(1, "Food", 10, 2);
            mail = new CargoType(2, "Mail", 8, 1);
            steel = new CargoType(3, "Steel", 15, 4);
            catalog = new CargoCatalog(new[] { food, mail, steel });

            nodeA = new Node(1, "A", 0, 0);
            nodeB = new Node(2, "B", 5, 0);
            network = new Network(new[] { nodeA, nodeB }, new Line[0]);

            config = new EconomyConfig();
            depot = new DepotSystem(network, catalog, config);
        }

        // ---------- Counts ----------

        [Test]
        public void NewDepot_IsEmpty()
        {
            Assert.That(depot.Count(nodeA, food), Is.EqualTo(0));
            Assert.That(depot.GetOffer(nodeA), Is.Empty);
        }

        [Test]
        public void Add_IncrementsCountAndFiresEvent()
        {
            var seen = new List<(Node, CargoType, int)>();
            depot.DepotChanged += (n, c, count) => seen.Add((n, c, count));

            bool added = depot.Add(nodeA, food);

            Assert.That(added, Is.True);
            Assert.That(depot.Count(nodeA, food), Is.EqualTo(1));
            Assert.That(seen.Count, Is.EqualTo(1));
            Assert.That(seen[0].Item1, Is.SameAs(nodeA));
            Assert.That(seen[0].Item2, Is.SameAs(food));
            Assert.That(seen[0].Item3, Is.EqualTo(1));
        }

        [Test]
        public void Add_PastCapacity_ReturnsFalseAndCountStaysAtCapacity()
        {
            for (int i = 0; i < config.DepotCapacity; i++)
                Assert.That(depot.Add(nodeA, food), Is.True);

            int events = 0;
            depot.DepotChanged += (n, c, count) => events++;

            bool added = depot.Add(nodeA, food);

            Assert.That(added, Is.False);
            Assert.That(depot.Count(nodeA, food), Is.EqualTo(config.DepotCapacity));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void Capacity_IsPerCargo_AndPerNode()
        {
            for (int i = 0; i < config.DepotCapacity; i++)
                depot.Add(nodeA, food);

            Assert.That(depot.Add(nodeA, mail), Is.True);
            Assert.That(depot.Add(nodeB, food), Is.True);
        }

        [Test]
        public void CustomConfig_DepotCapacityIsRespected()
        {
            var small = new DepotSystem(network, catalog, new EconomyConfig { DepotCapacity = 2 });

            Assert.That(small.Add(nodeA, food), Is.True);
            Assert.That(small.Add(nodeA, food), Is.True);
            Assert.That(small.Add(nodeA, food), Is.False);
            Assert.That(small.Count(nodeA, food), Is.EqualTo(2));
        }

        [Test]
        public void Take_RemovesUnitsAndFiresEvent()
        {
            for (int i = 0; i < 3; i++)
                depot.Add(nodeA, food);
            var seen = new List<int>();
            depot.DepotChanged += (n, c, count) => seen.Add(count);

            bool taken = depot.Take(nodeA, food, 2);

            Assert.That(taken, Is.True);
            Assert.That(depot.Count(nodeA, food), Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Take_MoreThanStored_ReturnsFalseAndChangesNothing()
        {
            depot.Add(nodeA, food);
            int events = 0;
            depot.DepotChanged += (n, c, count) => events++;

            bool taken = depot.Take(nodeA, food, 2);

            Assert.That(taken, Is.False);
            Assert.That(depot.Count(nodeA, food), Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void Take_NonPositiveAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => depot.Take(nodeA, food, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => depot.Take(nodeA, food, -1));
        }

        [Test]
        public void Depots_AreIndependentPerNode()
        {
            depot.Add(nodeA, food);

            Assert.That(depot.Count(nodeA, food), Is.EqualTo(1));
            Assert.That(depot.Count(nodeB, food), Is.EqualTo(0));
        }

        // ---------- Offer ----------

        [Test]
        public void Offer_ShowsStoredCargoWithCount()
        {
            for (int i = 0; i < 3; i++)
                depot.Add(nodeA, food);

            var offer = depot.GetOffer(nodeA);

            Assert.That(offer.Count, Is.EqualTo(1));
            Assert.That(offer[0].Cargo, Is.SameAs(food));
            Assert.That(offer[0].Count, Is.EqualTo(3));
        }

        [Test]
        public void Offer_ListsOneItemPerCargoInCatalogOrder_SkippingEmpty()
        {
            depot.Add(nodeA, steel);
            depot.Add(nodeA, food);
            depot.Add(nodeA, food);

            var offer = depot.GetOffer(nodeA);

            Assert.That(offer.Count, Is.EqualTo(2));
            Assert.That(offer[0].Cargo, Is.SameAs(food));
            Assert.That(offer[0].Count, Is.EqualTo(2));
            Assert.That(offer[1].Cargo, Is.SameAs(steel));
            Assert.That(offer[1].Count, Is.EqualTo(1));
        }

        [Test]
        public void Offer_ChangesWhenCargoIsTaken()
        {
            depot.Add(nodeA, food);
            depot.Take(nodeA, food, 1);

            Assert.That(depot.GetOffer(nodeA), Is.Empty);
        }

        // ---------- Taking many ----------

        [Test]
        public void CanTake_ChecksEverythingAndChangesNothing()
        {
            depot.Add(nodeA, food);
            depot.Add(nodeA, food);

            Assert.That(depot.CanTake(nodeA, new[] { new OfferItem(food, 2) }), Is.True);
            Assert.That(depot.CanTake(nodeA, new[] { new OfferItem(food, 3) }), Is.False);
            Assert.That(depot.Count(nodeA, food), Is.EqualTo(2));
        }

        [Test]
        public void CanTake_SumsRepeatedCargo()
        {
            depot.Add(nodeA, food);
            depot.Add(nodeA, food);

            var items = new[] { new OfferItem(food, 1), new OfferItem(food, 1), new OfferItem(food, 1) };

            Assert.That(depot.CanTake(nodeA, items), Is.False);
        }

        [Test]
        public void TryTakeMany_IsAllOrNothing()
        {
            depot.Add(nodeA, food);
            depot.Add(nodeA, food);
            int events = 0;
            depot.DepotChanged += (n, c, count) => events++;

            // Food is in stock, mail is not.
            bool taken = depot.TryTakeMany(nodeA,
                new[] { new OfferItem(food, 2), new OfferItem(mail, 1) });

            Assert.That(taken, Is.False);
            Assert.That(depot.Count(nodeA, food), Is.EqualTo(2));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void TryTakeMany_UpdatesEverythingBeforeRaisingEvents()
        {
            depot.Add(nodeA, food);
            depot.Add(nodeA, food);
            depot.Add(nodeA, mail);

            var foodSeenAtFirstEvent = -1;
            var mailSeenAtFirstEvent = -1;
            var eventCargos = new List<CargoType>();
            depot.DepotChanged += (n, c, count) =>
            {
                if (eventCargos.Count == 0)
                {
                    foodSeenAtFirstEvent = depot.Count(nodeA, food);
                    mailSeenAtFirstEvent = depot.Count(nodeA, mail);
                }
                eventCargos.Add(c);
            };

            bool taken = depot.TryTakeMany(nodeA,
                new[] { new OfferItem(food, 1), new OfferItem(mail, 1), new OfferItem(food, 1) });

            Assert.That(taken, Is.True);
            Assert.That(foodSeenAtFirstEvent, Is.EqualTo(0));
            Assert.That(mailSeenAtFirstEvent, Is.EqualTo(0));
            Assert.That(eventCargos, Is.EqualTo(new[] { food, mail })); // once per cargo type
        }

        // ---------- Unknown things ----------

        [Test]
        public void UnknownCargo_ThrowsOnAdd_AndCannotBeTaken()
        {
            var stranger = new CargoType(99, "Stranger", 1, 1);

            Assert.Throws<ArgumentException>(() => depot.Add(nodeA, stranger));
            Assert.That(depot.CanTake(nodeA, new[] { new OfferItem(stranger, 1) }), Is.False);
        }

        [Test]
        public void NodeOutsideNetwork_Throws()
        {
            var outside = new Node(99, "Outside", 0, 0);

            Assert.Throws<ArgumentException>(() => depot.Add(outside, food));
            Assert.Throws<ArgumentException>(() => depot.GetOffer(outside));
        }

        [Test]
        public void NegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new DepotSystem(network, catalog, new EconomyConfig { DepotCapacity = -1 }));
        }

        // ---------- Cargo (a load) ----------

        [Test]
        public void Cargo_KeepsWagonOrder()
        {
            var load = new Cargo(new[] { steel, food, food });

            Assert.That(load.Count, Is.EqualTo(3));
            Assert.That(load[0], Is.SameAs(steel));
            Assert.That(load[1], Is.SameAs(food));
            Assert.That(load[2], Is.SameAs(food));
        }

        [Test]
        public void Cargo_DefaultIsEmpty()
        {
            Assert.That(new Cargo().Count, Is.EqualTo(0));
            Assert.That(Cargo.Empty.Count, Is.EqualTo(0));
        }

        [Test]
        public void Cargo_IsNotAffectedByLaterChangesToTheSourceArray()
        {
            var source = new[] { food, mail };
            var load = new Cargo(source);

            source[0] = steel;

            Assert.That(load[0], Is.SameAs(food));
        }

        [Test]
        public void Cargo_NullWagon_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Cargo(new CargoType[] { food, null }));
        }
    }
}
