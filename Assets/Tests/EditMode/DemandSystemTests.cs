using NUnit.Framework;
using ChugHaul.Core;
using System;
using System.Collections.Generic;

namespace ChugHaul.Core.Tests
{
    public class DemandSystemTests
    {
        private CargoType passengers;
        private CargoType mail;
        private CargoType food;
        private Node city;
        private Node farm;
        private Network network;
        private EconomyConfig config;
        private DemandSystem demand;

        [SetUp]
        public void SetUp()
        {
            passengers = new CargoType(1, "Passengers", 5, 1, isPassengers: true);
            mail = new CargoType(2, "Mail", 8, 1);
            food = new CargoType(3, "Food", 10, 2);

            // City wants food and passengers, produces mail.
            city = new Node(1, "City", 0, 0,
                produces: new[] { mail },
                demands: new[] { food, passengers });
            farm = new Node(2, "Farm", 5, 0,
                produces: new[] { food },
                demands: new[] { mail });

            network = new Network(new[] { city, farm }, new Line[0]);
            config = new EconomyConfig();
            demand = new DemandSystem(network, config);
        }

        // ---------- Slots ----------

        [Test]
        public void NewNode_HasAllSlotsFreeForEachWantedCargo()
        {
            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(config.DemandSlots));
            Assert.That(demand.FreeSlots(farm, mail), Is.EqualTo(config.DemandSlots));
            Assert.That(config.DemandSlots, Is.EqualTo(5));
        }

        [Test]
        public void ThreeAccepts_ReturnFreeSlotsBefore_ThenTwoFree()
        {
            int first = demand.TryAccept(city, food);
            int second = demand.TryAccept(city, food);
            int third = demand.TryAccept(city, food);

            Assert.That(first, Is.EqualTo(5));
            Assert.That(second, Is.EqualTo(4));
            Assert.That(third, Is.EqualTo(3));
            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(2));
        }

        [Test]
        public void AcceptOnFullStock_ReturnsZeroAndStoresNothing()
        {
            for (int i = 0; i < config.DemandSlots; i++)
                demand.TryAccept(city, food);

            int events = 0;
            demand.DemandChanged += (n, c, free) => events++;

            int result = demand.TryAccept(city, food);

            Assert.That(result, Is.EqualTo(0));
            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(0));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void StocksAreIndependentPerNodeAndCargo()
        {
            demand.TryAccept(city, food);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(4));
            Assert.That(demand.FreeSlots(farm, mail), Is.EqualTo(5));
        }

        // ---------- Consumption ----------

        [Test]
        public void AfterOneInterval_OneSlotIsFreed()
        {
            demand.TryAccept(city, food);
            demand.TryAccept(city, food);
            demand.TryAccept(city, food); // 2 free

            demand.Tick(config.CityConsumeInterval);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(3));
        }

        [Test]
        public void OneBigTick_FreesSeveralSlots()
        {
            for (int i = 0; i < config.DemandSlots; i++)
                demand.TryAccept(city, food); // 0 free

            demand.Tick(3 * config.CityConsumeInterval);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(3));
        }

        [Test]
        public void SmallTicks_AccumulateToOneConsumption()
        {
            demand.TryAccept(city, food);

            for (int i = 0; i < 44; i++)
                demand.Tick(1f);
            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(4));

            demand.Tick(1f);
            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(5));
        }

        [Test]
        public void LeftoverTime_IsCarriedOver()
        {
            for (int i = 0; i < 3; i++)
                demand.TryAccept(city, food); // 2 free

            demand.Tick(config.CityConsumeInterval * 1.5f);
            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(3));

            demand.Tick(config.CityConsumeInterval * 0.5f);
            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(4));
        }

        [Test]
        public void EmptyStock_StaysFullyFree_AndRaisesNoEvent()
        {
            int events = 0;
            demand.DemandChanged += (n, c, free) => events++;

            demand.Tick(10 * config.CityConsumeInterval);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(config.DemandSlots));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void EmptyStock_DoesNotBankTime_ForLaterDeliveries()
        {
            demand.Tick(100 * config.CityConsumeInterval);

            demand.TryAccept(city, food);
            demand.Tick(config.CityConsumeInterval - 1f);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(4));
        }

        [Test]
        public void TimerRestartsWhenStockEmptiesAndRefills()
        {
            demand.TryAccept(city, food);
            demand.Tick(config.CityConsumeInterval * 1.9f); // empties, leftover discarded

            demand.TryAccept(city, food);
            demand.Tick(config.CityConsumeInterval * 0.5f);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(4));
        }

        [Test]
        public void NegativeTick_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => demand.Tick(-1f));
        }

        // ---------- Events ----------

        [Test]
        public void DemandChanged_FiresOnAcceptWithFreeSlotsAfter()
        {
            var seen = new List<(Node, CargoType, int)>();
            demand.DemandChanged += (n, c, free) => seen.Add((n, c, free));

            demand.TryAccept(city, food);

            Assert.That(seen.Count, Is.EqualTo(1));
            Assert.That(seen[0].Item1, Is.SameAs(city));
            Assert.That(seen[0].Item2, Is.SameAs(food));
            Assert.That(seen[0].Item3, Is.EqualTo(4));
        }

        [Test]
        public void DemandChanged_FiresOncePerConsumedUnit()
        {
            for (int i = 0; i < 3; i++)
                demand.TryAccept(city, food);

            var freeValues = new List<int>();
            demand.DemandChanged += (n, c, free) => freeValues.Add(free);

            demand.Tick(3 * config.CityConsumeInterval);

            Assert.That(freeValues, Is.EqualTo(new[] { 3, 4, 5 }));
        }

        // ---------- Which cargo has a stock ----------

        [Test]
        public void Passengers_HaveNoDemandStock()
        {
            Assert.That(demand.TryAccept(city, passengers), Is.EqualTo(0));
            Assert.Throws<ArgumentException>(() => demand.FreeSlots(city, passengers));
        }

        [Test]
        public void CargoTheNodeDoesNotDemand_IsNotAccepted()
        {
            // City produces mail, it does not want it.
            Assert.That(demand.TryAccept(city, mail), Is.EqualTo(0));
            Assert.Throws<ArgumentException>(() => demand.FreeSlots(city, mail));
        }

        [Test]
        public void FactoryInputCargo_IsNotConsumedByTheCity()
        {
            demand.IsFactoryInput = (n, c) => ReferenceEquals(c, food);
            demand.TryAccept(city, food);

            demand.Tick(10 * config.CityConsumeInterval);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(4));
        }

        [Test]
        public void FactoryInputCargo_DoesNotBankTime()
        {
            demand.IsFactoryInput = (n, c) => ReferenceEquals(c, food);
            demand.TryAccept(city, food);
            demand.Tick(10 * config.CityConsumeInterval);

            demand.IsFactoryInput = (n, c) => false;
            demand.Tick(config.CityConsumeInterval - 1f);

            Assert.That(demand.FreeSlots(city, food), Is.EqualTo(4));
        }

        // ---------- Config ----------

        [Test]
        public void CustomConfig_SlotsAndIntervalAreRespected()
        {
            var custom = new EconomyConfig
            {
                DemandSlots = 2,
                CityConsumeInterval = 10f,
                PriceBySlots = new[] { 0f, 0.5f, 1.5f } // one entry per free-slot count: 0, 1, 2
            };
            var system = new DemandSystem(network, custom);

            Assert.That(system.TryAccept(city, food), Is.EqualTo(2));
            Assert.That(system.TryAccept(city, food), Is.EqualTo(1));
            Assert.That(system.TryAccept(city, food), Is.EqualTo(0));

            system.Tick(10f);
            Assert.That(system.FreeSlots(city, food), Is.EqualTo(1));
        }

        [Test]
        public void NonPositiveConsumeInterval_Throws()
        {
            var bad = new EconomyConfig { CityConsumeInterval = 0f };

            Assert.Throws<ArgumentException>(() => new DemandSystem(network, bad));
        }

        [Test]
        public void DefaultConfig_FlagsPassengers()
        {
            var catalog = new CargoCatalog(new EconomyConfig().Cargos);

            Assert.That(catalog.GetByName("Passengers").IsPassengers, Is.True);
            Assert.That(catalog.GetByName("Food").IsPassengers, Is.False);
        }

        // ---------- Wants ----------

        [Test]
        public void Wants_IsTrueOnlyForCargoWithAStock()
        {
            Assert.That(demand.Wants(city, food), Is.True);
            Assert.That(demand.Wants(city, passengers), Is.False); // passengers have no stock
            Assert.That(demand.Wants(city, mail), Is.False);       // the city produces it
            Assert.That(demand.Wants(null, food), Is.False);
        }

        [Test]
        public void PriceTableWithWrongLength_Throws()
        {
            // Default table has 6 entries, which only fits 5 demand slots.
            var bad = new EconomyConfig { DemandSlots = 3 };

            Assert.Throws<ArgumentException>(() => new DemandSystem(network, bad));
        }

        // ---------- Economy.Tick ----------

        [Test]
        public void EconomyTick_AdvancesDemand()
        {
            var economy = new Economy(new GameState(100), network, config);
            economy.Demand.TryAccept(city, food);

            economy.Tick(config.CityConsumeInterval);

            Assert.That(economy.Demand.FreeSlots(city, food), Is.EqualTo(config.DemandSlots));
        }
    }
}
