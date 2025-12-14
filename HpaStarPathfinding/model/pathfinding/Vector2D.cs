using System.Windows;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.model.pathfinding
{
    public class Vector2D(int x, int y)
    {
        public int x { get; set; } = x;
        public int y { get; set; } = y;


        public static Vector2D ConvertMapPointToCanvasPos(Vector2D point)
        {
            int x = point.x * MainWindowViewModel.CellSize + MainWindowViewModel.CellSize / 2;
            int y = point.y * MainWindowViewModel.CellSize + MainWindowViewModel.CellSize / 2;
            return new Vector2D(x, y);
        }
        
        public static Vector2D ConvertPointToMapPoint(Point point)
        {
            int x = (int)point.X / MainWindowViewModel.CellSize;
            int y = (int)point.Y / MainWindowViewModel.CellSize;
            return new Vector2D(x, y);
        }
        
        public static Vector2D operator +(Vector2D a, Vector2D b)
        {
            return new Vector2D(a.x + b.x, a.y + b.y);
        }

        public override bool Equals(object? obj)
        {
            if (obj is Vector2D other)
            {
                return x == other.x && y == other.y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }
        
        
        public override string ToString() => $"[{x},{y}]";
    }
}