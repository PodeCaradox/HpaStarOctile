using System;
using System.Collections.Generic;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class Dijkstra
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

        private static Cell GetNodeWithLowestGCost(HashSet<Cell> openSet)
        {
            Cell lowest = null;
            foreach (var node in openSet)
            {
                if (lowest == null || node.GCost < lowest.GCost)
                {
                    lowest = node;
                }
            }

            return lowest;
        }

        /// <summary>
        /// Returns a dictionary of goal positions and their cost from the start.
        /// Stops once all reachable goals have been found.
        /// </summary>
        public static Dictionary<Vector2D, float> FindCostsToGoals(
            Cell[,] grid,
            Vector2D start,
            HashSet<Vector2D> goalsToFind,
            Vector2D min,
            Vector2D max)
        {
            Cell startCell = grid[start.y, start.x];
            if (!startCell.Walkable) return null;

            HashSet<Cell> openSet = new HashSet<Cell>();
            HashSet<Cell> closedSet = new HashSet<Cell>();
            Dictionary<Vector2D, float> goalCosts = new Dictionary<Vector2D, float>();

            startCell.GCost = 0;
            openSet.Add(startCell);

            while (openSet.Count > 0 && goalsToFind.Count > 0)
            {
                Cell currentCell = GetNodeWithLowestGCost(openSet);

                if (goalsToFind.Contains(currentCell.Position))
                {
                    goalCosts[currentCell.Position] = currentCell.GCost;
                    goalsToFind.Remove(currentCell.Position);

                    if (goalsToFind.Count == 0)
                        break; // All goals found
                }

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
                        neighbour.Parent = currentCell;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }
                }
            }

            return goalCosts;
        }
    }
}