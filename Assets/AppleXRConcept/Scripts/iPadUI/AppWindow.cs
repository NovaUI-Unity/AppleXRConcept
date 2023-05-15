using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// A simple container for a few components we'll use to animate an 2D app window
    /// </summary>
    public class AppWindow : MonoBehaviour
    {
        [Tooltip("The bounding box effect to fade in/out when the window first comes into view.")]
        public BoundingBoxEffect BoundingBox = null;
        [Tooltip("The clip mask to fade in/out to animate the window content.")]
        public ClipMask ClipMask = null;
        [Tooltip("The homebar visual to fade in when the particular app window is \"focused\".")]
        public UIBlock Homebar = null;
    }
}
