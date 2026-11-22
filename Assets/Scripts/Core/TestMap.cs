namespace ChugHaul.Core
{
    public static class TestMap
    {
        public static Network Create()
        {
            var cargo = new CargoCatalog(new EconomyConfig().Cargos);

            CargoType passengers = cargo.GetByName("Passengers");
            CargoType mail = cargo.GetByName("Mail");
            CargoType food = cargo.GetByName("Food");
            CargoType wood = cargo.GetByName("Wood");
            CargoType planks = cargo.GetByName("Planks");
            CargoType ironOre = cargo.GetByName("Iron ore");
            CargoType steel = cargo.GetByName("Steel");

            Node bucharest = new Node(1, "Bucharest", 0, 0,
                produces: new[] { passengers, steel },
                demands: new[] { food, planks });

            Node brasov = new Node(2, "Brasov", 1, 7,
                produces: new[] { wood, mail },
                demands: new[] { passengers, steel });

            Node sibiu = new Node(3, "Sibiu", 8, 7,
                produces: new[] { food, planks },
                demands: new[] { mail, ironOre });

            Node cluj = new Node(4, "Cluj", 9, 0,
                produces: new[] { ironOre, food },
                demands: new[] { wood, passengers });

            Line line1 = new Line(1, new[] { bucharest, brasov }, 20);
            Line line2 = new Line(2, new[] { brasov, sibiu }, 15);
            Line line3 = new Line(3, new[] { sibiu, cluj }, 25);
            Line line4 = new Line(4, new[] { bucharest, cluj }, 40);

            // 4-stop line. Brasov and Sibiu are middle stops, and every node
            // here is also shared with at least one of the lines above.
            Line line5 = new Line(5, new[] { bucharest, brasov, sibiu, cluj }, 60);

            return new Network(
                new[] { bucharest, brasov, sibiu, cluj },
                new[] { line1, line2, line3, line4, line5 }
            );
        }
    }
}
