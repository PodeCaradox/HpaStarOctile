using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding.PathfindingCache.PathfindingResultTypes;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding.PathfindingCache;

public static class PathFindingManager
{
    private const int MaxCachedPaths = 1000;

    //pathId => portal path, reused between requests; when the cache is full the least recently used path gets evicted
    private static readonly LruCache<long, List<int>> HighLevelPaths = new(MaxCachedPaths, RemovePathFromChunkIndex);
    //chunkId => cached high level paths going through this chunk, so they can be invalidated when the chunk changes
    private static readonly Dictionary<int, HashSet<long>> ChunkToPathIds = new();

    private static readonly Dictionary<int, List<Vector2D>> ShortPaths = new (1000);
    private static int CounterShortPaths;

    public static PathfindingResult GetPath(Cell[] map, Chunk[] chunks, Vector2D start, Vector2D goal)
    {
        int chunkStart = Chunk.CellPositionToChunkKey(start);
        int chunkEnd = Chunk.CellPositionToChunkKey(goal);
        var regionKeyEnd = RegionUtils.PositionToRegionKey(goal);
        var regionKeySart = RegionUtils.PositionToRegionKey(start);

        byte regionPortalEnd = chunks[chunkEnd].regions[regionKeyEnd];
        byte regionPortalStart = chunks[chunkStart].regions[regionKeySart];

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

    //Drops every cached path going through one of the given chunks, call after the portals of those chunks were rebuilt.
    public static void InvalidateChunks(IEnumerable<int> chunkKeys)
    {
        foreach (var chunkKey in chunkKeys)
        {
            if (!ChunkToPathIds.Remove(chunkKey, out var pathIds)) continue;
            foreach (var pathId in pathIds)
            {
                if (HighLevelPaths.Remove(pathId, out var path))
                    RemovePathFromChunkIndex(pathId, path!);
            }
        }
    }

    public static void ClearCache()
    {
        HighLevelPaths.Clear();
        ChunkToPathIds.Clear();
    }

    private static void CachePath(long pathId, List<int> path)
    {
        HighLevelPaths.Add(pathId, path);
        foreach (var portalKey in path)
        {
            int chunkId = portalKey / MaxPortalsInChunk;
            if (!ChunkToPathIds.TryGetValue(chunkId, out var pathIds))
            {
                pathIds = [];
                ChunkToPathIds.Add(chunkId, pathIds);
            }
            pathIds.Add(pathId);
        }
    }

    private static void RemovePathFromChunkIndex(long pathId, List<int> path)
    {
        foreach (var portalKey in path)
        {
            int chunkId = portalKey / MaxPortalsInChunk;
            if (!ChunkToPathIds.TryGetValue(chunkId, out var pathIds)) continue;
            pathIds.Remove(pathId);
            if (pathIds.Count == 0) ChunkToPathIds.Remove(chunkId);
        }
    }

    private static long CalculatePathId(int chunkStart, int chunkEnd, byte regionPortalStart, byte regionPortalEnd)
    {
        return chunkStart << 28 | regionPortalStart << 22 | chunkEnd << 6 | regionPortalEnd; 
    }


    public static List<Vector2D> PortalsToPath(Cell[] grid, Chunk[] chunks, Vector2D pathStart, Vector2D pathEnd, List<int> pathAsPortals)
    {
        
        
        List<Vector2D> path = AStar.FindPath(grid, pathStart, chunks[pathAsPortals[0] / MaxPortalsInChunk].portals[pathAsPortals[0] % MaxPortalsInChunk]!.CenterPos);
        for (int i = 0; i < pathAsPortals.Count - 1; i++)
        {
            path.AddRange(AStar.FindPath(grid, chunks[pathAsPortals[i] / MaxPortalsInChunk].portals[pathAsPortals[i] % MaxPortalsInChunk]!.CenterPos, chunks[pathAsPortals[i + 1] / MaxPortalsInChunk].portals[pathAsPortals[i + 1] % MaxPortalsInChunk]!.CenterPos));
        }

        int lastPortal = pathAsPortals.Last();
        path.AddRange(AStar.FindPath(grid, chunks[lastPortal / MaxPortalsInChunk].portals[lastPortal % MaxPortalsInChunk]!.CenterPos, pathEnd));
        return path;
    }
}
