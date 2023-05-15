using Nova;
using System;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Displays the date and time, updated at a given cadence.
    /// </summary>
    public class DateTimeIndicator : MonoBehaviour
    {
        public float UpdateEverXSeconds = 60;
        [SerializeField]
        private Timer Visuals = default;

        private AnimationHandle timerHandle = default;

        private void OnEnable()
        {
            timerHandle = Visuals.Run(UpdateEverXSeconds);
        }

        private void OnDisable()
        {
            timerHandle.Cancel();
        }

        [Serializable]
        private struct Timer : IAnimation
        {
            public TextBlock Time;
            public TextBlock Date;

            public void Update(float percentDone) 
            {
                if (percentDone > 0)
                {
                    return;
                }

                DateTime local = DateTime.UtcNow.ToLocalTime();
                Time.Text = $"{local.ToString("h:mm tt")}";
                Date.Text = $"{local.ToString("ddd").ToUpper()} {local.ToString("MMM dd")}";
            }
        }
    }
}
