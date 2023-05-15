using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// An abstract base class for components interested in reacting to homebar drag events
    /// </summary>
    public abstract class HomebarGestureHandler : NovaBehaviour
    {
        [Header("Trigger")]
        [Tooltip("The interactable root of the homebar")]
        public UIBlock Homebar = null;
        [Tooltip("The reference volume to calculate the drag percentage.\n\"Drag Trigger Threshold Percent\" refers to a percent of this volume.")]
        public UIBlock ThresholdVolume = null;
        [Min(0)]
        [Tooltip("The percent of the threshold volume the user must drag to trigger a complete homebar gesture")]
        public float DragTriggerThresholdPercent = 0.2f;

        [Header("Repositioning")]
        [Tooltip("The follow controller responsible for moving the root of this object.")]
        public FollowController FollowBehavior = null;

        private float totalDragDelta = 0;
        private float smoothDragDelta = 0;
        private AnimationHandle homebarAnimation = default;
        private AnimationHandle fadeAnimation = default;

        protected virtual void OnEnable()
        {
            if (Homebar != null)
            { 
                // Subscribe to gesture events
                Homebar.AddGestureHandler<Gesture.OnPress>(HandleHomebarPressed);
                Homebar.AddGestureHandler<Gesture.OnDrag>(HandleHomebarDragged);
                Homebar.AddGestureHandler<Gesture.OnRelease>(HandleHomebarReleased);
                Homebar.AddGestureHandler<Gesture.OnCancel>(HandleHomebarDragCanceled);
            }
        }

        protected virtual void OnDisable()
        {
            if (Homebar != null)
            { 
                // Unsubscribe from gesture events
                Homebar.RemoveGestureHandler<Gesture.OnPress>(HandleHomebarPressed);
                Homebar.RemoveGestureHandler<Gesture.OnDrag>(HandleHomebarDragged);
                Homebar.RemoveGestureHandler<Gesture.OnRelease>(HandleHomebarReleased);
                Homebar.RemoveGestureHandler<Gesture.OnCancel>(HandleHomebarDragCanceled);
            }

            // Clean up running animations
            homebarAnimation.Complete();
            fadeAnimation.Complete();
        }

        /// <summary>
        /// Handle pointer enter events
        /// </summary>
        private void HandleHomebarPressed(Gesture.OnPress evt)
        {
            homebarAnimation.Cancel();

            if (FollowBehavior != null)
            {
                FollowBehavior.enabled = false;
            }

            RunFadeAnimation(fadeIn: true, ref fadeAnimation);

            smoothDragDelta = 0;
            totalDragDelta = 0;

            evt.Consume();
        }

        /// <summary>
        /// Handle drag events
        /// </summary>
        private void HandleHomebarDragged(Gesture.OnDrag evt)
        {
            fadeAnimation.Complete();

            int dragAxis = evt.DraggableAxes.X ? Axis.X.Index() :
                           evt.DraggableAxes.Y ? Axis.Y.Index() :
                           evt.DraggableAxes.Z ? Axis.Z.Index() : -1;

            if (dragAxis >= 0)
            {
                smoothDragDelta = SmoothDragDelta(smoothDragDelta, evt.DragDeltaLocalSpace[dragAxis]);
                totalDragDelta = evt.RawTranslationLocalSpace[dragAxis];
            }
            else
            {
                smoothDragDelta = 0;
                totalDragDelta = 0;
            }

            HandleDrag(smoothDragDelta);

            evt.Consume();
        }

        /// <summary>
        /// Handle pointer release events and kick-off any animations triggered by the gesture
        /// </summary>
        private void HandleHomebarReleased(Gesture.OnRelease evt)
        {
            if (!homebarAnimation.IsComplete())
            {
                return;
            }

            if (!evt.WasDragged)
            {
                RunFadeAnimation(fadeIn: false, ref fadeAnimation);
                return;
            }

            ThreeD<bool> dragAxes = evt.Receiver.GetComponent<Interactable>().Draggable;

            int dragAxis = dragAxes.X ? Axis.X.Index() :
                           dragAxes.Y ? Axis.Y.Index() :
                           dragAxes.Z ? Axis.Z.Index() : -1;

            int dragDirection = 0;

            if (dragAxis >= 0)
            {
                // Determine if the gesture threshold was surpassed
                float baseSize = ThresholdVolume.CalculatedSize[dragAxis].Value;
                float percentDragged = baseSize == 0 ? 0 : totalDragDelta / baseSize;
                dragDirection = percentDragged <= -DragTriggerThresholdPercent ? 1 : percentDragged >= DragTriggerThresholdPercent ? -1 : 0;
            }

            if (FollowBehavior != null)
            {
                FollowController.OnEnableBehavior behavior = FollowBehavior.WhenEnabled;
                FollowBehavior.WhenEnabled = FollowController.OnEnableBehavior.None;
                FollowBehavior.enabled = true;
                FollowBehavior.WhenEnabled = behavior;
            }

            RunAllAnimations(dragDirection, ref homebarAnimation, ref fadeAnimation);

            evt.Consume();
        }

        /// <summary>
        /// Handle gesture canceled events
        /// </summary>
        private void HandleHomebarDragCanceled(Gesture.OnCancel evt)
        {
            if (!homebarAnimation.IsComplete())
            {
                return;
            }

            if (FollowBehavior != null)
            {
                FollowController.OnEnableBehavior behavior = FollowBehavior.WhenEnabled;
                FollowBehavior.WhenEnabled = FollowController.OnEnableBehavior.None;
                FollowBehavior.enabled = true;
                FollowBehavior.WhenEnabled = behavior;
            }

            RunAllAnimations(0, ref homebarAnimation, ref fadeAnimation);

            evt.Consume();
        }

        /// <summary>
        /// Creates a simple-moving-average of the drag deltas to smooth out jitters or inconsistent drag velocities
        /// </summary>
        private static float SmoothDragDelta(float current, float newDelta) => current * 0.5f + newDelta * 0.5f;

        protected abstract void HandleDrag(float dragDelta);
        protected abstract void RunFadeAnimation(bool fadeIn, ref AnimationHandle fadeAnimation);
        protected abstract void RunAllAnimations(int dragDirection, ref AnimationHandle dragAnimation, ref AnimationHandle fadeAnimation);
    }
}
