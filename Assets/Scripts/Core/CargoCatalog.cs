using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    /// <summary>All cargo types in the game, with lookups. Built from EconomyConfig.Cargos.</summary>
    public class CargoCatalog
    {
        private readonly List<CargoType> cargos;
        private readonly Dictionary<int, CargoType> byId = new Dictionary<int, CargoType>();
        private readonly Dictionary<string, CargoType> byName =
            new Dictionary<string, CargoType>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<CargoType> Cargos { get; }

        public CargoCatalog(IEnumerable<CargoType> cargos)
        {
            if (cargos == null)
                throw new ArgumentNullException(nameof(cargos));

            this.cargos = new List<CargoType>(cargos);

            foreach (CargoType cargo in this.cargos)
            {
                if (cargo == null)
                    throw new ArgumentException("Catalog cannot contain null cargo types.");

                if (!byId.TryAdd(cargo.Id, cargo))
                    throw new ArgumentException($"Duplicate cargo ID: {cargo.Id}");

                if (!byName.TryAdd(cargo.Name, cargo))
                    throw new ArgumentException($"Duplicate cargo name: {cargo.Name}");
            }

            Cargos = this.cargos.AsReadOnly();
        }

        /// <summary>The cargo with this id, or null if unknown.</summary>
        public CargoType GetById(int id)
        {
            byId.TryGetValue(id, out CargoType cargo);
            return cargo;
        }

        /// <summary>The cargo with this name (case-insensitive), or null if unknown.</summary>
        public CargoType GetByName(string name)
        {
            if (name == null) return null;
            byName.TryGetValue(name, out CargoType cargo);
            return cargo;
        }
    }
}
