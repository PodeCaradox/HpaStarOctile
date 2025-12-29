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
            for (int i = 0; i < dirtyChunk.DirectionDirty.Length; i++)
            {
                if(!dirtyChunk.DirectionDirty[i]) continue;
                PortalUtils.UpdateChunkPortalsInDirection(cells, ref portals,(Directions) i, chunkId);
            }
              
            ConnectAllPortalsInChunkInternal(cells, ref portals, chunkId);
        }
        else
        {
            for (int i = 0; i < dirtyChunk.DirectionDirty.Length; i++)
            {
                if(!dirtyChunk.DirectionDirty[i]) continue;
                RebuildChunkPortalsInDirectionNoChangesInChunk(cells, ref portals, (Directions) i, chunkId);
            }
        }
    }
    
    public static void InitPortalsInChunk(ref Cell[] cells, ref Portal?[] portals, int chunkId)
    {
        RegionUtils.ResetRegions(cells, chunkId);
        foreach (var direction in Enum.GetValues<Directions>())
        { 
            PortalUtils.UpdateChunkPortalsInDirection(cells, ref portals, direction, chunkId);
        }
        PortalUtils.ConnectInternalPortalsAllDir(cells, ref portals, chunkId);
    }

    private static void ConnectAllPortalsInChunkInternal(Cell[] cells, ref Portal?[] portals, int chunkId)
    { 
        RegionUtils.ResetRegions(cells, chunkId);
        PortalUtils.ConnectInternalPortalsAllDir(cells, ref portals, chunkId);
    }
    
    private static void RebuildChunkPortalsInDirectionNoChangesInChunk(Cell[] cells, ref Portal?[] portals, Directions direction, int chunkId)
    {
        RegionUtils.ResetRegionsInDirection(cells, portals, chunkId, direction);
        PortalUtils.UpdateChunkPortalsInDirection(cells, ref portals, direction, chunkId);
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
    
    public static Vector2D CellPositionToChunkPos(Vector2D pos)
    {
        Vector2D chunkPos = new Vector2D(pos.x / ChunkSize, pos.y / ChunkSize);
        return chunkPos;
    }

    public static void AddDirtyChunk(ref Dictionary<int, ChunkDirty> dirtyChunks, Vector2D mapCellPosition)
    {
        Vector2D chunkPos = Chunk.CellPositionToChunkPos(mapCellPosition);
        int sideX = mapCellPosition.x % ChunkSize;
        int sideY = mapCellPosition.x % ChunkSize;
        List<int> chunksEffected = [];
        ChunkDirty chunkDirty = new ChunkDirty(true);
        int offsetY = 0;
        int offsetX = 0;
        CheckDirDirty(chunksEffected, chunkDirty, Directions.N, sideY == 0, chunkPos.y + DirectionsVector.N.y < 0, ref offsetY, -1);
        CheckDirDirty(chunksEffected, chunkDirty, Directions.S, sideY == 9, chunkPos.y + DirectionsVector.S.y >= ChunkMapSizeY, ref offsetY, 1);
        CheckDirDirty(chunksEffected, chunkDirty, Directions.W, sideX == 0, chunkPos.x + DirectionsVector.W.x < 0, ref offsetX, -1);
        CheckDirDirty(chunksEffected, chunkDirty, Directions.E, sideX == 9, chunkPos.x + DirectionsVector.E.x >= ChunkMapSizeX, ref offsetX, 1);
        AddDirtyChunk(ref dirtyChunks, chunkPos.x + chunkPos.y * ChunkMapSizeX, chunkDirty, true);
        AddDirtyChunksStraight(ref dirtyChunks, chunksEffected, chunkPos);
        AddDirtyChunksDiagonal(ref dirtyChunks, chunksEffected, chunkPos, offsetX, offsetY);
    }
    
    private static void AddDirtyChunksDiagonal(ref Dictionary<int, ChunkDirty> dirtyChunks, List<int> chunksEffected, Vector2D chunkPos, int offsetX, int offsetY)
    {
        if (chunksEffected.Count != 2) return;
        
        int chunkKey = chunkPos.x + offsetX + (chunkPos.y + offsetY) * ChunkMapSizeX;
        ChunkDirty chunk = new ChunkDirty(false);
        foreach (var dir in chunksEffected)
            chunk.DirectionDirty[dir] = true;
        AddDirtyChunk(ref dirtyChunks, chunkKey, chunk, false);
    }

    private static void AddDirtyChunksStraight(ref Dictionary<int, ChunkDirty> dirtyChunks, List<int> chunksEffected, Vector2D chunkPos)
    {
        foreach (var dir in chunksEffected)
        {
            var dirVector = DirectionsVector.AllDirections[(dir + 2) % 4];
            int chunkKey = chunkPos.x + dirVector.x + (chunkPos.y + dirVector.y) * ChunkMapSizeX;
            ChunkDirty chunk = new ChunkDirty(false);
            chunk.DirectionDirty[dir] = true;
            AddDirtyChunk(ref dirtyChunks, chunkKey, chunk, false);
        }
    }

    private static void CheckDirDirty(List<int> chunkDirDirty, ChunkDirty chunkDirty, Directions dir, bool isChunkBorder, bool isOutsideMap, ref int offset, int offsetValue)
    {
        if (!isChunkBorder || isOutsideMap) return;
        chunkDirty.DirectionDirty[(int)dir] = true;
        chunkDirDirty.Add(((int)dir + 2) % 4);
        offset = offsetValue;
    }

    private static void AddDirtyChunk(ref Dictionary<int, ChunkDirty> dirtyChunks, int key, ChunkDirty chunkDirty, bool changed)
    {
        if (dirtyChunks.TryAdd(key, chunkDirty)) return;
        
        ChunkDirty chunk = dirtyChunks[key];
        if(changed) chunk.ChunkHasCellChanges = changed;
        foreach (int direction in Enum.GetValues<Directions>())
        {
            chunk.DirectionDirty[direction] = chunkDirty.DirectionDirty[direction];
        }
        
    }
}