using UnityEngine;
using UnityEngine.Video;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// A singleton-esque component used as an access point of active video playback.
    /// Enables synchronization of video playback UI across tablet mode, pip mode, 
    /// and full screen mode.
    /// </summary>
    public class SharedVideoPlayer : MonoBehaviour
    {
        [Tooltip("The video player to share.")]
        [SerializeField]
        private VideoPlayer videoPlayer = null;

        /// <summary>
        /// The shared video player.
        /// </summary>
        public VideoPlayer VideoPlayer => videoPlayer;

        /// <summary>
        /// The statically accessible instance.
        /// </summary>
        public static SharedVideoPlayer Instance { get; private set; }

        private void OnEnable()
        {
            if (Instance != null)
            {
                Debug.LogError($"Multiple {nameof(SharedVideoPlayer)}s exist in the scene, but there should only be one. Shared instance attached to ${Instance.name}. Remove all other instances.", this);
                return;
            }

            Instance = this;
        }
    }
}
