// Author: František Holubec
// Created: 23.09.2025

using System.Collections.Generic;

namespace EDIVE.EyeTracking
{
    public readonly struct EyeGazeFrame
    {
        public readonly Pose CamPose;
        public readonly double Timestamp;
        
        private readonly EyeSample _leftEye;
        private readonly EyeSample _rightEye;
        
        public EyeGazeFrame(Pose camPose, EyeSample leftEye, EyeSample rightEye, double timestamp)
        {
            CamPose = camPose;
            _leftEye = leftEye;
            _rightEye = rightEye;
            Timestamp = timestamp;
        }
        
        public EyeSample GetEyeSample(EyeKind eye, EyeSpaceKind space)
        {
            return ConvertNativeEyeSample(GetNativeEyeSample(eye), space);
        }
        
        public IEnumerable<EyeSample> GetAllEyeSamples(EyeSpaceKind space)
        {
            yield return ConvertNativeEyeSample(_leftEye, space);
            yield return ConvertNativeEyeSample(_rightEye, space);
        }
        
        private EyeSample GetNativeEyeSample(EyeKind eye)
        { 
            return eye switch
            {
                EyeKind.Left => _leftEye,
                EyeKind.Right => _rightEye,
                _ => EyeSample.INVALID
            };
        }
        
        private EyeSample ConvertNativeEyeSample(EyeSample nativeSample, EyeSpaceKind space)
        { 
            return space switch
            {
                EyeSpaceKind.HeadSpace => nativeSample,
                EyeSpaceKind.WorldSpace => nativeSample.ToWorldSpace(CamPose),
                _ => EyeSample.INVALID
            };
        }
    }
}
