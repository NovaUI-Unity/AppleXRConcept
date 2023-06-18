using Nova;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// A static utility for common animation curves.
    /// </summary>
    public static class AnimationUtil
    {
        public static AnimationCurve EaseInOut => AnimationCurve.EaseInOut(0, 0, 1, 1);
        public static AnimationCurve SpringOvershoot => (AnimationCurve)SpringCurve.Overshoot;
        public static AnimationCurve SpringEase => (AnimationCurve)SpringCurve.Ease;
        public static AnimationCurve Loopable => new AnimationCurve()
        {
            keys = new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0) }
        };
    }

    /// <summary>
    /// Animates a <see cref="UIBlock"/>'s color from its current value to <see cref="TargetColor"/>.
    /// </summary>
    [Serializable]
    public struct BodyColorAnimation : IAnimation
    {
        [Tooltip("The end body color of the animation.")]
        public Color TargetColor;
        [Tooltip("The UIBlock whose body color will be animated.")]
        public UIBlock Target;

        private Color startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0f)
            {
                startColor = Target.Color;
            }

            Target.Color = Color.Lerp(startColor, TargetColor, percentDone);
        }
    }

    /// <summary>
    /// Animates a <see cref="UIBlock2D"/>'s border color from its current value to <see cref="TargetColor"/>.
    /// </summary>
    [Serializable]
    public struct BorderColorAnimation : IAnimation
    {
        [Tooltip("The end border color of the animation.")]
        public Color TargetColor;
        [Tooltip("The UIBlock whose border color will be animated.")]
        public UIBlock2D Target;

        private Color startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0f)
            {
                startColor = Target.Border.Color;
            }

            Target.Border.Color = Color.Lerp(startColor, TargetColor, percentDone);
        }
    }

    /// <summary>
    /// Animates the <see cref="ClipMask.Tint"/> of a <see cref="ClipMask"/>.
    /// </summary>
    [Serializable]
    public struct ClipMaskTintAnimation : IAnimation
    {
        [Tooltip("The tint end color of the animation.")]
        public Color TargetColor;
        [Tooltip("The ClipMask whose tint color will be animated.")]
        public ClipMask Target;
        [Tooltip("The curve to use while transitioning the tint color.")]
        public AnimationCurve AnimationCurve;

        private Color startColor;

        public void Update(float percentDone)
        {
            if (percentDone == 0f)
            {
                startColor = Target.Tint;
            }

            float lerp = AnimationCurve != null && AnimationCurve.length > 0 ? AnimationCurve.Evaluate(percentDone) : percentDone; 

            Target.Tint = Color.Lerp(startColor, TargetColor, lerp);
        }
    }

    /// <summary>
    /// An animation whch disables the <see cref="Target"/> GameObject.
    /// </summary>
    [Serializable]
    public struct ActivateGameObjectAnimation : IAnimation
    {
        [Tooltip("The GameObject to enable/disable.")]
        public GameObject Target;
        [Tooltip("Should the Target be enabled (true) or disabled (false)?")]
        public bool TargetActive;

        public void Update(float percentDone)
        {
            // Don't care about lerping anything here, just set the active state
            Target.SetActive(TargetActive);
        }
    }

    /// <summary>
    /// An animation which sets the enabled state of the <see cref="Target"/> component to <see cref="TargetEnabled"/>.
    /// </summary>
    [Serializable]
    public struct EnableComponentAnimation<T> : IAnimation where T : MonoBehaviour
    {
        [Tooltip("The component to enable/disable.")]
        public T Target;
        [Tooltip("Should the Target be enabled (true) or disabled (false)?")]
        public bool TargetEnabled;

        public void Update(float percentDone)
        {
            // Don't care about lerping anything here, just set the active state
            Target.enabled = TargetEnabled;
        }
    }

    /// <summary>
    /// An animation which adjusts the size of the <see cref="Target"/> <see cref="UIBlock"/> along a single axis.
    /// </summary>
    [Serializable]
    public struct SizeAnimationSingleAxis : IAnimation
    {
        [Tooltip("The UI Block whose size should change.")]
        public UIBlock Target;
        [Tooltip("The size the Target should change to while animating.")]
        public float TargetSize;
        [Tooltip("The size the Target will start from when the animation begins.")]
        public float StartSize;
        [Tooltip("The size axis to change while animating.")]
        public Axis AxisToChange;
        [Tooltip("The animation curve to evaluate while resizing the Target from the start size to the target size.")]
        public AnimationCurve AnimationCurve;

        public void Update(float percentDone)
        {
            if (!AxisToChange.TryGetIndex(out int axis))
            {
                return;
            }

            float lerp = AnimationCurve != null && AnimationCurve.length > 0 ? AnimationCurve.Evaluate(percentDone) : percentDone;

            Length length = Target.Size[axis];
            length.Raw = Mathf.LerpUnclamped(StartSize, TargetSize, lerp);
            Target.Size[axis] = length;
        }
    }

    /// <summary>
    /// An animation which adjusts the layout position of the <see cref="Target"/> <see cref="UIBlock"/> along a single axis.
    /// </summary>
    [Serializable]
    public struct PositionAnimationSingleAxis : IAnimation
    {
        [Tooltip("The UI Block whose position should change.")]
        public UIBlock Target;
        [Tooltip("The position the Target should move towards while animating.")]
        public float TargetPosition;
        [Tooltip("The position the Target will start from when the animation begins.")]
        public float StartPosition;
        [Tooltip("The position axis to change while animating.")]
        public Axis AxisToChange;
        [Tooltip("The animation curve to evaluate while moving the Target from the start position to the target position.")]
        public AnimationCurve AnimationCurve;

        public void Update(float percentDone)
        {
            if (!AxisToChange.TryGetIndex(out int axis))
            {
                return;
            }

            float lerp = AnimationCurve != null && AnimationCurve.length > 0 ? AnimationCurve.Evaluate(percentDone) : percentDone;

            Length length = Target.Position[axis];
            length.Raw = Mathf.LerpUnclamped(StartPosition, TargetPosition, lerp);
            Target.Position[axis] = length;
        }
    }

    /// <summary>
    /// An animation which adjusts the Auto Layout Offset the <see cref="Target"/> <see cref="UIBlock"/>.
    /// </summary>
    [Serializable]
    public struct AutoLayoutOffsetAnimation : IAnimation
    {
        [Tooltip("The UI Block whose Auto Layout Offset should change.")]
        public UIBlock Target;
        [Tooltip("The offset the Target should start with when the animation begins.")]
        public float StartOffset;
        [Tooltip("The offset the Target should end with when the animation ends.")]
        public float EndOffset;
        [Tooltip("The animation curve to evaluate while lerping from the start offset to the end offset.")]
        public AnimationCurve Curve;

        public void Update(float percentDone)
        { 
            float lerp = Curve != null && Curve.length > 0 ? Curve.Evaluate(percentDone) : percentDone;

            Target.AutoLayout.Offset = Mathf.LerpUnclamped(StartOffset, EndOffset, lerp);
        }
    }

    /// <summary>
    /// An empty animation, which can be used to insert
    /// time delays betweem chained animations.
    /// </summary>
    public struct Delay : IAnimation
    {
        /// <summary>
        /// Creates a new <see cref="Delay"/> to start
        /// an animation sequence.
        /// </summary>
        public static AnimationHandle For(float duration) => new Delay().Run(duration);

        /// <summary>
        /// Chains a <see cref="Delay"/> for <paramref name="delayDuration"/> to the animation combination
        /// tracked by <paramref name="before"/>.
        /// </summary>
        /// <param name="before">The handle tracking the animation combination that will run before the delay.</param>
        /// <param name="delayDuration">The duration of the delay in seconds.</param>
        public static AnimationHandle For(AnimationHandle before, float delayDuration) => before.Chain(new Delay(), delayDuration);

        public void Update(float percentDone) { }
    }

    /// <summary>
    /// An animation which will fire a UnityEvent per animation update. 
    /// </summary>
    [Serializable]
    public struct UnityEventAnimation : IAnimation
    {
        [Tooltip("The event fired every frame the animation updates and provides the output of the \"Modifying Function\" evaluated at the point of the completed animation percentage per update.")]
        public UnityEvent<float> OnAnimationUpdate;
        [Tooltip("Allows the animation event to provide an evaluated curve position, as a function of the percent of the animation completed. If null or empty, the event will just provide the raw percent completed value [0, 1].")]
        public AnimationCurve ModifyingFunction;

        public void Update(float percentDone)
        {
            if (OnAnimationUpdate == null)
            {
                return;
            }

            float value = ModifyingFunction == null || ModifyingFunction.length == 0 ? percentDone : ModifyingFunction.Evaluate(percentDone);

            OnAnimationUpdate.Invoke(value);
        }
    }

    /// <summary>
    /// An animation which will invoke the <see cref="MethodToRunOnComplete"/> callback when it completes.
    /// </summary>
    public struct RunMethodOnCompleteAnimation : IAnimationWithEvents
    {
        public Action MethodToRunOnComplete;

        public void Begin(int currentIteration) { }

        public void Complete()
        {
            MethodToRunOnComplete?.Invoke();
        }

        public void End() { }

        public void OnCanceled() { }

        public void OnPaused() { }

        public void OnResumed() { }

        public void Update(float percentDone) { }
    }
}
