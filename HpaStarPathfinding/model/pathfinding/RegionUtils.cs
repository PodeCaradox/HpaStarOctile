using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.model.pathfinding;

public static class RegionUtils
{
    public static void ResetRegionsInDirection(ref Chunk chunk, Directions dir)
    {
        byte start = (byte)((byte)dir * ChunkSize);
        for (byte portalKey = start; portalKey < start + ChunkSize; portalKey++)
        {
            ref var portal = ref chunk.portals[portalKey];
            if (portal == null)
                continue;


            var regionKey = PositionToRegionKey(portal.CenterPos);
            if (chunk.regions[regionKey] != portalKey) continue;
            BFS.ResetRegionsForPortal(ref chunk, portal.CenterPos, portalKey);
        }
    }

    public static int PositionToRegionKey(Vector2D centerPos)
    {
        int startX = centerPos.x % ChunkSize;
        int startY = centerPos.y % ChunkSize;
        int regionKey = startY * ChunkSize + startX;
        return regionKey;
    }

    public static void ResetRegions(ref Chunk chunk)
    {
        for (int regionKey = 0; regionKey < chunk.regions.Length; regionKey++)
        {
            chunk.regions[regionKey] = byte.MaxValue;
        }
    }
    
    public static ushort[] GetCostFieldsAndUpdateRegions(Cell[] cells, ref Chunk chunk, Portal portal, byte portalKey, HashSet<byte> portalsFromRegionFillAdded)
    {
        var costFields = portalsFromRegionFillAdded.Add(portalKey)
            ? BFS.BfsFromStartPos(cells, portal.CenterPos, chunk, portalKey)
            : BFS.BfsFromStartPos(cells, portal.CenterPos);

        return costFields;
    }
}