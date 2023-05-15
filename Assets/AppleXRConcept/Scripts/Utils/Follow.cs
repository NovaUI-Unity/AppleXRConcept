using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    public enum OutOfViewTriggerPoint
    {
        Center,
        Corners,
    }

    /// <summary>
    /// A configuration of a behavior intended to follow the camera.
    /// </summary>
    [CreateAssetMenu(fileName = "Follow", menuName = "Nova/XR/Follow Asset")]
    public class Follow : ScriptableObject
    {
        public const int NumCorners = 4;

        [Header("Trigger")]
        public OutOfViewTriggerPoint OutOfViewTrigger;
        [Tooltip("Only applicable when \"Out of View Point\" is set to \"Corners\"."), Range(0, NumCorners)]
        public int MaxAllowedCornersOutOfView = 2;

        [Header("Positioning")]
        public float MinDistance = 0f;
        public float MaxDistance = 1.25f;
        public float TargetDistance = 0.5f;
        public bool AlwaysVerticalCenter = false;
        public Vector3 EyeOffsetFromCamera = Vector3.down * 0.07f;

        [Header("Animation")]
        public float AnimationDuration = 1;
        public SpringCurve SpringAnimation = SpringCurve.Ease;
    }
}
