using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.model.PathfindingCache.PathfindingResultTypes;
using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.model.PathfindingCache;

public static class PathFindingManager
{
    private static readonly Dictionary<long, List<int>> HighLevelPaths = new (1000);
    //private static readonly Dictionary<int, List<Vector2D>> LowLevelPaths = new (1000);
    private static readonly Dictionary<int, List<Vector2D>> ShortPaths = new (1000);
    private static int CounterShortPaths;
    
    public static PathfindingResult GetPath(Cell[] map, Portal?[] portals, Vector2D start, Vector2D goal)
    {
        HighLevelPaths.Clear();
        ShortPaths.Clear();
        CounterShortPaths = 0;
        byte regionPortalEnd = map[goal.y * CorrectedMapSizeX + goal.x].Region;
        byte regionPortalStart = map[start.y * CorrectedMapSizeX + start.x].Region;

        int chunkStart = Chunk.CellPositionToChunkKey(start);
        int chunkEnd = Chunk.CellPositionToChunkKey(goal);

        if (chunkStart == chunkEnd && regionPortalStart == regionPortalEnd) return GetShortPathId(map, start, goal); //No Path Or ShortPath
        if(regionPortalStart == byte.MaxValue || regionPortalEnd == byte.MaxValue) return new PathfindingResult(PathfindingType.NoPath);//No Path
        
        long pathId = CalculatePathId(chunkStart, chunkEnd, regionPortalStart, regionPortalEnd);
        if (HighLevelPaths.ContainsKey(pathId)) 
            return CheckNoPathOrHighLevelPath(pathId, goal);
        
        List<int> path = HpaStar.FindPath(map, portals, start, goal, regionPortalStart, regionPortalEnd);
        HighLevelPaths.Add(pathId, path);
        return CheckNoPathOrHighLevelPath(pathId, goal);
    }

    private static PathfindingResult GetShortPathId(Cell[] map, Vector2D start, Vector2D goal)
    {
        var path = AStar.FindPath(map, start, goal);
        if(path.Count == 0) return new PathfindingResult(PathfindingType.NoPath);
        ShortPaths.Add(CounterShortPaths, path);
        return new ShortPathResult(CounterShortPaths++);//goal 
    }

    private static PathfindingResult CheckNoPathOrHighLevelPath(long pathId, Vector2D goal)
    {
        var path = HighLevelPaths[pathId];
        return path.Count == 0 ? new PathfindingResult(PathfindingType.NoPath) : new HighLevelPathResult(pathId);// goal, path[0]
    }
    
    public static List<Vector2D> GetNextPath(ShortPathResult pathfindingResult)
    {
        ShortPaths.Remove(pathfindingResult.PathId, out var path);
        return path!;
    }

    public static List<int>? GetNextPath(HighLevelPathResult pathfindingResult)
    {
        HighLevelPaths.Remove(pathfindingResult.PathId, out var path);
        return path;
    }

    private static long CalculatePathId(int chunkStart, int chunkEnd, byte regionPortalStart, byte regionPortalEnd)
    {
        return chunkStart << 28 | regionPortalStart << 22 | chunkEnd << 6 | regionPortalEnd; 
    }
    
    
    public static List<Vector2D> PortalsToPath(Cell[] grid, Portal?[] portals, Vector2D pathStart, Vector2D pathEnd, List<int> pathAsPortals)
    {
        List<Vector2D> path = AStar.FindPath(grid, pathStart, portals[pathAsPortals[0]]!.CenterPos);
        for (int i = 0; i < pathAsPortals.Count - 1; i++)
        {
            path.AddRange(AStar.FindPath(grid, portals[pathAsPortals[i]]!.CenterPos, portals[pathAsPortals[i + 1]]!.CenterPos));
        }
    
        path.AddRange(AStar.FindPath(grid, portals[pathAsPortals.Last()]!.CenterPos, pathEnd));
        return path;
    }
}