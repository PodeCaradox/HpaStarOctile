using HpaStarPathfinding.model.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public static class BFS
    {
        private class NeighbourCell(Vector2D cellPos, ushort cost)
        {
            public readonly Vector2D CellPos = cellPos;
            public readonly ushort GCost = cost;
        }
        
        public static ushort[] BfsFromStartPos(Cell[] grid, Vector2D start, Vector2D min, Vector2D max)
        {
            ushort[] bfs = Enumerable.Repeat(ushort.MaxValue, ChunkSize * ChunkSize).ToArray();
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
        
        public static ushort[] BfsFromStartPosWithRegionFill(Cell[] grid, Vector2D start, Vector2D min, Vector2D max, byte portalKey)
        {
            ushort[] bfs = Enumerable.Repeat(ushort.MaxValue, ChunkSize * ChunkSize).ToArray();
            Queue<Vector2D> openList = new Queue<Vector2D>();
            openList.Enqueue(start); 
            int key = start.x % ChunkSize + start.y % ChunkSize * ChunkSize;
            bfs[key] = 0;
            while (openList.Count > 0)
            {
                Vector2D current = openList.Dequeue();
                key = current.x % ChunkSize + current.y % ChunkSize * ChunkSize;
                
                Cell currentCell = grid[current.y * MapSizeX + current.x];
                currentCell.Region = portalKey;
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
                    neighbours.Add(new NeighbourCell(new Vector2D(newX, newY), (i % 2 == 0)? Heuristic.StraightCost : Heuristic.DiagonalCost));
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