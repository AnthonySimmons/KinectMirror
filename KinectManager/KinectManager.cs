using System;
using System.Collections.Generic;
using System.Linq;
using Point = System.Windows.Point;

namespace KinectManager
{
    using Microsoft.Kinect;

    public delegate void SkeletonFrameReady();

    public delegate void ColorFrameReady();

    public delegate void DepthFrameReady();

    public class KinectManager : IDisposable
    {
        private KinectSensor _kinectSensor;
        private bool _isDisposed;

        public IList<Skeleton> Skeletons { get; } = new List<Skeleton>();

        public event SkeletonFrameReady SkeletonFrameReadyHandler;

        public event ColorFrameReady ColorFrameReadyHandler;

        public event DepthFrameReady DepthFrameReadyHandler;

        public byte[] ColorPixels { get; private set; }

        public DepthImagePixel [] DepthImagePixels { get; private set; }

        public byte[] DepthPixels { get; private set; }

        public int ColorFrameWidth => _kinectSensor.ColorStream.FrameWidth;

        public int ColorFrameHeight => _kinectSensor.ColorStream.FrameHeight;

        public int MaxDepth { get; private set; }

        public int MinDepth { get; private set; }

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

            _kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            _kinectSensor.ColorFrameReady += KinectSensor_ColorFrameReady;

            ColorPixels = new byte[_kinectSensor.ColorStream.FramePixelDataLength];

            _kinectSensor.SkeletonStream.Enable();
            _kinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;

            _kinectSensor.DepthStream.Enable();
            _kinectSensor.DepthFrameReady += KinectSensor_DepthFrameReady;

            DepthImagePixels = new DepthImagePixel[_kinectSensor.DepthStream.FramePixelDataLength];
            DepthPixels = new byte[_kinectSensor.DepthStream.FramePixelDataLength * sizeof(int)];

            _kinectSensor.Start();
        }

        private void KinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if(frame == null)
                {
                    return;
                }

                frame.CopyDepthImagePixelDataTo(DepthImagePixels);

                MaxDepth = frame.MaxDepth;
                MinDepth = frame.MinDepth;

                // Convert the depth to RGB
                int colorPixelIndex = 0;
                for (int i = 0; i < DepthImagePixels.Length; i++)
                {
                    // Get the depth for this pixel
                    short depth = DepthImagePixels[i].Depth;

                    // To convert to a byte, we're discarding the most-significant
                    // rather than least-significant bits.
                    // We're preserving detail, although the intensity will "wrap."
                    // Values outside the reliable depth range are mapped to 0 (black).

                    // Note: Using conditionals in this loop could degrade performance.
                    // Consider using a lookup table instead when writing production code.
                    // See the KinectDepthViewer class used by the KinectExplorer sample
                    // for a lookup table example.
                    byte intensity = (byte)(depth >= MinDepth && depth <= MaxDepth ? depth : 0);

                    // Write out blue byte
                    DepthPixels[colorPixelIndex++] = intensity;

                    // Write out green byte
                    DepthPixels[colorPixelIndex++] = intensity;

                    // Write out red byte                        
                    DepthPixels[colorPixelIndex++] = intensity;

                    // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                    // If we were outputting BGRA, we would write alpha here.
                    ++colorPixelIndex;
                }
            }
            OnDepthFrameReady();
        }

        private void KinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(ColorPixels);
                }
            }

            OnColorFrameReady();
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

        protected virtual void OnColorFrameReady()
        {
            ColorFrameReadyHandler?.Invoke();
        }

        protected virtual void OnDepthFrameReady()
        {
            DepthFrameReadyHandler?.Invoke();
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
