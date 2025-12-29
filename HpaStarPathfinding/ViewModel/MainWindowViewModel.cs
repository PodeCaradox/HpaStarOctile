using System.Windows.Media.Imaging;
using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.model.ui;
using HpaStarPathfinding.pathfinding;
using HpaStarPathfinding.pathfinding.PathfindingCache;
using HpaStarPathfinding.pathfinding.PathfindingCache.PathfindingResultTypes;

namespace HpaStarPathfinding.ViewModel;

public class MainWindowViewModel: ViewModelBase
{
    //Rendering
    public const int CellSize = 20;
    public static int multipliedCellSize => CellSize * 4;

    public const int ChunkSize = 16;
    public const int CellsInChunk = ChunkSize * ChunkSize;
    public const int MaxPortalsInChunk = ChunkSize * 4;//for each direction: 4 = Enum.GetValues(typeof(Directions)).Length

    #region Propertys UI

    public static int MapSizeX { get; set; } = 30;
    public static int MapSizeY { get; set; } = 30;
    public static int CorrectedMapSizeX { get; set; } = MapSizeX + ChunkSize - MapSizeX % ChunkSize;
    public static int CorrectedMapSizeY { get; set; } = MapSizeY +  ChunkSize - MapSizeY % ChunkSize;
    public static int ChunkMapSizeX { get; set; } = CorrectedMapSizeX / ChunkSize;
    public static int ChunkMapSizeY { get; set; } = CorrectedMapSizeY / ChunkSize;
        
    private int _uIMapX = MapSizeX;
    public int uiMapX
    {
        get => _uIMapX;
        set
        {
            _uIMapX = value;
            OnPropertyChanged();
        }
    }
        
    private int _uIMapY = MapSizeY;
    public int uiMapY
    {
        get => _uIMapY;
        set
        {
            _uIMapY = value;
            OnPropertyChanged();
        }
    }
        
    private Cell? _currentSelectedCell;
    public Cell? currentSelectedCell
    {
        get => _currentSelectedCell;
        set
        {
            _currentSelectedCell = value;
            OnPropertyChanged();
        }
    }
        
    private WriteableBitmap? _currentSelectedCellSource;
    public WriteableBitmap? currentSelectedCellSource
    {
        get => _currentSelectedCellSource;
        set
        {
            _currentSelectedCellSource = value;
            OnPropertyChanged();
        }
    }
        
    private WriteableBitmap?[] _cellStates = new WriteableBitmap?[byte.MaxValue + 1];
    public WriteableBitmap?[] cellStates
    {
        get => _cellStates;
        set
        {
            _cellStates = value;
            OnPropertyChanged();
        }
    }
        
    private bool _changePathfindingNodeEnabled;

    public bool changePathfindingNodeEnabled
    {
        get => _changePathfindingNodeEnabled;
        set
        {
            if (value == _changePathfindingNodeEnabled) return;
            _changePathfindingNodeEnabled = value;
            OnPropertyChanged();
        }
    }
        
            
    private AlgorithmSelection _selectedAlgorithm = Algorithm.AStar;

    public AlgorithmSelection selectedAlgorithm
    {
        get => _selectedAlgorithm;
        set
        {
            if (Equals(value, _selectedAlgorithm)) return;
            _selectedAlgorithm = value;
            OnPropertyChanged();
        }
    }

    public List<AlgorithmSelection> algorithms { get; } = [Algorithm.AStar, Algorithm.HPAStar];

    #endregion
        
    #region Properties
        
    private Vector2D? _pathEnd;

    public Vector2D? pathEnd
    {
        get => _pathEnd;
        set
        {
            if (Equals(value, _pathEnd)) return;
            _pathEnd = value;
            OnPropertyChanged();
        }
    }
        
    private Vector2D? _pathStart;

    public Vector2D? pathStart
    {
        get => _pathStart;
        set
        {
            if (Equals(value, _pathStart)) return;
            _pathStart = value;
            OnPropertyChanged();
        }
    }
        
    private List<Vector2D> _path = [];

    public List<Vector2D> path
    {
        get => _path;
        private set
        {
            if (value == _path) return;
            _path = value;
            OnPropertyChanged();
        }
    }
        
    private List<Vector2D> _otherPath = [];

    public List<Vector2D> otherPath
    {
        get => _otherPath;
        private set
        {
            if (value == _otherPath) return;
            _otherPath = value;
            OnPropertyChanged();
        }
    }
        
    public Cell[] map = [];
        
    public Portal?[] Portals = [];
        
    private Chunk[] _chunks = [];

    public Chunk[] chunks
    {
        get => _chunks;
        private set
        {
            if (value == _chunks) return;
            _chunks = value;
            OnPropertyChanged();
        }
    }

    public bool calcPortals { get; set; }

    #endregion

    #region Init

    public void Init()
    {
        chunks = new Chunk[ChunkMapSizeY * ChunkMapSizeX];
        pathStart = null;
        pathEnd = null;
        path = [];
        InitMap();
    }
        
    private void InitMap()
    {
        Portals = new Portal[chunks.Length * MaxPortalsInChunk];
        map = new Cell[CorrectedMapSizeY * CorrectedMapSizeX];
        for (int y = 0; y < CorrectedMapSizeY; y++)
        {
            for (int x = 0; x < CorrectedMapSizeX; x++)
            {
                var node = new Cell(new Vector2D(x, y));
                map[y * CorrectedMapSizeX + x] = node;
            }
        }
            
        for (int y = 0; y < CorrectedMapSizeY; y++)
        {
            for (int x = 0; x < CorrectedMapSizeX; x++)
            {
                if(x >= MapSizeX || y >= MapSizeY) map[y  * CorrectedMapSizeX + x].Connections = 0b_1111_1111;
                else map[y  * CorrectedMapSizeX + x].UpdateConnection(map);
            }
        }
        

    }

    #endregion

    #region Methods

    public bool PathPointIsWall(Vector2D vector2D)
    {
        return map[vector2D.y  * CorrectedMapSizeX + vector2D.x].Connections == DirectionsAsByte.NOT_WALKABLE;
    }

    public void FindPath()
    {
        if (pathStart is null || pathEnd is null)
            return;

        if (_selectedAlgorithm == Algorithm.AStar)
        {
            path = AStar.FindPath(map, _pathStart!, _pathEnd!);
            otherPath = HpaStarFindPath(_pathStart!, _pathEnd!);
        }
        else if (_selectedAlgorithm == Algorithm.HPAStar)
        {
            path = HpaStarFindPath(_pathStart!, _pathEnd!);
            otherPath = AStar.FindPath(map, _pathStart!, _pathEnd!);
        }
    }

    private List<Vector2D> HpaStarFindPath(Vector2D start, Vector2D end)
    {
        PathfindingResult pathfindingResult = PathFindingManager.GetPath(map, Portals, start, end);
        switch (pathfindingResult.Type)
        {
            case PathfindingType.NoPath: return [];
            case PathfindingType.HighLevelPath:
            {
                var pathAsPortals = PathFindingManager.GetNextPath((pathfindingResult as HighLevelPathResult)!);
                return PathFindingManager.PortalsToPath(map, Portals, start, end, pathAsPortals!);
            }
            case PathfindingType.ShortPath:
            {
                var shortPath = PathFindingManager.GetNextPath((pathfindingResult as ShortPathResult)!);
                return shortPath;
            }
            default: return [];
        }
    }

    #endregion
}