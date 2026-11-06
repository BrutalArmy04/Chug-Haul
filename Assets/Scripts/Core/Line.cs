using System;

namespace ChugHaul.Core
{
    public class Line
    {
        public int Id { get; }
        public Node NodeA { get; }
        public Node NodeB { get; }
        public int Cost { get; }
        public bool IsActive { get; set; }

        public float Length { get; }

        public Line(int id, Node nodeA, Node nodeB, int cost)
        {
            Id = id;
            NodeA = nodeA;
            NodeB = nodeB;
            Cost = cost;
            IsActive = false;

            double dx = nodeB.X - nodeA.X;
            double dz = nodeB.Z - nodeA.Z;

            Length = (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}