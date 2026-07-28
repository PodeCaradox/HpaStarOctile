using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class BFS
{
    //Reusable queue buffers, one per thread (chunk rebuilds run in parallel)
    [ThreadStatic] private static int[]? _openListBuffer;
    [ThreadStatic] private static byte[]? _inQueueBuffer;

    private static int[] OpenListBuffer => _openListBuffer ??= new int[CellsInChunk + 1];
    private static byte[] InQueueBuffer => _inQueueBuffer ??= new byte[CellsInChunk];

    //Flood fills the chunk from start and returns the cheapest cost to reach every cell.
    //When regionFill is set, every reached cell is additionally stamped with the given portal key.
    public static ushort[] BfsFromStartPos(Cell[] grid, Vector2D start, Chunk? regionFill = null, byte portalKey = 0)
    {
        ushort[] costs = new ushort[CellsInChunk];
        Array.Fill(costs, ushort.MaxValue);

        var openList = OpenListBuffer;
        var inQueue = InQueueBuffer;
        Array.Clear(inQueue, 0, inQueue.Length);
        int head = 0, count = 0;

        int startKey = start.y * CorrectedMapSizeX + start.x;
        int keyX = start.x % ChunkSize;
        int keyY = start.y % ChunkSize;
        costs[keyY * ChunkSize + keyX] = 0;
        openList[count++] = startKey;
        inQueue[keyY * ChunkSize + keyX] = 1;

        while (count > 0)
        {
            int current = openList[head];
            head = (head + 1) % openList.Length;
            count--;
            int x = current % CorrectedMapSizeX;
            int y = current / CorrectedMapSizeX;
            keyX = x % ChunkSize;
            keyY = y % ChunkSize;
            int key = keyY * ChunkSize + keyX;
            inQueue[key] = 0;
            if (regionFill != null) regionFill.regions[key] = portalKey;

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
                ushort newCost = (ushort)(costs[key] + (i % 2 == 0 ? Heuristic.StraightCost : Heuristic.DiagonalCost));
                if (costs[neighbourKey] <= newCost)
                    continue;

                costs[neighbourKey] = newCost;
                if (inQueue[neighbourKey] != 0) continue; //already waiting, will be processed with the better cost
                inQueue[neighbourKey] = 1;
                openList[(head + count) % openList.Length] = current + dir.y * CorrectedMapSizeX + dir.x;
                count++;
            }
        }

        return costs;
    }

    public static void ResetRegionsForPortal(ref Chunk chunk, Vector2D start, byte portalKey)
    {
        var openList = OpenListBuffer;
        int head = 0, count = 0;

        int startKey = start.y * CorrectedMapSizeX + start.x;
        chunk.regions[start.y % ChunkSize * ChunkSize + start.x % ChunkSize] = byte.MaxValue;
        openList[count++] = startKey;

        while (count > 0)
        {
            int current = openList[head];
            head = (head + 1) % openList.Length;
            count--;
            int keyX = current % CorrectedMapSizeX % ChunkSize;
            int keyY = current / CorrectedMapSizeX % ChunkSize;

            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                var dir = DirectionsVector.AllDirections[i];
                int newX = keyX + dir.x;
                int newY = keyY + dir.y;
                if (newX is < 0 or >= ChunkSize || newY is < 0 or >= ChunkSize ||
                    chunk.regions[newY * ChunkSize + newX] != portalKey)
                    continue;

                chunk.regions[newY * ChunkSize + newX] = byte.MaxValue;
                openList[(head + count) % openList.Length] = current + dir.y * CorrectedMapSizeX + dir.x;
                count++;
            }
        }
    }

    public static ushort GetCostForPath(ushort[] costFields, Vector2D goal)
    {
        int key = goal.x % ChunkSize + goal.y % ChunkSize * ChunkSize;
        return costFields[key];
    }
}
