namespace ChugHaul.Core
{
    /// <summary>
    /// The map the whole sprint is played and tested on:
    /// raw material -> factory -> product -> town, including a transfer between two lines.
    ///
    /// SKETCH (schematic, not to scale; exact positions are in the table below).
    /// Altitudes are PROPOSED values for the elevation task and are not in the code yet.
    /// "^" marks a steep segment, "~" a water crossing.
    ///
    ///                                                           (7) MINE  520 m
    ///                                                                 O
    ///                                                                /
    ///                                                               /   L7
    ///     (2) BRASOV  600 m                        (3) SIBIU  450 m
    ///         O============= L2 (+ L5) ============O
    ///        /^                                      \
    ///       / ^ L1 (steep)                            \   L3
    ///      /  ^                                        \
    ///     /                                             \                    (6) FOREST  700 m
    ///    /                                               \                          O
    ///   O ~~~~~~~~~~ L4 (river crossing) ~~~~~~~~~~~~~~~~~~O^^^^ L6 (steep) ^^^^^^^^^^/
    /// (1) BUCHAREST  80 m                           (4) CLUJ  330 m
    ///    /
    ///   /   L5 = Farm - Bucharest - Brasov - Sibiu
    ///  O
    /// (5) FARM  60 m
    ///
    /// id  node       kind      x   z   alt (m)  wants               makes / factory
    /// 1   Bucharest  City      0   0    80      Lumber, Ore
    /// 2   Brasov     City      1   7   600      Grain, Lumber       factory: Grain -> Food
    /// 3   Sibiu      City      8   7   450      Food, Grain
    /// 4   Cluj       City      9   0   330      Logs, Food          factory: Logs -> Lumber
    /// 5   Farm       Industry -3  -2    60      -                   Grain
    /// 6   Forest     Industry 12   3   700      -                   Logs
    /// 7   Mine       Industry 11   9   520      -                   Ore
    ///
    /// line  stops (node ids)   cost  notes
    /// L1    1 - 2              20    steep (80 m to 600 m)
    /// L2    2 - 3              15
    /// L3    3 - 4              25
    /// L4    1 - 4              40    water crossing (river)
    /// L5    5 - 1 - 2 - 3      70    4 stops: Farm, Bucharest, Brasov, Sibiu
    /// L6    6 - 4              20    steep (700 m to 330 m)
    /// L7    7 - 3              20
    ///
    /// CHAINS
    /// Food:   Farm -L5-> Brasov (factory) -L5-> Bucharest (does not want Food: lands in its depot)
    ///         -L4-> Cluj (wants Food). Bucharest is the transfer between L5 and L4.
    ///         (Sibiu also buys Food, directly from Brasov on L2/L5.)
    /// Lumber: Forest -L6-> Cluj (factory) -L4-> Bucharest (wants Lumber).
    /// Ore:    Mine -L7-> Sibiu (does not want Ore: depot) -L5-> Bucharest (wants Ore). Transfer at Sibiu.
    /// </summary>
    public static class TestMap
    {
        public static Network Create()
        {
            var cargo = new CargoCatalog(new EconomyConfig().Cargos);

            CargoType grain = cargo.GetByName("Grain");
            CargoType logs = cargo.GetByName("Logs");
            CargoType ore = cargo.GetByName("Ore");
            CargoType food = cargo.GetByName("Food");
            CargoType lumber = cargo.GetByName("Lumber");

            // Cities: they make nothing themselves (a factory's output goes straight into
            // the depot), and each wants a different set of goods.
            Node bucharest = new Node(1, "Bucharest", 0, 0,
                demands: new[] { lumber, ore });

            // Brasov's factory turns Grain into Food.
            Node brasov = new Node(2, "Brasov", 1, 7,
                demands: new[] { grain, lumber },
                factory: new Factory(grain, food));

            Node sibiu = new Node(3, "Sibiu", 8, 7,
                demands: new[] { food, grain });

            // Cluj's factory turns Logs into Lumber.
            Node cluj = new Node(4, "Cluj", 9, 0,
                demands: new[] { logs, food },
                factory: new Factory(logs, lumber));

            // Raw-material industries: one cargo each, no demands.
            Node farm = new Node(5, "Farm", -3, -2,
                produces: new[] { grain }, kind: NodeKind.Industry);

            Node forest = new Node(6, "Forest", 12, 3,
                produces: new[] { logs }, kind: NodeKind.Industry);

            Node mine = new Node(7, "Mine", 11, 9,
                produces: new[] { ore }, kind: NodeKind.Industry);

            // Costs are rough first values; they get balanced later.
            Line line1 = new Line(1, new[] { bucharest, brasov }, 20);
            Line line2 = new Line(2, new[] { brasov, sibiu }, 15);
            Line line3 = new Line(3, new[] { sibiu, cluj }, 25);
            Line line4 = new Line(4, new[] { bucharest, cluj }, 40);

            // 4-stop line, and the Farm's only way out. Brasov and Sibiu are middle or end stops
            // shared with other lines.
            Line line5 = new Line(5, new[] { farm, bucharest, brasov, sibiu }, 70);

            // Pickup lines for the other industries.
            Line line6 = new Line(6, new[] { forest, cluj }, 20);
            Line line7 = new Line(7, new[] { mine, sibiu }, 20);

            return new Network(
                new[] { bucharest, brasov, sibiu, cluj, farm, forest, mine },
                new[] { line1, line2, line3, line4, line5, line6, line7 }
            );
        }
    }
}
