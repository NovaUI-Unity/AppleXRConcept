using UnityEngine;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    public static class RayExtensions
    {
        public static Ray Smooth(this Ray ray, Ray update, float updateWeight = 1 / 2f)
        {
            return new Ray(ray.origin.Smooth(update.origin, updateWeight), ray.direction.Smooth(update.direction, updateWeight));
        }
    }

    public static class Vector3Extensions
    {
        public static Vector3 Smooth(this Vector3 v, Vector3 update, float updateWeight = 1 / 2f)
        {
            if (float.IsNaN(updateWeight) || float.IsInfinity(updateWeight))
            {
                return update;
            }

            updateWeight = Mathf.Clamp01(updateWeight);

            return ((1 - updateWeight) * v) + (update * updateWeight);
        }
    }
}
