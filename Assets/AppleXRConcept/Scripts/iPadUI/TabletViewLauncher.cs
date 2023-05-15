using Nova;
using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Responsible for the expand/collapse iPad gesture and animation.
    /// </summary>
    public class TabletViewLauncher : HomebarGestureHandler
    {
        /// <summary>
        /// Event triggered when an <see cref="FullScreenButtonVisuals"/> is clicked.
        /// </summary>
        public event Action OnFullScreenButtonClicked = null;

        /// <summary>
        /// Event triggered when an <see cref="ImmersiveAppButtonVisuals"/> is clicked.
        /// </summary>
        public event Action OnImmersiveAppSelected = null;

        [Header("Visuals")]
        [Tooltip("The parent of the tablet content and status bar.")]
        public UIBlock ContentRoot = null;
        [Tooltip("The tablet content root.")]
        public UIBlock HomescreenRoot = null;
        [Tooltip("The Clip Mask to fade in/out as the tablet content comes in/out of view.")]
        public ClipMask HomescreenMask = null;
        [Tooltip("The bounding box effect to fade in/out as the tablet contents comes in/out of view")]
        public BoundingBoxEffect BoundingBox = null;
        [Tooltip("The app icon grid.")]
        public UIBlock GridRoot = null;

        [Header("Positioning")]
        [Tooltip("The follow configuration to use when only the status bar is visible.")]
        public Follow CollapsedFollow = null;
        [Tooltip("The follow configuration to use when whole iPad view is visible.")]
        public Follow ExpandedFollow = null;

        [Header("Animations")]
        [Tooltip("The duration of the expand/collapse animation")]
        public float AnimationDuration = 0.5f;
        [Tooltip("The animation curve to use when animating the size of the expanding/collapsing content.")]
        public AnimationCurve SizeCurve = AnimationUtil.SpringEase;
        [Tooltip("The animation curve to use when fading in/out the bounding box effect.")]
        public AnimationCurve BoxFadeCurve = AnimationUtil.Loopable;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Subscribe to additional gestures
            UIBlock.AddGestureHandler<Gesture.OnClick, FullScreenButtonVisuals>(HandleFullScreenButtonClicked);
            UIBlock.AddGestureHandler<Gesture.OnClick, ImmersiveAppButtonVisuals>(HandleImmersiveAppLaunched);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Unsubscribe from additional gestures
            UIBlock.RemoveGestureHandler<Gesture.OnClick, FullScreenButtonVisuals>(HandleFullScreenButtonClicked);
            UIBlock.RemoveGestureHandler<Gesture.OnClick, ImmersiveAppButtonVisuals>(HandleImmersiveAppLaunched);
        }

        /// <summary>
        /// Increase the Y size of the <see cref="HomescreenRoot"/> by the
        /// amount dragged and fade in the box effect by the total percent dragged.
        /// </summary>
        protected override void HandleDrag(float dragDelta)
        {
            float total = HomescreenRoot.CalculatedSize.Y.Value + dragDelta;
            float percent = Mathf.Clamp01(total / ThresholdVolume.CalculatedSize.Y.Value);

            HomescreenRoot.Size.Y = total;

            HomescreenMask.enabled = true;

            SizeAnimationSingleAxis contentAnimation = GetContentRootAnimation(ContentRoot.SizeMinMax.X.Max);
            contentAnimation.StartSize = ContentRoot.SizeMinMax.X.Min;
            contentAnimation.Update(percent);

            BoundingBox.GetAnimation(fadeIn: false).Update(1);
            BoundingBox.GetAnimation(fadeIn: true, BoxFadeCurve).Update(percent);
            GetMaskAnimation(animateIn: true).Update(percent);
        }

        /// <summary>
        /// Handle click by firing non-UI events for components outside the UIBlock hierarchy to subscribe to. 
        /// </summary>
        private void HandleFullScreenButtonClicked(Gesture.OnClick evt, FullScreenButtonVisuals visuals) => OnFullScreenButtonClicked?.Invoke();
        
        /// <summary>
        /// Handle click by firing non-UI events for components outside the UIBlock hierarchy to subscribe to. 
        /// </summary>
        private void HandleImmersiveAppLaunched(Gesture.OnClick evt, ImmersiveAppButtonVisuals visuals) => OnImmersiveAppSelected?.Invoke();

        /// <summary>
        /// Run the collapse animation and return its animation handle.
        /// </summary>
        /// <returns></returns>
        public AnimationHandle Collapse()
        {
            AnimationHandle unused = default;
            AnimationHandle handle = default;

            RunAllAnimations(dragDirection: 1, ref handle, ref unused);

            return handle;
        }

        /// <summary>
        /// Chain the expand animation to the animation combination tracked by <paramref name="dependency"/> and return the new animation handle.
        /// </summary>
        public AnimationHandle Expand(AnimationHandle dependency)
        {
            AnimationHandle unused = default;

            RunAllAnimations(dragDirection: -1, ref dependency, ref unused);

            return dependency;
        }

        /// <summary>
        /// Perform the animation based on the triggered <paramref name="dragDirection"/>.
        /// </summary>
        /// <remarks>
        /// When <paramref name="dragDirection"/> &lt; 0, an expand animation will be executed. When <paramref name="dragDirection"/> &gt; 0, a collapse animation will be executed.
        /// </remarks>
        protected override void RunAllAnimations(int dragDirection, ref AnimationHandle dragAnimation, ref AnimationHandle fadeAnimation)
        {
            bool animateIn = dragDirection < 0;

            HomescreenMask.enabled = true;

            FollowBehavior.FollowConfiguration = animateIn ? ExpandedFollow : CollapsedFollow;

            float rootEndSize = animateIn ? HomescreenRoot.SizeMinMax.Y.Max : HomescreenRoot.SizeMinMax.Y.Min;

            SizeAnimationSingleAxis rootAnimation = GetHomescreenAnimation(rootEndSize);
            rootAnimation.StartSize = HomescreenRoot.CalculatedSize.Y.Value;

            float contentEndSize = animateIn ? ContentRoot.SizeMinMax.X.Max : ContentRoot.SizeMinMax.X.Min;
            SizeAnimationSingleAxis statusBarAnimation = GetContentRootAnimation(contentEndSize);
            statusBarAnimation.StartSize = ContentRoot.CalculatedSize.X.Value;

            BoundingBoxEffect.Animation boxFadeAnimation = BoundingBox.GetAnimation(fadeIn: false);

            ClipMaskTintAnimation maskAnimation = GetMaskAnimation(animateIn);

            dragAnimation = dragAnimation.Chain(rootAnimation, AnimationDuration);
            dragAnimation = dragAnimation.Include(statusBarAnimation);
            dragAnimation = dragAnimation.Include(boxFadeAnimation);
            dragAnimation = dragAnimation.Include(maskAnimation);

            dragAnimation = dragAnimation.Chain(new ActivateGameObjectAnimation() { Target = GridRoot.gameObject, TargetActive = animateIn }, 0f);

            if (animateIn)
            {
                dragAnimation = dragAnimation.Include(new EnableComponentAnimation<ClipMask>() { Target = HomescreenMask, TargetEnabled = false });
            }
            else
            {
                FollowBehavior.Recenter();
            }
        }

        private SizeAnimationSingleAxis GetHomescreenAnimation(float endSize)
        {
            return new SizeAnimationSingleAxis()
            {
                Target = HomescreenRoot,
                TargetSize = endSize,
                AxisToChange = Axis.Y,
                AnimationCurve = SizeCurve,
            };
        }

        private SizeAnimationSingleAxis GetContentRootAnimation(float endSize)
        {
            return new SizeAnimationSingleAxis()
            {
                Target = ContentRoot,
                TargetSize = endSize,
                AxisToChange = Axis.X,
                AnimationCurve = SizeCurve,
            };
        }

        private ClipMaskTintAnimation GetMaskAnimation(bool animateIn)
        {
            return new ClipMaskTintAnimation()
            {
                Target = HomescreenMask,
                TargetColor = animateIn ? Color.white : Color.white.Transparent(),
            };
        }

        protected override void RunFadeAnimation(bool fadeIn, ref AnimationHandle fadeAnimation) { }
    }
}
