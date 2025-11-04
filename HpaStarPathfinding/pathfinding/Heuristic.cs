using System;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class Heuristic
    {
        public static float StraightCost = 1f;
        public static float DiagonalCost = 1.414f;
        public static float OctileDistanceHeuristic(PathfindingCell source, PathfindingCell destination)
        {
            
            float dx = Math.Abs(source.Position.x - destination.Position.x);
            float dy = Math.Abs(source.Position.y - destination.Position.y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }
    }
}