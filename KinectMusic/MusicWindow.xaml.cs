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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectMusic
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public abstract partial class MusicWindow : Window
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

        private const bool seated = true;
        #endregion

        private KinectSensor sensor;
        private Skeleton[] skeletons;
        private Instrument instrument;

        public MusicWindow()
        {
            InitializeComponent();

            this.Loaded += MusicControl_Loaded;
            this.Unloaded += MusicControl_Unloaded;
        }

        private void MusicControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.sensor.Stop();
        }

        private void MusicControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetUpSensor();
            this.SetUpInstrument();
        }

        public abstract void SetUpInstrument();

        public abstract void ProcessSkeleton(Skeleton skeleton);

        public void runtime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
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
