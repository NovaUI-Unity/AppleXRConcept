using UnityEngine;
using UnityEngine.XR;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    public class VRRenderQuality : MonoBehaviour
    {
        private const float MinScale = 0.5f;
        private const float MaxScale = 2;

        [SerializeField]
        [Range(MinScale, MaxScale)]
        private float resolutionScale = 1.5f;

        public float ResolutionScale
        {
            get { return resolutionScale; }
            set
            {
                resolutionScale = Mathf.Clamp(value, MinScale, MaxScale);
                XRSettings.eyeTextureResolutionScale = resolutionScale;
            }
        }

        private void Awake()
        {
            XRSettings.eyeTextureResolutionScale = ResolutionScale;
        }

        private void OnValidate()
        {
            XRSettings.eyeTextureResolutionScale = ResolutionScale;
        }
    }
}
