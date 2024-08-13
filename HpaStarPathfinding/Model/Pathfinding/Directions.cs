namespace HpaStarPathfinding.ViewModel
{
    
    public static class Directions
    {
        public static readonly Vector2D N = new Vector2D(0, -1);
        public static readonly Vector2D NE = new Vector2D(1, -1);
        public static readonly Vector2D E = new Vector2D(1, 0);
        public static readonly Vector2D SE = new Vector2D(1, 1);
        public static readonly Vector2D S = new Vector2D(0, 1);
        public static readonly Vector2D SW = new Vector2D(-1, 1);
        public static readonly Vector2D W = new Vector2D(-1, 0);
        public static readonly Vector2D NW = new Vector2D(-1, -1);

        public static readonly Vector2D[] AllDirections =
        {
            N, NE, E, SE, S, SW, W, NW
        };
    }
}