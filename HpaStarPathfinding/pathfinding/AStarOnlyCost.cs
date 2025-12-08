using System;
using System.Collections.Generic;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class AStarOnlyCost
    {
        private class NeighbourCell
        {
            public int CellKey;
            public int GCost;
        }
        
        private static List<NeighbourCell> GetNeighbours(PathfindingCell cell, Vector2D min, Vector2D max)
        {
            List<NeighbourCell> neighbours = new List<NeighbourCell>();
            int i = 0;
            foreach (var direction in DirectionsVector.AllDirections)
            {
                int newX = cell.Position.x + direction.x;
                int newY = cell.Position.y + direction.y;
                if (newX >= min.x && newX < max.x && newY >= min.y && newY < max.y && (cell.Connections & DirectionsAsByte.AllDirectionsAsByte[i]) == DirectionsAsByte.WALKABLE)
                {
                    neighbours.Add(new NeighbourCell(){CellKey = newY * MainWindowViewModel.MapSizeX + newX, GCost = (i % 2 == 0)? Heuristic.StraightCost : Heuristic.DiagonalCost });
                }

                i++;
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

                closedSet.Add(currentCell.Position.x + currentCell.Position.y * MainWindowViewModel.MapSizeX);
                
                foreach (var neighbourKey in GetNeighbours(currentCell, min, max))
                {
                    if (getElement.TryGetValue(neighbourKey.CellKey, out var neighbour)){}
                    else
                    {
                        neighbour = new PathfindingCell(grid[neighbourKey.CellKey / MainWindowViewModel.MapSizeX,
                            neighbourKey.CellKey % MainWindowViewModel.MapSizeX]); 
                        getElement.Add(neighbourKey.CellKey, neighbour);
                    }
                    int g = currentCell.GCost + neighbourKey.GCost;

                    
                    if (closedSet.Contains(neighbour.Position.x + neighbour.Position.y * MainWindowViewModel.MapSizeX))
                        continue;

                   
                    if (!open.Contains(neighbour))
                    {
                        neighbour.GCost = g;
                        neighbour.HCost = Heuristic.GetHeuristic(neighbour, goalCell);
                        neighbour.Parent = currentCell;
                        open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
                    }
                }
            }

            
            if(!finished) return -1;

            return currentCell.GCost;
        }
    }
}