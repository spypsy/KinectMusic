using Microsoft.Kinect.Toolkit.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectMusic
{
	/// <summary>
	/// Interaction logic for tempoPathControl.xaml
	/// </summary>
	public partial class TempoControl : UserControl
	{   
        // Global variables
        ////private Geometry ellipseGeometry;
        private bool isHandInside;

        private PointCollection points;

        private Point midPoint;

        private Geometry boundGeometry;

        private DoubleAnimation handEnterAnimation;
        private DoubleAnimation handLeaveAnimation;

        private Storyboard handEnterStoryboard;
        private Storyboard handLeaveStoryboard;

        // Control value range
        public readonly int minControlValue = 25;
        public readonly int maxControlValue = 70;
        public int controlValueRange;

        /// <summary>
        /// The top point of the tempo path
        /// </summary>
        public Point Top
        {
            get { return this.tempoPath.Data.Bounds.TopLeft; }
        }

        /// <summary>
        /// The bottom point of the tempo path
        /// </summary>
        public Point Bottom
        {
            get { return this.points[points.Count - 1]; }
        }

        public bool IsHandInside
        {
            get { return this.isHandInside; }
        }

		public TempoControl()
        {
            this.Loaded += TempoControlLoaded;
			this.InitializeComponent();
		}

        private void TempoControlLoaded(object sender, RoutedEventArgs e)
        {
            SetUpPathPoints();
            SetUpEllipse();
            SetUpGeometry();
            SetUpAnimations();
            controlValueRange = maxControlValue - minControlValue;
            isHandInside = false;
        }

        public void TempoControl_OnHandEnter(object sender, EventArgs e)
        {
            this.PlayEnterAnimation();
            this.isHandInside = true;
        }

        public void TempoControl_OnHandLeave(object sender, EventArgs e)
        {
            this.PlayLeaveAnimation();
            this.isHandInside = false;
        }

        public void UpdateEllipsePosition(object sender, ControlEventArgs e)
        {
            int controlValue = e.GetControlValue();
            int index = Helpers.GetValueInRange(
                minControlValue,
                maxControlValue,
                controlValue,
                0,
                points.Count - 1);

            Canvas.SetLeft(ellipse, points[index].X - (ellipse.Width / 2));
            Canvas.SetTop(ellipse, points[index].Y - (ellipse.Height / 2));
        }

        public void UpdateEllipsePosition(Point handPosition)
        {
            Point closestPoint = Helpers.GetPointClosestToY(points, handPosition.Y);
            
            Canvas.SetLeft(ellipse, closestPoint.X - (ellipse.Width / 2));
            Canvas.SetTop(ellipse, closestPoint.Y - (ellipse.Height / 2));
        }

        private void SetUpGeometry()
        {
            this.boundGeometry = this.boundingPath.RenderedGeometry.Clone();
            boundGeometry.Transform = this.boundingPath.RenderTransform;
        }

        public bool ControlContainsPoint(Point point)
        {
            return this.boundGeometry.FillContains(point);
        }
    
        private void SetUpEllipse()
        {
            Canvas.SetLeft(ellipse, midPoint.X - (ellipse.Width / 2));
            Canvas.SetTop(ellipse, midPoint.Y - (ellipse.Height / 2));
        }

        private void SetUpPathPoints()
        {
            StreamGeometry streamGeo1 = (StreamGeometry)tempoPath.Data.Clone();
            PathGeometry pg = streamGeo1.GetFlattenedPathGeometry();
            PathFigure pf = pg.Figures[0];
            PolyLineSegment pls = (PolyLineSegment)pf.Segments[0];
            this.points = pls.Points;
            //this.midPoint = points[points.Count / 2];
            var midY = (Bottom.Y + Top.Y) / 2;
            this.midPoint = Helpers.GetPointClosestToY(points, midY);
        }

        private void SetUpAnimations()
        {
            // Create enter animation
            handEnterAnimation = new DoubleAnimation();
            handEnterAnimation.From = 0.0;
            handEnterAnimation.To = 1.0;
            handEnterAnimation.Duration = TimeSpan.FromMilliseconds(100);

            // Create leave animation
            handLeaveAnimation = new DoubleAnimation();
            handLeaveAnimation.From = 1.0;
            handLeaveAnimation.To = 0.0;
            handLeaveAnimation.Duration = TimeSpan.FromMilliseconds(100);

            // Set targets for enter animation
            Storyboard.SetTargetName(handEnterAnimation, "boundingBrush");
            Storyboard.SetTargetProperty(handEnterAnimation, new PropertyPath(SolidColorBrush.OpacityProperty));

            // set targets for leave animation
            Storyboard.SetTargetName(handLeaveAnimation, "boundingBrush");
            Storyboard.SetTargetProperty(handLeaveAnimation, new PropertyPath(SolidColorBrush.OpacityProperty));

            // Set up storyboard for enter animation
            handEnterStoryboard = new Storyboard();
            handEnterStoryboard.Children.Add(handEnterAnimation);

            // Set up storybaord for leave animation
            handLeaveStoryboard = new Storyboard();
            handLeaveStoryboard.Children.Add(handLeaveAnimation);
        }

        private void PlayEnterAnimation()
        {
            handEnterStoryboard.Begin(this);
        }

        private void PlayLeaveAnimation()
        {
            handLeaveStoryboard.Begin(this);
        }
    }
}