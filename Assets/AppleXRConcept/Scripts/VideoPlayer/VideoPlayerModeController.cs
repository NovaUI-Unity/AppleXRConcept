using Nova;
using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Manages the animation and visuals associated with
    /// full-screen video mode and picture-in-picture video mode.
    /// </summary>
    public class VideoPlayerModeController : MonoBehaviour
    {
        private const float FadeOutControlsAfterXSeconds = 2f;
        private const float PiPModeAnimationDuration = 1f;

        public event Action OnExitingFullScreenMode = null;

        [Header("Follow Controllers")]
        [Tooltip("The full-screen mode playback controls follow controller.")]
        public FollowController PlaybackControlsFollowController = null;
        [Tooltip("The follow controller for the video screen in both PiP and Full Screen modes")]
        public FollowController VideoPlayerFollowController = null;

        [Header("Video Player Follow Configs")]
        [Tooltip("The follow configuration to use in full-screen mode.")]
        public Follow FullScreenFollow = null;
        [Tooltip("The follow configuration to use in picture-in-picture mode.")]
        public Follow PIPFollow = null;

        [Header("Playback Controls Follow Configs")]
        [Tooltip("The follow configuration to use in full-screen mode.")]
        public Follow PlaybackControlsFollow = null;
        [Tooltip("The follow configuration to start from when animating into full screen mode.")]
        public Follow CollapsedControlsFollow = null;

        [Header("Playback Controls")]
        [Tooltip("The full-screen mode playback controls.")]
        public PlaybackControls PlaybackControls = null;
        [Tooltip("The full-screen mode playback controls animator.")]
        public PlaybackControlsAnimator PlaybackControlsAnimator = null;

        [Header("Animated Visuals")]
        [Tooltip("The UIBlock to resize when animating the video screen between full-screen mode and picture-in-picture mode.")]
        public UIBlock VideoPlayerResizeRoot = null;
        [Tooltip("The Clip Mask to fade in/out when activating the on screen controls in picture-in-picture mode.")]
        public ClipMask OnScreenControlsMask = null;
        [Tooltip("The duration of the on screen controls fade in/out animation.")]
        public float ControlsFadeAnimationDuration = 1;

        [Header("Dim Lights Events")]
        [Tooltip("Fade in/out passthrough animation duration.")]
        public float DimLightsAnimationDuration = 0.5f;
        [Tooltip("Fade out passthrough animation.")]
        public UnityEventAnimation DimLightsAnimation = new UnityEventAnimation()
        {
            ModifyingFunction = AnimationCurve.Linear(0, 1, 1, 0.5f),
        };
        [Tooltip("Fade in passthrough animation.")]
        public UnityEventAnimation UndimLightsAnimation = new UnityEventAnimation()
        {
            ModifyingFunction = AnimationCurve.Linear(0, 0.5f, 1, 1),
        };

        [Header("Unfocus/Refocus")]
        [Tooltip("Unfocus/refocus animation duration.")]
        public float UnfocusAnimationDuration = 0.5f;
        [Tooltip("Unfocus/refocus animation.")]
        public PositionAnimationSingleAxis OutOfFocusAnimation = new PositionAnimationSingleAxis()
        {
            AxisToChange = Axis.Y,
            StartPosition = 0,
        };

        private bool focused = false;
        private AnimationHandle controlsFadeTracker = default;

        private void OnEnable()
        {
            VideoPlayerResizeRoot.AddGestureHandler<Gesture.OnClick>(HandleOnScreenControlsActivated);
            OnScreenControlsMask.UIBlock.AddGestureHandler<Gesture.OnClick, CloseButtonVisuals>(HandleCloseButtonClicked);

            PlaybackControlsAnimator.OnPIPButtonClicked += HandleEnterPiPModeButtonClicked;

            PlaybackControlsFollowController.gameObject.SetActive(false);
            VideoPlayerFollowController.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            controlsFadeTracker.Complete();

            VideoPlayerResizeRoot.RemoveGestureHandler<Gesture.OnClick>(HandleOnScreenControlsActivated);
            OnScreenControlsMask.UIBlock.RemoveGestureHandler<Gesture.OnClick, CloseButtonVisuals>(HandleCloseButtonClicked);

            PlaybackControlsAnimator.OnPIPButtonClicked -= HandleEnterPiPModeButtonClicked;
        }

        /// <summary>
        /// Show on-screen controls and then queue a fade-out of the on screen controls.
        /// </summary>
        private void HandleOnScreenControlsActivated(Gesture.OnClick evt)
        {
            controlsFadeTracker.Cancel();

            controlsFadeTracker = new ActivateGameObjectAnimation()
            {
                Target = OnScreenControlsMask.gameObject,
                TargetActive = true,
            }.Run(0);

            controlsFadeTracker = controlsFadeTracker.Chain(new ClipMaskTintAnimation()
            {
                Target = OnScreenControlsMask,
                TargetColor = Color.white,
            }, ControlsFadeAnimationDuration);

            QueueFadeOutOnScreenControls();
        }

        /// <summary>
        /// Unfocus video player when close button clicked.
        /// </summary>
        private void HandleCloseButtonClicked(Gesture.OnClick evt, CloseButtonVisuals closeButton) => Unfocus();
        
        /// <summary>
        /// Notify PiP mode button clicked.
        /// </summary>
        public void HandleEnterPiPModeButtonClicked()
        {
            OnExitingFullScreenMode?.Invoke();
        }

        /// <summary>
        /// Expand the full-screen mode playback controls and animate 
        /// the video screen int full-screen mode.
        /// </summary>
        public AnimationHandle EnterFullScreenMode(Follow startFromFollowMode, Transform startFromTransform, AnimationHandle dependency = default)
        {
            controlsFadeTracker.Cancel();

            bool alreadyActive = VideoPlayerFollowController.gameObject.activeSelf;

            dependency = dependency.Chain(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = () =>
                {
                    VideoPlayerFollowController.transform.position = startFromTransform.position;
                    VideoPlayerFollowController.transform.rotation = startFromTransform.rotation;

                    VideoPlayerFollowController.FollowConfiguration = FullScreenFollow;
                    VideoPlayerFollowController.gameObject.SetActive(true);
                    VideoPlayerFollowController.Recenter();
                }
            }, 0);

            bool include = false;
            if (!focused)
            {
                if (alreadyActive)
                {
                    Refocus().Complete();
                }
                else
                {
                    include = true;
                    Unfocus().Complete();
                    dependency = Refocus(dependency);
                }
            }

            SizeAnimationSingleAxis resize = new SizeAnimationSingleAxis()
            {
                Target = VideoPlayerResizeRoot,
                TargetSize = VideoPlayerResizeRoot.SizeMinMax.X.Max,
                StartSize = VideoPlayerResizeRoot.CalculatedSize.X.Value,
                AxisToChange = Axis.X,
                AnimationCurve = (AnimationCurve)FullScreenFollow.SpringAnimation,
            };

            dependency = include ? dependency.Include(resize, FullScreenFollow.AnimationDuration * 0.5f) :  dependency.Chain(resize, FullScreenFollow.AnimationDuration * 0.5f);

            dependency = dependency.Include(DimLightsAnimation, DimLightsAnimationDuration);

            dependency = dependency.Include(new ClipMaskTintAnimation()
            {
                Target = OnScreenControlsMask,
                TargetColor = Color.clear,
            }, ControlsFadeAnimationDuration);

            dependency = dependency.Include(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = () =>
                {
                    PlaybackControlsFollowController.transform.position = startFromTransform.position;
                    PlaybackControlsFollowController.transform.rotation = startFromTransform.rotation;

                    PlaybackControlsFollowController.gameObject.SetActive(true);
                    PlaybackControlsFollowController.FollowConfiguration = startFromFollowMode;
                    PlaybackControlsFollowController.Recenter();
                }
            }, 0);

            dependency = PlaybackControlsAnimator.Expand(include: true, dependency: dependency);

            dependency = dependency.Chain(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = () =>
                {
                    PlaybackControlsFollowController.FollowConfiguration = PlaybackControlsFollow;
                    PlaybackControlsFollowController.Recenter();
                },
            }, 0);

            return dependency;
        }

        /// <summary>
        /// Collapse the full-screen mode playback controls and animate 
        /// the video screen from full-screen mode to picture-in-picture mode.
        /// </summary>
        public AnimationHandle EnterPiPMode(Follow endInFollowMode, AnimationHandle dependency = default)
        {
            if (!focused)
            {
                Refocus().Complete();
            }

            controlsFadeTracker.Cancel();

            dependency = dependency.Chain(new ActivateGameObjectAnimation()
            {
                Target = VideoPlayerFollowController.gameObject,
                TargetActive = true,
            }, 0);

            dependency = PlaybackControlsAnimator.Collapse(include: false, dependency);

            PlaybackControlsFollowController.FollowConfiguration = endInFollowMode;
            VideoPlayerFollowController.FollowConfiguration = PIPFollow;

            dependency = dependency.Include(new SizeAnimationSingleAxis()
            {
                Target = VideoPlayerResizeRoot,
                TargetSize = VideoPlayerResizeRoot.SizeMinMax.X.Min,
                StartSize = VideoPlayerResizeRoot.CalculatedSize.X.Value,
                AxisToChange = Axis.X,
                AnimationCurve = (AnimationCurve)PIPFollow.SpringAnimation
            }, PiPModeAnimationDuration);

            dependency = dependency.Include(UndimLightsAnimation, DimLightsAnimationDuration);

            dependency = dependency.Include(new ClipMaskTintAnimation()
            {
                Target = OnScreenControlsMask,
                TargetColor = Color.white,
            }, ControlsFadeAnimationDuration);

            dependency = dependency.Chain(new ActivateGameObjectAnimation()
            {
                Target = PlaybackControlsFollowController.gameObject,
                TargetActive = false,
            }, 0);

            dependency = dependency.Chain(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = QueueFadeOutOnScreenControls
            }, 0);

            return dependency;
        }

        /// <summary>
        /// Unfocus the video player and playback controls.
        /// </summary>
        public AnimationHandle Unfocus(AnimationHandle dependency = default)
        {
            focused = false;
            dependency = dependency.Chain(OutOfFocusAnimation, UnfocusAnimationDuration);
            return PlaybackControlsAnimator.Unfocus(include: true, dependency);
        }

        /// <summary>
        /// Refocus the video player and playback controls.
        /// </summary>
        public AnimationHandle Refocus(AnimationHandle dependency = default)
        {
            focused = true;

            PositionAnimationSingleAxis refocus = OutOfFocusAnimation;
            refocus.StartPosition = OutOfFocusAnimation.TargetPosition;
            refocus.TargetPosition = OutOfFocusAnimation.StartPosition;

            dependency = dependency.Chain(refocus, UnfocusAnimationDuration);
            return PlaybackControlsAnimator.Refocus(include: true, dependency);
        }

        /// <summary>
        /// Defer a fade out of the on screen controls.
        /// </summary>
        private void QueueFadeOutOnScreenControls()
        {
            controlsFadeTracker = Delay.For(controlsFadeTracker, FadeOutControlsAfterXSeconds);

            controlsFadeTracker = controlsFadeTracker.Chain(new ClipMaskTintAnimation()
            {
                Target = OnScreenControlsMask,
                TargetColor = Color.clear,
            }, ControlsFadeAnimationDuration);

            controlsFadeTracker = controlsFadeTracker.Chain(new ActivateGameObjectAnimation()
            {
                Target = OnScreenControlsMask.gameObject,
                TargetActive = false,
            }, 0);
        }
    }
}
