using Nova;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    /// <summary>
    /// Converts raw left/right eye gaze vectors into a more stablized "focus ray" for UI input
    /// </summary>
    public class EyeTrackingInputRay : MonoBehaviour
    {
        const float FocusBoundsGazeInsetPercent = 0.01f;

        [Header("Eyes")]
        public Transform LeftEye = null;
        public Transform RightEye = null;

        [Header("Tunables")]
        [Tooltip("Higher values will make eye movements more smooth but less responsive. Low values will make eye movements more responsive but also more noisy.")]
        public float EyeMovementSmoothFactor = 4f;
        [Tooltip("Higher values will make the input focus cursor position more smooth but less responsive. Low values will make the input focus cursor position more responsive but also more noisy.")]
        public float FocusRaySmoothFactor = 2f;
        [Tooltip("The radius of the sphere-casting sphere in world space units.")]
        public float SpherecastRadius = 0.075f;
        [Tooltip("Smoothed eye adjustments with a gaze position less than this distance (world space) from the current point of \"focus\" will be ignored.")]
        public float EyeAdjustmentDeadzone = 0.06f;
        [Tooltip("Eye gaze must remain fixated on a target for this amount of time (in seconds) before it's considered \"looked at\".")]
        public float GazeDwellTime = 0.025f;

        [Header("Debugging")]
        [Tooltip("Enable debugging visuals. Editor only.")]
        public bool DebugView = false;
        [Tooltip("The Transform to position (and scale) at the intersection point of the UI and the smoothed eye ray. This is not where clicks occur.")]
        public Transform SmoothedEyePosition = null;
        [Tooltip("The Transform to position (and scale) at the intersection point of the UI and the input focus ray. This is where clicks occur.")]
        public Transform FocusedInputPosition = null;

        [NonSerialized]
        private Ray focusRay = new Ray(Vector3.zero, Vector3.forward);

        [NonSerialized]
        private List<UIBlockHit> sphereCollisions = new List<UIBlockHit>();

        [NonSerialized]
        private UIBlock focusedUIBlock = null;
        [NonSerialized]
        private float focusedUIBlockTime = 0;

        [NonSerialized]
        private EyePos leftEye;
        [NonSerialized]
        private EyePos rightEye;
        [NonSerialized]
        private EyePos centerEye;

        /// <summary>
        /// Effectively an eye transform
        /// </summary>
        private struct EyePos
        {
            public Vector3 Position;
            public Quaternion Orientation;

            public Vector3 Forward => Orientation * Vector3.forward;

            public static EyePos Lerp(EyePos from, EyePos to, float lerp)
            {
                return new EyePos()
                {
                    Position = Vector3.Lerp(from.Position, to.Position, lerp),
                    Orientation = Quaternion.Slerp(from.Orientation, to.Orientation, lerp)
                };
            }
        }

        private void Awake()
        {
            if (SmoothedEyePosition != null)
            {
                SmoothedEyePosition.gameObject.SetActive(false);
            }

            if (FocusedInputPosition != null)
            {
                FocusedInputPosition.gameObject.SetActive(false);
            }
        }

        public Ray GetEyeGazeRay()
        {
            Vector3 eyePosL = LeftEye.transform.position;
            Vector3 eyePosR = RightEye.transform.position;

            Quaternion eyeRotL = LeftEye.transform.rotation;
            Quaternion eyeRotR = RightEye.transform.rotation;

            EyePos leftEyeRaw = new EyePos() { Position = eyePosL, Orientation = eyeRotL };
            EyePos rightEyeRaw = new EyePos() { Position = eyePosR, Orientation = eyeRotR };

            Ray prevGazeRay = new Ray(centerEye.Position, centerEye.Forward);

            // Smooth raw left eye pos with previous left eye pos 
            leftEye = EyePos.Lerp(leftEye, leftEyeRaw, 1 / EyeMovementSmoothFactor);

            // Smooth raw right eye pos with previous right eye pos 
            rightEye = EyePos.Lerp(rightEye, rightEyeRaw, 1 / EyeMovementSmoothFactor);

            // Center eye is the 50/50 between left and right, and then smooth that with previous center eye pos
            centerEye = EyePos.Lerp(centerEye, EyePos.Lerp(leftEye, rightEye, 0.5f), 1 / EyeMovementSmoothFactor);

            Ray currentGazeRay = new Ray(centerEye.Position, centerEye.Forward);
            bool hitUI = Interaction.Raycast(currentGazeRay, out UIBlockHit currentPoint);

            float distanceToPoint = hitUI ? Vector3.Distance(currentGazeRay.origin, currentPoint.Position) : 0;
            float distanceBetweenFocusAndGaze = hitUI ? Vector3.Distance(focusRay.GetPoint(distanceToPoint), currentPoint.Position) : 0;
            float distanceMovedBetweenFrames = hitUI ? Vector3.Distance(prevGazeRay.GetPoint(distanceToPoint), currentPoint.Position) : 0;

            bool surpassedFocusThreshold = distanceBetweenFocusAndGaze >= EyeAdjustmentDeadzone;

            sphereCollisions.Clear();

            if (hitUI && surpassedFocusThreshold)
            {
                Sphere sphere = new Sphere(currentPoint.Position, SpherecastRadius);

                // Because the eye data is pretty noisy and relatively imprecise, just using a ray
                // will often lead to focusing on the wrong object. So we go one step further here
                // and do a sphere collision test at the intersection point of the gaze ray/UI
                // which helps account for the spatial distribution of eye data. In the context
                // of primarily 2D content, this is effectively a spherecast.
                Interaction.SphereCollideAll(sphere, sphereCollisions);
            }

            UIBlockHit mostCentral = default;
            float minDistance = float.MaxValue;

            for (int i = 0; i < sphereCollisions.Count; ++i)
            {
                UIBlockHit hit = sphereCollisions[i];
                UIBlock hitBlock = hit.UIBlock;

                if (!hitBlock.TryGetComponent(out GestureRecognizer gr) || !gr.enabled)
                {
                    continue;
                }

                // The sphere collision test results are sorted by intersection point to sphere center distance,
                // but in this case we want the object whose center is closest to where the user is looking 
                float distanceToGaze = Vector3.Distance(hitBlock.transform.position, currentPoint.Position);

                if (distanceToGaze < minDistance)
                {
                    minDistance = distanceToGaze;
                    mostCentral = hit;
                }
            }

            bool pointingAtNewObject = mostCentral.UIBlock != null && mostCentral.UIBlock != focusedUIBlock;
            bool focusedObjectInvalid = focusedUIBlock == null || !focusedUIBlock.gameObject.activeInHierarchy;

            if (pointingAtNewObject || focusedObjectInvalid)
            {
                focusedUIBlock = mostCentral.UIBlock;
                focusedUIBlockTime = Time.unscaledTime;
            }

            if (hitUI && focusedUIBlock != null && Time.unscaledTime - focusedUIBlockTime >= GazeDwellTime)
            {
                Bounds focusedBounds = new Bounds(Vector3.zero, focusedUIBlock.CalculatedSize.Value);
                Vector3 sphereLocalPosition = focusedUIBlock.transform.InverseTransformPoint(currentPoint.Position);
                Vector3 hitLocalPosition = focusedBounds.ClosestPoint(sphereLocalPosition);

                float sphereLocalRadius = SpherecastRadius / focusedUIBlock.transform.lossyScale.x;

                bool containsXCenter = Mathf.Abs(sphereLocalPosition.x) <= sphereLocalRadius;
                bool containsYCenter = Mathf.Abs(sphereLocalPosition.y) <= sphereLocalRadius;

                if (containsXCenter || containsYCenter)
                {
                    float xPadding = FocusBoundsGazeInsetPercent * focusedBounds.size.x;
                    float yPadding = FocusBoundsGazeInsetPercent * focusedBounds.size.y;

                    // Snap to X/Y local origin when possible, since that will give us the most stable ray direction
                    float x = containsXCenter ? 0 : Mathf.Clamp(hitLocalPosition.x, focusedBounds.min.x + xPadding, focusedBounds.max.x - xPadding);
                    float y = containsYCenter ? 0 : Mathf.Clamp(hitLocalPosition.y, focusedBounds.min.y + yPadding, focusedBounds.max.y - yPadding);
                    float z = hitLocalPosition.z;

                    Vector3 position = focusedUIBlock.transform.TransformPoint(new Vector3(x, y, z));

                    focusRay = focusRay.Smooth(new Ray(currentGazeRay.origin, Vector3.Normalize(position - currentGazeRay.origin)), 1 / FocusRaySmoothFactor);
                }
            }

#if UNITY_EDITOR
            UpdateDebugVisuals();
#endif

            return focusRay;
        }

        private void UpdateDebugVisuals()
        {
            if (SmoothedEyePosition != null)
            {
                SmoothedEyePosition.gameObject.SetActive(DebugView);

                Ray ray = new Ray(centerEye.Position, centerEye.Forward);

                if (Interaction.Raycast(ray, out UIBlockHit hit))
                {
                    SmoothedEyePosition.position = hit.Position;
                }
                else
                {
                    SmoothedEyePosition.position = ray.GetPoint(2f);
                }

                SmoothedEyePosition.localScale = Vector3.one * SpherecastRadius;
            }

            if (FocusedInputPosition != null)
            {
                bool show = DebugView && focusedUIBlock != null;
                FocusedInputPosition.gameObject.SetActive(show);

                if (show && Interaction.Raycast(focusRay, out UIBlockHit hit))
                {
                    FocusedInputPosition.position = hit.Position;
                    FocusedInputPosition.localScale = Vector3.one * SpherecastRadius;
                }
            }
        }
    }
}
