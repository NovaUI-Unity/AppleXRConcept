using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Manages the homescreen grid of app buttons and will open a preset list of switchable apps when a <see cref="TabletAppButtonVisuals"/> is clicked.
    /// </summary>
    public class AppLauncher : NovaBehaviour
    {
        [Tooltip("The visual root of the homescreen content.\nE.g. the grid of apps' parent.")]
        public GameObject HomescreenRoot = null;
        [Tooltip("The 2D app view content to launch when a tablet app button is selected.")]
        public AppSwitcher AppSwitcher = null;
        [Tooltip("The clip mask used to clip the scrollable grid of apps.")]
        public ClipMask AppGridMask = null;
        [Tooltip("The clip mask to expand when launching a selected app.")]
        public ClipMask AppSwitcherMask = null;
        [Tooltip("The preset list of content to display when an app is launched.")]
        public UIBlock AppSwitcherContent = null;

        [Tooltip("The duration of the launching app animation.")]
        public float AnimationDuration = 0.5f;
        [Tooltip("The animation curve to use as a lerp function for the launching app animation.")]
        public AnimationCurve AnimationCurve = (AnimationCurve) new SpringCurve() { Oscillations = 1, OvershootPercent = 0.001f };

        /// <summary>
        /// tracks the animation state of the "launching app" animation
        /// </summary>
        private AnimationHandle launcherAnimation = default;

        private void OnEnable()
        {
            // Subscribe to 2D app button clicks
            UIBlock.AddGestureHandler<Gesture.OnClick, TabletAppButtonVisuals>(Handle2DAppSelected);
        }

        private void OnDisable()
        {
            // Unsubscribe from to 2D app button clicks
            UIBlock.RemoveGestureHandler<Gesture.OnClick, TabletAppButtonVisuals>(Handle2DAppSelected);

            // Cancel any active animation
            launcherAnimation.Cancel();

            // Reset state
            AppSwitcher.gameObject.SetActive(false);
            AppGridMask.Tint = Color.white;
            HomescreenRoot.SetActive(true);
        }

        private void Handle2DAppSelected(Gesture.OnClick evt, TabletAppButtonVisuals appButton)
        {
            // Cancel any active animation
            launcherAnimation.Cancel();

            // Enable the app launching visuals
            AppSwitcher.gameObject.SetActive(true);
            AppSwitcherMask.enabled = true;

            // Resizing along the aspect-locked axis allows us to animate one axis while resizing multiple axes
            Axis axis = AppSwitcher.UIBlock.AspectRatioAxis != Axis.None ? AppSwitcher.UIBlock.AspectRatioAxis : Axis.X;
            launcherAnimation = new SizeAnimationSingleAxis()
            {
                Target = AppSwitcher.UIBlock,
                AxisToChange = axis,
                StartSize = evt.Receiver.CalculatedSize[axis.Index()].Value,
                TargetSize = AppSwitcher.UIBlock.SizeMinMax[axis.Index()].Max,
                AnimationCurve = AnimationCurve,

            }.Run(AnimationDuration);

            // Initialize the App Switcher size/position/rotation to match that of the selected button
            AppSwitcher.UIBlock.transform.rotation = transform.rotation;
            AppSwitcher.UIBlock.Size[axis.Index()] = evt.Receiver.CalculatedSize[axis.Index()].Value;
            Vector3 localPosition = AppSwitcher.transform.parent.InverseTransformPoint(evt.Receiver.transform.position);
            localPosition.z = AppSwitcher.transform.localPosition.z;
            AppSwitcher.UIBlock.TrySetLocalPosition(localPosition);

            // Reposition while resizing
            launcherAnimation = launcherAnimation.Include(new LaunchingAppPositionAnimation()
            {
                Target = AppSwitcher.UIBlock,
                FixedContent = AppSwitcherContent,
                StartLayoutPosition = AppSwitcher.UIBlock.CalculatedPosition.Value,
                TargetLayoutPosition = Vector3.zero,
                TargetCenterRootPosition = AppSwitcher.UIBlock.Root.transform.InverseTransformPoint(AppSwitcher.transform.parent.position),
                Curve = AnimationCurve
            }, AnimationDuration);

            // Fade in the app view content
            AppSwitcherMask.Tint = Color.clear;
            launcherAnimation = launcherAnimation.Include(new ClipMaskTintAnimation()
            {
                Target = AppSwitcherMask,
                TargetColor = Color.white,
                AnimationCurve = AnimationCurve,
            });

            // Fade out the app button grid
            AppGridMask.Tint = Color.white;
            launcherAnimation = launcherAnimation.Include(new ClipMaskTintAnimation()
            {
                Target = AppGridMask,
                TargetColor = Color.clear,
                AnimationCurve = AnimationCurve,
            });

            // When the animation completes, we don't want to apply the clip any more,
            // so we can see the adjacent apps when that animation is triggered
            launcherAnimation = launcherAnimation.Chain(new EnableComponentAnimation<ClipMask>()
            {
                Target = AppSwitcherMask,
                TargetEnabled = false,
            }, 0);

            // Disable the grid content
            launcherAnimation = launcherAnimation.Include(new ActivateGameObjectAnimation()
            {
                Target = HomescreenRoot,
                TargetActive = false,
            });
        }

        /// <summary>
        /// The animation which will anchor the expanding app view content, <see cref="FixedContent"/>, to the <see cref="TargetCenterRootPosition"/>
        /// while simulataneously moving the <see cref="Target"/> app view parent. Helps create the illusion that the selected app is expanding out of
        /// the button selected by the user.
        /// </summary>
        private struct LaunchingAppPositionAnimation : IAnimation
        {
            /// <summary>
            /// The object to move as the app view expands.
            /// </summary>
            public UIBlock Target;

            /// <summary>
            /// The object to anchor to the <see cref="TargetCenterRootPosition"/> as the app view expands.
            /// </summary>
            public UIBlock FixedContent;

            /// <summary>
            /// The desired end layout position for the <see cref="Target"/>.
            /// </summary>
            public Vector3 TargetLayoutPosition;

            /// <summary>
            /// The <see cref="Target"/>'s starting layout position.
            /// </summary>
            public Vector3 StartLayoutPosition;

            /// <summary>
            /// The position in root space to anchor <see cref="FixedContent"/>.
            /// </summary>
            public Vector3 TargetCenterRootPosition;

            /// <summary>
            /// The animation curve to use as a lerp function while animating
            /// </summary>
            public AnimationCurve Curve;

            public void Update(float percentDone)
            {
                float lerp = percentDone == 1 ? 1 : Curve.Evaluate(percentDone);

                // Update the target layout position
                Target.Position.XY.Value = (Vector2)Vector3.LerpUnclamped(StartLayoutPosition, TargetLayoutPosition, lerp);

                // Since the target is an ancestor of the fixed content, we need a call to calculate layout 
                // to ensure the layout position has been converted/written to the transform position before
                // we convert from world space to the fixed content's parent space. Otherwise the fixed content
                // position will be off by one frame and will ruin the effect.
                Target.CalculateLayout();

                // Because the root may move while animating (since it's following the user around),
                // we do our anchoring in root space, as opposed to world space.
                Vector3 targetCenterWorldPosition = Target.Root.transform.TransformPoint(TargetCenterRootPosition);
                FixedContent.Position.XY  = (Vector2)FixedContent.transform.parent.InverseTransformPoint(targetCenterWorldPosition);
            }
        }
    }
}
