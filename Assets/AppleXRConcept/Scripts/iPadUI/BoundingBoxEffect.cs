using Nova;
using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Creates a sort of glowing box outline effect out of UIBlock2Ds, where each face of the box is a separate UIBlock2D
    /// </summary>
    [ExecuteAlways]
    public class BoundingBoxEffect : NovaBehaviour
    {
        [Tooltip("Fade out the box when it's first activated?")]
        public bool FadeOutOnAwake = true;
        
        [Header("Visual Properties")]
        [Tooltip("The color of sides.")]
        public Color Color = Color.black.WithAlpha(0.15f);
        [Tooltip("The color of outline.")]
        public Color BorderColor = Color.white;
        [Tooltip("The width of outline.")]
        public float BorderWidth = 8;
        [Tooltip("The \"glow\" amount of outline.")]
        public float Blur = 32;

        [SerializeField, HideInInspector]
        private UIBlock2D front = null;
        [SerializeField, HideInInspector]
        private UIBlock2D back = null;
        [SerializeField, HideInInspector]
        private UIBlock2D left = null;
        [SerializeField, HideInInspector]
        private UIBlock2D right = null;
        [SerializeField, HideInInspector]
        private UIBlock2D top = null;
        [SerializeField, HideInInspector]
        private UIBlock2D bottom = null;

        [SerializeField, HideInInspector]
        private Animation fadeInAnimation = default;
        [NonSerialized, HideInInspector]
        private Animation fadeOutAnimation = default;

        /// <summary>
        /// Get an animation to fade in/out the bounding box effect.
        /// </summary>
        public Animation GetAnimation(bool fadeIn, AnimationCurve curve = null)
        {
            return fadeIn ? GetFadeInAnimation(curve) : GetFadeOutAnimation(curve);
        }

        private Animation GetFadeInAnimation(AnimationCurve curve = null)
        {
            Animation fadeIn = fadeInAnimation;
            fadeIn.Target = this;
            fadeIn.AnimationCurve = curve;
            return fadeIn;
        }

        private Animation GetFadeOutAnimation(AnimationCurve curve = null)
        {
            Animation fadeOut = fadeOutAnimation;

            fadeOut.Target = this;
            fadeOut.AnimationCurve = curve;
            fadeOut.EndColor = fadeInAnimation.EndColor.Transparent();
            fadeOut.BorderEndColor = fadeInAnimation.BorderEndColor.Transparent();

            return fadeOut;
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif

            if (FadeOutOnAwake)
            {
                // Ensure initialized
                LateUpdate();

                GetFadeOutAnimation().Run(0).Complete();
            }
        }

        private void LateUpdate()
        {
            UIBlock.CalculateLayout();

            EnsureSides();
            UpdateSides();
        }

        /// <summary>
        /// Update all sides of the box effect.
        /// </summary>
        private void UpdateSides()
        {
            Vector3 size = UIBlock.PaddedSize;
            UpdateSide(front, new Vector3(size.x, size.y, 0), Vector3.back);
            UpdateSide(back, new Vector3(size.x, size.y, 0), Vector3.forward);
            UpdateSide(left, new Vector3(size.z, size.y, 0), Vector3.right);
            UpdateSide(right, new Vector3(size.z, size.y, 0), Vector3.left);
            UpdateSide(bottom, new Vector3(size.x, size.z, 0), Vector3.up);
            UpdateSide(top, new Vector3(size.x, size.z, 0), Vector3.down);
        }


        /// <summary>
        /// Apply the effect properties to the given side of the box effect,
        /// and make sure the side is positioned/sized correctly
        /// </summary>
        private void UpdateSide(UIBlock2D side, Vector3 size, Vector3 forward)
        {
            side.CalculateLayout();

            side.RotateSize = true;
            side.Size = size;
            side.Position = Vector3.zero;
            side.transform.localRotation = Quaternion.LookRotation(forward);
            side.transform.localScale = Vector3.one;
            side.Border = new Border()
            {
                Enabled = BorderWidth > 0,
                Direction = BorderDirection.In,
                Width = BorderWidth,
                Color = BorderColor
            };

            side.Shadow = new Shadow()
            {
                Enabled = Blur > 0,
                Width = BorderWidth,
                Blur = Blur,
                Direction = ShadowDirection.In,
                Color = BorderColor
            };

            side.BodyEnabled = true;
            side.Color = Color;
        }

        /// <summary>
        /// Ensure all sides of the box exist.
        /// </summary>
        private void EnsureSides()
        {
            if (front == null)
            {
                front = EnsureSide(nameof(Alignment.Front), Alignment.Front);
            }

            if (back == null)
            {
                back = EnsureSide(nameof(Alignment.Back), Alignment.Back);
            }

            if (left == null)
            {
                left = EnsureSide(nameof(Alignment.Left), Alignment.Left);
            }

            if (right == null)
            {
                right = EnsureSide(nameof(Alignment.Right), Alignment.Right);
            }

            if (top == null)
            {
                top = EnsureSide(nameof(Alignment.Top), Alignment.Top);
            }

            if (bottom == null)
            {
                bottom = EnsureSide(nameof(Alignment.Bottom), Alignment.Bottom);
            }
        }

        /// <summary>
        /// Get or create a UIBlock2D for a given side of the box
        /// </summary>
        private UIBlock2D EnsureSide(string name, Alignment alignment)
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);

                if (child.name == name && TryGetComponent(out UIBlock2D side))
                {
                    side.Alignment = alignment;
                    return side;
                }
            }

            UIBlock2D childObject = new GameObject(name).AddComponent<UIBlock2D>();
            childObject.Alignment = alignment;
            childObject.transform.parent = transform;

            return childObject;
        }

        /// <summary>
        /// Used to synchronize the serialized state with a fade in/out animation
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif

            fadeInAnimation = new Animation()
            {
                EndColor = Color,
                BorderEndColor = BorderColor,
                BorderEndWidth = BorderWidth,
                BorderEndBlur = Blur,
            };
        }

        /// <summary>
        /// Animate the properties of a <see cref="BoundingBoxEffect"/>.
        /// </summary>
        [Serializable]
        public struct Animation : IAnimation
        {
            [Tooltip("The bounding box effect to animate.")]
            public BoundingBoxEffect Target;
            [Tooltip("The animation curve to evaluate over the course of the animation.")]
            public AnimationCurve AnimationCurve;

            [Tooltip("The color of the box's sides when the animation ends.")]
            public Color EndColor;
            [Tooltip("The color of the box's outline when the animation ends.")]
            public Color BorderEndColor;
            [Tooltip("The width of the box's outline when the animation ends.")]
            public float BorderEndWidth;
            [Tooltip("The amount of \"glow\" in the effect when the animation ends.")]
            public float BorderEndBlur;

            private Color startColor;
            private Color borderStartColor;
            private float borderStartWidth;
            private float borderStartBlur;

            private bool initialized;

            public void Update(float percentDone)
            {
                EnsureInitialized();

                float lerp = AnimationCurve != null ? AnimationCurve.Evaluate(percentDone) : percentDone;

                Target.BorderColor = Color.Lerp(borderStartColor, BorderEndColor, lerp);
                Target.Color = Color.Lerp(startColor, EndColor, lerp);
                Target.BorderWidth = Mathf.Lerp(borderStartWidth, BorderEndWidth, lerp);
                Target.Blur = Mathf.Lerp(borderStartBlur, BorderEndBlur, lerp);

                // Nova Animations run after LateUpdate, so
                // we explicitly call update here to sync before
                // end of frame
                Target.LateUpdate();
            }

            /// <summary>
            /// Get the starting values
            /// </summary>
            private void EnsureInitialized()
            {
                if (initialized)
                {
                    return;
                }

                borderStartColor = Target.BorderColor;
                startColor = Target.Color;
                borderStartWidth = Target.BorderWidth;
                borderStartBlur = Target.Blur;

                initialized = true;
            }
        }
    }
}
