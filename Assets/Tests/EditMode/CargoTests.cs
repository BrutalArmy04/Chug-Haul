using NUnit.Framework;
using ChugHaul.Core;
using System;
using System.Linq;

namespace ChugHaul.Core.Tests
{
    public class CargoTests
    {
        private CargoType passengers;
        private CargoType mail;
        private CargoType food;
        private CargoCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            passengers = new CargoType(1, "Passengers", 5, 1);
            mail = new CargoType(2, "Mail", 8, 1);
            food = new CargoType(3, "Food", 10, 2);
            catalog = new CargoCatalog(new[] { passengers, mail, food });
        }

        // ---------- CargoType ----------

        [Test]
        public void CargoType_StoresItsValues()
        {
            Assert.That(food.Id, Is.EqualTo(3));
            Assert.That(food.Name, Is.EqualTo("Food"));
            Assert.That(food.BaseValue, Is.EqualTo(10));
            Assert.That(food.WagonMass, Is.EqualTo(2));
        }

        [Test]
        public void CargoType_EmptyName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CargoType(1, " ", 5, 1));
        }

        [Test]
        public void CargoType_NegativeValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CargoType(1, "X", -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CargoType(1, "X", 1, -1));
        }

        // ---------- CargoCatalog ----------

        [Test]
        public void Catalog_GetById_FindsCargo()
        {
            Assert.That(catalog.GetById(2), Is.SameAs(mail));
            Assert.That(catalog.GetById(3), Is.SameAs(food));
        }

        [Test]
        public void Catalog_GetById_UnknownReturnsNull()
        {
            Assert.That(catalog.GetById(999), Is.Null);
        }

        [Test]
        public void Catalog_GetByName_IsCaseInsensitive()
        {
            Assert.That(catalog.GetByName("mail"), Is.SameAs(mail));
            Assert.That(catalog.GetByName("Nope"), Is.Null);
        }

        [Test]
        public void Catalog_DuplicateId_Throws()
        {
            var clash = new CargoType(1, "Other", 1, 1);

            Assert.Throws<ArgumentException>(() => new CargoCatalog(new[] { passengers, clash }));
        }

        [Test]
        public void Catalog_DuplicateName_Throws()
        {
            var clash = new CargoType(9, "passengers", 1, 1);

            Assert.Throws<ArgumentException>(() => new CargoCatalog(new[] { passengers, clash }));
        }

        [Test]
        public void Catalog_DefaultConfigCargos_AreValid()
        {
            var defaults = new CargoCatalog(new EconomyConfig().Cargos);

            Assert.That(defaults.Cargos.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(defaults.GetByName("Passengers"), Is.Not.Null);
            Assert.That(defaults.GetByName("Mail"), Is.Not.Null);
            Assert.That(defaults.GetByName("Food"), Is.Not.Null);
        }

        // ---------- Node cargo lists ----------

        [Test]
        public void Node_WithoutCargo_HasEmptyLists()
        {
            var node = new Node(1, "A", 0, 0);

            Assert.That(node.Produces, Is.Not.Null);
            Assert.That(node.Demands, Is.Not.Null);
            Assert.That(node.Produces.Count, Is.EqualTo(0));
            Assert.That(node.Demands.Count, Is.EqualTo(0));
        }

        [Test]
        public void Node_StoresProducesAndDemands()
        {
            var node = new Node(1, "A", 0, 0,
                produces: new[] { passengers },
                demands: new[] { mail, food });

            Assert.That(node.Produces, Is.EqualTo(new[] { passengers }));
            Assert.That(node.Demands, Is.EqualTo(new[] { mail, food }));
        }

        [Test]
        public void Node_DemandingWhatItProduces_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "A", 0, 0,
                produces: new[] { food },
                demands: new[] { mail, food }));
        }

        [Test]
        public void Node_DemandingWhatItProduces_IsCheckedById()
        {
            var sameIdDifferentInstance = new CargoType(3, "Food", 10, 2);

            Assert.Throws<ArgumentException>(() => new Node(1, "A", 0, 0,
                produces: new[] { food },
                demands: new[] { sameIdDifferentInstance }));
        }

        [Test]
        public void Node_RepeatedCargoInProduces_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "A", 0, 0,
                produces: new[] { food, food }));
        }

        [Test]
        public void Node_RepeatedCargoInDemands_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "A", 0, 0,
                demands: new[] { mail, mail }));
        }

        [Test]
        public void Node_NullCargoEntry_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Node(1, "A", 0, 0,
                produces: new CargoType[] { null }));
        }

        // ---------- TestMap ----------

        private static string CargoKey(Node node)
        {
            string produced = string.Join(",", node.Produces.Select(c => c.Id).OrderBy(i => i));
            string demanded = string.Join(",", node.Demands.Select(c => c.Id).OrderBy(i => i));
            return $"P:{produced}|D:{demanded}";
        }

        [Test]
        public void TestMap_NoTwoCitiesAreIdentical()
        {
            Network network = TestMap.Create();

            var keys = network.Nodes
                .Where(n => n.Kind == NodeKind.City)
                .Select(CargoKey)
                .ToList();

            Assert.That(keys.Count, Is.GreaterThan(1));
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Count));
        }

        [Test]
        public void TestMap_CitiesMakeNothingAndWantSomething()
        {
            Network network = TestMap.Create();

            foreach (Node city in network.Nodes.Where(n => n.Kind == NodeKind.City))
            {
                Assert.That(city.Produces.Count, Is.EqualTo(0), city.Name);
                Assert.That(city.Demands.Count, Is.GreaterThan(0), city.Name);
            }
        }

        [Test]
        public void TestMap_HasAFarmAForestAndAMine()
        {
            Network network = TestMap.Create();

            var industryCargo = network.Nodes
                .Where(n => n.Kind == NodeKind.Industry)
                .Select(n => n.Produces[0].Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.That(industryCargo, Is.EqualTo(new[] { "Food", "Iron ore", "Wood" }));
        }

        [Test]
        public void TestMap_IndustriesMakeOneCargoAndWantNothing()
        {
            Network network = TestMap.Create();

            foreach (Node industry in network.Nodes.Where(n => n.Kind == NodeKind.Industry))
            {
                Assert.That(industry.Produces.Count, Is.EqualTo(1), industry.Name);
                Assert.That(industry.Demands.Count, Is.EqualTo(0), industry.Name);
            }
        }

        [Test]
        public void TestMap_EveryIndustryIsOnALine()
        {
            Network network = TestMap.Create();

            foreach (Node industry in network.Nodes.Where(n => n.Kind == NodeKind.Industry))
                Assert.That(network.LinesAt(industry).Count, Is.GreaterThan(0), industry.Name);
        }

        [Test]
        public void TestMap_EveryIndustryCargoIsWantedByACity()
        {
            Network network = TestMap.Create();

            foreach (Node industry in network.Nodes.Where(n => n.Kind == NodeKind.Industry))
            {
                CargoType cargo = industry.Produces[0];
                Assert.That(
                    network.Nodes.Any(n => n.Kind == NodeKind.City &&
                                           n.Demands.Any(c => c.Id == cargo.Id)),
                    Is.True, $"{cargo.Name} is made but no city wants it");
            }
        }
    }
}
