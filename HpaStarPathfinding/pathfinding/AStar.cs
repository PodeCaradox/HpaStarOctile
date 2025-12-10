using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.ViewModel;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class Astar
    {

        private class NeighbourCell
        {
            public int CellKey;
            public int GCost;
        }

        private static List<NeighbourCell> GetNeighbours(PathfindingCell cell)
        {
            List<NeighbourCell> neighbours = new List<NeighbourCell>();
            int i = 0;
            foreach (var direction in DirectionsVector.AllDirections)
            {
                if ((cell.Connections & DirectionsAsByte.AllDirectionsAsByte[i]) == DirectionsAsByte.WALKABLE)
                {
                    int newX = cell.Position.x + direction.x;
                    int newY = cell.Position.y + direction.y;
                    neighbours.Add(new NeighbourCell(){CellKey = newY * MapSizeX + newX, GCost = (i % 2 == 0)? Heuristic.StraightCost : Heuristic.DiagonalCost });
                }

                i++;
            }

            return neighbours;
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

                closedSet.Add(currentCell.Position.x + currentCell.Position.y * MapSizeX);

                
                
                foreach (var neighbourKey in GetNeighbours(currentCell))
                {
                    if (getElement.TryGetValue(neighbourKey.CellKey, out var neighbour)){}
                    else
                    {
                        neighbour = new PathfindingCell(grid[neighbourKey.CellKey / MapSizeX,
                            neighbourKey.CellKey % MapSizeX]); 
                        getElement.Add(neighbourKey.CellKey, neighbour);
                    }
                    int g = currentCell.GCost + neighbourKey.GCost;
                    
                    if (closedSet.Contains(neighbour.Position.x + neighbour.Position.y * MapSizeX))
                        continue;

                   
                    if (!open.Contains(neighbour))
                    {
                        neighbour.GCost = g;
                        neighbour.HCost = Heuristic.GetHeuristic(neighbour, goalCell);
                        neighbour.Parent = currentCell;
                        open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
                    } 
                    else if (g + neighbour.HCost < neighbour.FCost) {
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