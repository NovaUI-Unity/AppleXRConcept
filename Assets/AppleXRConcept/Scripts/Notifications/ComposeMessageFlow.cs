using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Does the Siri-composed message dialog animations
    /// </summary>
    public class ComposeMessageFlow : MonoBehaviour
    {
        [Tooltip("The dictation animator.")]
        public DictateMessageAnimator MessageDictator = null;
        [Tooltip("The parent of the different compose-flow beats.")]
        public UIBlock ComposeUIRoot = null;

        [Header("Appear/Disappear")]
        [Tooltip("Hide/show animation duration.")]
        public float PositionAnimationDuration = 0.5f;
        [Tooltip("Hide/show animation.")]
        public PositionAnimationSingleAxis PositionAnimation = new PositionAnimationSingleAxis()
        {
            AxisToChange = Axis.Y,
            AnimationCurve = AnimationUtil.SpringEase,
        };

        [Header("Scroll")]
        [Tooltip("Scroll to next beat animation duration.")]
        public float ScrollDuration = 0.25f;
        [Tooltip("Scroll animation.")]
        public AutoLayoutOffsetAnimation ScrollAnimation = new AutoLayoutOffsetAnimation()
        {
            Curve = AnimationUtil.SpringEase,
        };

        /// <summary>
        /// Animate in the dictation modal after <paramref name="dependency"/> runs.
        /// </summary>
        public AnimationHandle StartComposeFlow(AnimationHandle dependency)
        {
            // Ensure we're starting from an expected state
            AutoLayoutOffsetAnimation unscroll = ScrollAnimation;
            unscroll.EndOffset = ScrollAnimation.StartOffset;
            unscroll.Run(0).Complete();

            dependency = dependency.Chain(new ActivateGameObjectAnimation()
            {
                Target = ComposeUIRoot.gameObject,
                TargetActive = true,
            }, 0);

            return dependency.Include(PositionAnimation, PositionAnimationDuration);
        }

        /// <summary>
        /// Start the dictation animation after <paramref name="dependency"/> runs.
        /// </summary>
        public AnimationHandle StartDictation(AnimationHandle dependency)
        {
            return MessageDictator.StartDictation(dependency);
        }

        /// <summary>
        /// Scroll to the conversation model after <paramref name="dependency"/> runs.
        /// </summary>
        public AnimationHandle ShowCoversation(AnimationHandle dependency)
        {
            return dependency.Chain(ScrollAnimation, ScrollDuration);
        }

        /// <summary>
        /// Hide the compose-flow visuals after <paramref name="dependency"/> runs.
        /// </summary>
        public AnimationHandle EndComposeFlow(AnimationHandle dependency)
        {
            PositionAnimationSingleAxis reverse = PositionAnimation;
            reverse.StartPosition = PositionAnimation.TargetPosition;
            reverse.TargetPosition = PositionAnimation.StartPosition;

            dependency = dependency.Chain(reverse, PositionAnimationDuration);

            return dependency.Chain(new ActivateGameObjectAnimation()
            {
                Target = ComposeUIRoot.gameObject,
                TargetActive = false,
            }, 0);
        }
    }
}
