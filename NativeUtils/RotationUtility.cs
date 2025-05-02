// Author: František Holubec
// Created: 28.11.2021

using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class RotationUtility
    {
        public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
        {
            var c = current.eulerAngles;
            var t = target.eulerAngles;
            return Quaternion.Euler(
                Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
                Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
                Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
            );
        }
        
        public static Vector3 SmoothDampEuler(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime)
        {
            return new Vector3(
                Mathf.SmoothDampAngle(current.x, target.x, ref currentVelocity.x, smoothTime),
                Mathf.SmoothDampAngle(current.y, target.y, ref currentVelocity.y, smoothTime),
                Mathf.SmoothDampAngle(current.z, target.z, ref currentVelocity.z, smoothTime)
            );
        }

    }
}
