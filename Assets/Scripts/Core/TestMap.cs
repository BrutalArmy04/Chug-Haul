namespace ChugHaul.Core
{
    public static class TestMap
    {
        public static Network Create()
        {
            Node bucharest = new Node(1, "Bucharest", 0, 0);
            Node brasov = new Node(2, "Brasov", 1, 7);
            Node sibiu = new Node(3, "Sibiu", 8, 7);
            Node cluj = new Node(4, "Cluj", 9, 0);

            Line line1 = new Line(1, bucharest, brasov, 20);
            Line line2 = new Line(2, brasov, sibiu, 15);
            Line line3 = new Line(3, sibiu, cluj, 25);
            Line line4 = new Line(4, bucharest, cluj, 40);

            return new Network(
                new[] { bucharest, brasov, sibiu, cluj },
                new[] { line1, line2, line3, line4 }
            );
        }
    }
}