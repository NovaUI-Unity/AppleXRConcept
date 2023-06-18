using Nova;
using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    public enum PinchState
    {
        None,
        Active,
        Ending
    }

    /// <summary>
    /// Tracks the pinch state for both hands
    /// </summary>
    public class PinchToClick : MonoBehaviour
    {
        [Header("Hands")]
        [SerializeField]
        [Tooltip("The left hand to track.")]
        private SingleHand leftHand = new SingleHand();

        [SerializeField]
        [Tooltip("The right hand to track.")]
        private SingleHand rightHand = new SingleHand();

        [Header("Pinch Tunables")]
        [Tooltip("When the change in pinch strength increases by this much, that will trigger a \"pinch started\" state and locks the gaze ray.")]
        [Range(0, 1)]
        public float PinchStartDelta = 0.05f;
        [Tooltip("The amount of time (in seconds) from \"pinch started\" for the user to activate the pinch gesture before the gaze ray unlocks.")]
        public float PinchStartToTriggerTime = 0.3f;
        [Tooltip("When the pinch strength is greater than or equal to this value, that will trigger a \"pinch active\" state.")]
        [Range(0, 1)]
        public float PinchActiveThreshold = 1;
        [Tooltip("Hand movements within this distance (world space) from the pinch origin will be considered unmoved.")]
        [Min(0)]
        public float PinchActiveDeadzone = 0.0015f;
        [Tooltip("The delay (in seconds) from \"pinch active\" before hand translations begin dragging content. Exists to handle cases where fast pinches lead to unintentional hand movements.")]
        [Min(0)]
        public float PinchActivationTime = 0.025f;
        [Tooltip("The delay (in seconds) after \"pinch ended\" before the gesture ray unlocks from the released position.")]
        [Min(0)]
        public float PinchEndCooldownTime = 0.3f;

        [Header("Drag Tunables")]
        [Tooltip("Sensitivity of drag interactions. Higher values allow for smaller physical hand movements when dragging.")]
        [Min(0)]
        public float DragSensitivity = 2f;
        [Tooltip("Higher values will make dragging more smooth but less responsive. Low values will make dragging more responsive but also more noisy.")]
        public float DragSmoothRate = 4;

        /// <summary>
        /// A struct tracking a single OVRHand and a SphereCollider on tip of the hand's index finger
        /// </summary>
        [Serializable]
        private struct SingleHand
        {
            [Tooltip("A sphere collider on the tip of the Hand's index finger")]
            public SphereCollider Collider;
            [Tooltip("The tracked hand.")]
            public OVRHand Hand;

            public float GetPinchStrength() => Tracked ? Hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) : 0;
            public bool Tracked => Hand != null && Hand.IsTracked && Hand.IsPointerPoseValid && Hand.IsDataHighConfidence && Hand.IsDataValid && !Hand.IsSystemGestureInProgress;
        }


        [NonSerialized]
        bool wasPinching = false;
        [NonSerialized]
        bool pinchStarted = false;
        [NonSerialized]
        UIBlock pinchRoot = null;
        [NonSerialized]
        float pinchStartTime = 0;
        [NonSerialized]
        float pinchTriggerTime = 0;
        [NonSerialized]
        float pinchEndTime = 0;
        [NonSerialized]
        bool pinchWasActivated = false;
        [NonSerialized]
        SingleHand pinchHand = default;
        [NonSerialized]
        Vector3 pinchHandOriginRootSpace = Vector3.zero;

        [NonSerialized]
        private float previousRightPinch = 0;
        [NonSerialized]
        private float previousLeftPinch = 0;

        public PinchState UpdatePinchState(Ray gazeRay, ref Vector3 totalDrag)
        {
            float rightPinch = rightHand.GetPinchStrength();
            float leftPinch = leftHand.GetPinchStrength();

            leftPinch = float.IsInfinity(leftPinch) || float.IsNaN(leftPinch) ? 0 : leftPinch;
            rightPinch = float.IsInfinity(rightPinch) || float.IsNaN(rightPinch) ? 0 : rightPinch;

            bool rightPinchStarting = rightPinch >= previousRightPinch + PinchStartDelta;
            bool leftPinchStarting = leftPinch >= previousLeftPinch + PinchStartDelta;

            bool isPinching = leftPinch >= PinchActiveThreshold || rightPinch >= PinchActiveThreshold;

            if (isPinching && !wasPinching)
            {
                pinchTriggerTime = Time.unscaledTime;
            }

            if (!isPinching && wasPinching)
            {
                pinchEndTime = Time.unscaledTime;
            }

            bool pinchEnding = !isPinching && Time.unscaledTime - pinchEndTime <= PinchEndCooldownTime;

            if ((rightPinchStarting || leftPinchStarting) && !pinchStarted)
            {
                pinchStartTime = Time.unscaledTime;
            }

            bool pinchStarting = !pinchEnding && !isPinching && Time.unscaledTime - pinchStartTime <= PinchStartToTriggerTime;

            bool pinchTriggered = pinchStarting || pinchEnding || isPinching || wasPinching;

            bool pinchActivated = isPinching && Time.unscaledTime - pinchTriggerTime >= PinchActivationTime;

            if (pinchTriggered)
            {
                if (pinchActivated && !pinchWasActivated)
                {
                    pinchHand = rightPinch >= PinchActiveThreshold ? rightHand : leftHand;
                    pinchHandOriginRootSpace = pinchHand.Collider.transform.position;

                    if (Interaction.Raycast(gazeRay, out UIBlockHit hit))
                    {
                        pinchRoot = hit.UIBlock.Root;
                        pinchHandOriginRootSpace = pinchRoot.transform.InverseTransformPoint(pinchHand.Collider.transform.position);
                    }
                }

                if (pinchActivated && !pinchEnding && pinchRoot != null)
                {
                    Vector3 pinchOriginWorldSpace = pinchRoot.transform.TransformPoint(pinchHandOriginRootSpace);
                    Vector3 translation = pinchHand.Collider.transform.position - pinchOriginWorldSpace;

                    Vector3 currentDrag = translation * Vector3.Distance(pinchOriginWorldSpace, pinchRoot.transform.position) * DragSensitivity;

                    totalDrag = totalDrag.Smooth(currentDrag, 1 / DragSmoothRate);
                    totalDrag = totalDrag.magnitude < PinchActiveDeadzone ? Vector3.zero : totalDrag;
                }
            }

            wasPinching = isPinching;
            pinchWasActivated = pinchActivated;
            pinchStarted = (pinchStarting || isPinching) && !pinchEnding;
            previousLeftPinch = leftPinch;
            previousRightPinch = rightPinch;

            return pinchActivated ? PinchState.Active : pinchEnding ? PinchState.Ending : PinchState.None;
        }
    }
}

