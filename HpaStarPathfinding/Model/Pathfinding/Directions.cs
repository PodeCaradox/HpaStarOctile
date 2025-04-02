namespace HpaStarPathfinding.ViewModel
{
    
    public enum Directions: byte
    {
        // N = 0b_0000_0001,
        // NE = 0b_0000_0010,
        // E = 0b_0000_0100,
        // SE = 0b_0000_1000,
        // S = 0b_0001_0000,
        // SW = 0b_0010_0000,
        // W = 0b_0100_0000,
        // NW = 0b_1000_0000
        N = 0,
        NE = 1,
        E = 2,
        SE = 3,
        S = 4,
        SW = 5,
        W = 6,
        NW = 7
    }
    
    public static class DirectionsVector
    {
        public static readonly Vector2D N = new Vector2D(0, -1);
        public static readonly Vector2D NE = new Vector2D(1, -1);
        public static readonly Vector2D W = new Vector2D(-1, 0);
        public static readonly Vector2D NW = new Vector2D(-1, -1);
        public static readonly Vector2D E = new Vector2D(1, 0);
        public static readonly Vector2D SE = new Vector2D(1, 1);
        public static readonly Vector2D S = new Vector2D(0, 1);
        public static readonly Vector2D SW = new Vector2D(-1, 1);

        public static readonly Vector2D[] AllDirections =
        {
            N, NE, E, SE, S, SW, W, NW
        };
    }
}