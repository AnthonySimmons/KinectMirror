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


namespace KinectMirror
{
    using KinectManager;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen _trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen _inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush _inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 480.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 640.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        private KinectManager _kinectManager;

        private DrawingGroup _drawingGroup;

        private DrawingImage _drawingImage;

        private WriteableBitmap _colorBitmap;

        private bool _depthEnabled = false, _colorEnabled = true, _skeletonEnabled = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadKinect()
        {
            try
            {
                _kinectManager = new KinectManager();
                _kinectManager.SkeletonFrameReadyHandler += KinectManager_SkeletonFrameReadyHandler;
                _kinectManager.ColorFrameReadyHandler += KinectManager_ColorFrameReadyHandler;
                _kinectManager.DepthFrameReadyHandler += KinectManager_DepthFrameReadyHandler;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadDrawing()
        {
            _drawingGroup = new DrawingGroup();
            _drawingImage = new DrawingImage(_drawingGroup);
            Image.Source = _drawingImage;

            _colorBitmap = new WriteableBitmap(_kinectManager.ColorFrameWidth, _kinectManager.ColorFrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            ColorStreamImage.Source = _colorBitmap;
        }

        private void DrawSkeleton()
        {
            using (DrawingContext dc = _drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, _kinectManager.ColorFrameWidth, _kinectManager.ColorFrameHeight));

                if (_kinectManager.Skeletons.Count != 0)
                {
                    foreach (Skeleton skel in _kinectManager.Skeletons)
                    {
                        if (skel.IsTracked())
                        {
                            DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.IsPositionTracked())
                        {
                            dc.DrawEllipse(
                            Brushes.Blue,
                            null,
                            _kinectManager.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                _drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }

        }

        private void ClearSkeletonImage()
        {
            using (DrawingContext dc = _drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, _kinectManager.ColorFrameWidth, _kinectManager.ColorFrameHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.IsTracked())
                {
                    drawBrush = this._trackedJointBrush;
                }
                else if (joint.IsInferred())
                {
                    drawBrush = _inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, _kinectManager.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.GetJoint(jointType0);
            Joint joint1 = skeleton.GetJoint(jointType1);

            // If we can't find either of these joints, exit
            if (joint0.IsNotTracked() || joint1.IsNotTracked())
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.IsInferred() && joint1.IsInferred())
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = _inferredBonePen;
            if (joint0.IsTracked() && joint1.IsTracked())
            {
                drawPen = _trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, _kinectManager.SkeletonPointToScreen(joint0.Position), _kinectManager.SkeletonPointToScreen(joint1.Position));
        }

        private void KinectManager_SkeletonFrameReadyHandler()
        {
            if (_skeletonEnabled)
            {
                DrawSkeleton();
            }
        }


        private void KinectManager_DepthFrameReadyHandler()
        {
            if (_depthEnabled)
            {
                // Write the pixel data into our bitmap
                _colorBitmap.WritePixels(
                    new Int32Rect(0, 0, _colorBitmap.PixelWidth, _colorBitmap.PixelHeight),
                    _kinectManager.DepthPixels,
                    _colorBitmap.PixelWidth * sizeof(int),
                    0);
            }
        }

        private void KinectManager_ColorFrameReadyHandler()
        {
            if (_colorEnabled)
            {
                _colorBitmap.WritePixels(
                            new Int32Rect(0, 0, _colorBitmap.PixelWidth, _colorBitmap.PixelHeight),
                            _kinectManager.ColorPixels,
                            _colorBitmap.PixelWidth * sizeof(int),
                            0);
            }
        }


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            LoadKinect();
            LoadDrawing();
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Options options = new Options(_kinectManager);

            options.SkeletonStreamChanged += Options_SkeletonStreamChanged;
            options.ColorStreamChanged += Options_ColorStreamChanged;
            options.DepthStreamChanged += Options_DepthStreamChanged;

            options.ShowDialog();

            options.SkeletonStreamChanged -= Options_SkeletonStreamChanged;
            options.ColorStreamChanged -= Options_ColorStreamChanged;
            options.DepthStreamChanged -= Options_DepthStreamChanged;
        }

        private void Options_DepthStreamChanged(bool isEnabled)
        {
            _depthEnabled = isEnabled;
        }

        private void Options_ColorStreamChanged(bool isEnabled)
        {
            _colorEnabled = isEnabled;
        }

        private void Options_SkeletonStreamChanged(bool isEnabled)
        {
            _skeletonEnabled = isEnabled;
            if (!_skeletonEnabled)
            {
                ClearSkeletonImage();
            }
        }
    }
}
