using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class Astar
    {
        private const float StraightCost = 1;
        private static readonly float DiagonalCost = (float)Math.Sqrt(2);

        private class NeighbourCell
        {
            public int CellKey;
            public float GCost;
        }

        private static List<NeighbourCell> GetNeighbours(Cell[,] grid, PathfindingCell cell)
        {
            //TODO use here the bits
            List<NeighbourCell> neighbours = new List<NeighbourCell>();
            int i = 0;
            foreach (var direction in DirectionsVector.AllDirections)
            {
                
                int newX = cell.Position.x + direction.x;
                int newY = cell.Position.y + direction.y;

                if (newX >= 0 && newX < grid.GetLength(1) && newY >= 0 && newY < grid.GetLength(0))
                {
                    neighbours.Add(new NeighbourCell(){CellKey = newY * MainWindowViewModel.MapSize + newX, GCost = (i % 2 == 0)? StraightCost : DiagonalCost });
                }

                i++;
            }

            return neighbours;
        }

        public static float Heuristic(PathfindingCell source, PathfindingCell destination)
        {
            
            float dx = Math.Abs(source.Position.x - destination.Position.x);
            float dy = Math.Abs(source.Position.y - destination.Position.y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }

        public static List<Vector2D> FindPath(Cell[,] grid, Vector2D start, Vector2D end)
        {
            FastPriorityQueue open = new FastPriorityQueue(grid.GetLength(0) * grid.GetLength(1));
            Cell startCell = grid[end.y, end.x];
            PathfindingCell goalCell  = new PathfindingCell(grid[start.y, start.x]);
            
            HashSet<int> closedSet = new HashSet<int>();
            Dictionary<int, PathfindingCell> getElement = new Dictionary<int, PathfindingCell>();

            open.Enqueue(new PathfindingCell(startCell), 0);

            PathfindingCell currentCell = null;
            bool finished = false;
            while (open.Count > 0) 
            {
                currentCell = open.Dequeue();

                if (goalCell.Position.x == currentCell.Position.x && goalCell.Position.y == currentCell.Position.y)
                {
                    finished = true;
                    break;
                }

                closedSet.Add(currentCell.Position.x + currentCell.Position.y * MainWindowViewModel.MapSize);

                
                
                foreach (var neighbourKey in GetNeighbours(grid, currentCell))
                {
                    if (getElement.TryGetValue(neighbourKey.CellKey, out var neighbour)){}
                    else
                    {
                        neighbour = new PathfindingCell(grid[neighbourKey.CellKey / MainWindowViewModel.MapSize,
                            neighbourKey.CellKey % MainWindowViewModel.MapSize]); 
                        getElement.Add(neighbourKey.CellKey, neighbour);
                    }
                    var g = currentCell.GCost + neighbourKey.GCost;
                    
                    if (!neighbour.Walkable || closedSet.Contains(neighbour.Position.x + neighbour.Position.y * MainWindowViewModel.MapSize))
                        continue;

                   
                    if (!open.Contains(neighbour))
                    {
                        neighbour.GCost = g;
                        neighbour.HCost = Heuristic(neighbour, goalCell);
                        neighbour.Parent = currentCell;
                        open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
                    } 
                    else if (g + neighbour.HCost < neighbour.fCost) {
                        neighbour.GCost = g;
                        neighbour.Parent = currentCell;
                        open.UpdatePriority(neighbour, neighbour.GCost + neighbour.HCost);
                    }
                }
            }

            var path = new List<Vector2D>();
            if(!finished) return path;
            
            while (currentCell != null) {
                path.Add(currentCell.Position);
                currentCell = currentCell.Parent;
            }

            return path;
        }
    }
}