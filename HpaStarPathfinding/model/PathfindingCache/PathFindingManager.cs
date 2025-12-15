using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.model.PathfindingCache;

public class PathFindingManager
{
    private static readonly Dictionary<int, List<int>> HighLevelPaths = new (1000) { [int.MaxValue] = [] };
    
    public static int GetPathId(Cell[] map, Portal?[] portals, Vector2D start, Vector2D end)
    {
        byte regionPortalEnd = map[end.y * MapSizeX + end.x].Region;
        byte regionPortalStart = map[start.y * MapSizeX + start.x].Region;

        int chunkStart = Chunk.CellPositionToChunkKey(start);
        int chunkEnd = Chunk.CellPositionToChunkKey(end);

        if (chunkStart == chunkEnd && regionPortalStart == regionPortalEnd) return 0; //regionPortalStart == byte.MaxValue and the oher but there can still be no path in same chunk.
        if(regionPortalStart == byte.MaxValue || regionPortalEnd == byte.MaxValue) return int.MaxValue;
        
        int pathId = CalculatePathId(chunkStart, chunkEnd, regionPortalStart, regionPortalEnd);
        if (HighLevelPaths.ContainsKey(pathId)) return pathId;
        List<int> path = HpaStar.FindPath(map, portals, start, end, regionPortalStart, regionPortalEnd);
        HighLevelPaths.Add(pathId, path);
        return pathId;
    }

    public static List<int> GetNextPath(int pathId)
    {
        HighLevelPaths.Remove(pathId, out var path);
        return path!;
    }

    private static int CalculatePathId(int chunkStart, int chunkEnd, byte regionPortalStart, byte regionPortalEnd)
    {
        return chunkStart << 22 | regionPortalStart << 16 | chunkEnd << 6 | regionPortalEnd;
    }
}