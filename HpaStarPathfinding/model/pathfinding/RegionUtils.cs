using HpaStarPathfinding.model.map;
using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.model.pathfinding;

public static class RegionUtils
{
    public static void ResetRegionsInDirection(Cell[] cells, Portal?[] portals, int chunkId, Directions dir)
    {
        int key = Portal.GeneratePortalKey(chunkId, 0, 0);
        byte start = (byte)((byte)dir * ChunkSize);
        for (byte i = start; i < start + ChunkSize; i++)
        {
            int portalKey = key + i;
            if (portals[portalKey] == null)
                continue;
            var portal = portals[portalKey]!;
            Cell currentCell = cells[portal.CenterPos.y * CorrectedMapSizeX + portal.CenterPos.x];
            if (currentCell.Region != i) continue;
            BFS.ResetRegionsForPortal(cells, portal.CenterPos, currentCell.Region);
        }
    }
    
    public static void ResetRegions(Cell[] vmMap, int chunkKey)
    {
        int chunkX = chunkKey % ChunkMapSizeX;
        int chunkY = chunkKey / ChunkMapSizeX;
            
        int tileKey =  chunkX * ChunkSize + chunkY * CellsInChunk * ChunkMapSizeX;
        for (int y = 0; y < ChunkSize; y++)
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                vmMap[tileKey + x].Region = byte.MaxValue;
            }
            tileKey += CorrectedMapSizeX;
        }
    }
    
    public static ushort[] GetCostFieldsAndUpdateRegions(Cell[] cells, Portal portal, byte portalKey, HashSet<byte> portalsFromRegionFillAdded)
    {
        var costFields = portalsFromRegionFillAdded.Add(portalKey) ? 
            BFS.BfsFromStartPosWithRegionFill(cells, portal.CenterPos, portalKey) : 
            BFS.BfsFromStartPos(cells, portal.CenterPos);

        return costFields;
    }
}