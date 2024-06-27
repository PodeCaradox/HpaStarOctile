using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.Model
{
    public class CircleUi
    {
        //Bindings are better but here just for simple fast testing
        private const int CircleRadius = 10;
        private Ellipse ellipse { get; }

        public CircleUi(Brush color)
        {
            ellipse = new Ellipse
            {
                Width = CircleRadius * 2,
                Height = CircleRadius * 2,
                Fill = color
            };
        }

        public void ChangePosition(Canvas canvas, Vector2D pos)
        {
            canvas.Children.Remove(ellipse);
            Canvas.SetLeft(ellipse, pos.x - CircleRadius);
            Canvas.SetTop(ellipse, pos.y - CircleRadius);
            canvas.Children.Add(ellipse);
        }
    }
}