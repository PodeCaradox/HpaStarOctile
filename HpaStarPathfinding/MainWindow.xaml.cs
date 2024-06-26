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
        private Rectangle[,] chunks;
        
        private List<Line> lines = new List<Line>();
        
        public static int CellSize = 40;
        
        private CircleUI[] pathStart = {new CircleUI(Brushes.Green), new CircleUI(Brushes.Red)};

        private MainWindowViewModel _vm;
        
        public MainWindow()
        {
            DataContextChanged += OnDataContextChanged;
            InitializeComponent();
            PathfindingWindow.Width = CellSize * MainWindowViewModel.GridSize + 22;
            PathfindingWindow.Height = CellSize * MainWindowViewModel.GridSize + 76;
            PathCanvas.Height = CellSize * MainWindowViewModel.GridSize;
            PathCanvas.Width = CellSize * MainWindowViewModel.GridSize;
            PathCanvas.MouseLeftButtonDown += PathCanvasOnMouseLeftButtonDown;
            PathCanvas.IsEnabled = false;
            InitializeGrid();
            PathCanvas.IsEnabled = true;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _vm = DataContext as MainWindowViewModel;
        }

        private void PathCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = Mouse.GetPosition(PathCanvas);
            Vector2D mapPoint = Vector2D.ConvertPointToIndex(p);
            if (_vm.PathPointIsWall(mapPoint))
                return;

            var screenPoint = Vector2D.ConvertVector2DToScreenPosCentered(mapPoint);
            if (_vm.ChangeStart)
            {
                _vm.PathStart = mapPoint;
                pathStart[0].ChangePosition(PathCanvas, screenPoint);
            }

            if(_vm.ChangeEnd)
            {
                _vm.PathEnd = mapPoint;
                pathStart[1].ChangePosition(PathCanvas, screenPoint);
            }
        }



        private void InitializeGrid()
        {
            _vm.Init();
            for (int i = 0; i < MainWindowViewModel.GridSize; i++)
            {
                for (int j = 0; j < MainWindowViewModel.GridSize; j++)
                {
                    
                    _vm.Cells[i, j] = new Cell();
                    
                    Rectangle rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = Brushes.Black,
                        Fill = _vm.Cells[i, j].wall ? Brushes.Black : Brushes.White
                    };
                    rect.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
                    rect.Tag = _vm.Cells[i, j];
                    Canvas.SetLeft(rect, i * CellSize);
                    Canvas.SetTop(rect, j * CellSize);
                    PathCanvas.Children.Add(rect);
                }
            }

            chunks = new Rectangle[MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize, MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize];
            for (int i = 0; i < MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize; i++)
            {
                for (int j = 0; j < MainWindowViewModel.GridSize / MainWindowViewModel.ChunkSize; j++)
                {
                    
                    _vm.Chunks[i, j] = new Chunk();
                    Rectangle rect = new Rectangle
                    {
                        Width = CellSize * MainWindowViewModel.ChunkSize - 4,
                        Height = CellSize * MainWindowViewModel.ChunkSize - 4,
                        Stroke = Brushes.Yellow,
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false
                    };
                    rect.Tag = _vm.Chunks[i, j];
                    Canvas.SetLeft(rect, i * CellSize * MainWindowViewModel.ChunkSize + 2);
                    Canvas.SetTop(rect, j * CellSize * MainWindowViewModel.ChunkSize + 2);
                    chunks[i, j] = rect;
                }
            }
            
            
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Rectangle) || _vm.ChangeEnd || _vm.ChangeStart) 
                return;

            var rect = sender as Rectangle;
            var cell = (rect.Tag as Cell);
            if (cell == null) 
                return;
            
            cell.wall = !cell.wall;
            if (cell.wall)
            {
                rect.Fill = Brushes.Black;
                return;
            }
            
            rect.Fill = Brushes.White;
        }


        private void ToggleChunksButton_OnChecked(object sender, RoutedEventArgs e)
        {
            foreach (var chunk in chunks)
            { 
                PathCanvas.Children.Add(chunk);
            }
           
        }

        private void ToggleChunksButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var chunk in chunks)
            { 
                PathCanvas.Children.Remove(chunk);
            }
        }

        private void CalcPath(object sender, RoutedEventArgs e)
        {
            foreach (var line in lines)
            {
                PathCanvas.Children.Remove(line);
            }
            lines.Clear();

            if (_vm.Path.Count < 2) 
                return;
            
            for (int i = 1; i < _vm.Path.Count; i++)
            {
                var point1 = Vector2D.ConvertVector2DToScreenPosCentered(_vm.Path[i - 1]);
                var point2 = Vector2D.ConvertVector2DToScreenPosCentered(_vm.Path[i]);
                Line line = new Line
                {
                    StrokeThickness  = 2,
                    X1 = point1.X,
                    X2 = point2.X,
                    Y1 = point1.Y,
                    Y2 = point2.Y,
                    Stroke = Brushes.Red,
                    IsHitTestVisible = false
                };
                
                lines.Add(line);
                PathCanvas.Children.Add(line);
            }
        }
    }
}