using System;
using System.Collections.Generic;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{

    public class Astar
    {

        private static readonly Vector2D[] Directions = new Vector2D[]
        {
            new Vector2D(0, 1), // N
            new Vector2D(1, 1), // NE
            new Vector2D(1, 0), // E
            new Vector2D(1, -1), // SE
            new Vector2D(0, -1), // S
            new Vector2D(-1, -1), // SW
            new Vector2D(-1, 0), // W
            new Vector2D(-1, 1) // NW
        };

        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.414f;

        private static List<Node> GetNeighbours(Node[,] grid,Node node)
        {
            List<Node> neighbours = new List<Node>();

            foreach (var direction in Directions)
            {
                int newX = node.Position.X + direction.X;
                int newY = node.Position.Y + direction.Y;

                if (newX >= 0 && newX < grid.GetLength(0) && newY >= 0 && newY < grid.GetLength(1))
                {
                    neighbours.Add(grid[newX, newY]);
                }
            }

            return neighbours;
        }

        private static float GetDistance(Node a, Node b)
        {
            int dstX = Math.Abs(a.Position.X - b.Position.X);
            int dstY = Math.Abs(a.Position.Y - b.Position.Y);
            if (dstX > dstY)
                return DiagonalCost * dstY + StraightCost * (dstX - dstY);
            return DiagonalCost * dstX + StraightCost * (dstY - dstX);
        }

        private static Node GetNodeWithLowestFCost(HashSet<Node> openSet)
        {
            Node lowest = null;
            foreach (var node in openSet)
            {
                if (lowest == null || node.FCost < lowest.FCost ||
                    (node.FCost == lowest.FCost && node.HCost < lowest.HCost))
                {
                    lowest = node;
                }
            }

            return lowest;
        }

        public static List<Vector2D> FindPath(Node[,] grid, Vector2D start, Vector2D end)
        {
            Node startNode = grid[start.X, start.Y];
            Node endNode = grid[end.X, end.Y];

            if (!startNode.Walkable || !endNode.Walkable)
                return null;

            HashSet<Node> openSet = new HashSet<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();

            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = GetNodeWithLowestFCost(openSet);

                if (currentNode == endNode)
                    return RetracePath(startNode, endNode);

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                foreach (var neighbour in GetNeighbours(grid, currentNode))
                {
                    if (!neighbour.Walkable || closedSet.Contains(neighbour))
                        continue;

                    float tentativeGCost = currentNode.GCost + GetDistance(currentNode, neighbour);
                    if (tentativeGCost < neighbour.GCost || !openSet.Contains(neighbour))
                    {
                        neighbour.GCost = tentativeGCost;
                        neighbour.HCost = GetDistance(neighbour, endNode);
                        neighbour.Parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }
                }
            }

            return null;
        }

        private static List<Vector2D> RetracePath(Node startNode, Node endNode)
        {
            List<Vector2D> path = new List<Vector2D>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.Position);
                currentNode = currentNode.Parent;
            }

            path.Add(startNode.Position);

            path.Reverse();
            return path;
        }
    } 
}

