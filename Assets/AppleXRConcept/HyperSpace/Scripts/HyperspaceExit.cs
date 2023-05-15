using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    public class HyperspaceExit : MonoBehaviour
    {
        private const string CUTOFF_SHADER_PROP = "_Cutoff";

        public ParticleSystem ps = null;
        public Material trailMat = null;
        public AnimationCurve cutoffCurve = null;
        public bool playing = true;

        private void Update()
        {
            if (!ps.isPlaying)
            {
                return;
            }

            float percentageDone = ps.time / ps.main.duration;

            float cutoff = cutoffCurve.Evaluate(percentageDone);

            trailMat.SetFloat(CUTOFF_SHADER_PROP, Mathf.Clamp(cutoff, -1f, 1f));
        }
    }
}

