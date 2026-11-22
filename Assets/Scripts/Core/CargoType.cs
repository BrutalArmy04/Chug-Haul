using System;

namespace ChugHaul.Core
{
    /// <summary>A kind of cargo (passengers, mail, steel, ...). Immutable.</summary>
    public sealed class CargoType
    {
        public int Id { get; }
        public string Name { get; }
        public int BaseValue { get; }
        public int WagonMass { get; }

        public CargoType(int id, string name, int baseValue, int wagonMass)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A cargo type needs a name.", nameof(name));
            if (baseValue < 0)
                throw new ArgumentOutOfRangeException(nameof(baseValue));
            if (wagonMass < 0)
                throw new ArgumentOutOfRangeException(nameof(wagonMass));

            Id = id;
            Name = name;
            BaseValue = baseValue;
            WagonMass = wagonMass;
        }
    }
}
