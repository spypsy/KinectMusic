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
    public delegate void NoteEventHandler(object source, NoteEventArgs e);
    public delegate void ControlEventHandler(object source, ControlEventArgs e);

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
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

        //private const float Smoothing = 0.75f;
        //private const float Correction = 0.0f;
        //private const float Prediction = 0.0f;
        //private const float JitterRadius = 0.1f;
        //private const float MaxDeviationRadius = 0.01f;
              
        private const float Smoothing = 0.3f;
        private const float Correction = 0.5f;
        private const float Prediction = 0.5f;
        private const float JitterRadius = 0.2f;
        private const float MaxDeviationRadius = 0.5f;

        /// Very smooth, but with a lot of latency.
        /// Filters out large jitters.
        /// Good for situations where smooth data is absolutely required
        /// and latency is not an issue.
        //private const float Smoothing = 0.7f;
        //private const float Correction = 0.3f;
        //private const float Prediction = 1.0f;
        //private const float JitterRadius = 1.0f;
        //private const float MaxDeviationRadius = 1.0f;
        
        #endregion

        private KinectSensor sensor;

        private bool seated = false;

        private string HoldMelodyFile = @"C:\Dev\KinectMusic\holdMelody.flp";

        private string StaccMelodyFile = @"C:\Dev\KinectMusic\staccMelody.flp";

        private string wobbleFile = @"C:\Dev\KinectMusic\wobbleBass.flp";

        private string performanceFile = @"C:\Dev\KinectMusic\Performance.flp";

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Closed += new EventHandler(MainWindow_Closed);
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            this.sensor.Stop();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetUpSensor();
        }

        private void MelodyHold_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(HoldMelodyFile);
            var instrument = new Instrument(5, true);
            MelodyWindow window = new MelodyWindow(instrument, this.sensor);
            window.Show();
        }

        private void MelodyStacc_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(StaccMelodyFile);
            var instrument = new Instrument(5, false);
            MelodyWindow window = new MelodyWindow(instrument, this.sensor);
            window.Show();
        }

        private void LongMelodyHold_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(HoldMelodyFile);
            var instrument = new Instrument(10, true);
            LongMelodyWindow window = new LongMelodyWindow(instrument, this.sensor);
            window.Show();
        }

        private void LongMelodyStacc_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(StaccMelodyFile);
            var instrument = new Instrument(10, false);
            LongMelodyWindow window = new LongMelodyWindow(instrument, this.sensor);
            window.Show();
        }

        private void Bass_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(wobbleFile);
            var instrument = new Instrument(false);
            BassWindow window = new BassWindow(instrument, this.sensor);
            window.Show();
        }

        private void Drums_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(performanceFile);
            var instrument = new Instrument(true);
            DrumsWindow window = new DrumsWindow(instrument, this.sensor);
            window.Show();
        }

        public void SetUpSensor()
        {
            // Sensor initializations
            sensor = KinectSensor.KinectSensors[0];
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.SkeletonStream.Enable(new TransformSmoothParameters()
            {
                Smoothing = Smoothing,
                Correction = Correction,
                Prediction = Prediction,
                JitterRadius = JitterRadius,
                MaxDeviationRadius = MaxDeviationRadius
            });
            sensor.SkeletonStream.EnableTrackingInNearRange = true;
            sensor.SkeletonStream.TrackingMode = seated ? SkeletonTrackingMode.Seated : SkeletonTrackingMode.Default;

            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.DepthStream.Range = DepthRange.Default;

            //kinectRegion.KinectSensor = this.sensor;

            //KinectRegion.AddHandPointerGripHandler(this, this.tempoControl.OnHandPointerGrip);
            //KinectRegion.AddHandPointerGripReleaseHandler(this, this.tempoControl.OnHandPointerGripRelease);        

            sensor.Start();
        }
    }
}
