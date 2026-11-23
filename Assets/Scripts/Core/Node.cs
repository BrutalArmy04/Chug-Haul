using System;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    public class Node
    {
        private static readonly CargoType[] NoCargo = new CargoType[0];

        public int Id { get; }
        public string Name { get; }
        public float X { get; }
        public float Z { get; }

        public NodeKind Kind { get; }

        /// <summary>The city's factory, or null. A city has 0 or 1.</summary>
        public Factory Factory { get; }

        /// <summary>Cargo this node supplies.</summary>
        public IReadOnlyList<CargoType> Produces { get; }

        /// <summary>Cargo this node wants. Never overlaps Produces.</summary>
        public IReadOnlyList<CargoType> Demands { get; }

        public Node(
            int id,
            string name,
            float x,
            float z,
            IEnumerable<CargoType> produces = null,
            IEnumerable<CargoType> demands = null,
            NodeKind kind = NodeKind.City,
            Factory factory = null)
        {
            Id = id;
            Name = name;
            X = x;
            Z = z;
            Kind = kind;

            CargoType[] produced = produces == null ? NoCargo : new List<CargoType>(produces).ToArray();
            CargoType[] demanded = demands == null ? NoCargo : new List<CargoType>(demands).ToArray();

            var producedIds = new HashSet<int>();
            foreach (CargoType cargo in produced)
            {
                if (cargo == null)
                    throw new ArgumentException("Produces cannot contain null.", nameof(produces));
                if (!producedIds.Add(cargo.Id))
                    throw new ArgumentException(
                        $"Node {id} lists cargo {cargo.Id} twice in Produces.", nameof(produces));
            }

            var demandedIds = new HashSet<int>();
            foreach (CargoType cargo in demanded)
            {
                if (cargo == null)
                    throw new ArgumentException("Demands cannot contain null.", nameof(demands));
                if (!demandedIds.Add(cargo.Id))
                    throw new ArgumentException(
                        $"Node {id} lists cargo {cargo.Id} twice in Demands.", nameof(demands));
                if (producedIds.Contains(cargo.Id))
                    throw new ArgumentException(
                        $"Node {id} cannot demand cargo {cargo.Id} because it produces it.",
                        nameof(demands));
            }

            if (kind == NodeKind.Industry)
            {
                if (produced.Length != 1)
                    throw new ArgumentException(
                        $"Industry {id} must produce exactly one cargo.", nameof(produces));
                if (demanded.Length != 0)
                    throw new ArgumentException(
                        $"Industry {id} cannot demand anything.", nameof(demands));
            }

            if (factory != null)
            {
                if (kind != NodeKind.City)
                    throw new ArgumentException(
                        $"Node {id}: only a city can have a factory.", nameof(factory));
                if (!demandedIds.Contains(factory.Input.Id))
                    throw new ArgumentException(
                        $"Node {id}: the factory input must be listed in Demands.", nameof(factory));
                if (demandedIds.Contains(factory.Output.Id))
                    throw new ArgumentException(
                        $"Node {id} cannot demand cargo {factory.Output.Id} because its factory makes it.",
                        nameof(factory));
            }

            Produces = produced;
            Demands = demanded;
            Factory = factory;
        }
    }
}
