using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// A component responsible for applying a <see cref="Follow"/> configuration to the attached transform.
    /// </summary>
    public class FollowController : MonoBehaviour
    {
        public enum OnEnableBehavior
        {
            RecenterInstant,
            RecenterAnimated,
            None,
        }
        
        public Follow FollowConfiguration = null;

        [Header("Content")]
        [Tooltip("The root UIBlock of content to move.\n\nThere should not be a UIBlock on this Follow Controller Game Object.")]
        public UIBlock UIRoot = null;

        [Header("Positioning")]
        [Tooltip("Recenter behavior when the component is enabled.")]
        public OnEnableBehavior WhenEnabled = OnEnableBehavior.RecenterInstant;

        [Header("Auto Fill")]
        [Tooltip("Should the \"UI Root\" be resized to fill the viewport?")]
        public bool TryFillViewport = false;
        [Tooltip("Scale the \"filled viewport\" size.")]
        public Vector2 SizeScalar = new Vector2(0.5f, 0.5f);
        [Tooltip("Apply the local position to \"filled viewport\" position.")]
        public Vector3 UIRootOffset = new Vector3(0, 300, 0);

        private Camera cameraToFollow = null;

        /// <summary>
        /// The camera this controller will follow.
        /// </summary>
        public Camera Camera
        {
            get
            {
                if (cameraToFollow == null)
                {
                    cameraToFollow = Camera.main;
                }

                return cameraToFollow;
            }
        }

        private Vector3[] worldCorners = new Vector3[Follow.NumCorners];
        private AnimationHandle followAnimation = default;
        private bool wasInView = false;

        private void OnEnable()
        {
            wasInView = false;

            EnsureUIRootFillsViewport();

            if (WhenEnabled != OnEnableBehavior.None)
            {
                Recenter();

                if (WhenEnabled == OnEnableBehavior.RecenterInstant)
                {
                    // Ensure this is calculated before calling complete,
                    // since the content may just be getting enabled
                    UIRoot.CalculateLayout();

                    followAnimation.Complete();
                }
            }
        }

        private void OnDisable()
        {
            followAnimation.Cancel();
        }

        private void Update()
        {
            EnsureUIRootFillsViewport();

            if (!NeedsReposition())
            {
                return;
            }

            Recenter();
        }


        /// <summary>
        /// Starts a new follow/recenter animation
        /// </summary>
        public void Recenter()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            FollowAnimation anim = new FollowAnimation()
            {
                Destination = Camera.transform,
                Follower = transform,
                Distance = FollowConfiguration.TargetDistance,
                SpringAnimation = FollowConfiguration.SpringAnimation,
                TargetLocalOffset = FollowConfiguration.EyeOffsetFromCamera * (1 / Camera.transform.lossyScale.x),
                AlwaysVerticalCenter = FollowConfiguration.AlwaysVerticalCenter,
            };

            followAnimation = anim.Run(FollowConfiguration.AnimationDuration);
        }


        /// <summary>
        /// Applies the filled size to <see cref="UIRoot"/> when <see cref="TryFillViewport"/> is enabled.
        /// </summary>
        private void EnsureUIRootFillsViewport()
        {
            if (!TryFillViewport)
            {
                return;
            }

            float distance = FollowConfiguration.TargetDistance;

            Vector3 bottomLeft = Camera.ViewportToWorldPoint(new Vector3(0, 0, distance));
            Vector3 topLeft = Camera.ViewportToWorldPoint(new Vector3(0, 1, distance));
            Vector3 bottomRight = Camera.ViewportToWorldPoint(new Vector3(1, 0, distance));

            Vector2 scaledSize = new Vector2(Vector3.Distance(bottomLeft, bottomRight), Vector3.Distance(bottomLeft, topLeft));
            Vector2 unscaledSize = SizeScalar * scaledSize / UIRoot.transform.lossyScale.x;

            if (UIRoot.CalculatedSize.XY.Value == unscaledSize)
            {
                return;
            }

            UIRoot.Position = UIRootOffset;

            UIRoot.Size.XY = unscaledSize;
            UIRoot.CalculateLayout();
        }

        /// <summary>
        /// Checks if the <see cref="UIRoot"/> needs to be repositioned back into view.
        /// </summary>
        private bool NeedsReposition()
        {
            if (!followAnimation.IsComplete())
            {
                bool isInView = false;

                GetWorldCorners(UIRoot, worldCorners);
                for (int i = 0; i < Follow.NumCorners; ++i)
                {
                    if (WithinCameraViewport(worldCorners[i], Camera, FollowConfiguration.MinDistance, FollowConfiguration.MaxDistance))
                    {
                        isInView = true;
                        break;
                    }
                }

                if (wasInView && !isInView)
                {
                    wasInView = isInView;
                    followAnimation.Cancel();
                    return true;
                }

                wasInView = isInView;
                return false;
            }

            if (FollowConfiguration.OutOfViewTrigger == OutOfViewTriggerPoint.Center)
            {
                bool outOfView = !WithinCameraViewport(UIRoot.transform.position, Camera, FollowConfiguration.MinDistance, FollowConfiguration.MaxDistance);

                return outOfView;
            }

            GetWorldCorners(UIRoot, worldCorners);
            int inViewCorners = FollowConfiguration.MaxAllowedCornersOutOfView;

            for (int i = 0; i < Follow.NumCorners; ++i)
            {
                if (!WithinCameraViewport(worldCorners[i], Camera, FollowConfiguration.MinDistance, FollowConfiguration.MaxDistance))
                {
                    inViewCorners--;

                    if (inViewCorners <= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given <paramref name="worldPos"/> is within 
        /// the given <paramref name="camera"/>'s viewport. Otherwise returns false.
        /// </summary>
        /// <param name="worldPos">The world position to check.</param>
        /// <param name="camera">The camera to use as the viewport.</param>
        public static bool WithinCameraViewport(Vector3 worldPos, Camera camera, float minDistance = 0, float maxDistance = float.PositiveInfinity)
        {
            if (camera == null)
            {
                return false;
            }

            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPos);

            float minZ = Mathf.Max(minDistance, camera.nearClipPlane);
            float maxZ = Mathf.Min(maxDistance, camera.farClipPlane);

            if (minZ > maxZ)
            {
                float swap = minZ;
                minZ = maxZ;
                maxZ = swap;
            }

            bool xInBounds = viewportPoint.x >= 0 && viewportPoint.x <= 1;
            bool yInBounds = viewportPoint.y >= 0 && viewportPoint.y <= 1;
            bool zInBounds = viewportPoint.z > minZ && viewportPoint.z < maxZ;

            return xInBounds && yInBounds && zInBounds;
        }

        /// <summary>
        /// Gets the corners of the given <paramref name="uiBlock"/> in world space.
        /// </summary>
        public static void GetWorldCorners(UIBlock uiBlock, Vector3[] fourCornersArray)
        {
            // UIBlock.CalculatedSize is the unscaled, unrotated size of the UIBlock
            Vector3 boundsMaxLocalSpace = uiBlock.CalculatedSize.Value * 0.5f;
            Vector3 boundsMinLocalSpace = -boundsMaxLocalSpace;

            // Create a new point for each corner in local space
            Vector3 bottomLeftLocalSpace = new Vector3(boundsMinLocalSpace.x, boundsMinLocalSpace.y, 0);
            Vector3 topLeftLocalSpace = new Vector3(boundsMinLocalSpace.x, boundsMaxLocalSpace.y, 0);
            Vector3 topRightLocalSpace = new Vector3(boundsMaxLocalSpace.x, boundsMaxLocalSpace.y, 0);
            Vector3 bottomRightLocalSpace = new Vector3(boundsMaxLocalSpace.x, boundsMinLocalSpace.y, 0);

            // Convert local space points into world space
            // Bottom Left
            fourCornersArray[0] = uiBlock.transform.TransformPoint(bottomLeftLocalSpace);

            // Top Left
            fourCornersArray[1] = uiBlock.transform.TransformPoint(topLeftLocalSpace);

            // Top Right
            fourCornersArray[2] = uiBlock.transform.TransformPoint(topRightLocalSpace);

            // Bottom Right
            fourCornersArray[3] = uiBlock.transform.TransformPoint(bottomRightLocalSpace);
        }

        /// <summary>
        /// The animation responsible for animating the <see cref="Follower"/> back into view.
        /// </summary>
        private struct FollowAnimation : IAnimation
        {
            private const float MaxUpdateVelocity = 1e-1f;
            private const float MinUpdateRotationVelocity = 0.15f;

            public Transform Destination;
            public Transform Follower;

            public Vector3 TargetLocalOffset;

            public SpringCurve SpringAnimation;
            public float Distance;
            public bool AlwaysVerticalCenter;

            private bool initialized;
            private Vector3 startPosition;
            private Vector3 startTangentPosition;
            private Quaternion startRotation;
            private Vector3 endPosition;
            private Vector3 previousEndPosition;
            private Vector2 startDirections;
            private float DestinationVelocity;

            private Vector3 Forward { get; set; }
            private Vector3 TargetPosition { get; set; }
            private Matrix4x4 TargetToWorld { get; set; }
            private Matrix4x4 WorldToTarget { get; set; }

            private bool done;
            public void Update(float percentDone)
            {
                bool firstFrame = !initialized;

                if ((initialized && SpringAnimation.IsDone(percentDone)) || done)
                {
                    done = true;
                    return;
                }

                Vector3 forward = AlwaysVerticalCenter ? Vector3.ProjectOnPlane(Destination.forward, Vector3.up).normalized : Destination.forward;

                Forward = forward;
                TargetToWorld = Destination.localToWorldMatrix * Matrix4x4.Translate(TargetLocalOffset);
                WorldToTarget = TargetToWorld.inverse;
                TargetPosition = TargetToWorld.GetPosition();

                EnsureInitialized();

                Vector3 newEndPositionWorldSpace = TargetPosition + forward * Distance;

                DestinationVelocity = 0.5f * DestinationVelocity + 0.5f * Vector3.Distance(newEndPositionWorldSpace, previousEndPosition);

                // Update when the destination is moving slowly and the animation isn't already settling
                if (DestinationVelocity <= MaxUpdateVelocity && !SpringAnimation.IsSettling(percentDone))
                {
                    Vector3 currentEndPositionLocalSpace = WorldToTarget.MultiplyPoint(endPosition);
                    Vector3 currentEndDirectionLocalSpace = currentEndPositionLocalSpace.normalized;
                    Vector2 newDirections = new Vector2(Sign(AngleBetweenAroundAxis(currentEndDirectionLocalSpace, Vector3.forward, Vector3.up)),
                                                        Sign(AngleBetweenAroundAxis(currentEndDirectionLocalSpace, Vector3.forward, Vector3.right)));

                    Vector3 newEndPositionLocalSpace = WorldToTarget.MultiplyPoint(newEndPositionWorldSpace);
                    Vector3 correctedDirectionLocalSpace = new Vector3(newDirections.x * startDirections.x >= 0 ? newEndPositionLocalSpace.x : currentEndPositionLocalSpace.x,
                                                                       newDirections.y * startDirections.y >= 0 ? newEndPositionLocalSpace.y : currentEndPositionLocalSpace.y,
                                                                       currentEndPositionLocalSpace.z);

                    endPosition = TargetPosition + TargetToWorld.MultiplyVector(correctedDirectionLocalSpace).normalized * Distance;
                }

                float lerp = SpringAnimation.GetPosition(percentDone);

                Follower.position = SampleCurve(startPosition, startTangentPosition, endPosition, endPosition, lerp);

                // Only update rotation when moving quickly
                if (firstFrame || Mathf.Abs(SpringAnimation.GetVelocity(percentDone)) > MinUpdateRotationVelocity)
                {
                    Quaternion endRotation = Quaternion.LookRotation((Follower.position - TargetPosition).normalized, Vector3.up);
                    Follower.rotation = SampleCurve(startRotation, Follower.rotation, endRotation, endRotation, lerp);
                }

                previousEndPosition = newEndPositionWorldSpace;
            }

            /// <summary>
            /// Slerps along a quaternion-equivalent bezier curve defined by q0, q1, q2, and q3.
            /// </summary>
            private Quaternion SampleCurve(Quaternion q0, Quaternion q1, Quaternion q2, Quaternion q3, float t)
            {
                Quaternion worldToTarget = WorldToTarget.rotation;

                q0 *= worldToTarget;
                q1 *= worldToTarget;
                q2 *= worldToTarget;
                q3 *= worldToTarget;

                Quaternion a = Quaternion.SlerpUnclamped(q0, q1, t);
                Quaternion b = Quaternion.SlerpUnclamped(q1, q2, t);
                Quaternion c = Quaternion.SlerpUnclamped(q2, q3, t);
                Quaternion d = Quaternion.SlerpUnclamped(a, b, t);
                Quaternion e = Quaternion.SlerpUnclamped(b, c, t);
                Quaternion localRotation = Quaternion.SlerpUnclamped(d, e, t);

                return localRotation * TargetToWorld.rotation;
            }

            /// <summary>
            /// Slerps along a bezier curve defined by p0, p1, p2, and p3.
            /// </summary>
            private Vector3 SampleCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                p0 = WorldToTarget.MultiplyPoint(p0);
                p1 = WorldToTarget.MultiplyPoint(p1);
                p2 = WorldToTarget.MultiplyPoint(p2);
                p3 = WorldToTarget.MultiplyPoint(p3);

                Vector3 a = Vector3.SlerpUnclamped(p0, p1, t);
                Vector3 b = Vector3.SlerpUnclamped(p1, p2, t);
                Vector3 c = Vector3.SlerpUnclamped(p2, p3, t);
                Vector3 d = Vector3.SlerpUnclamped(a, b, t);
                Vector3 e = Vector3.SlerpUnclamped(b, c, t);
                Vector3 localPosition = Vector3.SlerpUnclamped(d, e, t);

                return TargetToWorld.MultiplyPoint(localPosition);
            }

            private void EnsureInitialized()
            {
                if (initialized)
                {
                    return;
                }

                startRotation = Follower.rotation;

                startPosition = Follower.position;

                endPosition = TargetPosition + Forward * Distance;
                previousEndPosition = endPosition;

                startTangentPosition = TargetPosition + (Follower.position - TargetPosition).normalized * Distance;

                Vector3 startDirection = WorldToTarget.MultiplyPoint(Follower.position);

                startDirections = new Vector2(Sign(AngleBetweenAroundAxis(startDirection, Vector3.forward, Vector3.up)),
                                              Sign(AngleBetweenAroundAxis(startDirection, Vector3.forward, Vector3.right)));

                initialized = true;
            }

            /// <summary>
            /// Given a "from" vector, a "to" vector, and a "rotation axis" vector
            /// returns the unsigned angle between from and to around the rotation axis
            /// https://forum.unity.com/threads/is-vector3-signedangle-working-as-intended.694105/
            /// </summary>
            /// <param name="to"></param>
            /// <param name="from"></param>
            /// <param name="axis"></param>
            /// <returns></returns>
            public static float AngleBetweenAroundAxis(Vector3 from, Vector3 to, Vector3 axis)
            {
                Vector3 right = Vector3.Cross(axis, from);
                from = Vector3.Cross(right, axis);

                return Mathf.Rad2Deg * Mathf.Atan2(Vector3.Dot(to, right), Vector3.Dot(to, from));
            }

            private float Sign(float f) => f < 0 ? -1 : f > 0 ? 1 : 0;
        }
    }
}
