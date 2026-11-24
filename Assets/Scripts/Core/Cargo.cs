using System;
using System.Collections;
using System.Collections.Generic;

namespace ChugHaul.Core
{
    /// <summary>
    /// What a train carries, or a load handed to TryDispatch: an ordered list of wagons,
    /// one unit of one CargoType each. Immutable.
    /// </summary>
    public sealed class Cargo : IEnumerable<CargoType>
    {
        public static readonly Cargo Empty = new Cargo();

        private readonly CargoType[] wagons;

        public Cargo(IEnumerable<CargoType> wagons = null)
        {
            this.wagons = wagons == null
                ? new CargoType[0]
                : new List<CargoType>(wagons).ToArray();

            foreach (CargoType wagon in this.wagons)
            {
                if (wagon == null)
                    throw new ArgumentException("A load cannot contain a null wagon.", nameof(wagons));
            }
        }

        public int Count => wagons.Length;

        public CargoType this[int index] => wagons[index];

        public IReadOnlyList<CargoType> Wagons => wagons;

        public IEnumerator<CargoType> GetEnumerator()
        {
            return ((IEnumerable<CargoType>)wagons).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
