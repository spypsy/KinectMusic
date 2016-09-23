using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectMusic
{
    /// <summary>
    /// Interaction logic for BassWindow.xaml
    /// </summary>
    public partial class BassWindow : Window
    {
        #region constant Kinect variables
        //private const float Smoothing = 0.7f;
        //private const float Correction = 0.3f;
        //private const float Prediction = 0.4f;
        //private const float JitterRadius = 1.0f;
        //private const float MaxDeviationRadius = 0.5f;

        //private const float Smoothing = 0.0f;
        //private const float Correction = 0.0f;
        //private const float Prediction = 0.0f;
        //private const float JitterRadius = 0.0f;
        //private const float MaxDeviationRadius = 0.0f;

        private const float Smoothing = 0.75f;
        private const float Correction = 0.0f;
        private const float Prediction = 0.0f;
        private const float JitterRadius = 0.1f;
        private const float MaxDeviationRadius = 0.01f;
        #endregion

        // Global Objects
        private KinectSensor sensor;

        public Instrument instrument;

        private Skeleton[] skeletons;
        
        // Active area variables
        private int activeAreaTop;

        private int activeAreaBotton;

        private int activeAreaHeight;

        // The range of the control changes that will be sent
        private readonly int minControlValue = 85;

        private readonly int maxControlValue = 100;

        public int controlValueRange;

        // Hand Event
        public event ControlEventHandler OnHandChanged;

        // Previous hand position
        public double previousHandPosition = 0;

        public BassWindow(Instrument instrument, KinectSensor sensor)
        {
            InitializeComponent();

            this.instrument = instrument;

            this.sensor = sensor;
            this.sensor.SkeletonFrameReady += runtime_SkeletonFrameReady;
            this.kinectRegion.KinectSensor = this.sensor;

            this.Loaded += new RoutedEventHandler(BassWindow_Loaded);
            this.Unloaded += new RoutedEventHandler(BassWindow_Unloaded);    
        }

        private void BassWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            this.sensor.Stop();
            this.instrument.Close();
        }

        private void BassWindow_Loaded(object sender, RoutedEventArgs e)
        {
            controlValueRange = maxControlValue - minControlValue;
            this.wobbleLine.X1 = this.ActualWidth / 2;
            this.wobbleLine.Y1 = this.ActualHeight;

            this.OnHandChanged += new ControlEventHandler(this.instrument.ChangeControl);

            SetUpActiveArea();
        }

        private void processSkeleton(Skeleton skeleton)
        {
            var rightHandPoint = SetEllipseAndLinePosition(rightHand, skeleton.Joints[JointType.HandRight]);
            Point rPoint = new Point(rightHandPoint.X, rightHandPoint.Y);            
            
            if (rPoint.Y < activeAreaBotton && rPoint.Y > activeAreaTop && rPoint.Y != previousHandPosition)
            {
                ////int cc = GetControlValue(rPoint.Y);
                int cc = Helpers.GetValueInRange(
                    activeAreaTop, 
                    activeAreaBotton, 
                    rPoint.Y, 
                    minControlValue, 
                    maxControlValue);

                OnHandChanged(this, new ControlEventArgs(Midi.Control.Wobble, cc));
                previousHandPosition = rPoint.Y;
            }
        }

        private void SetUpActiveArea()
        {
            this.activeAreaTop = (int)Canvas.GetTop(activeArea);
            this.activeAreaHeight = (int)activeArea.ActualHeight;
            this.activeAreaBotton = activeAreaTop + activeAreaHeight;
        }
        
        private ColorImagePoint SetEllipseAndLinePosition(Ellipse ellipse, Joint joint)
        {
            ColorImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                joint.Position, 
                ColorImageFormat.RgbResolution640x480Fps30);

            point.X = (int)((point.X * canvas.ActualWidth) / 640.0);
            point.Y = (int)((point.Y * canvas.ActualHeight) / 480.0);

            Canvas.SetLeft(ellipse, point.X);
            Canvas.SetTop(ellipse, point.Y);
            wobbleLine.X2 = point.X + (ellipse.ActualWidth / 2);
            wobbleLine.Y2 = point.Y + (ellipse.ActualHeight / 2);
            
            return point;
        }
        
        /// <summary>
        /// Skeleton identifier
        /// </summary>
        private void runtime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
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
                            processSkeleton(currentSkeleton);
                        }
                    }
                }
            }
        }
    }
}
