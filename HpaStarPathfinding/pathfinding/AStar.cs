using System;
using System.Collections.Generic;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class Astar
    {
        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.414f;

        private static List<Cell> GetNeighbours(Cell[,] grid, Cell cell)
        {
            List<Cell> neighbours = new List<Cell>();

            foreach (var direction in DirectionsVector.AllDirections)
            {
                int newX = cell.Position.x + direction.x;
                int newY = cell.Position.y + direction.y;

                if (newX >= 0 && newX < grid.GetLength(1) && newY >= 0 && newY < grid.GetLength(0))
                {
                    neighbours.Add(grid[newY, newX]);
                }
            }

            return neighbours;
        }

        public static float GetDistance(Cell a, Cell b)
        {
            int dstX = Math.Abs(a.Position.x - b.Position.x);
            int dstY = Math.Abs(a.Position.y - b.Position.y);
            if (dstX > dstY)
                return DiagonalCost * dstY + StraightCost * (dstX - dstY);
            return DiagonalCost * dstX + StraightCost * (dstY - dstX);
        }

        public static Cell GetNodeWithLowestFCost(HashSet<Cell> openSet)
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

        public static List<Vector2D> FindPath(Cell[,] grid, Vector2D start, Vector2D end)
        {
            Cell startCell = grid[start.y, start.x];
            Cell endCell = grid[end.y, end.x];

            if (!startCell.Walkable || !endCell.Walkable)
                return null;

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

                foreach (var neighbour in GetNeighbours(grid, currentCell))
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

            return null;
        }

        public static List<Vector2D> RetracePath(Cell startCell, Cell endCell)
        {
            List<Vector2D> path = new List<Vector2D>();
            Cell currentCell = endCell;

            while (currentCell != startCell)
            {
                path.Add(currentCell.Position);
                currentCell = currentCell.Parent;
            }

            path.Add(startCell.Position);

            path.Reverse();
            return path;
        }
    }
}