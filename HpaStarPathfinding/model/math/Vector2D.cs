using System.Windows;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.model.math;

public readonly struct Vector2D : IEquatable<Vector2D>
{
    public int x { get; }
    public int y { get; }

    public Vector2D(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

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

    public static bool operator ==(Vector2D a, Vector2D b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Vector2D a, Vector2D b)
    {
        return a.x != b.x || a.y != b.y;
    }

    public bool Equals(Vector2D other)
    {
        return x == other.x && y == other.y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode();
    }

    public override string ToString() => $"[{x},{y}]";
}
