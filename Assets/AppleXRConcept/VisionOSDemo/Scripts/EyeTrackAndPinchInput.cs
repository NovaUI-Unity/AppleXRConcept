using Nova;
using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    /// <summary>
    /// Effectively replicates touchscreen input using eyetracking and hand-tracked pinch gestures.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class EyeTrackAndPinchInput : MonoBehaviour
    {
        const int ControlID = 0;

        [Tooltip("Component managing the pinch state for both hands to detect click and drag gestures.")]
        public PinchToClick PinchStateMachine = null;
        [Tooltip("Component managing the eye tracking input position.")]
        public EyeTrackingInputRay EyeGazeRaycaster = null;

        [NonSerialized]
        private Vector3 totalDrag = Vector3.zero;
        [NonSerialized]
        private Ray eyeRay = default;

        private void Update()
        {
            Ray gestureRay = eyeRay;

            PinchState pinchState = PinchStateMachine.UpdatePinchState(eyeRay, ref totalDrag);

            switch (pinchState)
            {
                case PinchState.None:
                    totalDrag = Vector3.zero;
                    eyeRay = EyeGazeRaycaster.GetEyeGazeRay();
                    break;
                default:
                    gestureRay.origin += totalDrag;
                    break;
            }

            Interaction.Point(new Interaction.Update(gestureRay, ControlID), pinchState == PinchState.Active);
        }
    }
}
