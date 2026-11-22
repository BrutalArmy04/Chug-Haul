namespace ChugHaul.Core
{
    public class Train
    {
        public Line Line { get; }

        /// <summary>Where the train is, or last was. Updated on arrival.</summary>
        public Node CurrentStop { get; internal set; }

        /// <summary>Target of the current trip. Null while AtStop.</summary>
        public Node Destination { get; internal set; }

        public TrainState State { get; internal set; }

        public Cargo Cargo { get; internal set; }

        public int Capacity { get; }

        internal Train(Line line, int capacity)
        {
            Line = line;
            Capacity = capacity;
            CurrentStop = line.Stops[0];
            Destination = null;
            State = TrainState.AtStop;
            Cargo = new Cargo();
        }
    }
}
