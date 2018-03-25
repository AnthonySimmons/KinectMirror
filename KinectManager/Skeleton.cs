
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KinectManager
{
    public class Skeleton
    {
        private readonly Microsoft.Kinect.Skeleton _skeleton;

        public Skeleton(Microsoft.Kinect.Skeleton skeleton)
        {
            _skeleton = skeleton;
        }

        public bool IsTracked() => _skeleton.TrackingState == SkeletonTrackingState.Tracked;

        public bool IsPositionTracked() => _skeleton.TrackingState == SkeletonTrackingState.PositionOnly;

        public SkeletonPoint Position => new SkeletonPoint(_skeleton.Position.X, _skeleton.Position.Y, _skeleton.Position.Z);

        public Joint GetJoint(JointType joint)
        {
            Microsoft.Kinect.JointType jointType = (Microsoft.Kinect.JointType)Enum.Parse(typeof(Microsoft.Kinect.JointType), joint.ToString());
            return new Joint(_skeleton.Joints[jointType]);
        }

        public IEnumerable<Joint> Joints => _skeleton.Joints.Select(j => new Joint(j));
    }
}
