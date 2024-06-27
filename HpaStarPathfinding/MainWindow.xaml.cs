using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private Rectangle[,] _chunks;
        
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
            PathCanvas.IsEnabled = true;
        }

        private void InitializeGridMap()
        {
            for (int y = 0; y < _vm.map.GetLength(0); y++)
            {
                for (int x = 0; x < _vm.map.GetLength(1); x++)
                {
                    var node = _vm.map[y, x];
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
                    _vm.chunks[y, x] = new Chunk();
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
            CalcPath();
            rect.Fill = GetCellColor(mapCell);
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


    }
}