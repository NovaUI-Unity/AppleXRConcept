using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    public class HyperSpaceController : MonoBehaviour
    {
        public event Action OnAnimationEnded = null;

        private enum State
        {
            Default,
            EnteringHyperSpace,
            InHyperSpace,
            ExitingHyperSpace
        }

        public float InHyperSpaceDuration = 2f;
        public float OverlapDuration = 0.25f;
        public ParticleSystem EnterHyperSpace = null;
        public ParticleSystem InHyperSpace = null;
        public ParticleSystem ExitHyperSpace = null;
        public bool Loop = true;

        [NonSerialized]
        private State currentState = State.ExitingHyperSpace;
        private float waitStartTime = 0f;

        private bool keepGoing = true;

        private void OnEnable()
        {
            currentState = State.ExitingHyperSpace;
            waitStartTime = 0;
            keepGoing = true;

            if (ExitHyperSpace.TryGetComponent(out ParticleSystemStoppedListener listener))
            {
                listener.OnParticlesStopped += HandleExitHyperspaceStopped;
            }
        }

        private void OnDisable()
        {   
            if (ExitHyperSpace.TryGetComponent(out ParticleSystemStoppedListener listener))
            {
                listener.OnParticlesStopped -= HandleExitHyperspaceStopped;
            }

            EnterHyperSpace.Stop();
            InHyperSpace.Stop();
            ExitHyperSpace.Stop();
        }

        private void Update()
        {
            if (!keepGoing)
            {
                return;
            }

            switch (currentState)
            {
                case State.EnteringHyperSpace:
                    {
                        if ((Time.unscaledTime - waitStartTime) < (EnterHyperSpace.main.duration - OverlapDuration))
                        {
                            // Still playing
                            break;
                        }

                        // Done
                        PlayInHyperSpaceAnimation();
                        break;
                    }
                case State.InHyperSpace:
                    {
                        if ((Time.unscaledTime - waitStartTime) < InHyperSpaceDuration)
                        {
                            break;
                        }

                        // Exit hyperspace
                        PlayExitAnimation();
                        break;
                    }
                case State.ExitingHyperSpace:
                    {
                        if (ExitHyperSpace.isPlaying)
                        {
                            break;
                        }

                        PlayEnterAnimation();
                        break;
                    }
            }
        }

        private void HandleExitHyperspaceStopped()
        {
            OnAnimationEnded?.Invoke();
        }

        [Button("Enter")]
        public void PlayEnterAnimation()
        {
            ResetPosition();
            EnterHyperSpace.gameObject.SetActive(true);
            EnterHyperSpace.Play();
            currentState = State.EnteringHyperSpace;
            waitStartTime = Time.unscaledTime;
        }

        [Button("Flying")]
        public void PlayInHyperSpaceAnimation()
        {
            InHyperSpace.gameObject.SetActive(true);
            InHyperSpace.Play();
            currentState = State.InHyperSpace;
            waitStartTime = Time.unscaledTime;
        }

        [Button("Exit")]
        public void PlayExitAnimation()
        {
            InHyperSpace.Stop();
            ExitHyperSpace.gameObject.SetActive(true);
            ExitHyperSpace.Play();
            currentState = State.ExitingHyperSpace;
            keepGoing = Loop;
        }

        [Button("Reset")]
        private void ResetPosition()
        {
            Camera mainCam = Camera.main;
            transform.position = mainCam.transform.position;

            Vector3 camForward = Vector3.ProjectOnPlane(mainCam.transform.forward, Vector3.up);
            transform.forward = camForward;
        }
    }
}
