using Nova;
using System;
using UnityEngine;
using UnityEngine.Video;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Synchronizes the play/pause state and video timeline with the video player attached to <see cref="SharedVideoPlayer.Instance"/>.
    /// </summary>
    public class PlaybackControls : NovaBehaviour
    {
        private const string TimeFormat = @"m\:ss";

        [Header("Play/Pause Button")]
        [Tooltip("The visual root of the play/pause button.")]
        public UIBlock PlayPauseButton = null;
        [Tooltip("The visual to enable when the video is paused.")]
        public UIBlock PlayVisual = null;
        [Tooltip("The visual to enable when the video is playing.")]
        public UIBlock PauseVisual = null;

        [Header("Elapsed Time")]
        [Tooltip("The text to display the playing video duration. Can be null.")]
        public TextBlock VideoDurationText = null;
        [Tooltip("The text to display the current playback time of the playing video. Can be null.")]
        public TextBlock VideoElapseTimeText = null;
        [Tooltip("The UIBlock to resize to indicate the percent of video remaining/elapsed. Can be null.")]
        public UIBlock ElapsedTimeIndicator = null;

        private void OnEnable()
        {
            // Subscribe to play/pause button clicks.
            PlayPauseButton.AddGestureHandler<Gesture.OnClick>(HandlePlayPauseButtonClicked);
        }

        private void OnDisable()
        {
            // Unsubscribe from play/pause button clicks.
            PlayPauseButton.RemoveGestureHandler<Gesture.OnClick>(HandlePlayPauseButtonClicked);
        }

        private void Update()
        {
            // Update the play/pause visuals
            UpdatePlayPauseButton(toggle: false);

            SharedVideoPlayer shared = SharedVideoPlayer.Instance;

            if (shared == null || shared.VideoPlayer == null || shared.VideoPlayer.clip == null)
            {
                return;
            }

            VideoPlayer player = shared.VideoPlayer;

            double elapsedTime = player.time;
            double totalTime = player.clip.length;
            float percentDone = Mathf.Clamp01((float)(elapsedTime / totalTime));

            // Update the other playback indicators/visuals

            if (VideoDurationText != null)
            {
                VideoDurationText.Text = TimeSpan.FromSeconds(totalTime).ToString(TimeFormat);
            }

            if (VideoElapseTimeText != null)
            {
                VideoElapseTimeText.Text = TimeSpan.FromSeconds(elapsedTime).ToString(TimeFormat);
            }

            if (ElapsedTimeIndicator != null)
            {
                ElapsedTimeIndicator.Size.X = Length.Percentage(percentDone);
            }
        }

        /// <summary>
        /// Toggle play/pause state
        /// </summary>
        private void HandlePlayPauseButtonClicked(Gesture.OnClick evt) => UpdatePlayPauseButton(toggle:true);

        /// <summary>
        /// Sync the play/pause button visual state with the state of the <see cref="SharedVideoPlayer"/>. Toggle playback if desired.
        /// </summary>
        private void UpdatePlayPauseButton(bool toggle)
        {
            bool playing = PauseVisual.gameObject.activeSelf;
            SharedVideoPlayer shared = SharedVideoPlayer.Instance;

            if (shared != null || shared.VideoPlayer != null || shared.VideoPlayer.clip != null)
            {
                playing = shared.VideoPlayer.isPlaying;

                if (toggle)
                {
                    if (playing)
                    {
                        shared.VideoPlayer.Pause();
                    }
                    else
                    {
                        shared.VideoPlayer.Play();
                    }

                    playing = shared.VideoPlayer.isPlaying;
                }
            }

            PauseVisual.gameObject.SetActive(playing);
            PlayVisual.gameObject.SetActive(!playing);
        }
    }
}
