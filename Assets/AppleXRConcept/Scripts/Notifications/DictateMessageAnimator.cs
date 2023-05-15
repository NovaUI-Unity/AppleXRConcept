using Nova;
using NovaSamples.UIControls;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Animates a string of text, appending one word at a time.
    /// Creates the illusion that the text is being generated via speech-to-text. 
    /// </summary>
    public class DictateMessageAnimator : MonoBehaviour
    {
        private const float EnableTextDuration = 0.25f;

        [Header("Text Field")]
        [Tooltip("The Text Block whose text will animate.")]
        public TextBlock MessageText = null;
        [Tooltip("The text sequence to animate.")]
        public string[] MessagePiecesToAnimate = new string[]
        {
                "Busy",
                "Busy now",
                "Busy now, but",
                "Busy now, but I'll",
                "Busy now, but I'll call",
                "Busy now, but I'll call you",
                "Busy now, but I'll call you later",
                "Busy now, but I'll call you later!",
        };
        [Tooltip("The duration of the animation")]
        public float DictationDuration = 3f;

        [Header("Send Button")]
        [Tooltip("The button to visually change to an \"enabled\" state once the dictation begins.")]
        public Button SendButton = null;
        [Tooltip("The background color of the button in the \"disabled\" state.")]
        public Color DisabledColor = Color.black;
        [Tooltip("The label color of the button in the \"disabled\" state.")]
        public Color DisabledTextColor = Color.white.WithAlpha(0.3f);
        [Tooltip("The background color of the button in the \"enabled\" state.")]
        public Color EnabledColor = Color.blue;
        [Tooltip("The label color of the button in the \"enabled\" state.")]
        public Color EnabledTextColor = Color.white;

        private void OnEnable()
        {
            ItemView view = SendButton.GetComponent<ItemView>();
            if (view.TryGetVisuals(out ButtonVisuals visuals))
            {
                visuals.Label.Color = DisabledTextColor;
                visuals.DefaultColor = DisabledColor;
                visuals.UpdateVisualState(VisualState.Default);
            }

            MessageText.Text = string.Empty;
        }

        /// <summary>
        /// Chain the dication animation to the animation combination tracked
        /// by <paramref name="dependency"/>, and return the new animation handle.
        /// </summary>
        public AnimationHandle StartDictation(AnimationHandle dependency)
        {
            dependency = dependency.Chain(new DictationAnimation()
            {
                Target = MessageText,
                MessagePieces = MessagePiecesToAnimate,
            }, DictationDuration);

            ItemView view = SendButton.GetComponent<ItemView>();

            // Update the button visuals along with the dictation to indicate there's now a message to send.
            if (view.TryGetVisuals(out ButtonVisuals visuals))
            {
                dependency = dependency.Include(new BodyColorAnimation()
                {
                    Target = visuals.Label,
                    TargetColor = EnabledTextColor,
                }, EnableTextDuration);

                dependency = dependency.Include(new RunMethodOnCompleteAnimation()
                {
                    MethodToRunOnComplete = () =>
                    {
                        visuals.DefaultColor = EnabledColor;
                        visuals.UpdateVisualState(VisualState.Default);
                    }
                });
            }

            return dependency;
        }

        /// <summary>
        /// A discrete text string animation
        /// </summary>
        private struct DictationAnimation : IAnimation
        {
            public TextBlock Target;
            public string[] MessagePieces;

            public void Update(float percentDone)
            {
                Target.Text = MessagePieces[Mathf.Min(Mathf.FloorToInt(percentDone * MessagePieces.Length), MessagePieces.Length - 1)];
            }
        }
    }
}
