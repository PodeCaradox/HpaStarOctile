﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HpaStarPathfinding.pathfinding;

namespace HpaStarPathfinding.ViewModel
{
    public class MainWindowViewModel: ViewModelBase
    {
        //Rendering
        public const int CellSize = 20;
        
        //Map config MapSize/ChunkSize should have no Remains  could be checked with modulo in future to handle the exception
        public const int MapSize = 40;
        public const int ChunkSize = 10;
        public const int ChunkMapSize = MapSize / ChunkSize;
        public const int MaxPortalsInChunk = ChunkSize * 4;//for each direction: 4 = Enum.GetValues(typeof(Directions)).Length

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
        
            
        private String _selectedAlgorithm = Algorithm.AStar;

        public String SelectedAlgorithm
        {
            get => _selectedAlgorithm;
            set
            {
                if (Equals(value, _selectedAlgorithm)) return;
                _selectedAlgorithm = value;
                OnPropertyChanged();
            }
        }
            
        private List<String> _algorithms = new List<String>() {Algorithm.AStar, Algorithm.HPAStar};

        public List<String> Algorithms
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

        public Cell[,] Map
        {
            get => _map;
            set
            {
                if (value == _map) return;
                _map = value;
                OnPropertyChanged();
            }
        }
        
        public Portal[] Portals;
        
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
            chunks = new Chunk[MapSize / ChunkSize, MapSize / ChunkSize];
            path = new List<Vector2D>();
        }
        
        private void InitMap()
        {
            int mapChunkSize = MapSize / ChunkSize;
            //2 bits direction + 4 bits position + rest chunkindex.
            Portals = new Portal[(mapChunkSize * mapChunkSize) * MaxPortalsInChunk];
            Map = new Cell[MapSize, MapSize];
            for (int y = 0; y < Map.GetLength(0); y++)
            {
                for (int x = 0; x < Map.GetLength(1); x++)
                {
                    var node = new Cell(new Vector2D(x, y));
                    Map[y, x] = node;
                }
            }
            
            for (int y = 0; y < Map.GetLength(0); y++)
            {
                for (int x = 0; x < Map.GetLength(1); x++)
                {
                    
                    Map[y, x].UpdateConnection(Map);
                }
            }
        }

        #endregion

        #region Methods

        public bool PathPointIsWall(Vector2D vector2D)
        {
            return !_map[vector2D.y, vector2D.x].Walkable;
        }

        public void FindPath()
        {
            if (pathStart == null || pathEnd == null)
                return;

            if (_selectedAlgorithm == Algorithm.AStar)
            {
                path = Astar.FindPath(_map, _pathStart, _pathEnd);
            }
            else if (_selectedAlgorithm == Algorithm.HPAStar)
            {
                var pathAsPortals = HPAStar.FindPath(_map, Portals, _pathStart, _pathEnd);
                if (pathAsPortals.Count == 0) return;
                path = HPAStar.PortalsToPath(_map, Portals, _pathStart, _pathEnd, pathAsPortals);
            }
        }

        

        #endregion
    }
}