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

            // 4 stops. Brasov is a middle stop here and the end of line 2, so the two lines share it
            // (and Cluj). Costs are rough: line 1 is affordable with the scene's 50 starting coins.
            Line line1 = new Line(1, new[] { bucharest, brasov, sibiu, cluj }, 45);
            Line line2 = new Line(2, brasov, cluj, 30);

            return new Network(
                new[] { bucharest, brasov, sibiu, cluj },
                new[] { line1, line2 }
            );
        }
    }
}