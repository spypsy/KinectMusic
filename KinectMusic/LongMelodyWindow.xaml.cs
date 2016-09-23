﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;

using Midi;
using System.Windows.Media.Animation;

namespace KinectMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LongMelodyWindow : Window
    {       
        // Global Objects
        private KinectSensor sensor;

        public Instrument instrument;

        private Storyboard[] handEnterStoryboards;

        private Storyboard[] handLeaveStoryboards;

        private Skeleton[] skeletons;

        // Tempo control values
        private int tempoControlTop;

        private int tempoControlBottom;

        private int tempoControlHeight;

        private int controlValue;

        // Geometries used for hit detection
        private Geometry[] geoArray;

        // Animations
        private DoubleAnimation[] animations;

        // Hand events
        public event NoteEventHandler OnHandEnter;

        public event NoteEventHandler OnHandLeave;
        
        // Hand events for tempo Control
        public event ControlEventHandler OnHandChange;

        public event EventHandler OnHandEnterTempo;

        public event EventHandler OnHandLeaveTempo;

        public LongMelodyWindow(Instrument instrument, KinectSensor sensor)
        {
            InitializeComponent();
            this.instrument = instrument;

            // sensor initializations
            this.sensor = sensor;
            this.sensor.SkeletonFrameReady += SkeletonFrameReady;
            kinectRegion.KinectSensor = this.sensor;

            this.ContentRendered += new EventHandler(MelodyWindow_Rendered);
            this.Closed += new EventHandler(MelodyWindow_Closed);
        }

        private void MelodyWindow_Closed(object sender, EventArgs e)
        {
            this.instrument.Close();
        }

        private void MelodyWindow_Rendered(object sender, EventArgs e)
        {
            // Hand entering notes events
            this.OnHandEnter += new NoteEventHandler(this.instrument.PlayNote);
            this.OnHandLeave += new NoteEventHandler(this.instrument.StopNote);
            
            // Hand entering tempo control events
            this.OnHandEnterTempo += new EventHandler(this.tempoControl.TempoControl_OnHandEnter);
            this.OnHandLeaveTempo += new EventHandler(this.tempoControl.TempoControl_OnHandLeave);

            this.OnHandChange += new ControlEventHandler(this.instrument.ChangeControl);
            
            sensor.SkeletonFrameReady += SkeletonFrameReady;

            SetUpGeometries();
            SetUpTempoControl();
            SetUpAnimations();
        }

        void ProcessSkeleton(Skeleton skeleton)
        {
            var rightHandPoint = SetEllipsePosition(rightHand, skeleton.Joints[JointType.HandRight]);
            var leftHandPoint = SetEllipsePosition(leftHand, skeleton.Joints[JointType.HandLeft]);

            Point rPoint = new Point(rightHandPoint.X, rightHandPoint.Y);
            Point lPoint = new Point(leftHandPoint.X, leftHandPoint.Y);
            Point relativeRightPoint = this.TranslatePoint(rPoint, tempoControl);

            for (int i = 0; i < geoArray.Length; i++)
            {
                // Checking for tempo control interaction
                if (tempoControl.ControlContainsPoint(relativeRightPoint))         
                {
                    if (!tempoControl.IsHandInside)
                        OnHandEnterTempo(this, new EventArgs());

                    int controlValue = Helpers.GetValueInRange(
                        tempoControlTop, 
                        tempoControlBottom, 
                        rPoint.Y, 
                        tempoControl.minControlValue, 
                        tempoControl.maxControlValue);

                    if (controlValue != this.controlValue)
                    {
                        tempoControl.UpdateEllipsePosition(rPoint);
                        OnHandChange(this, new ControlEventArgs(Midi.Control.Tempo, controlValue));
                        this.controlValue = controlValue;
                    }
                }
                else if (!tempoControl.ControlContainsPoint(relativeRightPoint) && tempoControl.IsHandInside)
                {
                    OnHandLeaveTempo(this, new EventArgs());
                }

                if (geoArray[i].FillContains(rPoint) && !instrument.IsNotePlaying(i, 'R'))
                {
                    OnHandEnter(this, new NoteEventArgs(i, 'R'));
                }
                else if (!geoArray[i].FillContains(rPoint) && instrument.IsNotePlaying(i, 'R'))
                {
                    OnHandLeave(this, new NoteEventArgs(i, 'R'));
                }

                if (geoArray[i].FillContains(lPoint) && !instrument.IsNotePlaying(i, 'L'))
                {
                    OnHandEnter(this, new NoteEventArgs(i, 'L'));
                }
                else if (!geoArray[i].FillContains(lPoint) && instrument.IsNotePlaying(i, 'L'))
                {
                    OnHandLeave(this, new NoteEventArgs(i, 'L'));
                }
            }
        }


        /// <summary>
        /// Skeleton Identifier
        /// </summary>
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

        private void SetUpAnimations()
        {
            animations = new DoubleAnimation[geoArray.Length];
            handEnterStoryboards = new Storyboard[geoArray.Length];
            handLeaveStoryboards = new Storyboard[geoArray.Length];

            DoubleAnimation handEnterAnimation = new DoubleAnimation();
            handEnterAnimation.From = 0.0;
            handEnterAnimation.To = 1.0;
            handEnterAnimation.Duration = TimeSpan.FromMilliseconds(50);

            if (!this.instrument.HoldNote)
                handEnterAnimation.AutoReverse = true;

            for (int i = 0; i < animations.Length; i++)
            {
                animations[i] = handEnterAnimation.Clone();
                Storyboard.SetTargetName(animations[i], String.Format("brush{0}", i + 1));
                Storyboard.SetTargetProperty(animations[i], new PropertyPath(SolidColorBrush.OpacityProperty));

                handEnterStoryboards[i] = new Storyboard();
                handEnterStoryboards[i].Children.Add(animations[i]);
            }

            // Hand leave animations enabled when the note is held by hand's user
            if (this.instrument.HoldNote)
            {
                DoubleAnimation handLeaveAnimation = new DoubleAnimation();
                handLeaveAnimation.From = 1.0;
                handLeaveAnimation.To = 0.0;
                handLeaveAnimation.Duration = TimeSpan.FromMilliseconds(50);

                for (int i = 0; i < animations.Length; i++)
                {
                    animations[i] = handLeaveAnimation.Clone();
                    Storyboard.SetTargetName(animations[i], String.Format("brush{0}", i + 1));
                    Storyboard.SetTargetProperty(animations[i], new PropertyPath(SolidColorBrush.OpacityProperty));

                    handLeaveStoryboards[i] = new Storyboard();
                    handLeaveStoryboards[i].Children.Add(animations[i]);
                }
                this.OnHandLeave += new NoteEventHandler(PlayLeaveAnimation);
            }

            this.OnHandEnter += new NoteEventHandler(PlayEnterAnimation);
        }

        private void PlayEnterAnimation(object sender, NoteEventArgs e)
        {
            handEnterStoryboards[e.GetNoteIndex()].Begin(this);
        }

        private void PlayLeaveAnimation(object sender, NoteEventArgs e)
        {
            handLeaveStoryboards[e.GetNoteIndex()].Begin(this);
        }
        
        private void SetUpTempoControl()
        {
            tempoControlHeight = (int)tempoControl.ActualHeight;
            tempoControlTop = (int)tempoControl.TranslatePoint(tempoControl.Top, this.canvas).Y - 100;
            tempoControlBottom = (int)tempoControl.TranslatePoint(tempoControl.Bottom, this.canvas).Y;
        }
        
        private void SetUpGeometries()
        {
            geoArray = new Geometry[10];

            Geometry geo1 = btn1.Data.Clone();
            geo1.Transform = btn1.RenderTransform;
            geoArray[0] = geo1;

            Geometry geo2 = btn2.Data.Clone();
            geo2.Transform = btn2.RenderTransform;
            geoArray[1] = geo2;

            Geometry geo3 = btn3.Data.Clone();
            geo3.Transform = btn3.RenderTransform;
            geoArray[2] = geo3;

            Geometry geo4 = btn4.Data.Clone();
            geo4.Transform = btn4.RenderTransform;
            geoArray[3] = geo4;

            Geometry geo5 = btn5.Data.Clone();
            geo5.Transform = btn5.RenderTransform;
            geoArray[4] = geo5;

            Geometry geo6 = btn6.Data.Clone();
            geo6.Transform = btn6.RenderTransform;
            geoArray[5] = geo6;

            Geometry geo7 = btn7.Data.Clone();
            geo7.Transform = btn7.RenderTransform;
            geoArray[6] = geo7;

            Geometry geo8 = btn8.Data.Clone();
            geo8.Transform = btn8.RenderTransform;
            geoArray[7] = geo8;

            Geometry geo9 = btn9.Data.Clone();
            geo9.Transform = btn9.RenderTransform;
            geoArray[8] = geo9;

            Geometry geo10 = btn10.Data.Clone();
            geo10.Transform = btn10.RenderTransform;
            geoArray[9] = geo10;
        }
    }
}
