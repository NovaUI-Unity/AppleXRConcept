using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    /// <summary>
    /// Applies some color change animations based on
    /// gesture events without handling click/drag behavior
    /// </summary>
    public class GestureStateIndicator : NovaBehaviour
    {
        public UIBlock Indicator = null;

        public float AnimationDuration = 0.15f;

        [Header("Body")]
        public bool ChangeBodyColor = true;
        public Color DefaultBodyColor = Color.white;
        public Color HoverBodyColor = Color.gray;
        public Color PressedBodyColor = Color.black;

        [Header("Border")]
        public bool ChangeBorderColor = false;
        public Color DefaultBorderColor = Color.white;
        public Color HoverBorderColor = Color.gray;
        public Color PressedBorderColor = Color.black;

        AnimationHandle gestureAnimation = default;

        private void OnEnable()
        {
            Animate(DefaultBodyColor, DefaultBorderColor).Complete();

            UIBlock.AddGestureHandler<Gesture.OnHover>(HandleHovered);
            UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleUnovered);
            UIBlock.AddGestureHandler<Gesture.OnPress>(HandlePressed);
            UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleReleased);
            UIBlock.AddGestureHandler<Gesture.OnCancel>(HandleCanceled);
        }

        private void OnDisable()
        {
            gestureAnimation.Complete();

            UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleHovered);
            UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleUnovered);
            UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandlePressed);
            UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleReleased);
            UIBlock.RemoveGestureHandler<Gesture.OnCancel>(HandleCanceled);
        }

        private void HandleHovered(Gesture.OnHover evt) 
        {
            gestureAnimation.Cancel();
            gestureAnimation = Animate(HoverBodyColor, HoverBorderColor); 
        }

        private void HandleUnovered(Gesture.OnUnhover evt) 
        {
            gestureAnimation.Cancel();
            gestureAnimation = Animate(DefaultBodyColor, DefaultBorderColor);
        }

        private void HandlePressed(Gesture.OnPress evt) 
        {
            gestureAnimation.Cancel();
            gestureAnimation = Animate(PressedBodyColor, PressedBorderColor);
        }

        private void HandleReleased(Gesture.OnRelease evt) 
        {
            gestureAnimation.Cancel();

            if (evt.Hovering)
            {
                gestureAnimation = Animate(HoverBodyColor, HoverBorderColor);
            }
            else
            {
                gestureAnimation = Animate(DefaultBodyColor, DefaultBorderColor);
            }
        }

        private void HandleCanceled(Gesture.OnCancel evt) 
        {
            gestureAnimation.Cancel();
            gestureAnimation = Animate(DefaultBodyColor, DefaultBorderColor);
        }

        private AnimationHandle Animate(Color bodyColor, Color borderColor)
        {
            AnimationHandle handle = default;

            if (ChangeBodyColor)
            {
                handle = handle.Include(new BodyColorAnimation()
                {
                    Target = Indicator,
                    TargetColor = bodyColor,
                }, AnimationDuration);
            }

            if (ChangeBorderColor && Indicator is UIBlock2D uiBlock2D)
            {
                handle = handle.Include(new BorderColorAnimation()
                {
                    Target = uiBlock2D,
                    TargetColor = borderColor,
                }, AnimationDuration);
            }

            return handle;
        }
    }
}
