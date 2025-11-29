namespace HpaStarPathfinding.ViewModel
{
    
    public enum Directions
    {
        N = 0,
        E = 1,
        S = 2,
        W = 3,
    }
    
    public static class DirectionsAsByte
    {
        public const byte N = 0b_0000_0001;
        public const byte NE = 0b_0000_0010;
        public const byte E = 0b_0000_0100;
        public const byte SE = 0b_0000_1000;
        public const byte S = 0b_0001_0000;
        public const byte SW = 0b_0010_0000;
        public const byte W = 0b_0100_0000;
        public const byte NW = 0b_1000_0000;
        public const byte NOT_WALKABLE = 0b_1111_1111;
        public const byte WALKABLE = 0b_0;
        
        public static readonly byte[] AllDirectionsAsByte =
        {
            N, NE, E, SE, S, SW, W, NW
        };
    }
    
    public static class DirectionsVector
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