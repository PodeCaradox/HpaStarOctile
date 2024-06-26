using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HpaStarPathfinding
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Rectangle[,] grid;
        private const int gridSize = 10;
        private const int cellSize = 40;
        
        public MainWindow()
        {
            InitializeComponent();
            InitializeGrid();
            ChangeRectangleColor(3, 3, Brushes.Red);
        }
        
        private void InitializeGrid()
        {
            grid = new Rectangle[gridSize, gridSize];
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    Rectangle rect = new Rectangle
                    {
                        Width = cellSize,
                        Height = cellSize,
                        Stroke = Brushes.Black,
                        Fill = Brushes.White // Initial color
                    };
                    Canvas.SetLeft(rect, i * cellSize);
                    Canvas.SetTop(rect, j * cellSize);
                    PathCanvas.Children.Add(rect);
                    grid[i, j] = rect;
                }
            }
        }

        private void ChangeRectangleColor(int x, int y, Brush color)
        {
            if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
            {
                grid[x, y].Fill = color;
            }
        }
    }
}