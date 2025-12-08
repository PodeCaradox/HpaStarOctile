using System;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class Heuristic
    {
        public static ushort StraightOctileCost = 10;
        public static ushort DiagonalOctileCost = 14;
        
        public static float StraightChebyshevCost = 1;
        public static float DiagonalChebyshevCost = 1;
        
        public static ushort StraightCost = StraightOctileCost;
        public static ushort DiagonalCost = DiagonalOctileCost;
        
        public static int GetHeuristic(PathfindingCell source, PathfindingCell destination)
        {
            return OctileDistanceHeuristic(source, destination);
        }
        
        public static int OctileDistanceHeuristic(PathfindingCell source, PathfindingCell destination)
        {
            
            int dx = Math.Abs(source.Position.x - destination.Position.x);
            int dy = Math.Abs(source.Position.y - destination.Position.y);
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