using HpaStarPathfinding.model.math;

namespace HpaStarPathfinding.pathfinding;

internal static class Heuristic
{
    private const ushort StraightOctileCost = 10;
    private const ushort DiagonalOctileCost = 14;

    public const ushort StraightCost = StraightOctileCost;
    public const ushort DiagonalCost = DiagonalOctileCost;
        
    //public static readonly float StraightChebyshevCost = 1;
    //public static readonly float DiagonalChebyshevCost = 1;
        
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
        
    // public static float ChebyshevDistance(Vector2D source, Vector2D destination)
    // {
    //     
    //     int dx = Math.Abs(source.x - destination.x);
    //     int dy = Math.Abs(source.y - destination.y);
    //     return Math.Max(dx, dy);
    // }
}