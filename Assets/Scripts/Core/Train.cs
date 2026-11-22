namespace ChugHaul.Core
{
    public class Train
    {
        private readonly int locomotiveMass;

        public Line Line { get; }

        /// <summary>Where the train is, or last was. Updated on arrival.</summary>
        public Node CurrentStop { get; internal set; }

        /// <summary>Target of the current trip. Null while AtStop.</summary>
        public Node Destination { get; internal set; }

        public TrainState State { get; internal set; }

        public Cargo Cargo { get; internal set; }

        public int Capacity { get; }

        /// <summary>Locomotive mass plus the mass of every wagon currently loaded.</summary>
        public int TotalMass
        {
            get
            {
                int mass = locomotiveMass;
                foreach (CargoType wagon in Cargo)
                    mass += wagon.WagonMass;
                return mass;
            }
        }

        internal Train(Line line, int capacity, int locomotiveMass)
        {
            Line = line;
            Capacity = capacity;
            this.locomotiveMass = locomotiveMass;
            CurrentStop = line.Stops[0];
            Destination = null;
            State = TrainState.AtStop;
            Cargo = new Cargo();
        }
    }
}
