using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class BFS
{
    private class NeighbourCell(Vector2D cellPos, ushort cost, ushort key)
    {
        public readonly Vector2D CellPos = cellPos;
        public readonly ushort GCost = cost;
        public readonly ushort Key = key;
    }
        
    public static ushort[] BfsFromStartPos(Cell[] grid, Vector2D start)
    {
        ushort[] bfs = Enumerable.Repeat(ushort.MaxValue, ChunkSize * ChunkSize).ToArray();
        Queue<Vector2D> openList = new Queue<Vector2D>();
        openList.Enqueue(start); 
        int keyX = start.x % ChunkSize;
        int keyY = start.y % ChunkSize;
        bfs[keyY * ChunkSize + keyX] = 0;
        while (openList.Count > 0)
        {
            Vector2D current = openList.Dequeue();
            Cell currentCell = grid[current.y * MapSizeX + current.x];
                
            keyX = current.x % ChunkSize;
            keyY = current.y % ChunkSize;
            int key = keyY * ChunkSize + keyX;
            foreach (var neighbourKey in GetNeighbours(currentCell, keyX, keyY))
            {
                if (bfs[neighbourKey.Key] != ushort.MaxValue)
                {
                    if(bfs[neighbourKey.Key] > bfs[key] + neighbourKey.GCost) 
                        bfs[neighbourKey.Key] = (ushort)(bfs[key] + neighbourKey.GCost);
                        
                    continue;
                }
                    
                openList.Enqueue(neighbourKey.CellPos);
                bfs[neighbourKey.Key] = (ushort)(bfs[key] + neighbourKey.GCost);
            }

        }

        return bfs;
    }
        
    public static ushort[] BfsFromStartPosWithRegionFill(Cell[] grid, Vector2D start, byte portalKey)
    {
        ushort[] bfs = Enumerable.Repeat(ushort.MaxValue, ChunkSize * ChunkSize).ToArray();
        Queue<Vector2D> openList = new Queue<Vector2D>();
        openList.Enqueue(start); 
        int keyX = start.x % ChunkSize;
        int keyY = start.y % ChunkSize;
        bfs[keyY * ChunkSize + keyX] = 0;
        while (openList.Count > 0)
        {
            Vector2D current = openList.Dequeue();
            Cell currentCell = grid[current.y * MapSizeX + current.x];
            currentCell.Region = portalKey;
                
            keyX = current.x % ChunkSize;
            keyY = current.y % ChunkSize;
            int key = keyY * ChunkSize + keyX;
            foreach (var neighbourKey in GetNeighbours(currentCell, keyX, keyY))
            {
                if (bfs[neighbourKey.Key] != ushort.MaxValue)
                {
                    if(bfs[neighbourKey.Key] > bfs[key] + neighbourKey.GCost) 
                        bfs[neighbourKey.Key] = (ushort)(bfs[key] + neighbourKey.GCost);
                    continue;
                }
                    
                openList.Enqueue(neighbourKey.CellPos);
                bfs[neighbourKey.Key] = (ushort)(bfs[key] + neighbourKey.GCost);
            }

        }

        return bfs;
    }
        
    private static List<NeighbourCell> GetNeighbours(Cell cell, int posX, int posY)
    {
        List<NeighbourCell> neighbours = [];
        for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
        {
            var dir = DirectionsVector.AllDirections[i];
            int newX = posX + dir.x;
            int newY = posY + dir.y;
            if (newX is < 0 or >= ChunkSize || newY is < 0 or >= ChunkSize ||
                (cell.Connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE) continue;
            var pos = cell.Position + dir;
            ushort key = (ushort)(newX + newY * ChunkSize);
            neighbours.Add(new NeighbourCell(pos, i % 2 == 0? Heuristic.StraightCost : Heuristic.DiagonalCost, key));
        }

        return neighbours;
    }

    public static ushort GetCostForPath(ushort[] costFields, Vector2D goal)
    {
        int key = goal.x % ChunkSize + goal.y % ChunkSize * ChunkSize;
        return costFields[key];
    }
}