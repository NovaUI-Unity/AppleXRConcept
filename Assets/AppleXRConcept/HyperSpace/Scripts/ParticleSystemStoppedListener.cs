using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    public class ParticleSystemStoppedListener : MonoBehaviour
    {
        public event Action OnParticlesStopped = null;

        private void OnParticleSystemStopped()
        {
            OnParticlesStopped?.Invoke();
        }
    }
}
