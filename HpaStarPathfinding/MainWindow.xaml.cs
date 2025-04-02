using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        private const byte WALKABLE = 0b_0;
        private const byte BLOCKED = 0b_1111_1111;
        private const byte FREE = 0b_0000_0000;

        private Rectangle[,] _chunks;
        
        private Dictionary<Portal, Rectangle> _portals;
        
        private readonly List<Line> _lines = new List<Line>();
        
        private readonly CircleUi[] _pathStartEnd = { new CircleUi(Brushes.Green), new CircleUi(Brushes.Red)};

        private MainWindowViewModel _vm;


        #endregion

        #region Constructor

        public MainWindow()
        {
            DataContextChanged += OnDataContextChanged;
            InitializeComponent();
            PathfindingWindow.Width = MainWindowViewModel.CellSize * MainWindowViewModel.GridSize + 22;
            PathfindingWindow.Height = MainWindowViewModel.CellSize * MainWindowViewModel.GridSize + 76;
            PathCanvas.Height = MainWindowViewModel.CellSize * MainWindowViewModel.GridSize;
            PathCanvas.Width = MainWindowViewModel.CellSize * MainWindowViewModel.GridSize;
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
        }

        private void InitializePortals()
        {
            _portals = new Dictionary<Portal, Rectangle>();
            for (int y = 0; y < _vm.chunks.GetLength(0); y++)
            {
                for (int x = 0; x < _vm.chunks.GetLength(1); x++)
                {
                    ref Chunk chunk = ref _vm.chunks[y, x];
                    chunk.RebuildPortals(_vm.Map);
                    CreatePortalsOnCanvas(chunk);
                }
            }
        }

        private void CreatePortalsOnCanvas(Chunk chunk)
        {
            var brushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow};
            int i = 0;
            foreach (var portal in chunk.portals)
            {
                var dir = DirectionsVector.AllDirections[(int)portal.direction];
                Rectangle rect = new Rectangle
                {
                    Width = Math.Max(MainWindowViewModel.CellSize * (dir.x * portal.length) - 8,
                        MainWindowViewModel.CellSize - 8),
                    Height = Math.Max(MainWindowViewModel.CellSize * (dir.y * portal.length) - 8,
                        MainWindowViewModel.CellSize - 8),
                    Stroke = brushes[i],
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = false
                };
                i++;
                if (i >= brushes.Length)
                {
                    i = 0;
                }
                Canvas.SetLeft(rect, portal.startPos.x * MainWindowViewModel.CellSize + 4);
                Canvas.SetTop(rect, portal.startPos.y * MainWindowViewModel.CellSize + 4);
                _portals.Add(portal, rect);
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
            _chunks = new Rectangle[MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize,
                MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize];
            for (int y = 0; y < MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize; y++)
            {
                for (int x = 0; x < MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize; x++)
                {
                    _vm.chunks[y, x] = new Chunk(x, y);
                    Rectangle rect = new Rectangle
                    {
                        Width = MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize - 4,
                        Height = MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize - 4,
                        Stroke = Brushes.Yellow,
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false
                    };
                    rect.Tag = _vm.chunks[y, x];
                    Canvas.SetLeft(rect, x * MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize + 2);
                    Canvas.SetTop(rect, y * MainWindowViewModel.CellSize * MainWindowViewModel.ChunkSize + 2);
                    _chunks[y, x] = rect;
                }
            }
        }

        #endregion

        #region Events
        
        private void ClearClicked(object sender, RoutedEventArgs e)
        {
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
        
        private void ChunksButtonChecked(object sender, RoutedEventArgs e)
        {
            foreach (var chunk in _chunks)
            { 
                PathCanvas.Children.Add(chunk);
            }
           
        }

        private void ChunksButtonUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var chunk in _chunks)
            { 
                PathCanvas.Children.Remove(chunk);
            }
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
            mapCell.Connections = (mapCell.Walkable)? FREE : BLOCKED;
            byte i = 0;
            foreach (Vector2D dirVec in DirectionsVector.AllDirections)
            {
                if (mapCell.Position.y + dirVec.y >= _vm.Map.GetLength(0) ||
                    mapCell.Position.x + dirVec.x >= _vm.Map.GetLength(0) || mapCell.Position.x + dirVec.x < 0 ||
                    mapCell.Position.y + dirVec.y < 0)
                {
                    continue;
                }
                var otherCell = _vm.Map[mapCell.Position.y + dirVec.y, mapCell.Position.x + dirVec.x];
                byte walkable = (byte)(NOT_WALKABLE << i);
                walkable = (byte)(mapCell.Walkable? otherCell.Connections & ~walkable : otherCell.Connections | walkable);
          
                otherCell.Connections = walkable; 
                i++;
            }
            
            
            Vector2D chunkPos= new Vector2D( mapCell.Position.y/MainWindowViewModel.ChunkSize, mapCell.Position.x / MainWindowViewModel.ChunkSize);
            foreach (var dir in DirectionsVector.AllDirections)
            {
                RebuildPortal(chunkPos + dir);
            }
            RebuildPortal(chunkPos);
            
            CalcPath();
            rect.Fill = GetCellColor(mapCell);
        }

        private void RebuildPortal(Vector2D chunkPos)
        {
            if (chunkPos.x >= _vm.chunks.GetLength(1) 
                || chunkPos.y >= _vm.chunks.GetLength(0)
                || chunkPos.x < 0
                || chunkPos.y < 0) return;
            ref Chunk chunk = ref _vm.chunks[chunkPos.x, chunkPos.y];
            foreach (var portal in chunk.portals)
            {
                PathCanvas.Children.Remove(_portals[portal]);
                _portals.Remove(portal);
            }

            chunk.RebuildPortals(_vm.Map);
            CreatePortalsOnCanvas(chunk);
            foreach (var portal in chunk.portals)
            {
                PathCanvas.Children.Add(_portals[portal]);
            }
        }

        private static Brush GetCellColor(Cell mapCell)
        {
            if (!mapCell.Walkable)
            {
                return Brushes.Black;
            }

            return Brushes.White;
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
            if (!_vm.changePathfindingEndNodeEnabled)//Could be also a key pressed
                return;
            
            _vm.pathEnd = mapPoint;
            _pathStartEnd[1].ChangePosition(PathCanvas, screenPoint);
        }

        private void ChangePathfindingStartPoint(Vector2D mapPoint, Vector2D screenPoint)
        {
            if (!_vm.changePathfindingStartNodeEnabled)//Could be also a key pressed
                return;
            
            _vm.pathStart = mapPoint;
            _pathStartEnd[0].ChangePosition(PathCanvas, screenPoint);
        }

        #endregion


        private void DrawPortalsButtonChecked(object sender, RoutedEventArgs e)
        {
            foreach (var portal in _portals)
            { 
                PathCanvas.Children.Add(portal.Value);
            }
        }

        private void DrawPortalsButtonUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var portal in _portals)
            { 
                PathCanvas.Children.Remove(portal.Value);
            }
        }
    }
}