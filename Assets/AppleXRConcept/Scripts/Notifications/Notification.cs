using System;
using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Animates in the FaceTime notification
    /// </summary>
    public class Notification : MonoBehaviour
    {
        /// <summary>
        /// The animation curve for the floating "bounce" effect while the notification is visible.
        /// </summary>
        private static readonly AnimationCurve BounceCurve = AnimationUtil.Loopable;
        
        /// <summary>
        /// Event fired when the dismiss/close button on the notification is clicked.
        /// </summary>
        public event Action OnDismissNotification = null;

        [Tooltip("The root object which can be disabled once the notification is out of view.")]
        public GameObject VisualRoot = null;

        [Header("Close Button")]
        [Tooltip("The UIBlock to subscribe to for close button click events.")]
        public UIBlock CloseNotificationButton = null;

        [Header("Animations")]
        [Tooltip("Duration of the appear/disappear animation.")]
        public float InOutDuration = 0.5f;
        [Tooltip("The appear/disappear animation.")]
        public PositionAnimationSingleAxis PositionAnimation = new PositionAnimationSingleAxis()
        {
            AnimationCurve = AnimationUtil.SpringEase,
            AxisToChange = Axis.Y,
        };
        [Tooltip("Duration of a single iteration of the floating bounce effect (which runs continuously while the notification is in view).")]
        public float BounceLoopDuration = 2;

        private AnimationHandle animationHandle = default;

        private void OnEnable()
        {
            VisualRoot.SetActive(false);
            CloseNotificationButton.AddGestureHandler<Gesture.OnClick>(HandleDismissButtonClicked);
        }

        private void OnDisable()
        {
            CloseNotificationButton.RemoveGestureHandler<Gesture.OnClick>(HandleDismissButtonClicked);
            animationHandle.Complete();
        }

        /// <summary>
        /// Handle click by firing non-UI events for components outside the UIBlock hierarchy to subscribe to. 
        /// </summary>
        private void HandleDismissButtonClicked(Gesture.OnClick evt) => OnDismissNotification?.Invoke();

        /// <summary>
        /// Animate in the FaceTime notification
        /// </summary>
        public void ShowNotification()
        {
            animationHandle.Cancel();

            VisualRoot.SetActive(true);
            animationHandle = PositionAnimation.Run(InOutDuration);

            PositionAnimationSingleAxis bounce = PositionAnimation;
            bounce.TargetPosition = 0;
            bounce.StartPosition = PositionAnimation.TargetPosition;
            bounce.AnimationCurve = BounceCurve;

            animationHandle = animationHandle.Chain(bounce, BounceLoopDuration, AnimationHandle.Infinite);
        }

        /// <summary>
        /// Animate out the FaceTime notification
        /// </summary>
        public AnimationHandle DismissNotification()
        {
            PositionAnimationSingleAxis outAnimation = PositionAnimation;

            int axis = PositionAnimation.AxisToChange.Index();

            outAnimation.StartPosition = PositionAnimation.Target.Position[axis].Raw;
            outAnimation.TargetPosition = PositionAnimation.StartPosition;

            animationHandle.Cancel();
            animationHandle = outAnimation.Run(InOutDuration);
            animationHandle = animationHandle.Chain(new ActivateGameObjectAnimation()
            {
                Target = VisualRoot,
                TargetActive = false,
            }, 0);

            return animationHandle;
        }
    }
}
