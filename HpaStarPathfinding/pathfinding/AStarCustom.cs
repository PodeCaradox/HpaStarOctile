using System;
using System.Collections.Generic;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class AStarCustom
    {
        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.414f;

        private static List<int> GetNeighbours(PathfindingCell cell, Vector2D min, Vector2D max)
        {
            //TODO use here the bits
            List<int> neighbours = new List<int>();

            foreach (var direction in DirectionsVector.AllDirections)
            {
                int newX = cell.Position.x + direction.x;
                int newY = cell.Position.y + direction.y;

                if (newX >= min.x && newX < max.x && newY >= min.y && newY < max.y)
                {
                    neighbours.Add(newY * MainWindowViewModel.MapSize + newX);
                }
            }

            return neighbours;
        }

        public static float FindPath(Cell[,] grid, Vector2D start, Vector2D end, Vector2D min, Vector2D max)
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

                var g = currentCell.GCost + 1;
                
                foreach (var neighbourKey in GetNeighbours(currentCell, min, max))
                {
                    if (getElement.TryGetValue(neighbourKey, out var neighbour)){}
                    else
                    {
                        neighbour = new PathfindingCell(grid[neighbourKey / MainWindowViewModel.MapSize,
                            neighbourKey % MainWindowViewModel.MapSize]); 
                        getElement.Add(neighbourKey, neighbour);
                    }
                        
                    
                    if (!neighbour.Walkable || closedSet.Contains(neighbour.Position.x + neighbour.Position.y * MainWindowViewModel.MapSize))
                        continue;

                   
                    if (!open.Contains(neighbour))
                    {
                        neighbour.GCost = g;
                        neighbour.HCost = Astar.Heuristic(neighbour, goalCell);
                        neighbour.Parent = currentCell;
                        open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
                    }
                }
            }

            
            if(!finished) return -1;
            float cost = 0;
            while (currentCell != null) {
                cost += currentCell.GCost;
                currentCell = currentCell.Parent;
            }

            return cost;
        }
    }
}