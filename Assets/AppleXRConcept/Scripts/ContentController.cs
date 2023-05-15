using Nova;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Manages the different beats of the demo
    /// </summary>
    public class ContentController : MonoBehaviour
    {
        [Header("Activity Controllers")]
        public TabletViewLauncher TabletViewLauncher = null;
        public VideoPlayerModeController VideoPlayer = null;
        public Notification Notification = null;
        public SiriBubbleAnimator Siri = null;
        public ComposeMessageFlow MessageComposer = null;

        [Header("Follow Controllers")]
        public FollowController TabletFollowController = null;

        [Header("Immersive Visuals")]
        public float FadeOutPassthroughDuration = 0.5f;
        public UnityEventAnimation FadeOutPassthroughAnimation = new UnityEventAnimation()
        {
            ModifyingFunction = AnimationCurve.Linear(0, 1, 1, 0)
        };

        public HyperSpaceController HyperspaceAnimation = null;
        public string SceneToLoad = "HorizonWorlds";

        private AnimationHandle pipAnimation = default;
        private AnimationHandle fullScreenAnimation = default;

        private void OnEnable()
        {
            TabletViewLauncher.gameObject.SetActive(true);
            VideoPlayer.gameObject.SetActive(false);
            Notification.gameObject.SetActive(false);
            MessageComposer.gameObject.SetActive(false);
            Siri.gameObject.SetActive(false);

            TabletViewLauncher.OnFullScreenButtonClicked += EnterFullScreenMode;
            TabletViewLauncher.OnImmersiveAppSelected += GoToHorizonWorlds;
            VideoPlayer.OnExitingFullScreenMode += EnterPiPScreenMode;
            Notification.OnDismissNotification += HideNotification;
            HyperspaceAnimation.OnAnimationEnded += LaunchHorizonWorlds;
        }

        private void OnDisable()
        {
            TabletViewLauncher.OnFullScreenButtonClicked -= EnterFullScreenMode;
            TabletViewLauncher.OnImmersiveAppSelected -= GoToHorizonWorlds;
            VideoPlayer.OnExitingFullScreenMode -= EnterPiPScreenMode;
            Notification.OnDismissNotification -= HideNotification;
            HyperspaceAnimation.OnAnimationEnded -= LaunchHorizonWorlds;

            pipAnimation.Complete();
            fullScreenAnimation.Complete();
        }

        private void LaunchHorizonWorlds()
        {
            Camera.main.clearFlags = CameraClearFlags.Skybox;
            HyperspaceAnimation.gameObject.SetActive(false);
            SceneManager.LoadSceneAsync(SceneToLoad, LoadSceneMode.Additive);
        }

        [Button("Show Siri")]
        private void ShowSiri()
        {
            AnimationHandle animation = VideoPlayer.Unfocus();

            animation = animation.Chain(new ActivateGameObjectAnimation()
            {
                Target = Siri.gameObject,
                TargetActive = true,
            }, 0);

            animation = Siri.ShowSiri(animation);

            animation = Delay.For(animation, 2f);

            animation = animation.Chain(new ActivateGameObjectAnimation()
            {
                Target = MessageComposer.gameObject,
                TargetActive = true,
            }, 0);

            animation = MessageComposer.StartComposeFlow(animation);
            animation = Siri.ShowPrompt(animation, "What do you want to say?");
            animation = Delay.For(animation, 1f);
            animation = MessageComposer.StartDictation(animation);
            animation = Delay.For(animation, 1f);
            animation = Siri.HidePrompt(animation);
            animation = Siri.ShowPrompt(animation, "Send it?");
            animation = Delay.For(animation, 1.5f);
            animation = MessageComposer.ShowCoversation(animation);
            animation = Siri.HidePrompt(animation, include: true);
            animation = Delay.For(animation, 2f);
            animation = MessageComposer.EndComposeFlow(animation);
            animation = Siri.HideSiri(animation);

            animation = animation.Chain(new ActivateGameObjectAnimation()
            {
                Target = MessageComposer.gameObject,
                TargetActive = false,
            }, 0);

            animation = animation.Include(new ActivateGameObjectAnimation()
            {
                Target = Siri.gameObject,
                TargetActive = false,
            });

            animation = animation.Chain(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = HideSiri
            }, 0);
        }

        [Button("Hide Siri")]
        private void HideSiri()
        {
            AnimationHandle hideSiri = Siri.HideSiri();

            hideSiri = hideSiri.Chain(new ActivateGameObjectAnimation()
            {
                Target = Siri.gameObject,
                TargetActive = false,
            }, 0);

            hideSiri = VideoPlayer.Refocus(hideSiri);
        }

        [Button("Show Notification")]
        private void ShowNotification()
        {
            Notification.gameObject.SetActive(true);
            Notification.ShowNotification();
        }

        [Button("Hide Notification")]
        private void HideNotification()
        {
            AnimationHandle dismiss = Notification.DismissNotification();
            dismiss = dismiss.Chain(new ActivateGameObjectAnimation()
            {
                Target = Notification.gameObject,
                TargetActive = false,
            }, 0);

            dismiss = Delay.For(dismiss, 2f);

            dismiss = dismiss.Chain(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = ShowSiri
            }, 0);
        }

        [Button("Open Tablet View")]
        private void OpenTabletView()
        {
            TabletFollowController.gameObject.SetActive(true);

            TabletViewLauncher.gameObject.SetActive(true);

            TabletViewLauncher.Expand(default);
        }

        [Button("Close Tablet View")]
        private void CloseTabletView()
        {
            if (!TabletViewLauncher.gameObject.activeInHierarchy)
            {
                return;
            }

            AnimationHandle closing = TabletViewLauncher.Collapse();

            closing = closing.Chain(new ActivateGameObjectAnimation()
            {
                Target = TabletViewLauncher.gameObject,
                TargetActive = false,
            }, 0);

            closing = closing.Chain(new ActivateGameObjectAnimation()
            {
                Target = TabletFollowController.gameObject,
                TargetActive = false,
            }, 0);
        }

        [Button("Enter Full Screen Mode")]
        private void EnterFullScreenMode()
        {
            pipAnimation.Complete();
            fullScreenAnimation.Complete();

            VideoPlayer.gameObject.SetActive(true);

            fullScreenAnimation = TabletViewLauncher.Collapse();
            fullScreenAnimation = fullScreenAnimation.Chain(new ActivateGameObjectAnimation()
            {
                Target = TabletFollowController.gameObject,
                TargetActive = false,
            }, 0);

            fullScreenAnimation = VideoPlayer.EnterFullScreenMode(TabletFollowController.FollowConfiguration, TabletFollowController.transform, fullScreenAnimation);

            fullScreenAnimation = Delay.For(fullScreenAnimation, 5f);

            fullScreenAnimation = fullScreenAnimation.Chain(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = ShowNotification,
            }, 0);
        }

        [Button("Enter PIP Mode")]
        private void EnterPiPScreenMode()
        {
            pipAnimation.Complete();
            fullScreenAnimation.Complete();

            VideoPlayer.gameObject.SetActive(true);

            pipAnimation = VideoPlayer.EnterPiPMode(TabletFollowController.FollowConfiguration);

            pipAnimation = pipAnimation.Chain(new ActivateGameObjectAnimation()
            {
                Target = TabletFollowController.gameObject,
                TargetActive = true,
            }, 0);

            pipAnimation = TabletViewLauncher.Expand(pipAnimation);
        }

        [Button("Launch Horizon Worlds")]
        private void GoToHorizonWorlds()
        {
            AnimationHandle closing = TabletViewLauncher.Collapse();

            closing = closing.Chain(FadeOutPassthroughAnimation, FadeOutPassthroughDuration);

            closing = closing.Chain(new ActivateGameObjectAnimation()
            {
                Target = HyperspaceAnimation.gameObject,
                TargetActive = true,
            }, 0);
        }
    }
}
