using System.Windows;

namespace HpaStarPathfinding.ViewModel
{
    public class Vector2D
    {
        public Vector2D(int x, int y)
        {
            X = x;
            Y = y;
        }
        

        public int X { get; set; }
        public int Y { get; set; }
        
        
        public static Vector2D ConvertVector2DToScreenPosCentered(Vector2D point)
        {
            int x = point.X * MainWindow.CellSize + MainWindow.CellSize / 2;
            int y = point.Y * MainWindow.CellSize + MainWindow.CellSize / 2;
            return new Vector2D(x, y);
        }
        
        public static Vector2D ConvertPointToIndex(Point point)
        {
            int x = (int)point.X / MainWindow.CellSize;
            int y = (int)point.Y / MainWindow.CellSize;
            return new Vector2D(x, y);
        }
        
        public static Vector2D operator +(Vector2D a, Vector2D b)
        {
            return new Vector2D(a.X + b.X, a.Y + b.Y);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2D other)
            {
                return X == other.X && Y == other.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
    }
}