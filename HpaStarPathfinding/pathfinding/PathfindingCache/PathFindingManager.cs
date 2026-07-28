using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding.PathfindingCache.PathfindingResultTypes;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding.PathfindingCache;

public static class PathFindingManager
{
    private const int MaxCachedPaths = 1000;

    //pathId (chunk/region pair, not exact cells) => portal path, shared by all entities with the same
    //chunk/region pair; the shared path can include a portal detour compared to the cell-optimal route.
    //When the cache is full the least recently used path gets evicted.
    private static readonly LruCache<long, List<int>> HighLevelPaths = new(MaxCachedPaths, RemovePathFromChunkIndex);
    //chunkId => cached high level paths going through this chunk, so they can be invalidated when the chunk changes
    private static readonly Dictionary<int, HashSet<long>> ChunkToPathIds = new();

    //(fromPortal, toPortal) => cell path between the two portals, shared by every entity using the same high level path
    private static readonly LruCache<long, List<Vector2D>> LowLevelPaths = new(MaxCachedPaths, RemoveSegmentFromChunkIndex);
    //chunkId => cached low level path segments with waypoints in this chunk, so they can be invalidated when the chunk changes
    private static readonly Dictionary<int, HashSet<long>> ChunkToSegmentIds = new();

    private static readonly Dictionary<int, List<Vector2D>> ShortPaths = new (1000);
    private static int CounterShortPaths;

    public static PathfindingResult GetPath(Cell[] map, Chunk[] chunks, Vector2D start, Vector2D goal)
    {
        int chunkStart = Chunk.CellPositionToChunkKey(start);
        int chunkEnd = Chunk.CellPositionToChunkKey(goal);
        var regionKeyEnd = RegionUtils.PositionToRegionKey(goal);
        var regionKeyStart = RegionUtils.PositionToRegionKey(start);

        byte regionPortalEnd = chunks[chunkEnd].regions[regionKeyEnd];
        byte regionPortalStart = chunks[chunkStart].regions[regionKeyStart];

        if (chunkStart == chunkEnd && regionPortalStart == regionPortalEnd) return GetShortPathId(map, start, goal); //No Path Or ShortPath
        if(regionPortalStart == byte.MaxValue || regionPortalEnd == byte.MaxValue) return new PathfindingResult(PathfindingType.NoPath);//No Path

        long pathId = CalculatePathId(chunkStart, chunkEnd, regionPortalStart, regionPortalEnd);
        if (!HighLevelPaths.TryGet(pathId, out var path)) //reuse the already calculated path if there is one
        {
            path = HpaStar.FindPath(map, chunks, start, goal, regionPortalStart, regionPortalEnd);
            //"no path" results are not cached, they can change without any portal on the way changing
            if (path.Count > 0) CachePath(pathId, path);
        }

        return path!.Count == 0 ? new PathfindingResult(PathfindingType.NoPath) : new HighLevelPathResult(pathId);// goal, path[0]
    }

    private static PathfindingResult GetShortPathId(Cell[] map, Vector2D start, Vector2D goal)
    {
        var path = AStar.FindPath(map, start, goal);
        if(path.Count == 0) return new PathfindingResult(PathfindingType.NoPath);
        ShortPaths.Add(CounterShortPaths, path);
        return new ShortPathResult(CounterShortPaths++);//goal 
    }

    public static List<Vector2D> GetNextPath(ShortPathResult pathfindingResult)
    {
        ShortPaths.Remove(pathfindingResult.PathId, out var path);
        return path!;
    }

    public static List<int>? GetNextPath(HighLevelPathResult pathfindingResult)
    {
        //The path stays in the cache so later requests can reuse it.
        HighLevelPaths.TryGet(pathfindingResult.PathId, out var path);
        return path;
    }

    //Drops every cached path and segment going through one of the given chunks, call after the portals of those chunks were rebuilt.
    public static void InvalidateChunks(IEnumerable<int> chunkKeys)
    {
        foreach (var chunkKey in chunkKeys)
        {
            if (ChunkToPathIds.Remove(chunkKey, out var pathIds))
            {
                foreach (var pathId in pathIds)
                {
                    if (HighLevelPaths.Remove(pathId, out var path))
                        RemovePathFromChunkIndex(pathId, path!);
                }
            }

            if (ChunkToSegmentIds.Remove(chunkKey, out var segmentIds))
            {
                foreach (var segmentId in segmentIds)
                {
                    if (LowLevelPaths.Remove(segmentId, out var segment))
                        RemoveSegmentFromChunkIndex(segmentId, segment!);
                }
            }
        }
    }

    public static void ClearCache()
    {
        HighLevelPaths.Clear();
        ChunkToPathIds.Clear();
        LowLevelPaths.Clear();
        ChunkToSegmentIds.Clear();
    }

    private static void CachePath(long pathId, List<int> path)
    {
        HighLevelPaths.Add(pathId, path);
        foreach (var portalKey in path)
        {
            AddToChunkIndex(ChunkToPathIds, portalKey / MaxPortalsInChunk, pathId);
        }
    }

    private static void RemovePathFromChunkIndex(long pathId, List<int> path)
    {
        foreach (var portalKey in path)
        {
            RemoveFromChunkIndex(ChunkToPathIds, portalKey / MaxPortalsInChunk, pathId);
        }
    }

    private static void CacheSegment(long segmentId, List<Vector2D> segment, int fromPortal, int toPortal)
    {
        LowLevelPaths.Add(segmentId, segment);
        //the low level search is not limited to the two portal chunks, so index every chunk the segment actually crosses
        AddToChunkIndex(ChunkToSegmentIds, fromPortal / MaxPortalsInChunk, segmentId);
        AddToChunkIndex(ChunkToSegmentIds, toPortal / MaxPortalsInChunk, segmentId);
        foreach (var cell in segment)
        {
            AddToChunkIndex(ChunkToSegmentIds, Chunk.CellPositionToChunkKey(cell), segmentId);
        }
    }

    private static void RemoveSegmentFromChunkIndex(long segmentId, List<Vector2D> segment)
    {
        int fromPortal = (int)(segmentId >> 32);
        int toPortal = (int)(segmentId & 0xFFFFFFFF);
        RemoveFromChunkIndex(ChunkToSegmentIds, fromPortal / MaxPortalsInChunk, segmentId);
        RemoveFromChunkIndex(ChunkToSegmentIds, toPortal / MaxPortalsInChunk, segmentId);
        foreach (var cell in segment)
        {
            RemoveFromChunkIndex(ChunkToSegmentIds, Chunk.CellPositionToChunkKey(cell), segmentId);
        }
    }

    private static void AddToChunkIndex(Dictionary<int, HashSet<long>> chunkIndex, int chunkId, long id)
    {
        if (!chunkIndex.TryGetValue(chunkId, out var ids))
        {
            ids = [];
            chunkIndex.Add(chunkId, ids);
        }
        ids.Add(id);
    }

    private static void RemoveFromChunkIndex(Dictionary<int, HashSet<long>> chunkIndex, int chunkId, long id)
    {
        if (!chunkIndex.TryGetValue(chunkId, out var ids)) return;
        ids.Remove(id);
        if (ids.Count == 0) chunkIndex.Remove(chunkId);
    }

    //6 bits per region portal key (0-39), 26 bits per chunk id
    private static long CalculatePathId(int chunkStart, int chunkEnd, byte regionPortalStart, byte regionPortalEnd)
    {
        return (long)chunkStart << 38 | (long)regionPortalStart << 32 | (long)chunkEnd << 6 | regionPortalEnd;
    }

    private static long CalculateSegmentId(int fromPortal, int toPortal)
    {
        return (long)fromPortal << 32 | (uint)toPortal;
    }

    private static Vector2D GetPortalCenter(Chunk[] chunks, int portalKey)
    {
        return chunks[portalKey / MaxPortalsInChunk].portals[portalKey % MaxPortalsInChunk]!.CenterPos;
    }

    private static List<Vector2D> GetPortalToPortalPath(Cell[] grid, Chunk[] chunks, int fromPortal, int toPortal)
    {
        long segmentId = CalculateSegmentId(fromPortal, toPortal);
        if (LowLevelPaths.TryGet(segmentId, out var segment)) //reuse the already calculated segment if there is one
            return segment!;

        segment = AStar.FindPath(grid, GetPortalCenter(chunks, fromPortal), GetPortalCenter(chunks, toPortal));
        CacheSegment(segmentId, segment, fromPortal, toPortal);
        return segment;
    }

    public static List<Vector2D> PortalsToPath(Cell[] grid, Chunk[] chunks, Vector2D pathStart, Vector2D pathEnd, List<int> pathAsPortals)
    {
        //entity specific: from the actual start to the first portal
        List<Vector2D> path = AStar.FindPath(grid, pathStart, GetPortalCenter(chunks, pathAsPortals[0]));
        for (int i = 0; i < pathAsPortals.Count - 1; i++)
        {
            AppendSegment(path, GetPortalToPortalPath(grid, chunks, pathAsPortals[i], pathAsPortals[i + 1]));
        }

        //entity specific: from the last portal to the actual goal
        AppendSegment(path, AStar.FindPath(grid, GetPortalCenter(chunks, pathAsPortals[^1]), pathEnd));
        return path;
    }

    private static void AppendSegment(List<Vector2D> path, List<Vector2D> segment)
    {
        int i = path.Count == 0 ? 0 : 1; //the first waypoint duplicates the last waypoint of the previous segment
        for (; i < segment.Count; i++)
        {
            path.Add(segment[i]);
        }
    }
}
