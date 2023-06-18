using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    [RequireComponent(typeof(Interactable))]
    public class VisionOSAppButton : NovaBehaviour
    {
        public PositionAnimationSingleAxis IconPositionAnimation = new PositionAnimationSingleAxis()
        {
            AxisToChange = Axis.Z,
            AnimationCurve = AnimationUtil.SpringEase,
            StartPosition = 0,
            TargetPosition = -50,
        };

        public PositionAnimationSingleAxis BackPlatePositionAnimation = new PositionAnimationSingleAxis()
        {
            AxisToChange = Axis.Z,
            AnimationCurve = AnimationUtil.SpringEase,
            StartPosition = 10,
            TargetPosition = -20,
        };

        public SizeAnimationSingleAxis GrowAnimation = new SizeAnimationSingleAxis()
        {
            AxisToChange = Axis.X,
            AnimationCurve = AnimationUtil.SpringEase,
            StartSize = 144,
            TargetSize = 164,
        };

        public float AnimationDuration = 0.15f;

        private AnimationHandle gestureAnimation = default;

        private void OnEnable()
        {
            UIBlock.AddGestureHandler<Gesture.OnHover>(HandleHover);
            UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleUnhover);
            UIBlock.AddGestureHandler<Gesture.OnPress>(HandlePress);
            UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleRelease);
            UIBlock.AddGestureHandler<Gesture.OnCancel>(HandleCanceled);
        }

        private void OnDisable()
        {
            UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleHover);
            UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleUnhover);
            UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandlePress);
            UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleRelease);
            UIBlock.RemoveGestureHandler<Gesture.OnCancel>(HandleCanceled);
        }

        private void HandleHover(Gesture.OnHover evt) => Elevate(AnimationDuration);

        private void HandleUnhover(Gesture.OnUnhover evt) => Deelevate(AnimationDuration);

        private void HandleCanceled(Gesture.OnCancel evt) => Deelevate(AnimationDuration * 0.5f);
        private void HandlePress(Gesture.OnPress evt)
        {
            gestureAnimation.Cancel();

            SizeAnimationSingleAxis growAnimation = GrowAnimation;

            if (growAnimation.Target != null)
            {
                growAnimation.StartSize = growAnimation.Target.Size.X.Raw;
                growAnimation.TargetSize = GrowAnimation.StartSize;
                gestureAnimation = growAnimation.Run(AnimationDuration * 0.5f);
            }
        }
        
        private void HandleRelease(Gesture.OnRelease evt)
        {
            if (evt.Hovering)
            {
                Elevate(AnimationDuration * 0.5f);
            }
            else
            {
                Deelevate(AnimationDuration);
            }
        }

        private void Elevate(float duration)
        {
            gestureAnimation.Cancel();

            PositionAnimationSingleAxis iconAnimation = IconPositionAnimation;
            PositionAnimationSingleAxis backPlateAnimation = BackPlatePositionAnimation;
            SizeAnimationSingleAxis growAnimation = GrowAnimation;

            if (iconAnimation.Target != null)
            {
                iconAnimation.StartPosition = iconAnimation.Target.Position.Z.Raw;
                gestureAnimation = iconAnimation.Run(duration);
            }

            if (backPlateAnimation.Target != null)
            {
                backPlateAnimation.StartPosition = backPlateAnimation.Target.Position.Z.Raw;
                gestureAnimation = gestureAnimation.Include(backPlateAnimation, duration);
            }

            if (growAnimation.Target != null)
            {
                growAnimation.StartSize = growAnimation.Target.Size.X.Raw;
                gestureAnimation = gestureAnimation.Include(growAnimation, duration);
            }
        }

        private void Deelevate(float duration)
        {
            gestureAnimation.Cancel();

            PositionAnimationSingleAxis iconAnimation = IconPositionAnimation;
            PositionAnimationSingleAxis backPlateAnimation = BackPlatePositionAnimation;
            SizeAnimationSingleAxis growAnimation = GrowAnimation;

            if (iconAnimation.Target != null)
            {
                iconAnimation.StartPosition = iconAnimation.Target.Position.Z.Raw;
                iconAnimation.TargetPosition = IconPositionAnimation.StartPosition;
                gestureAnimation = iconAnimation.Run(duration);
            }

            if (backPlateAnimation.Target != null)
            {
                backPlateAnimation.StartPosition = backPlateAnimation.Target.Position.Z.Raw;
                backPlateAnimation.TargetPosition = BackPlatePositionAnimation.StartPosition;
                gestureAnimation = gestureAnimation.Include(backPlateAnimation, duration);
            }

            if (growAnimation.Target != null)
            {
                growAnimation.StartSize = growAnimation.Target.Size.X.Raw;
                growAnimation.TargetSize = GrowAnimation.StartSize;
                gestureAnimation = gestureAnimation.Include(growAnimation, duration);
            }
        }
    }
}
