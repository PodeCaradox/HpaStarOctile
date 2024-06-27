using System.Collections.Generic;
using HpaStarPathfinding.pathfinding;

namespace HpaStarPathfinding.ViewModel
{
    public class MainWindowViewModel: ViewModelBase
    {
        public const int GridSize = 50;
        public const int ChunkSize = 10;
        public const int CellSize = 20;

        #region Propertys UI

        private bool _changePathfindingStartNodeEnabled = false;

        public bool changePathfindingStartNodeEnabled
        {
            get { return _changePathfindingStartNodeEnabled; }
            set
            {
                if (value != _changePathfindingStartNodeEnabled)
                {
                    if (value) changePathfindingEndNodeEnabled = false;
                    _changePathfindingStartNodeEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private bool _changePathfindingEndNodeEnabled = false;

        public bool changePathfindingEndNodeEnabled
        {
            get => _changePathfindingEndNodeEnabled;
            set
            {
                if (value == _changePathfindingEndNodeEnabled) return;
                if (value) changePathfindingStartNodeEnabled = false;
                _changePathfindingEndNodeEnabled = value;
                OnPropertyChanged();
            }
        }
        
        #endregion
        
        #region Properties
        
        private Vector2D _pathEnd;

        public Vector2D pathEnd
        {
            get => _pathEnd;
            set
            {
                if (Equals(value, _pathEnd)) return;
                _pathEnd = value;
                OnPropertyChanged();
            }
        }
        
        private Vector2D _pathStart;

        public Vector2D pathStart
        {
            get => _pathStart;
            set
            {
                if (Equals(value, _pathStart)) return;
                _pathStart = value;
                OnPropertyChanged();
            }
        }
        
        private List<Vector2D> _path = new List<Vector2D>();

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
        
        private Cell[,] _map;

        public Cell[,] map
        {
            get => _map;
            set
            {
                if (value == _map) return;
                _map = value;
                OnPropertyChanged();
            }
        }
        
        private Chunk[,] _chunks;

        public Chunk[,] chunks
        {
            get => _chunks;
            set
            {
                if (value == _chunks) return;
                _chunks = value;
                OnPropertyChanged();
            }
        }
        
        #endregion

        #region Init

        public void Init()
        {
            InitMap();
            pathStart = null;
            pathEnd = null;
            chunks = new Chunk[GridSize / ChunkSize, GridSize / ChunkSize];
            path = new List<Vector2D>();
        }
        
        private void InitMap()
        {
            map = new Cell[GridSize, GridSize];
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    var node = new Cell(new Vector2D(x, y));
                    map[y, x] = node;
                }
            }
        }

        #endregion

        #region Methods

        public bool PathPointIsWall(Vector2D vector2D)
        {
            return !_map[vector2D.x, vector2D.y].Walkable;
        }

        public void FindPath()
        {
            if (pathStart == null || pathEnd == null)
                return;
            path = Astar.FindPath(_map, _pathStart, _pathEnd);
        }

        

        #endregion
    }
}