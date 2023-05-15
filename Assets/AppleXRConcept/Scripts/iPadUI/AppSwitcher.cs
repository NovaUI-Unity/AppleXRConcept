using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Handles the 2D app switching when performing a horizontal drag along the homebar.
    /// </summary>
    public class AppSwitcher : HomebarGestureHandler
    {
        [Header("App Switcher")]
        [Tooltip("The parent of the list of open apps to switch between.")]
        public UIBlock AppsRoot = null;

        [Header("Position Animation")]
        [Min(0)]
        [Tooltip("The duration of the scrolling animation. In seconds.")]
        public float PositionAnimationDuration = 0.5f;
        [Tooltip("The animation curve used for the scrolling animation.")]
        public AnimationCurve PositionAnimationCurve = (AnimationCurve)new SpringCurve() { Oscillations = 1.84f, OvershootPercent = 0.001f };

        [Header("Fade Animation")]
        [Min(0)]
        [Tooltip("The duration of the fade in/out animation of the adjacent apps. In seconds.")]
        public float FadeAnimationDuration = 0.25f;
        [Min(0)]
        [Tooltip("Once a scroll has ended, wait this amount of time before fading out the adjacent apps. In seconds.")]
        public float FadeOutDelay = 0.5f;
        [Tooltip("The animation curve to use for fading in/out the boxes around the adjacent apps.")]
        public AnimationCurve BoxFadeCurve = AnimationUtil.Loopable;
        [Tooltip("The animation curve to use for fading in/out the clip masks attached to the adjacent apps.")]
        public AnimationCurve MaskFadeCurve = AnimationUtil.EaseInOut;

        // The sibling index of the UIBlock centered in view, defaults to 1 for demo purposes.
        private int centralUIBlockIndex = 1;

        protected override void HandleDrag(float dragDelta)
        {
            // Update the content offset based on the drag amount 
            AppsRoot.AutoLayout.Offset += dragDelta;
        }

        protected override void RunFadeAnimation(bool fadeIn, ref AnimationHandle fadeAnimation)
        {
            // Out adjacent app windows and any bounding box effects
            FadeAppWindows(AppsRoot.GetChild(centralUIBlockIndex), fadeIn ? Color.white : Color.clear, ref fadeAnimation);
        }

        protected override void RunAllAnimations(int dragDirection, ref AnimationHandle dragAnimation, ref AnimationHandle fadeAnimation)
        {
            // Update the centralUIBlockIndex based on the drag direction
            centralUIBlockIndex = Mathf.Clamp(centralUIBlockIndex + dragDirection, 0, AppsRoot.ChildCount - 1);

            // Kick off the fade in/out animations along with the scroll animation
            StartAnimations(AppsRoot.GetChild(centralUIBlockIndex), ref dragAnimation, ref fadeAnimation);
        }

        /// <summary>
        /// Given a <paramref name="child"/> of the <see cref="AppsRoot"/>, returns the target auto layout 
        /// offset we need to scroll to in order for the given child to be centered in the parent.
        /// </summary>
        private float GetOffsetOfChild(UIBlock child)
        {
            if (child == null)
            {
                return 0;
            }

            // Get the total child bounds in parent space
            Bounds totalChildBounds = AppsRoot.ChildBounds;

            // Create bounds of the given child in parent space 
            Bounds targetChildBounds = new Bounds(child.transform.localPosition, child.CalculatedSize.Value);

            // The distance between the edge of the child bounds 
            // relative to the total bounds of its siblings
            return totalChildBounds.min.x - targetChildBounds.min.x;
        }

        /// <summary>
        /// Kickoff the fade and scroll animations to a target app window, designated by <paramref name="centralUIBlock"/>.
        /// </summary>
        private void StartAnimations(UIBlock centralUIBlock, ref AnimationHandle homebarAnimation, ref AnimationHandle fadeAnimation)
        {
            // Gat the target offset
            float endOffset = GetOffsetOfChild(centralUIBlock);

            // Animate the auto layout offset of the Apps Root to the center the child in view
            homebarAnimation = homebarAnimation.Chain(new AutoLayoutOffsetAnimation()
            {
                Target = AppsRoot,
                EndOffset = endOffset,
                StartOffset = AppsRoot.AutoLayout.Offset,
                Curve = PositionAnimationCurve,
            }, PositionAnimationDuration);

            if (AppsRoot.ChildCount == 0)
            {
                // AppsRoot doesn't have any children
                return;
            }

            // Each app window has a homebar visual, so fade out the homebars of
            // the adjacent windows and fade in the homebar of the centered window

            UIBlock child = AppsRoot.GetChild(0);
            UIBlock homebar = child.GetComponent<AppWindow>().Homebar;

            homebarAnimation = homebarAnimation.Chain(new BodyColorAnimation()
            {
                Target = homebar,
                TargetColor = child == centralUIBlock ? homebar.Color.WithAlpha(1) : homebar.Color.WithAlpha(0),
            }, PositionAnimationDuration);

            for (int i = 1; i < AppsRoot.ChildCount; ++i)
            {
                child = AppsRoot.GetChild(i);
                homebar = child.GetComponent<AppWindow>().Homebar;
                homebarAnimation = homebarAnimation.Include(new BodyColorAnimation()
                {
                    Target = child.GetComponent<AppWindow>().Homebar,
                    TargetColor = child == centralUIBlock ? homebar.Color.WithAlpha(1) : homebar.Color.WithAlpha(0),
                });
            }

            // Fade in/out the window contents and effects
            FadeAppWindows(centralUIBlock, adjacentWindowColor: Color.clear, ref fadeAnimation);
        }

        /// <summary>
        /// Fade out all <see cref="BoundingBoxEffect"/>s, fade out adjacent app windows, and ensure the central app window is faded in.
        /// </summary>
        private void FadeAppWindows(UIBlock centralUIBlock, Color adjacentWindowColor, ref AnimationHandle fadeAnimation)
        {
            fadeAnimation.Complete();

            // Add a delay between the gesture being released and the fade out animation activating
            fadeAnimation = new Delay().Run(FadeOutDelay);

            UIBlock child = AppsRoot.GetChild(0);
            AppWindow childWindow = child.GetComponent<AppWindow>();
            fadeAnimation = fadeAnimation.Chain(new ClipMaskTintAnimation()
            {
                Target = childWindow.ClipMask,
                TargetColor = child == centralUIBlock ? Color.white : adjacentWindowColor,
                AnimationCurve = MaskFadeCurve,
            }, FadeAnimationDuration / 2);

            fadeAnimation = fadeAnimation.Include(childWindow.BoundingBox.GetAnimation(fadeIn: child != centralUIBlock, BoxFadeCurve), FadeAnimationDuration);

            for (int i = 1; i < AppsRoot.ChildCount; ++i)
            {
                child = AppsRoot.GetChild(i);
                childWindow = child.GetComponent<AppWindow>();
                fadeAnimation = fadeAnimation.Include(new ClipMaskTintAnimation()
                {
                    Target = childWindow.ClipMask,
                    TargetColor = child == centralUIBlock ? Color.white : adjacentWindowColor,
                    AnimationCurve = MaskFadeCurve,
                }, FadeAnimationDuration / 2);

                fadeAnimation = fadeAnimation.Include(childWindow.BoundingBox.GetAnimation(fadeIn: child != centralUIBlock, BoxFadeCurve), FadeAnimationDuration);
            }
        }
    }
}