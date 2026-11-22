namespace ChugHaul.Core
{
    public static class TestMap
    {
        public static Network Create()
        {
            var cargo = new CargoCatalog(new EconomyConfig().Cargos);

            CargoType food = cargo.GetByName("Food");
            CargoType wood = cargo.GetByName("Wood");
            CargoType planks = cargo.GetByName("Planks");
            CargoType ironOre = cargo.GetByName("Iron ore");
            CargoType steel = cargo.GetByName("Steel");

            // Cities: they make nothing for now, and each wants a different set of goods.
            Node bucharest = new Node(1, "Bucharest", 0, 0,
                demands: new[] { food, planks });

            Node brasov = new Node(2, "Brasov", 1, 7,
                demands: new[] { steel, wood });

            Node sibiu = new Node(3, "Sibiu", 8, 7,
                demands: new[] { ironOre, planks });

            Node cluj = new Node(4, "Cluj", 9, 0,
                demands: new[] { food, wood });

            // Raw-material industries: one cargo each, no demands.
            Node farm = new Node(5, "Farm", -3, -2,
                produces: new[] { food }, kind: NodeKind.Industry);

            Node forest = new Node(6, "Forest", 12, 3,
                produces: new[] { wood }, kind: NodeKind.Industry);

            Node mine = new Node(7, "Mine", 11, 9,
                produces: new[] { ironOre }, kind: NodeKind.Industry);

            Line line1 = new Line(1, new[] { bucharest, brasov }, 20);
            Line line2 = new Line(2, new[] { brasov, sibiu }, 15);
            Line line3 = new Line(3, new[] { sibiu, cluj }, 25);
            Line line4 = new Line(4, new[] { bucharest, cluj }, 40);

            // 4-stop line. Brasov and Sibiu are middle stops, and every node
            // here is also shared with at least one of the lines above.
            Line line5 = new Line(5, new[] { bucharest, brasov, sibiu, cluj }, 60);

            // Pickup lines for the industries (none of them touches Brasov).
            Line line6 = new Line(6, new[] { farm, bucharest }, 15);
            Line line7 = new Line(7, new[] { forest, cluj }, 20);
            Line line8 = new Line(8, new[] { mine, sibiu }, 20);

            return new Network(
                new[] { bucharest, brasov, sibiu, cluj, farm, forest, mine },
                new[] { line1, line2, line3, line4, line5, line6, line7, line8 }
            );
        }
    }
}
