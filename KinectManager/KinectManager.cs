using System;
using System.Collections.Generic;
using System.Linq;
using Point = System.Windows.Point;

namespace KinectManager
{
    using Microsoft.Kinect;

    public delegate void SkeletonFrameReady();

    public class KinectManager : IDisposable
    {
        private KinectSensor _kinectSensor;
        private bool _isDisposed;

        public IList<Skeleton> Skeletons { get; } = new List<Skeleton>();

        public event SkeletonFrameReady SkeletonFrameReadyHandler;

        public KinectManager()
        {
            LoadSensor();
        }

        private void LoadSensor()
        {
            _kinectSensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            if(_kinectSensor == null)
            {
                throw new InvalidOperationException("Could not load Kinect Sensor");
            }

            _kinectSensor.SkeletonStream.Enable();
            _kinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;
            _kinectSensor.Start();
        }

        private void KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Microsoft.Kinect.Skeleton[] skeletons = new Microsoft.Kinect.Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Microsoft.Kinect.Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);

                    Skeletons.Clear();
                    foreach(Microsoft.Kinect.Skeleton skeleton in skeletons)
                    {
                        Skeletons.Add(new Skeleton(skeleton));
                    }
                }
            }

            OnSkeletonFrameReady();
        }

        protected virtual void OnSkeletonFrameReady()
        {
            SkeletonFrameReadyHandler?.Invoke();
        }

        public Point SkeletonPointToScreen(SkeletonPoint skelPoint)
        {
            return SkeletonPointToScreen(skelPoint.X, skelPoint.Y, skelPoint.Z);
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        public Point SkeletonPointToScreen(float x, float y, float z)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            Microsoft.Kinect.SkeletonPoint skelPoint = new Microsoft.Kinect.SkeletonPoint()
            {
                X = x,
                Y = y,
                Z = z
            };
            DepthImagePoint depthPoint = _kinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelPoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        public int ElevationAngle
        {
            get => _kinectSensor.ElevationAngle;
            set => _kinectSensor.ElevationAngle = value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing && !_isDisposed)
            {
                _isDisposed = true;
                _kinectSensor.SkeletonFrameReady -= KinectSensor_SkeletonFrameReady;
                _kinectSensor.Stop();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
