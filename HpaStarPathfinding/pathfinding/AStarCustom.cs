namespace HpaStarPathfinding.pathfinding
{
    
    using System;
    using System.Collections.Generic;
    using ViewModel;
    
    public class AStarCustom
    {

        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.414f;

        private static List<Cell> GetNeighbours(Cell[,] grid, Cell cell, Vector2D min, Vector2D max)
        {
            List<Cell> neighbours = new List<Cell>();

            foreach (var direction in DirectionsVector.AllDirections)
            {
                int newX = cell.Position.x + direction.x;
                int newY = cell.Position.y + direction.y;

                if (newX >= min.x && newX < max.x && newY >= min.y && newY < max.y)
                {
                    neighbours.Add(grid[newY, newX]);
                }
            }

            return neighbours;
        }

        private static float GetDistance(Cell a, Cell b)
        {
            int dstX = Math.Abs(a.Position.x - b.Position.x);
            int dstY = Math.Abs(a.Position.y - b.Position.y);
            if (dstX > dstY)
                return DiagonalCost * dstY + StraightCost * (dstX - dstY);
            return DiagonalCost * dstX + StraightCost * (dstY - dstX);
        }

        private static Cell GetNodeWithLowestFCost(HashSet<Cell> openSet)
        {
            Cell lowest = null;
            foreach (var node in openSet)
            {
                if (lowest == null || node.fCost < lowest.fCost ||
                    (node.fCost == lowest.fCost && node.HCost < lowest.HCost))
                {
                    lowest = node;
                }
            }

            return lowest;
        }

        public static float FindPath(Cell[,] grid, Vector2D start, Vector2D end, Vector2D min, Vector2D max)
        {
            Cell startCell = grid[start.y, start.x];
            Cell endCell = grid[end.y, end.x];

            //can be ignored on hpaStar
            if (!startCell.Walkable || !endCell.Walkable)
                return -1;

            HashSet<Cell> openSet = new HashSet<Cell>();
            HashSet<Cell> closedSet = new HashSet<Cell>();

            openSet.Add(startCell);

            while (openSet.Count > 0)
            {
                Cell currentCell = GetNodeWithLowestFCost(openSet);

                if (currentCell.Position.Equals(endCell.Position))
                    return RetracePath(startCell, endCell);

                openSet.Remove(currentCell);
                closedSet.Add(currentCell);

                foreach (var neighbour in GetNeighbours(grid, currentCell, min, max))
                {
                    if (!neighbour.Walkable || closedSet.Contains(neighbour))
                        continue;

                    float tentativeGCost = currentCell.GCost + GetDistance(currentCell, neighbour);
                    if (tentativeGCost < neighbour.GCost || !openSet.Contains(neighbour))
                    {
                        neighbour.GCost = tentativeGCost;
                        neighbour.HCost = GetDistance(neighbour, endCell);
                        neighbour.Parent = currentCell;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }
                }
            }

            return -1;
        }

        private static float RetracePath(Cell startCell, Cell endCell)
        {
            float cost = 0;

            cost += endCell.GCost;
            return cost;
        }
    
    }
}