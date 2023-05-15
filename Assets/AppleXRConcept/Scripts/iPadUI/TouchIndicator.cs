using Nova;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Creates a finger proximity effect using the gradient of a UIBlock2D 
    /// </summary>
    [DisallowMultipleComponent]
    public class TouchIndicator : NovaBehaviour2D
    {
        [Tooltip("The hand which will activate this indicator.")]
        public Handedness Handedness = Handedness.Right;
        [Tooltip("The radius of the proximity indicator at \"Enable Indicator Distance\".")]
        public float MaxIndicatorRadius = 250;
        [Tooltip("The max distance in local space from the attached UI Block that the indicator is visible.")]
        public float EnableIndicatorDistance = 200;

        private void LateUpdate()
        {
            if (Handedness == Handedness.Invalid)
            {
                return;
            }

            // Get the finger world position
            Vector3 touchPointWorldSpace = Handedness == Handedness.Left ? XRHandsInputManager.LeftFingerPosition : XRHandsInputManager.RightFingerPosition;
            
            // Convert to local position
            Vector3 touchPointLocalSpace = transform.InverseTransformPoint(touchPointWorldSpace);
            float touchRadiusLocalSpace = XRHandsInputManager.ColliderRadius / transform.lossyScale.x;

            // Get local space normalized distance, but snap to 1 when behind the UIBlock
            float touchDistance = touchPointLocalSpace.z <= 0 ? Mathf.Abs(touchPointLocalSpace.z + touchRadiusLocalSpace) : EnableIndicatorDistance;
            float normalizedTouchDistance = Mathf.Clamp01(touchDistance / EnableIndicatorDistance);

            // Lerp the opacity and radius of the gradient based on the normalized distance of the finger,
            // where opacity = 0 and radius = MaxIndicatorRadius when distance == 1,
            // and opacity = 1 and radius = 0 when distance == 0

            UIBlock.Gradient = new RadialGradient()
            {
                Enabled = 1 - normalizedTouchDistance > 0,
                Center = (Vector2)touchPointLocalSpace,
                Radius = Vector2.one * MaxIndicatorRadius * normalizedTouchDistance,
                Color = UIBlock.Gradient.Color.WithAlpha(1 - normalizedTouchDistance),
            };
        }
    }
}