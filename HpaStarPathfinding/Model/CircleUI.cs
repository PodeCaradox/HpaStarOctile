using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.Model
{
    public class CircleUI
    {
        //I know bindings are better but here just for simply fast testing
        private static int circleRadius = 10;
        public Ellipse Ellipse { get; set; }

        public CircleUI(Brush color)
        {
            Ellipse = new Ellipse
            {
                Width = circleRadius * 2,
                Height = circleRadius * 2,
                Fill = color
            };
        }

        public void ChangePosition(Canvas canvas, Vector2D pos)
        {
            
            canvas.Children.Remove(Ellipse);
            Canvas.SetLeft(Ellipse, pos.X - circleRadius);
            Canvas.SetTop(Ellipse, pos.Y - circleRadius);
            
            canvas.Children.Add(Ellipse);
        }
    }
}