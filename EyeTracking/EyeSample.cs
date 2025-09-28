// Author: František Holubec
// Created: 25.09.2025

namespace EDIVE.EyeTracking
{
    public readonly struct EyeSample
    {
        public readonly Pose GazePose;
        public readonly float Confidence;
        public readonly bool IsValid;

        public static readonly EyeSample INVALID = new(Pose.IDENTITY, 0, false);

        public EyeSample(Pose gazePose, float confidence, bool isValid = true)
        {
            GazePose = gazePose;
            Confidence = confidence;
            IsValid = isValid;
        }
        
        public EyeSample ToWorldSpace(Pose cameraPose)
        {
            return new EyeSample(GazePose.ToWorldSpace(cameraPose), Confidence, IsValid);
        }
    }
}
