using System;
using System.Numerics;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.model.pathfinding.PathfindingCellTypes;
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
        
        public static int GetHeuristic(Vector2D source, Vector2D destination)
        {
            return OctileDistanceHeuristic(source, destination);
        }
        
        public static int OctileDistanceHeuristic(Vector2D source, Vector2D destination)
        {
            
            int dx = Math.Abs(source.x - destination.x);
            int dy = Math.Abs(source.y - destination.y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }
        
        
        public static float ChebyshevDistance(Vector2D source, Vector2D destination)
        {
            
            int dx = Math.Abs(source.x - destination.x);
            int dy = Math.Abs(source.y - destination.y);
            return Math.Max(dx, dy);
        }
    }
}