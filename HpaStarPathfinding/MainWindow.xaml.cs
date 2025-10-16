using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using HpaStarPathfinding.Model;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties

        private const byte NOT_WALKABLE = 0b_1;
        private const byte BLOCKED = 0b_1111_1111;
        private const byte WALKABLE = 0b_0000_0000;
        private bool drawPortals = false;

        private Rectangle[,] _chunks;

        private Dictionary<Portal, (Rectangle, Rectangle)> _portals;

        private readonly List<Line> _lines = new List<Line>();

        private readonly CircleUi[] _pathStartEnd = { new CircleUi(Brushes.Green), new CircleUi(Brushes.Red) };

        private MainWindowViewModel _vm;


        private HashSet<Vector2D> _dirtyChunks;
        private HashSet<Vector2D> _dirtyTiles;

        private readonly List<Line> _portalConnections = new List<Line>();

        #endregion

        #region Constructor

        public MainWindow()
        {
            DataContextChanged += OnDataContextChanged;
            InitializeComponent();
            PathfindingWindow.Width = MainWindowViewModel.CellSize * MainWindowViewModel.MapSize + 22;
            PathfindingWindow.Height = MainWindowViewModel.CellSize * MainWindowViewModel.MapSize + 76;
            PathCanvas.Height = MainWindowViewModel.CellSize * MainWindowViewModel.MapSize;
            PathCanvas.Width = MainWindowViewModel.CellSize * MainWindowViewModel.MapSize;
            PathCanvas.MouseLeftButtonDown += MapOnMouseLeftButtonDown;
            Init();
        }

        #endregion

        #region Init

        private void Init()
        {
            PathCanvas.IsEnabled = false;
            _vm.Init();
            InitializeGridMap();
            InitializeGridChunks();
            InitializePortals();
            PathCanvas.IsEnabled = true;

            _dirtyTiles = new HashSet<Vector2D>();
            _dirtyChunks = new HashSet<Vector2D>();
        }


        private void InitializePortals()
        {
            _portals = new Dictionary<Portal, (Rectangle, Rectangle)>();
            Parallel.For(0, _vm.chunks.GetLength(0), y =>
            {
                for (int x = 0; x < _vm.chunks.GetLength(1); x++)
                {
                    ref Chunk chunk = ref _vm.chunks[y, x];
                    chunk.RebuildPortals(_vm.Map, ref _vm.Portals, x, y);
                    chunk.ConnectInternalPortals(_vm.Map, ref _vm.Portals, x, y);
                }
            });

            UpdatePortalsOnCanvas();
        }

        private void UpdatePortalsOnCanvas()
        {
            for (int y = 0; y < _vm.chunks.GetLength(0); y++)
            {
                for (int x = 0; x < _vm.chunks.GetLength(1); x++)
                {
                    ref Chunk chunk = ref _vm.chunks[y, x];
                    int chunkId = x + y * MainWindowViewModel.ChunkMapSize;
                    CreatePortalsOnCanvas(chunk, chunkId);
                }
            }
        }

        private void CreatePortalsOnCanvas(Chunk chunk, int chunkId)
        {
            double opacity = 0.0;
            if (drawPortals)
            {
                opacity = 1.0;
            }

            var brushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Violet };
            int i = 0; //Looking in Directions.N        Directions.E        Directions.S        Directions.W
            var steppingVector = new[]
                { DirectionsVector.E, DirectionsVector.S, DirectionsVector.W, DirectionsVector.N };

            foreach (Directions dirVec in Enum.GetValues(typeof(Directions)))
            {
                for (int j = 0; j < MainWindowViewModel.ChunkSize; j++)
                {
                    int key = Portal.GeneratePortalKey(chunkId, j, dirVec);
                    var portal = _vm.Portals[key];
                    if (portal == null) continue;
                    var dir = steppingVector[(int)portal.direction];

                    int startPosX = portal.startPos.x;
                    int startPosY = portal.startPos.y;
                    int centerPosX = portal.centerPos.x;
                    int centerPosY = portal.centerPos.y;
                    int width = dir.x * portal.portalLength;
                    int height = dir.y * portal.portalLength;

                    if (dir.x < 0)
                    {
                        startPosX += (width + 1);
                        width *= -1;
                    }

                    if (dir.y < 0)
                    {
                        startPosY += (height + 1);
                        height *= -1;
                    }

                    Rectangle rect = new Rectangle
                    {
                        Width = Math.Max(MainWindowViewModel.CellSize * (width) - 8,
                            MainWindowViewModel.CellSize - 8),
                        Height = Math.Max(MainWindowViewModel.CellSize * (height) - 8,
                            MainWindowViewModel.CellSize - 8),
                        Stroke = brushes[i],
                        Fill = Brushes.Transparent,
                        Opacity = opacity,
                        IsHitTestVisible = false
                    };
                    i++;
                    if (i >= brushes.Length)
                    {
                        i = 0;
                    }

                    Canvas.SetLeft(rect, startPosX * MainWindowViewModel.CellSize + 4);
                    Canvas.SetTop(rect, startPosY * MainWindowViewModel.CellSize + 4);

                    Brush color = Brushes.Green;
                    if (portal.mapBorderPortal)
                    {
                        color= Brushes.Red;
                    }
                    
                    Rectangle center = new Rectangle
                    {
                        Width = MainWindowViewModel.CellSize - 10,
                        Height = MainWindowViewModel.CellSize - 10,
                        Stroke = Brushes.Transparent,
                        Fill = color,
                        Opacity = opacity,
                        IsHitTestVisible = false
                    };

                    Canvas.SetLeft(center, centerPosX * MainWindowViewModel.CellSize + 5);
                    Canvas.SetTop(center, centerPosY * MainWindowViewModel.CellSize + 5);

                    _portals.Add(portal, (rect, center));
                    PathCanvas.Children.Add(rect);
                    PathCanvas.Children.Add(center);
                }
            }
        }

        private void InitializeGridMap()
        {
            for (int y = 0; y < _vm.Map.GetLength(0); y++)
            {
                for (int x = 0; x < _vm.Map.GetLength(1); x++)
                {
                    var node = _vm.Map[y, x];
                    Rectangle rect = new Rectangle
                    {
                        Width = MainWindowViewModel.CellSize,
                        Height = MainWindowViewModel.CellSize,
                        Stroke = Brushes.Black,
                        Fill = GetCellColor(node)
                    };
                    rect.MouseDown += MapCellMouseLeftButtonDown;
                    rect.MouseEnter += MapCellOnMouseEnter;
                    rect.Tag = node;
                    Canvas.SetLeft(rect, x * MainWindowViewModel.CellSize);
                    Canvas.SetTop(rect, y * MainWindowViewModel.CellSize);
                    PathCanvas.Children.Add(rect);
                }
            }
        }

        private void InitializeGridChunks()
        {
            _chunks = new Rectangle[MainWindowViewModel.MapSize / MainWindowViewModel.ChunkSize,
                MainWindowViewModel.MapSize / MainWindowViewModel.ChunkSize];
            for (int y = 0; y < MainWindowViewModel.MapSize / MainWindowViewModel.ChunkSize; y++)
            {
                for (int x = 0; x < MainWindowViewModel.MapSize / MainWindowViewModel.ChunkSize; x++)
                {
                    _vm.chunks[y, x] = new Chunk();
                    Rectangle rect = new Rectangle
                    {
                        Width = MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize - 4,
                        Height = MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize - 4,
                        Stroke = Brushes.Yellow,
                        Fill = Brushes.Transparent,
                        Opacity = 0.0,
                        IsHitTestVisible = false
                    };
                    rect.Tag = _vm.chunks[y, x];
                    Canvas.SetLeft(rect, x * MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize + 2);
                    Canvas.SetTop(rect, y * MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize + 2);
                    _chunks[y, x] = rect;
                    PathCanvas.Children.Add(rect);
                }
            }
        }

        #endregion

        #region Events

        private void ClearClicked(object sender, RoutedEventArgs e)
        {
            var button1 = (ToggleButton)Template.FindName("DrawPortalsButton", this);
            button1.IsChecked = false;

            PathCanvas.Children.Clear();
            Init();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _vm = DataContext as MainWindowViewModel;
        }

        private void MapOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = Mouse.GetPosition(PathCanvas);
            Vector2D mapPoint = Vector2D.ConvertPointToMapPoint(p);
            if (_vm.PathPointIsWall(mapPoint))
                return;

            var screenPoint = Vector2D.ConvertMapPointToCanvasPos(mapPoint);
            ChangePathfindingStartPoint(mapPoint, screenPoint);
            ChangePathfindingEndPoint(mapPoint, screenPoint);
            CalcPath();
        }

        private void MapCellMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChangeMapCell(sender);
        }

        private void MapCellOnMouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            ChangeMapCell(sender);
        }

        #endregion

        #region Methods

        private void ChangeMapCell(object sender)
        {
            if (_vm.changePathfindingEndNodeEnabled || _vm.changePathfindingStartNodeEnabled)
                return;

            if (!(sender is Rectangle rect))
                return;

            if (!(rect.Tag is Cell mapCell))
                return;

            if (mapCell.Position.Equals(_vm.pathStart) || mapCell.Position.Equals(_vm.pathEnd))
                return;

            mapCell.Walkable = !mapCell.Walkable;
            mapCell.Connections = (mapCell.Walkable) ? WALKABLE : BLOCKED;
            _vm.Map[mapCell.Position.y, mapCell.Position.x] = mapCell;
            rect.Fill = GetCellColor(mapCell);

            _dirtyTiles.Add(mapCell.Position);
            foreach (var dir in DirectionsVector.AllDirections)
            {
                var pos = mapCell.Position + dir;
                if (pos.y >= _vm.Map.GetLength(0) ||
                    pos.x >= _vm.Map.GetLength(1) ||
                    pos.x < 0 ||
                    pos.y < 0)
                {
                    continue;
                }

                _dirtyTiles.Add(pos);
            }

            Vector2D chunkPos = new Vector2D(mapCell.Position.x / MainWindowViewModel.ChunkSize,
                mapCell.Position.y / MainWindowViewModel.ChunkSize);
            _dirtyChunks.Add(chunkPos);
            foreach (var dir in DirectionsVector.AllDirections)
            {
                var pos = chunkPos + dir;
                if (pos.x >= _vm.chunks.GetLength(1)
                    || pos.y >= _vm.chunks.GetLength(0)
                    || pos.x < 0
                    || pos.y < 0) continue;
                _dirtyChunks.Add(pos);
            }


            RebuildTiles();
        }

        private void RebuildTiles()
        {
            PathCanvas.IsEnabled = false;
            foreach (var mapCellPos in _dirtyTiles)
            {
                ref Cell mapCell = ref _vm.Map[mapCellPos.y, mapCellPos.x];
                mapCell.UpdateConnection(_vm.Map);
            }

            RebuildPortals();

            PathCanvas.IsEnabled = true;
        }


        private void RebuildPortals()
        {
            //_timerRedrawPortals.Stop(); // Stop timer so it doesn’t repeat
            foreach (var chunk in _dirtyChunks)
            {
                RebuildPortalsInChunk(chunk);
            }

            _dirtyChunks.Clear();

            CalcPath();
        }

        private void RebuildPortalsInChunk(Vector2D chunkPos)
        {
            ref Chunk chunk = ref _vm.chunks[chunkPos.y, chunkPos.x];
            int chunkId = chunkPos.x + chunkPos.y * MainWindowViewModel.ChunkMapSize;
            foreach (Directions dirVec in Enum.GetValues(typeof(Directions)))
            {
                for (int j = 0; j < MainWindowViewModel.ChunkSize; j++)
                {
                    int key = Portal.GeneratePortalKey(chunkId, j, dirVec);
                    var portal = _vm.Portals[key];
                    if (portal == null) continue;
                    PathCanvas.Children.Remove(_portals[portal].Item1);
                    PathCanvas.Children.Remove(_portals[portal].Item2);
                    _portals.Remove(portal);
                }
            }

            chunk.RebuildPortals(_vm.Map, ref _vm.Portals, chunkPos.x, chunkPos.y);
            chunk.ConnectInternalPortals(_vm.Map, ref _vm.Portals, chunkPos.x, chunkPos.y);
            CreatePortalsOnCanvas(chunk, chunkId);
            DeletePortalConnectionsDrawn();
            DrawPortalConnections();
        }

        private static Brush GetCellColor(Cell mapCell)
        {
            if (mapCell.Walkable)
            {
                return Brushes.White;
            }

            return Brushes.Black;
        }

        private void CalcPath()
        {
            ResetPathUi();

            _vm.FindPath();

            if (_vm.path == null || _vm.path.Count < 2)
                return;

            var path = _vm.path.ToArray();

            DrawPathUi(path);
        }

        private void DrawPathUi(Vector2D[] path)
        {
            for (int i = 1; i < _vm.path.Count; i++)
            {
                var point1 = Vector2D.ConvertMapPointToCanvasPos(path[i - 1]);
                var point2 = Vector2D.ConvertMapPointToCanvasPos(path[i]);
                Line line = new Line
                {
                    StrokeThickness = 2,
                    X1 = point1.x,
                    X2 = point2.x,
                    Y1 = point1.y,
                    Y2 = point2.y,
                    Stroke = Brushes.Green,
                    IsHitTestVisible = false
                };

                _lines.Add(line);
                PathCanvas.Children.Add(line);
            }
        }

        private void ResetPathUi()
        {
            foreach (var line in _lines)
            {
                PathCanvas.Children.Remove(line);
            }

            _lines.Clear();
        }

        private void ChangePathfindingEndPoint(Vector2D mapPoint, Vector2D screenPoint)
        {
            if (!_vm.changePathfindingEndNodeEnabled) //Could be also a key pressed
                return;

            _vm.pathEnd = mapPoint;
            _pathStartEnd[1].ChangePosition(PathCanvas, screenPoint);
        }

        private void ChangePathfindingStartPoint(Vector2D mapPoint, Vector2D screenPoint)
        {
            if (!_vm.changePathfindingStartNodeEnabled) //Could be also a key pressed
                return;

            _vm.pathStart = mapPoint;
            _pathStartEnd[0].ChangePosition(PathCanvas, screenPoint);
        }

        #endregion


        private void DrawPortalsButtonChecked(object sender, RoutedEventArgs e)
        {
            drawPortals = true;
            foreach (var chunk in _chunks)
            {
                chunk.Opacity = 1.0;
            }

            foreach (var portal in _portals)
            {
                portal.Value.Item1.Opacity = 1.0;
                portal.Value.Item2.Opacity = 1.0;
            }
        }

        private void DrawPortalsButtonUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var chunk in _chunks)
            {
                chunk.Opacity = 0.0;
            }

            drawPortals = false;
            foreach (var portal in _portals)
            {
                portal.Value.Item1.Opacity = 0.0;
                portal.Value.Item2.Opacity = 0.0;
            }
        }

        private void DrawPortalsConnectionsChecked(object sender, RoutedEventArgs e)
        {
            DrawPortalConnections();
        }

        private void DrawPortalConnections()
        {
            HashSet<int> alreadyDrawn = new HashSet<int>();
            for (int key = 0; key < _vm.Portals.Length; key++)
            {
                if(key % MainWindowViewModel.MaxPortalsInChunk == 0) alreadyDrawn.Clear();
                ref var portal = ref _vm.Portals[key];
                if (portal == null) continue;
                int chunkIndexinPortalArray = key / MainWindowViewModel.MaxPortalsInChunk * MainWindowViewModel.MaxPortalsInChunk;
                foreach (var connection in portal.internalPortalConnections)
                {
                    if (connection.portal == byte.MaxValue) break;
                    
                    
                    int keyOtherPortal = chunkIndexinPortalArray + connection.portal;
                    var otherPortal = _vm.Portals[keyOtherPortal];
                    var keyInChunk = key % MainWindowViewModel.MaxPortalsInChunk;
                    int connectionKey1 = keyInChunk * MainWindowViewModel.MaxPortalsInChunk + connection.portal;
                    int connectionKey2 = keyInChunk + connection.portal * MainWindowViewModel.MaxPortalsInChunk;
                    if(!alreadyDrawn.Add(connectionKey1)) continue;
                    if(!alreadyDrawn.Add(connectionKey2)) continue;
                    var point1 = Vector2D.ConvertMapPointToCanvasPos(portal.centerPos);
                    var point2 = Vector2D.ConvertMapPointToCanvasPos(otherPortal.centerPos);
                    Line line = new Line
                    {
                        StrokeThickness = 2,
                        X1 = point1.x,
                        X2 = point2.x,
                        Y1 = point1.y,
                        Y2 = point2.y,
                        Stroke = Brushes.Yellow,
                        IsHitTestVisible = false
                    };

                    _portalConnections.Add(line);
                    PathCanvas.Children.Add(line);
                    
                }
                
                
            }
        }


        private void DrawPortalsConnectionsUnchecked(object sender, RoutedEventArgs e)
        {
            DeletePortalConnectionsDrawn();
        }

        private void DeletePortalConnectionsDrawn()
        {
            foreach (var portalConnection in _portalConnections)
            {
                PathCanvas.Children.Remove(portalConnection);
            }

            _portalConnections.Clear();
        }
    }
}