using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.model.pathfinding;

public class Chunk
{
    public static void UpdateDirtyChunk(ref Cell[] cells, ref Portal?[] portals, ChunkDirty dirtyChunk, int chunkId)
    {
        if (dirtyChunk.ChunkHasCellChanges)
        {
            for (int i = 0; i < Enum.GetValues<Directions>().Length; i++)
            {
                if(dirtyChunk.DirectionsDirty >> (i * 2) == 0) continue;
                PortalUtils.UpdateChunkPortalsInDir(cells, ref portals,(Directions) i, chunkId);
            }
              
            ConnectAllPortalsInChunkInternal(cells, ref portals, chunkId);
        }
        else
        {
            //Diagonals needs to be calculated first otherwise there is a problem when we remove the diagonal portal
            //and this one is the last one, region fill will be messed up :/
            for (byte i = 1; i < Enum.GetValues<DirtyDirections>().Length; i+=2)
            {
                if((dirtyChunk.DirectionsDirty & (1 << i)) == 0 ) continue;
                RebuildChunkPortalsInDiagonalDirectionNoChangesInChunk(cells, ref portals, (DirtyDirections) i, chunkId);
            }
            
            //Calculate Straight Ones
            for (byte i = 0; i < Enum.GetValues<DirtyDirections>().Length; i+=2)
            {
                if((dirtyChunk.DirectionsDirty & (1 << i)) == 0 ) continue;
                RebuildChunkPortalsInStraightDirectionNoChangesInChunk(cells, ref portals, (Directions) (i / 2), chunkId);
            }
        }
    }

    public static void InitPortalsInChunk(ref Cell[] cells, ref Portal?[] portals, int chunkId)
    {
        RegionUtils.ResetRegions(cells, chunkId);
        foreach (var direction in Enum.GetValues<Directions>())
        { 
            PortalUtils.UpdateChunkPortalsInDir(cells, ref portals, direction, chunkId);
        }
        PortalUtils.ConnectInternalPortalsAllDir(cells, ref portals, chunkId);
    }

    private static void ConnectAllPortalsInChunkInternal(Cell[] cells, ref Portal?[] portals, int chunkId)
    { 
        RegionUtils.ResetRegions(cells, chunkId);
        PortalUtils.ConnectInternalPortalsAllDir(cells, ref portals, chunkId);
    }
    
    private static void RebuildChunkPortalsInDiagonalDirectionNoChangesInChunk(Cell[] cells, ref Portal?[] portals, DirtyDirections dirtyDirections, int chunkId)
    {
        int dir = ((int)dirtyDirections / 2 + 1) % 4;
        int side = dir / 2;
        byte portalKeyInternal =  (byte)(ChunkSize * dir + side * (ChunkSize - 1));
        int portalKey =  portalKeyInternal + chunkId * MaxPortalsInChunk;
        if (portals[portalKey] == null)
        {
            PortalUtils.UpdateChunkPortalsInDirDiagonal(cells, ref portals, (Directions)dir, chunkId);
            if (portals[portalKey] == null) return;
            PortalUtils.ConnectInternalPortalsInDiagonalDir(cells, ref portals, chunkId, portalKeyInternal - 1, portalKeyInternal + 1);
            return;
        }
        
        var dirtyPortal = portals[portalKey]!;
        portals[portalKey] = null;
        PortalUtils.UpdateChunkPortalsInDirDiagonal(cells, ref portals, (Directions)dir, chunkId);
        ref var portal = ref portals[portalKey];
        int key = portalKey + PortalUtils.OppositePortalKeyOffsets[dir] + 1 - 2 * side;
        if (portal == null)
        {
            for (int i = 0; i < dirtyPortal.ExternalPortalCount; i++)
            {
                var oppositePortalKey = dirtyPortal.ExternalPortalConnections[i];
                if(oppositePortalKey != key) continue;
                dirtyPortal.ExternalPortalConnections[0] = oppositePortalKey;
                dirtyPortal.ExternalPortalCount = 1;
                portal = dirtyPortal;
                break;
            }
            PortalUtils.ConnectInternalPortalsInDiagonalDir(cells, ref portals, chunkId, 0, 0);
            return;
        }
        
        portal = dirtyPortal;
        portal.ExternalPortalCount = 0;
        for (int i = 0; i < dirtyPortal.ExternalPortalCount; i++)
        {
            var oppositePortalKey = dirtyPortal.ExternalPortalConnections[i];
            if(oppositePortalKey != key) continue;
            portal.ExternalPortalConnections[portal.ExternalPortalCount++] = oppositePortalKey;
            break;
        }
            
    }
    
    private static void RebuildChunkPortalsInStraightDirectionNoChangesInChunk(Cell[] cells, ref Portal?[] portals, Directions direction, int chunkId)
    {
        RegionUtils.ResetRegionsInDirection(cells, portals, chunkId, direction);
        PortalUtils.UpdateChunkPortalsInDir(cells, ref portals, direction, chunkId);
        PortalUtils.ConnectInternalPortalsInDir(cells, ref portals, direction, chunkId);
    }
    
    public static bool IsDiagonalOppositeChunk(int chunkPosition, int outsidePosition)
    {
        return chunkPosition == outsidePosition;
    }
    
    public static int CellPositionToChunkKey(Vector2D pos)
    {
        Vector2D chunkPos = CellPositionToChunkPos(pos);
        return chunkPos.x + chunkPos.y * ChunkMapSizeX;
    }

    private static Vector2D CellPositionToChunkPos(Vector2D pos)
    {
        Vector2D chunkPos = new Vector2D(pos.x / ChunkSize, pos.y / ChunkSize);
        return chunkPos;
    }

    public static void CheckWhichChunksAreDirty(ref Dictionary<int, ChunkDirty> dirtyChunks, Vector2D mapCellPosition)
    {
        Vector2D chunkPos = CellPositionToChunkPos(mapCellPosition);
        int sideX = mapCellPosition.x % ChunkSize;
        int sideY = mapCellPosition.y % ChunkSize;
        ChunkDirty chunkDirty = new ChunkDirty(true);
        CheckStraightSAndN(ref dirtyChunks, sideY, chunkPos, chunkDirty, sideX);
        CheckStraightWAndE(ref dirtyChunks, sideX, chunkPos, chunkDirty, sideY);
        AddDirtyChunk(ref dirtyChunks, chunkPos.x + chunkPos.y * ChunkMapSizeX, chunkDirty, true);
    }

    private static void CheckStraightWAndE(ref Dictionary<int, ChunkDirty> dirtyChunks, int sideX, Vector2D chunkPos, ChunkDirty chunkDirty,
        int sideY)
    {
        switch (sideX)
        {
            case 0:
                chunkDirty.SetBit(DirtyDirections.W);
                if (chunkPos.x + DirectionsVector.W.x < 0) return;
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.E, chunkPos + DirectionsVector.W);
                var pos1 = chunkPos + DirectionsVector.NW;
                var pos2 = chunkPos + DirectionsVector.SW;
                CheckDiagonalDirty(ref dirtyChunks, ChunkMapSizeY, sideY, pos1.y, pos2.y, chunkDirty, DirtyDirections.NW,DirtyDirections.SW,
                    DirtyDirections.SE, DirtyDirections.NE, DirtyDirections.SW, DirtyDirections.SE, 
                    pos1, chunkPos + DirectionsVector.N, pos2, chunkPos + DirectionsVector.W);
                break;
            case 9:
                chunkDirty.SetBit(DirtyDirections.E);
                if (chunkPos.x + DirectionsVector.E.x >= ChunkMapSizeX) return;
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.W, chunkPos + DirectionsVector.E);
                pos1 = chunkPos + DirectionsVector.NE;
                pos2 = chunkPos + DirectionsVector.SE;
                CheckDiagonalDirty(ref dirtyChunks, ChunkMapSizeY, sideY, pos1.y, pos2.y, chunkDirty,
                    DirtyDirections.NE, DirtyDirections.SE,
                    DirtyDirections.SW, DirtyDirections.NW, DirtyDirections.NW, DirtyDirections.NE,
                    pos1, chunkPos + DirectionsVector.E, pos2, chunkPos + DirectionsVector.S);
                break;
        }
    }

    private static void CheckStraightSAndN(ref Dictionary<int, ChunkDirty> dirtyChunks, int sideY, Vector2D chunkPos, ChunkDirty chunkDirty,
        int sideX)
    {
        switch (sideY)
        {
            case 0:
                chunkDirty.SetBit(DirtyDirections.N);
                if (chunkPos.y + DirectionsVector.N.y < 0) return;
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.S, chunkPos + DirectionsVector.N);
                var pos1 = chunkPos + DirectionsVector.NW;
                var pos2 = chunkPos + DirectionsVector.NE;
                CheckDiagonalDirty(ref dirtyChunks, ChunkMapSizeX, sideX, pos1.x, pos2.x, chunkDirty, DirtyDirections.NW,DirtyDirections.NE,
                    DirtyDirections.SE,  DirtyDirections.SW,  DirtyDirections.SW,  DirtyDirections.NW, 
                    pos1, chunkPos + DirectionsVector.N, pos2, chunkPos + DirectionsVector.E);
                break;
            case 9:
                chunkDirty.SetBit(DirtyDirections.S);
                if (chunkPos.y + DirectionsVector.S.y >= ChunkMapSizeY) return;
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.N, chunkPos + DirectionsVector.S);
                pos1 = chunkPos + DirectionsVector.SW;
                pos2 = chunkPos + DirectionsVector.SE;
                
                CheckDiagonalDirty(ref dirtyChunks, ChunkMapSizeX, sideX, pos1.x, pos2.x, chunkDirty, DirtyDirections.SW,DirtyDirections.SE
                    ,DirtyDirections.NE,  DirtyDirections.NW,  DirtyDirections.SE,  DirtyDirections.NE, 
                    pos1, chunkPos + DirectionsVector.W, pos2, chunkPos + DirectionsVector.S);
                break;
        }
    }

    private static void CheckDiagonalDirty(ref Dictionary<int, ChunkDirty> dirtyChunks, int chunkMapSize, int side, int mapPos1, int mapPos2,
        ChunkDirty chunkDirty, DirtyDirections own1, DirtyDirections own2, 
        DirtyDirections other1, DirtyDirections other2, DirtyDirections other3, DirtyDirections other4,
        Vector2D chunkPos1, Vector2D chunkPos2, Vector2D chunkPos3, Vector2D chunkPos4)
    {
        switch (side)
        {
            case < 2:
                if (mapPos1 < 0)
                    break;
                AddDirtyChunk(ref dirtyChunks, other2, chunkPos2);
                chunkDirty.SetBit(own1);
                AddDirtyChunk(ref dirtyChunks, other1, chunkPos1);
                return;
            case > ChunkSize - 2:
                if (mapPos2 >= chunkMapSize)
                    break;
                AddDirtyChunk(ref dirtyChunks, other4, chunkPos4);
                chunkDirty.SetBit(own2);
                AddDirtyChunk(ref dirtyChunks, other3, chunkPos3);
                return;
        }
    }

    private static void AddDirtyChunk(ref Dictionary<int, ChunkDirty> dirtyChunks, DirtyDirections dir, Vector2D chunkPos)
    {
        int chunkKey = chunkPos.x + chunkPos.y * ChunkMapSizeX;
        ChunkDirty chunk = new ChunkDirty(false);
        chunk.SetBit(dir);
        AddDirtyChunk(ref dirtyChunks, chunkKey, chunk, false);
    }

    private static void AddDirtyChunk(ref Dictionary<int, ChunkDirty> dirtyChunks, int key, ChunkDirty chunkDirty, bool changed)
    {
        if (dirtyChunks.TryAdd(key, chunkDirty)) return;
        
        ChunkDirty chunk = dirtyChunks[key];
        if(changed) chunk.ChunkHasCellChanges = changed;
        chunk.DirectionsDirty |= chunkDirty.DirectionsDirty;
    }
}