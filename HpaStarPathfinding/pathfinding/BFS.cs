using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class BFS
{
    public static ushort[] BfsFromStartPos(Cell[] grid, Vector2D start)
    {
        ushort[] bfs = Enumerable.Repeat(ushort.MaxValue, ChunkSize * ChunkSize).ToArray();
        Queue<int> openList = new Queue<int>();
        openList.Enqueue(start.y * CorrectedMapSizeX + start.x);
        int keyX = start.x % ChunkSize;
        int keyY = start.y % ChunkSize;
        bfs[keyY * ChunkSize + keyX] = 0;
        while (openList.Count > 0)
        {
            int current = openList.Dequeue();
            int x = current % CorrectedMapSizeX;
            int y = current / CorrectedMapSizeX;
            keyX = x % ChunkSize;
            keyY = y % ChunkSize;
            int key = keyY * ChunkSize + keyX;

            byte connections = grid[current].Connections;
            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                if ((connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE)
                    continue;

                var dir = DirectionsVector.AllDirections[i];
                int newX = keyX + dir.x;
                int newY = keyY + dir.y;
                if (newX is < 0 or >= ChunkSize || newY is < 0 or >= ChunkSize)
                    continue;

                int neighbourKey = newY * ChunkSize + newX;
                ushort newCost = (ushort)(bfs[key] + (i % 2 == 0 ? Heuristic.StraightCost : Heuristic.DiagonalCost));
                if (bfs[neighbourKey] != ushort.MaxValue)
                {
                    if (bfs[neighbourKey] > newCost)
                        bfs[neighbourKey] = newCost;

                    continue;
                }

                openList.Enqueue(current + dir.y * CorrectedMapSizeX + dir.x);
                bfs[neighbourKey] = newCost;
            }
        }

        return bfs;
    }
        
    public static ushort[] BfsFromStartPosWithRegionFill(Cell[] grid, ref Chunk chunk, Vector2D start, byte portalKey)
    {
        ushort[] bfs = Enumerable.Repeat(ushort.MaxValue, ChunkSize * ChunkSize).ToArray();
        Queue<int> openList = new Queue<int>();
        openList.Enqueue(start.y * CorrectedMapSizeX + start.x);
        int keyX = start.x % ChunkSize;
        int keyY = start.y % ChunkSize;
        bfs[keyY * ChunkSize + keyX] = 0;
        while (openList.Count > 0)
        {
            int current = openList.Dequeue();
            int x = current % CorrectedMapSizeX;
            int y = current / CorrectedMapSizeX;
            keyX = x % ChunkSize;
            keyY = y % ChunkSize;
            int key = keyY * ChunkSize + keyX;
            chunk.regions[key] = portalKey;

            byte connections = grid[current].Connections;
            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                if ((connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE)
                    continue;

                var dir = DirectionsVector.AllDirections[i];
                int newX = keyX + dir.x;
                int newY = keyY + dir.y;
                if (newX is < 0 or >= ChunkSize || newY is < 0 or >= ChunkSize)
                    continue;

                int neighbourKey = newY * ChunkSize + newX;
                ushort newCost = (ushort)(bfs[key] + (i % 2 == 0 ? Heuristic.StraightCost : Heuristic.DiagonalCost));
                if (bfs[neighbourKey] != ushort.MaxValue)
                {
                    if (bfs[neighbourKey] > newCost)
                        bfs[neighbourKey] = newCost;

                    continue;
                }

                openList.Enqueue(current + dir.y * CorrectedMapSizeX + dir.x);
                bfs[neighbourKey] = newCost;
            }
        }

        return bfs;
    }
    
    public static void ResetRegionsForPortal(Cell[] cells, ref Chunk chunk, Vector2D start, byte portalKey)
    {
        Queue<int> openList = new Queue<int>();
        openList.Enqueue(start.y * CorrectedMapSizeX + start.x);
        while (openList.Count > 0)
        {
            int current = openList.Dequeue();
            int x = current % CorrectedMapSizeX;
            int y = current / CorrectedMapSizeX;
            int keyX = x % ChunkSize;
            int keyY = y % ChunkSize;
            chunk.regions[keyY * ChunkSize + keyX] = byte.MaxValue;

            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                var dir = DirectionsVector.AllDirections[i];
                int newX = keyX + dir.x;
                int newY = keyY + dir.y;
                if (newX is < 0 or >= ChunkSize || newY is < 0 or >= ChunkSize ||
                    chunk.regions[newY * ChunkSize + newX] != portalKey)
                    continue;

                openList.Enqueue(current + dir.y * CorrectedMapSizeX + dir.x);
            }
        }
    }
        
    public static ushort GetCostForPath(ushort[] costFields, Vector2D goal)
    {
        int key = goal.x % ChunkSize + goal.y % ChunkSize * ChunkSize;
        return costFields[key];
    }
}
