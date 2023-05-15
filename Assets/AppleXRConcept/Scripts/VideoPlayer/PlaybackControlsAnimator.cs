using Nova;
using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Animates the playback control visuals when in full screen video mode.
    /// </summary>
    public class PlaybackControlsAnimator : NovaBehaviour
    {
        /// <summary>
        /// Event fired when the enter picture-in-picture mode button is clicked.
        /// </summary>
        public event Action OnPIPButtonClicked = null;

        [Tooltip("The duration of the hide/show and focus/unfocus animations.")]
        public float AnimationDuration = 0.5f;

        [Tooltip("The hide/show animation.")]
        public SizeAnimationSingleAxis ExpandAnimation = new SizeAnimationSingleAxis()
        {
            AxisToChange = Axis.Y,
            AnimationCurve = AnimationUtil.SpringEase,
        };

        [Tooltip("The focus/unfocus animation.")]
        public PositionAnimationSingleAxis OutOfFocusAnimation = new PositionAnimationSingleAxis()
        {
            AxisToChange = Axis.Y,
            StartPosition = 0,
        };

        private void OnEnable()
        {
            // Subscribe to picture-in-picture mode button click events
            UIBlock.AddGestureHandler<Gesture.OnClick, PIPButtonVisuals>(HandlePIPButtonClicked);
        }

        private void OnDisable()
        {
            // Unsubscribe from picture-in-picture mode button click events
            UIBlock.RemoveGestureHandler<Gesture.OnClick, PIPButtonVisuals>(HandlePIPButtonClicked);
        }

        /// <summary>
        /// Handle click by firing non-UI events for components outside the UIBlock hierarchy to subscribe to. 
        /// </summary>
        private void HandlePIPButtonClicked(Gesture.OnClick evt, PIPButtonVisuals visual)
        {
            OnPIPButtonClicked?.Invoke();
        }

        /// <summary>
        /// Expand the playback controls. If <c><paramref name="include"/> == true</c>, then expand  
        /// the controls at the same time as <paramref name="dependency"/>. Otherwise do the expand once 
        /// <paramref name="dependency"/> completes (chained instead of included).
        /// </summary>
        public AnimationHandle Expand(bool include, AnimationHandle dependency = default)
        {
            return include ? dependency.Include(ExpandAnimation, AnimationDuration) : dependency.Chain(ExpandAnimation, AnimationDuration);
        }

        /// <summary>
        /// Collapse the playback controls. If <c><paramref name="include"/> == true</c>, then collapse  
        /// the controls at the same time as <paramref name="dependency"/>. Otherwise do the collapse once 
        /// <paramref name="dependency"/> completes (chained instead of included).
        /// </summary>
        public AnimationHandle Collapse(bool include, AnimationHandle dependency = default)
        {
            SizeAnimationSingleAxis collapseAnimation = ExpandAnimation;
            collapseAnimation.StartSize = ExpandAnimation.TargetSize;
            collapseAnimation.TargetSize = ExpandAnimation.StartSize;

            return include ? dependency.Include(collapseAnimation, AnimationDuration) : dependency.Chain(collapseAnimation, AnimationDuration);
        }

        /// <summary>
        /// Unfocus the playback controls. If <c><paramref name="include"/> == true</c>, then unfocus  
        /// the controls at the same time as <paramref name="dependency"/>. Otherwise do the unfocus once 
        /// <paramref name="dependency"/> completes (chained instead of included).
        /// </summary>
        public AnimationHandle Unfocus(bool include, AnimationHandle dependency = default)
        {
            return include ? dependency.Include(OutOfFocusAnimation, AnimationDuration) : dependency.Chain(OutOfFocusAnimation, AnimationDuration);
        }

        /// <summary>
        /// Refocus the playback controls. If <c><paramref name="include"/> == true</c>, then refocus  
        /// the controls at the same time as <paramref name="dependency"/>. Otherwise do the refocus once 
        /// <paramref name="dependency"/> completes (chained instead of included).
        /// </summary>
        public AnimationHandle Refocus(bool include, AnimationHandle dependency = default)
        {
            PositionAnimationSingleAxis refocus = OutOfFocusAnimation;
            refocus.StartPosition = OutOfFocusAnimation.TargetPosition;
            refocus.TargetPosition = OutOfFocusAnimation.StartPosition;

            return include ? dependency.Include(refocus, AnimationDuration) : dependency.Chain(refocus, AnimationDuration);
        }
    }
}
