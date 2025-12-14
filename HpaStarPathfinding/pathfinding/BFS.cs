using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.ViewModel;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class BFS
    {
        private class NeighbourCell
        {
            public Vector2D CellPos;
            public ushort GCost;
        }
        
        public static ushort[] FindAllCostsInChunkFromStartPos(Cell[] grid, Vector2D start, Vector2D min, Vector2D max)
        {
            ushort[] bfs = new ushort[ChunkSize * ChunkSize];
            for (int i = 0; i < bfs.Length; i++)
            {
                bfs[i] = ushort.MaxValue;
            }
            Queue<Vector2D> openList = new Queue<Vector2D>();
            openList.Enqueue(start); 
            int key = start.x % ChunkSize + start.y % ChunkSize * ChunkSize;
            bfs[key] = 0;
            while (openList.Count > 0)
            {
                Vector2D current = openList.Dequeue();
                key = current.x % ChunkSize + current.y % ChunkSize * ChunkSize;
                
                Cell currentCell = grid[current.y * MapSizeX + current.x];
                foreach (var neighbourKey in GetNeighbours(currentCell, min, max))
                {
                    int otherKey = neighbourKey.CellPos.x % ChunkSize + neighbourKey.CellPos.y % ChunkSize * ChunkSize;
                    if (bfs[otherKey] != ushort.MaxValue)
                    {
                        if(bfs[otherKey] > bfs[key] + neighbourKey.GCost) 
                            bfs[otherKey] = (ushort)(bfs[key] + neighbourKey.GCost);
                        
                        continue;
                    }
                    
                    openList.Enqueue(neighbourKey.CellPos);
                    bfs[otherKey] = (ushort)(bfs[key] + neighbourKey.GCost);
                }

            }

            return bfs;
        }
        
        private static List<NeighbourCell> GetNeighbours(Cell cell, Vector2D min, Vector2D max)
        {
            List<NeighbourCell> neighbours = new List<NeighbourCell>();
            int i = 0;
            foreach (var direction in DirectionsVector.AllDirections)
            {
                int newX = cell.Position.x + direction.x;
                int newY = cell.Position.y + direction.y;
                if (newX >= min.x && newX < max.x && newY >= min.y && newY < max.y && (cell.Connections & DirectionsAsByte.AllDirectionsAsByte[i]) == DirectionsAsByte.WALKABLE)
                {
                    neighbours.Add(new NeighbourCell(){CellPos = new Vector2D(newX, newY), GCost = (i % 2 == 0)? Heuristic.StraightCost : Heuristic.DiagonalCost });
                }
                i++;
            }

            return neighbours;
        }

        public static ushort GetCostForPath(ushort[] costFields, Vector2D goal)
        {
            int key = goal.x % ChunkSize + goal.y % ChunkSize * ChunkSize;
            return costFields[key];
        }
    }
}