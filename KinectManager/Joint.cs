
namespace KinectManager
{
    public class Joint
    {
        private readonly Microsoft.Kinect.Joint _joint;

        public Joint(Microsoft.Kinect.Joint joint)
        {
            _joint = joint;
        }

        public bool IsTracked() => _joint.TrackingState == Microsoft.Kinect.JointTrackingState.Tracked;

        public bool IsNotTracked() => _joint.TrackingState == Microsoft.Kinect.JointTrackingState.NotTracked;

        public bool IsInferred() => _joint.TrackingState == Microsoft.Kinect.JointTrackingState.Inferred;

        public SkeletonPoint Position => new SkeletonPoint(_joint.Position.X, _joint.Position.Y, _joint.Position.Z);
    }
}
