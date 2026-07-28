using HpaStarPathfinding.model.math;

namespace HpaStarPathfinding.pathfinding;

internal static class Heuristic
{
    public const ushort StraightCost = 10;
    public const ushort DiagonalCost = 14;

    public static int GetHeuristic(Vector2D source, Vector2D destination)
    {
        return OctileDistanceHeuristic(source, destination);
    }

    private static int OctileDistanceHeuristic(Vector2D source, Vector2D destination)
    {
        int dx = Math.Abs(source.x - destination.x);
        int dy = Math.Abs(source.y - destination.y);
        return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
    }
}
