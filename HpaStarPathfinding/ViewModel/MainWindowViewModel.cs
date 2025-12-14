using System.Windows.Media.Imaging;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding;

namespace HpaStarPathfinding.ViewModel
{
    public class MainWindowViewModel: ViewModelBase
    {
        //Rendering
        public const int CellSize = 20;
        public static int multipliedCellSize { get; } = CellSize * 4;

        //Map config MapSize/ChunkSize should have no Remains  could be checked with modulo in future to handle the exception
        public const int ChunkSize = 10;
        public const int CellsInChunk = ChunkSize * ChunkSize;
        public const int MaxPortalsInChunk = ChunkSize * 4;//for each direction: 4 = Enum.GetValues(typeof(Directions)).Length

        #region Propertys UI
        
        public static int MapSizeX = 50;
        public static int MapSizeY = 40;
        public static int ChunkMapSizeX = MapSizeX / ChunkSize;
        public static int ChunkMapSizeY = MapSizeY / ChunkSize;
        
        private int _uIMapX = MapSizeX;
        public int uiMapX
        {
            get { return _uIMapX; }
            set
            {
                _uIMapX = value;
                OnPropertyChanged();
            }
        }
        
        private int _uIMapY = MapSizeY;
        public int uiMapY
        {
            get { return _uIMapY; }
            set
            {
                _uIMapY = value;
                OnPropertyChanged();
            }
        }

        private bool _enabledChangeCellBorderImage;
        public bool enabledChangeCellBorderImage
        {
            get { return _enabledChangeCellBorderImage; }
            set
            {
                _enabledChangeCellBorderImage = value;
                OnPropertyChanged();
            }
        }
        
        private Cell? _currentSelectedCell;
        public Cell? currentSelectedCell
        {
            get { return _currentSelectedCell; }
            set
            {
                _currentSelectedCell = value;
                OnPropertyChanged();
            }
        }
        
        private WriteableBitmap? _currentSelectedCellSource;
        public WriteableBitmap? currentSelectedCellSource
        {
            get { return _currentSelectedCellSource; }
            set
            {
                _currentSelectedCellSource = value;
                OnPropertyChanged();
            }
        }
        
        private WriteableBitmap?[] _cellStates = new WriteableBitmap?[byte.MaxValue + 1];
        public WriteableBitmap?[] cellStates
        {
            get { return _cellStates; }
            set
            {
                _cellStates = value;
                OnPropertyChanged();
            }
        }
        
        private bool _changePathfindingNodeEnabled;

        public bool changePathfindingNodeEnabled
        {
            get { return _changePathfindingNodeEnabled; }
            set
            {
                if (value != _changePathfindingNodeEnabled)
                {
                    _changePathfindingNodeEnabled = value;
                    OnPropertyChanged();
                }
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
            
        private List<AlgorithmSelection> _algorithms = new List<AlgorithmSelection>() {Algorithm.AStar, Algorithm.HPAStar};

        public List<AlgorithmSelection> algorithms
        {
            get => _algorithms;
            set
            {
                if (Equals(value, _algorithms)) return;
                _algorithms = value;
                OnPropertyChanged();
            }
        }
        
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
            set
            {
                if (value == _path) return;
                _path = value;
                OnPropertyChanged();
            }
        }
        
        private List<Vector2D> _otherPath = new List<Vector2D>();

        public List<Vector2D> otherPath
        {
            get => _otherPath;
            set
            {
                if (value == _otherPath) return;
                _otherPath = value;
                OnPropertyChanged();
            }
        }
        
        private Cell[] _map = [];

        public Cell[] map
        {
            get => _map;
            set
            {
                if (value == _map) return;
                _map = value;
                OnPropertyChanged();
            }
        }
        
        public Portal?[] Portals = [];
        
        private Chunk[] _chunks = [];

        public Chunk[] chunks
        {
            get => _chunks;
            set
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
            InitMap();
            pathStart = null;
            pathEnd = null;
            path = new List<Vector2D>();
        }
        
        private void InitMap()
        {
            Portals = new Portal[chunks.Length * MaxPortalsInChunk];
            map = new Cell[MapSizeY * MapSizeX];
            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    var node = new Cell(new Vector2D(x, y));
                    map[y  * MapSizeX + x] = node;
                }
            }
            
            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    map[y  * MapSizeX + x].UpdateConnection(map);
                }
            }
        }

        #endregion

        #region Methods

        public bool PathPointIsWall(Vector2D vector2D)
        {
            return _map[vector2D.y  * MapSizeX + vector2D.x].Connections == DirectionsAsByte.NOT_WALKABLE;
        }

        public void FindPath()
        {
            if (pathStart == null || pathEnd == null)
                return;

            if (_selectedAlgorithm == Algorithm.AStar)
            {
                path = Astar.FindPath(_map, _pathStart!, _pathEnd!);
                otherPath = HpaStarFindPath(_pathStart!, _pathEnd!);
            }
            else if (_selectedAlgorithm == Algorithm.HPAStar)
            {
                path = HpaStarFindPath(_pathStart!, _pathEnd!);
                otherPath = Astar.FindPath(_map, _pathStart!, _pathEnd!);
            }
        }

        private List<Vector2D> HpaStarFindPath(Vector2D start, Vector2D end)
        {
            var pathAsPortals = HpaStar.FindPath(_map, Portals, start, end);
            if (pathAsPortals.Count == 0) return [];
            return HpaStar.PortalsToPath(_map, Portals, start, end, pathAsPortals);
        }

        #endregion
    }
}