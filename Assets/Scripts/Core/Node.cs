namespace ChugHaul.Core
{
    public class Node
    {
        public int Id { get; }
        public string Name { get; }
        public float X { get; }
        public float Z { get; }

        public Node(int id, string name, float x, float z)
        {
            Id = id;
            Name = name;
            X = x;
            Z = z;
        }
    }
}