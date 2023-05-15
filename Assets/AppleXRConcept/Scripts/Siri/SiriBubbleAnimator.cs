using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Animates in the Siri bubble, removes the black background from the 
    /// Siri video, and displays different response prompts from Siri.
    /// </summary>
    public class SiriBubbleAnimator : MonoBehaviour
    {
        /// <summary>
        /// Cache the property ID.
        /// </summary>
        private static readonly int MinAlphaID = Shader.PropertyToID("_MinAlpha");

        [Header("Bubble Animation")]
        [Tooltip("The duration of the hide/show animation")]
        public float ResizeDuration = 0.5f;
        [Tooltip("The hide/show animation. \"Axis To Change\" should be set to the aspect-locked axis of the \"Target\".")]
        public SizeAnimationSingleAxis SizeAnimation = new SizeAnimationSingleAxis
        {
            AxisToChange = Axis.X,
            StartSize = 0.01f,
            TargetSize = 1024,
            AnimationCurve = AnimationUtil.SpringEase,
        };

        [Header("Prompt Animation Visuals")]
        [Tooltip("The duration of the fade in/out prompt animations.")]
        public float FadeInPromptDuration = 0.5f;
        [Tooltip("The background UIBlock of the prompt to fade in/out.")]
        public UIBlock PromptBackground = null;
        [Tooltip("The the prompt text to fade in/out and used to display the prompt.")]
        public TextBlock PromptText = null;

        [Header("Video Texture Animation")]
        [Tooltip("The render texture assigned to the Siri video player.")]
        public RenderTexture VideoPlayerTexture;
        [Tooltip("The render texture assigned to the Siri UI Block 2D where the video will be played.")]
        public RenderTexture UIBlockTexture;
        [Tooltip("The material to apply to the Siri video (used to remove the black background from the video).")]
        public Material SiriVideoMaterial;
        [Range(0, 1)]
        [Tooltip("The minimum alpha for a pixel in the video texture.")]
        public float MinAlpha = 0.6f;

        // Update is called once per frame
        void Update()
        {
            if (VideoPlayerTexture == null || UIBlockTexture == null || SiriVideoMaterial == null)
            {
                return;
            }

            // Update the min alpha
            SiriVideoMaterial.SetFloat(MinAlphaID, MinAlpha);

            // Apply the material + video to the UIBlock texture 
            Graphics.Blit(VideoPlayerTexture, UIBlockTexture, SiriVideoMaterial);
        }

        /// <summary>
        /// Animate the Siri bubble into view after <paramref name="dependency"/>.
        /// </summary>
        public AnimationHandle ShowSiri(AnimationHandle dependency = default)
        {
            HidePrompt(default(AnimationHandle)).Complete();

            return dependency.Include(SizeAnimation, ResizeDuration);
        }

        /// <summary>
        /// Fade in the response prompt displaying the given <paramref name="promptMessage"/> after <paramref name="dependency"/>.
        /// </summary>
        public AnimationHandle ShowPrompt(AnimationHandle dependency, string promptMessage)
        {
            BodyColorAnimation showPromptBackground = new BodyColorAnimation()
            {
                Target = PromptBackground,
                TargetColor = PromptBackground.Color.WithAlpha(1),
            };

            BodyColorAnimation showPromptText = new BodyColorAnimation()
            {
                Target = PromptText,
                TargetColor = PromptText.Color.WithAlpha(1),
            };

            dependency = dependency.Chain(new RunMethodOnCompleteAnimation()
            {
                MethodToRunOnComplete = () => PromptText.Text = promptMessage,
            }, 0);

            return dependency.Chain(showPromptBackground, FadeInPromptDuration).Include(showPromptText);
        }

        /// <summary>
        /// Fade out the response prompt. If <c><paramref name="include"/> == true</c>, then fade out the 
        /// prompt at the same time as <paramref name="dependency"/>. Otherwise do the fade out once 
        /// <paramref name="dependency"/> completes (chained instead of included).
        /// </summary>
        public AnimationHandle HidePrompt(AnimationHandle dependency, bool include = true)
        {
            BodyColorAnimation hidePromptBackground = new BodyColorAnimation()
            {
                Target = PromptBackground,
                TargetColor = PromptBackground.Color.Transparent(),
            };

            BodyColorAnimation hidePromptText = new BodyColorAnimation()
            {
                Target = PromptText,
                TargetColor = PromptText.Color.Transparent(),
            };

            if (include)
            {
                return dependency.Include(hidePromptBackground, FadeInPromptDuration).Include(hidePromptText, FadeInPromptDuration);
            }

            return dependency.Chain(hidePromptBackground, FadeInPromptDuration).Include(hidePromptText);
        }

        /// <summary>
        /// Animate the Siri bubble out of view after <paramref name="dependency"/>.
        /// </summary>
        public AnimationHandle HideSiri(AnimationHandle dependency = default)
        {
            SizeAnimationSingleAxis hideAnimation = SizeAnimation;
            hideAnimation.TargetSize = SizeAnimation.StartSize;
            hideAnimation.StartSize = SizeAnimation.TargetSize;

            return dependency.Chain(hideAnimation, ResizeDuration);
        }
    }
}
