using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Kinect;

namespace KinectMusic
{
	/// <summary>
	/// Interaction logic for DrumsWindow.xaml
	/// </summary>
	public partial class DrumsWindow : Window
    {        
        // global Objects
        private Instrument instrument;

        private KinectSensor sensor;

        private Skeleton[] skeletons;

        /// <summary>
        /// These array holds the information as such:
        /// [leftHand, rightHand]
        /// </summary>
        private bool[,] isDrumPlayingArray;

        private bool[] hasExited;

        // Geometries used for hit detection
        private Rect[] rectArray;

        // Animations & storyboards
        private DoubleAnimation[] animations;

        private Storyboard[] startPlayingStoryboards;

        private Storyboard[] stopPlayingStoryboards;

        // Hand events for notes
        public event NoteEventHandler OnStartPlay;

        public event NoteEventHandler OnStopPlay;
        
        /// <summary>
        /// Window for drums instrument
        /// </summary>
		public DrumsWindow(Instrument instrument, KinectSensor sensor)
		{
			this.InitializeComponent();
            this.instrument = instrument;

            // sensor initializations
            this.sensor = sensor;

            // Loaded and closed events
            this.ContentRendered += new EventHandler(DrumsWindow_Rendered);
            this.Closed += new EventHandler(DrumsWindow_Closed);     
		}

        private void DrumsWindow_Rendered(object sender, EventArgs e)
        {
            // Hand entering notes events
            this.OnStartPlay += new NoteEventHandler(this.instrument.PlayPerformanceNote);
            this.OnStopPlay += new NoteEventHandler(this.instrument.StopPerformanceNote);

            this.sensor.SkeletonFrameReady += SkeletonFrameReady;
            kinectRegion.KinectSensor = this.sensor;

            SetUpRectangles();
            //SetUpTempoControl();
            SetUpAnimations();

            // initialize array that holds drum rectangle state
            this.isDrumPlayingArray = new bool[2, rectArray.Length];
            this.hasExited = new bool[rectArray.Length];
        }

        private void DrumsWindow_Closed(object sender, EventArgs e)
        {
            this.instrument.Close();
        }

        private void ProcessSkeleton(Skeleton skeleton)
        {
            var rightHandPoint = SetEllipsePosition(
                rightHand,
                skeleton.Joints[JointType.HandRight]);

            var leftHandPoint = SetEllipsePosition(
                leftHand,
                skeleton.Joints[JointType.HandLeft]);

            Point rPoint = new Point(rightHandPoint.X, rightHandPoint.Y);
            Point lPoint = new Point(leftHandPoint.X, leftHandPoint.Y);

            for (int i = 0; i < rectArray.Length; i++)
            {
                // check if hand has entered and nothing is playing
                if (rectArray[i].Contains(lPoint) && hasExited[i])
                {
                    if (!IsDrumPlaying(i, 'L'))
                    {
                        OnStartPlay(this, new NoteEventArgs(i, 'L'));
                        setDrumState(i, true, 'L');
                        hasExited[i] = false;
                    }
                    // then check if the drum is playing already and the hand has already exited
                    else if (IsDrumPlaying(i, 'L'))
                    {
                        OnStopPlay(this, new NoteEventArgs(i, 'L'));
                        setDrumState(i, false, 'L');
                        hasExited[i] = false;
                    }
                }
                // check if hand has entered and nothing is playing
                if (rectArray[i].Contains(rPoint) && hasExited[i])
                {
                    if (!IsDrumPlaying(i, 'R'))
                    {
                        OnStartPlay(this, new NoteEventArgs(i, 'R'));
                        setDrumState(i, true, 'R');
                        hasExited[i] = false;
                    }
                    // then check if the drum is playing already and the hand has already exited
                    else if (IsDrumPlaying(i, 'R'))
                    {
                        OnStopPlay(this, new NoteEventArgs(i, 'R'));
                        setDrumState(i, false, 'R');
                        hasExited[i] = false;
                    }
                }
                // Now check if the hand is outside the drum but the music is playing
                // indicating that the hand has exited
                else if (!rectArray[i].Contains(rPoint) && !rectArray[i].Contains(lPoint))
                {
                    hasExited[i] = true;
                }
            }
        }

        public bool IsDrumPlaying(int index, char hand)
        {
            if (hand == 'L')
                return isDrumPlayingArray[0, index];
            else if (hand == 'R')
                return isDrumPlayingArray[1, index];
            else
                throw new ArgumentException("Hand identifying char needs to be 'L' or 'R'");
        }    
    
        public void setDrumState(int index, bool state, char hand)
        {
            if (hand == 'L')
                this.isDrumPlayingArray[0, index] = state;
            else if (hand == 'R')
                this.isDrumPlayingArray[1, index] = state;
            else throw new ArgumentException("Hand identifying char needs to be 'L' or 'R'");
        }

        private ColorImagePoint SetEllipsePosition(Ellipse ellipse, Joint joint)
        {
            ColorImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                joint.Position,
                ColorImageFormat.RgbResolution640x480Fps30);

            point.X = (int)((point.X * canvas.ActualWidth) / 640.0);
            point.Y = (int)((point.Y * canvas.ActualHeight) / 480.0);

            Canvas.SetLeft(ellipse, point.X);
            Canvas.SetTop(ellipse, point.Y);

            return point;
        }

        private void SetUpRectangles()
        {
            var foo = this.canvas.Children.OfType<Rectangle>();
            rectArray = new Rect[foo.Count()];
            int i = 0;
            foreach(Rectangle rectangle in foo)
            {
                // Make a brush to fill the rectangles with
                SolidColorBrush brush = new SolidColorBrush { Color = Colors.Purple, Opacity = 0 };
                rectangle.Fill = brush;
                // Register the name to attach to storyboards
                this.RegisterName("brush" + i, brush);

                Point topLeft = new Point(Canvas.GetLeft(rectangle), Canvas.GetTop(rectangle));
                Point bottomRight = new Point(Canvas.GetRight(rectangle), Canvas.GetBottom(rectangle));
                rectArray[i] = new Rect(topLeft, new Size(rectangle.ActualWidth, rectangle.ActualHeight));
                i++;
            }
        }

        public void SetUpAnimations()
        {
            // initialize animation & storyboard arrays
            animations = new DoubleAnimation[rectArray.Length];
            startPlayingStoryboards = new Storyboard[rectArray.Length];
            stopPlayingStoryboards = new Storyboard[rectArray.Length];

            // create 0-1 opacity animation
            DoubleAnimation startPlayingAnimation = new DoubleAnimation();
            startPlayingAnimation.From = 0.0;
            startPlayingAnimation.To = 1.0;
            startPlayingAnimation.Duration = TimeSpan.FromMilliseconds(100);

            // Set animation targets and add them to storyboards
            for (int i = 0; i < animations.Length; i++)
            {
                animations[i] = startPlayingAnimation.Clone();
                Storyboard.SetTargetName(animations[i], String.Format("brush{0}", i));
                Storyboard.SetTargetProperty(animations[i], new PropertyPath(SolidColorBrush.OpacityProperty));

                startPlayingStoryboards[i] = new Storyboard();
                startPlayingStoryboards[i].Children.Add(animations[i]);
            }

            // create 1-0 opacity animation
            DoubleAnimation stopPlayingAnimation = new DoubleAnimation();
            stopPlayingAnimation.From = 1.0;
            stopPlayingAnimation.To = 0.0;
            stopPlayingAnimation.Duration = TimeSpan.FromMilliseconds(50);

            // set animation targets and add them to storyboards
            for (int i = 0; i < animations.Length; i++)
            {
                animations[i] = stopPlayingAnimation.Clone();
                Storyboard.SetTargetName(animations[i], String.Format("brush{0}", i));
                Storyboard.SetTargetProperty(animations[i], new PropertyPath(SolidColorBrush.OpacityProperty));

                stopPlayingStoryboards[i] = new Storyboard();
                stopPlayingStoryboards[i].Children.Add(animations[i]);
            }

            // Attach animations to events
            this.OnStartPlay += new NoteEventHandler(PlayStartAnimation);
            this.OnStopPlay += new NoteEventHandler(PlayStopAnimation);
        }

        private void PlayStartAnimation(object sender, NoteEventArgs e)
        {
            int c = e.GetNoteIndex();
            startPlayingStoryboards[c].Begin(this);
        }

        private void PlayStopAnimation(object sender, NoteEventArgs e)
        {
            int c = e.GetNoteIndex();
            stopPlayingStoryboards[c].Begin(this);
        }

        private void SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            bool receivedData = false;

            using (SkeletonFrame SFrame = e.OpenSkeletonFrame())
            {
                if (SFrame == null) { }
                else
                {
                    skeletons = new Skeleton[SFrame.SkeletonArrayLength];
                    SFrame.CopySkeletonDataTo(skeletons);
                    receivedData = true;
                }

                if (receivedData)
                {
                    IEnumerable<Skeleton> sel = (from s in skeletons
                                                 where s.TrackingState == SkeletonTrackingState.Tracked
                                                 select s);
                    foreach (Skeleton currentSkeleton in sel)
                    {
                        if (currentSkeleton != null)
                        {
                            ProcessSkeleton(currentSkeleton);
                        }
                    }
                }
            }
        }
	}
}