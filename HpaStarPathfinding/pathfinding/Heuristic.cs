using System;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class Heuristic
    {
        public static float StraightOctileCost = 10;
        public static float DiagonalOctileCost = 14;
        
        public static float StraightChebyshevCost = 1f;
        public static float DiagonalChebyshevCost = 1f;
        
        public static float StraightCost = StraightOctileCost;
        public static float DiagonalCost = DiagonalOctileCost;
        
        public static float GetHeuristic(PathfindingCell source, PathfindingCell destination)
        {
            return OctileDistanceHeuristic(source, destination);
        }
        
        public static float OctileDistanceHeuristic(PathfindingCell source, PathfindingCell destination)
        {
            
            float dx = Math.Abs(source.Position.x - destination.Position.x);
            float dy = Math.Abs(source.Position.y - destination.Position.y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }
        
        
        public static float ChebyshevDistance(PathfindingCell source, PathfindingCell destination)
        {
            
            int dx = Math.Abs(source.Position.x - destination.Position.x);
            int dy = Math.Abs(source.Position.y - destination.Position.y);
            return Math.Max(dx, dy);
        }
    }
}