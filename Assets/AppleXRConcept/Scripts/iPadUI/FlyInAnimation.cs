using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Used to apply an iOS-style fly/bounce animation to the app icons in the homescreen grid.
    /// </summary>
    public class FlyInAnimation : NovaBehaviour
    {
        [Header("Fly In")]
        [Min(0), Tooltip("The animation will be delayed by a randomly selected value between [0, MaxTimeDelay] for a more staggered effect.")]
        public float MaxTimeDelay = 0.1f;
        [Min(0), Tooltip("The duration of the animation.")]
        public float FlyInDuration = 1f;
        [SerializeField]
        private FlyAnimation flyAnimation = default;

        private AnimationHandle runningAnimation = default;

        private void OnEnable()
        {
            // Start disabled
            flyAnimation.Target.gameObject.SetActive(false);

            // Delay by a random amount and then run the animation
            runningAnimation = Delay.For(Random.Range(0, MaxTimeDelay)).Chain(flyAnimation, FlyInDuration);
        }

        private void OnDisable()
        {
            runningAnimation.Complete();
        }

        [System.Serializable]
        private struct FlyAnimation : IAnimation
        {
            [Tooltip("The UIBlock whose position will be animated")]
            public UIBlock Target;

            [Tooltip("The spring parameters to create the bounce/overshoot effect.")]
            public SpringCurve Overshoot;

            [Tooltip("The distance from 0 the target position will start from.")]
            public float StartDistance;

            public void Update(float percentDone)
            {
                Target.gameObject.SetActive(true);
                Vector3 flyFromPositionWorldSpace = GetFlyFromPosition();
                Vector3 flyFromPositionLocalSpace = Target.transform.parent != null ? Target.transform.parent.InverseTransformPoint(flyFromPositionWorldSpace) : flyFromPositionWorldSpace;

                Target.Position.Value = Vector3.LerpUnclamped(flyFromPositionLocalSpace, Vector3.zero, Overshoot.GetPosition(percentDone));
            }

            private Vector3 GetFlyFromPosition()
            {
                Camera cam = Camera.main;

                if (Target.Root == null)
                {
                    return cam.transform.position;
                }

                Vector3 positionRootSpace = Target.Root.transform.InverseTransformPoint(Target.transform.parent.position);
                Vector3 directionRootSpace = ((Vector2)positionRootSpace).normalized;
                Vector3 flyFromPositionRootSpace = positionRootSpace + directionRootSpace * StartDistance;

                return Target.Root.transform.TransformPoint(flyFromPositionRootSpace);
            }
        }

        public FlyInAnimation()
        {
            flyAnimation = new FlyAnimation() { Overshoot = SpringCurve.Overshoot };
        }
    }
}
