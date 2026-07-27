using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.model.pathfinding;

public class Chunk
{
    internal static int ChunkIdCounter;
    
    public readonly int ChunkId;
    public readonly byte[] regions = new byte[ChunkSize * ChunkSize];
    public readonly Portal?[] portals = new Portal?[ChunkSize * 4];

    public Chunk()
    {
        ChunkId = ChunkIdCounter++;
    }
    
    public static void UpdateDirtyChunk(ref Cell[] cells, ref Chunk chunk, ChunkDirty dirtyChunk)
    {
        if (dirtyChunk.ChunkHasCellChanges)
        {
            
            //Calculate Straight Ones
            for (byte i = 0; i < Enum.GetValues<DirtyDirections>().Length; i+=2)
            {
                if((dirtyChunk.DirectionsDirty & (1 << i)) == 0 ) continue;
                PortalUtils.UpdateChunkPortalsInDir(cells, ref chunk,(Directions) (i / 2));
            }
            
            //Calculate Diagonal Ones
            for (byte i = 1; i < Enum.GetValues<DirtyDirections>().Length; i+=2)
            {
                if((dirtyChunk.DirectionsDirty & (1 << i)) == 0 ) continue;
                PortalUtils.UpdateChunkPortalsInDirDiagonal(cells, ref chunk, (Directions) ((i / 2 + 1) % 4));
            }
              
            ConnectAllPortalsInChunkInternal(cells, ref chunk);
        }
        else
        {
            //Calculate Straight Ones
            for (byte i = 0; i < Enum.GetValues<DirtyDirections>().Length; i+=2)
            {
                if((dirtyChunk.DirectionsDirty & (1 << i)) == 0 ) continue;
                RebuildChunkPortalsInStraightDirectionNoChangesInChunk(cells, ref chunk, (Directions) (i / 2));
            }
            
            //Calculate Diagonal Ones
            for (byte i = 1; i < Enum.GetValues<DirtyDirections>().Length; i+=2)
            {
                if((dirtyChunk.DirectionsDirty & (1 << i)) == 0 ) continue;
                RebuildChunkPortalsInDiagonalDirectionNoChangesInChunk(cells, ref chunk, (DirtyDirections) i);
            }
        }
    }

    public static void InitPortalsInChunk(ref Cell[] cells, ref Chunk chunk)
    {
        RegionUtils.ResetRegions(ref chunk);
        foreach (var direction in Enum.GetValues<Directions>())
        { 
            PortalUtils.UpdateChunkPortalsInDir(cells, ref chunk, direction);
            PortalUtils.UpdateChunkPortalsInDirDiagonal(cells, ref chunk, direction);
        }
        PortalUtils.ConnectInternalPortalsAllDir(cells, ref chunk);
    }

    private static void ConnectAllPortalsInChunkInternal(Cell[] cells, ref Chunk chunk)
    { 
        RegionUtils.ResetRegions(ref chunk);
        PortalUtils.ConnectInternalPortalsAllDir(cells, ref chunk);
    }
    
    private static void RebuildChunkPortalsInDiagonalDirectionNoChangesInChunk(Cell[] cells, ref Chunk chunk, DirtyDirections dirtyDirections)
    {
        int dir = ((int)dirtyDirections / 2 + 1) % 4;
        int side = dir / 2;
        byte portalKey =  (byte)(ChunkSize * dir + side * (ChunkSize - 1));
        if (chunk.portals[portalKey] == null)
        {
            PortalUtils.UpdateChunkPortalsInDirDiagonal(cells, ref chunk, (Directions)dir);
            if (chunk.portals[portalKey] == null) return;
            PortalUtils.ConnectInternalPortalsInDiagonalDir(cells, ref chunk, portalKey);
            return;
        }
        
        var dirtyPortal = chunk.portals[portalKey]!;
        chunk.portals[portalKey] = null;
        PortalUtils.UpdateChunkPortalsInDirDiagonal(cells, ref chunk, (Directions)dir);
        ref var portal = ref chunk.portals[portalKey];
        if (portal == null)
        {
            //Only reset the region when the removed portal actually stamped it; otherwise the region
            //belongs to another portal and resetting would leave an unreachable hole in it.
            if (chunk.regions[RegionUtils.PositionToRegionKey(dirtyPortal.CenterPos)] == portalKey)
                BFS.ResetRegionsForPortal(cells, ref chunk, dirtyPortal.CenterPos, portalKey);
            PortalUtils.DisconnectInternalPortalsInDiagonalDir(cells, ref chunk, portalKey);
            return;
        }
        
        Array.Copy(portal.ExternalPortalConnections, 0, dirtyPortal.ExternalPortalConnections, 0 ,portal.ExternalPortalCount);
        dirtyPortal.ExternalPortalCount = portal.ExternalPortalCount;
        portal = dirtyPortal;
    }
    
    private static void RebuildChunkPortalsInStraightDirectionNoChangesInChunk(Cell[] cells, ref Chunk chunk, Directions direction)
    {
        RegionUtils.ResetRegionsInDirection(cells, ref chunk, direction);
        PortalUtils.UpdateChunkPortalsInDir(cells, ref chunk, direction);
        PortalUtils.UpdateChunkPortalsInDirDiagonal(cells, ref chunk, direction);
        PortalUtils.ConnectInternalPortalsInDir(cells, ref chunk, direction);
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
                switch (sideY)
                {
                    case < 2:
                        CheckNorthWestDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                    case >= ChunkSize - 2:
                        CheckSouthWestDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                }
                if (chunkPos.x + DirectionsVector.W.x < 0) return;
                chunkDirty.SetBit(DirtyDirections.W);
                chunkDirty.SetBit(DirtyDirections.SW);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.E, chunkPos + DirectionsVector.W);
                return;
            case 9:
                switch (sideY)
                {
                    case < 2:
                        CheckNorthEastDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                    case >= ChunkSize - 2:
                        CheckSouthEastDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                }
                if (chunkPos.x + DirectionsVector.E.x >= ChunkMapSizeX) return;
                chunkDirty.SetBit(DirtyDirections.E);
                chunkDirty.SetBit(DirtyDirections.NE);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.W, chunkPos + DirectionsVector.E);
                return;
        }
    }

    private static void CheckStraightSAndN(ref Dictionary<int, ChunkDirty> dirtyChunks, int sideY, Vector2D chunkPos, ChunkDirty chunkDirty,
        int sideX)
    {
        switch (sideY)
        {
            case 0:
                switch (sideX)
                {
                    case < 2:
                        CheckNorthWestDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                    case >= ChunkSize - 2:
                        CheckNorthEastDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                }
                if (chunkPos.y + DirectionsVector.N.y < 0) return;
                chunkDirty.SetBit(DirtyDirections.N);
                chunkDirty.SetBit(DirtyDirections.NW);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.S, chunkPos + DirectionsVector.N);
                return;
            case 9:
                switch (sideX)
                {
                    case < 2:
                        CheckSouthWestDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                    case >= ChunkSize - 2:
                        CheckSouthEastDiagonal(ref dirtyChunks, chunkDirty, chunkPos);
                        break;
                }
                if (chunkPos.y + DirectionsVector.S.y >= ChunkMapSizeY) return;
                chunkDirty.SetBit(DirtyDirections.S);
                chunkDirty.SetBit(DirtyDirections.SE);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.N, chunkPos + DirectionsVector.S);
                return;
        }
    }
    
    private static void CheckSouthEastDiagonal(ref Dictionary<int, ChunkDirty> dirtyChunks, ChunkDirty chunkDirty, Vector2D chunkPos)
    {
        chunkDirty.SetBit(DirtyDirections.SE);
        var newPos = chunkPos + DirectionsVector.SE;
        if(newPos.x < ChunkMapSizeX)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.SE, chunkPos + DirectionsVector.E);
            
            if(newPos.y < ChunkMapSizeY) 
            { 
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.NW, newPos);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.NE, chunkPos + DirectionsVector.S);
                return;
            }
        }
        
        if(newPos.y < ChunkMapSizeY)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.NE, chunkPos + DirectionsVector.S);
        }
    }
    
    private static void CheckSouthWestDiagonal(ref Dictionary<int, ChunkDirty> dirtyChunks, ChunkDirty chunkDirty, Vector2D chunkPos)
    {
        chunkDirty.SetBit(DirtyDirections.SW);
        var newPos = chunkPos + DirectionsVector.SW;
        if(newPos.x >= 0)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.SE, chunkPos + DirectionsVector.W);
            
            if(newPos.y < ChunkMapSizeY) 
            { 
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.NE, newPos);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.NW, chunkPos + DirectionsVector.S);
                return;
            }
        }
        
        if(newPos.y < ChunkMapSizeY)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.NW, chunkPos + DirectionsVector.S);
        }
    }

    private static void CheckNorthEastDiagonal(ref Dictionary<int, ChunkDirty> dirtyChunks, ChunkDirty chunkDirty, Vector2D chunkPos)
    {
        chunkDirty.SetBit(DirtyDirections.NE);
        var newPos = chunkPos + DirectionsVector.NE;
        
        if(newPos.y >= 0)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.SE, chunkPos + DirectionsVector.N);
            
            if(newPos.x < ChunkMapSizeX) 
            { 
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.SW, newPos);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.NW, chunkPos + DirectionsVector.E);
                return;
            }
        }
        
        if(newPos.x < ChunkMapSizeX)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.NW, chunkPos + DirectionsVector.E);
        }
    }

    private static void CheckNorthWestDiagonal(ref Dictionary<int, ChunkDirty> dirtyChunks, ChunkDirty chunkDirty, Vector2D chunkPos)
    {
        chunkDirty.SetBit(DirtyDirections.NW);
        var newPos = chunkPos + DirectionsVector.NW;
        
        if(newPos.y >= 0)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.SW, chunkPos + DirectionsVector.N);
            
            if(newPos.x >= 0)
            { 
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.SE, newPos);
                AddDirtyChunk(ref dirtyChunks, DirtyDirections.NE, chunkPos + DirectionsVector.W);
                return;
            }
        }
        
        if(newPos.x >= 0)
        { 
            AddDirtyChunk(ref dirtyChunks, DirtyDirections.NE, chunkPos + DirectionsVector.W);
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

    public static int WorldPosToChunkId(Vector2D pos)
    {
        return pos.x / ChunkSize + pos.y / ChunkSize * ChunkMapSizeX;
    }
}