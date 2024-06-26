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
        private Rectangle[,] grid;
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
            
            InitializeGrid();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _vm = DataContext as MainWindowViewModel;
        }

        private void PathCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = Mouse.GetPosition(PathCanvas);
            Vector2D vector2D = Vector2D.ConvertPointToIndex(p);
            if (_vm.PathPointIsWall(vector2D))
                return;

            vector2D = Vector2D.ConvertVector2DToMapPosCentered(vector2D);
            if (_vm.ChangeStart)
            {
                pathStart[0].ChangePosition(PathCanvas, vector2D);
            }

            if(_vm.ChangeEnd)
            {
                pathStart[1].ChangePosition(PathCanvas, vector2D);
            }
        }



        private void InitializeGrid()
        {
            grid = new Rectangle[MainWindowViewModel.GridSize, MainWindowViewModel.GridSize];
            _vm.Cells = new Cell[MainWindowViewModel.GridSize, MainWindowViewModel.GridSize];
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
                        Fill = _vm.Cells[i, j].Wall ? Brushes.Black : Brushes.White
                    };
                    rect.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
                    rect.Tag = _vm.Cells[i, j];
                    Canvas.SetLeft(rect, i * CellSize);
                    Canvas.SetTop(rect, j * CellSize);
                    PathCanvas.Children.Add(rect);
                    grid[i, j] = rect;
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
            
            cell.Wall = !cell.Wall;
            if (cell.Wall)
            {
                rect.Fill = Brushes.Black;
                return;
            }
            
            rect.Fill = Brushes.White;
                
            
        }
        



        
    }
}