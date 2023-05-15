using Nova;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Treats a scroller's child items as "pages" and updates a
    /// corresponding page indicator based on the scrolled page in view. 
    /// </summary>
    /// <remarks>
    /// Assumes <see cref="Scroller"/> and <see cref="IndicatorRoot"/> 
    /// have an equal number of children and will map the <see cref="IndicatorRoot"/>
    /// child at a given index to the <see cref="Scroller"/>'s child at the same index.
    /// </remarks>
    public class HomescreenPager : MonoBehaviour
    {
        [Tooltip("The scroller whose items make up the \"pages\".")]
        public Scroller Scroller = null;
        [Tooltip("The direct parent of the page indicators.")]
        public UIBlock IndicatorRoot = null;
        [Tooltip("The alpha of the focused page indiactor.")]
        public float SelectedPageIndicatorAlpha = 1;
        [Tooltip("The alpha of the unfocused page indiactors.")]
        public float UnselectedPageIndicatorAlpha = 0.4f;
        [Tooltip("The percent the scroller must be dragged to snap to the next page.")]
        public float NextPageThresholdPercent = 0.2f;

        private int currentPageIndex = 0;
        private float amountScrolled = 0;

        private void OnEnable()
        {
            // Initialize default state
            Scroller.CancelScroll();

            UIBlock previousIndicator = IndicatorRoot.GetChild(currentPageIndex);
            previousIndicator.Color = previousIndicator.Color.WithAlpha(UnselectedPageIndicatorAlpha);

            currentPageIndex = 0;
            UIBlock currentIndicator = IndicatorRoot.GetChild(currentPageIndex);
            currentIndicator.Color = currentIndicator.Color.WithAlpha(SelectedPageIndicatorAlpha);
            
            Scroller.UIBlock.AutoLayout.Offset = 0;

            // Subscribe to gesture events
            Scroller.UIBlock.AddGestureHandler<Gesture.OnScroll>(HandleScroll, includeHierarchy: false);
            Scroller.UIBlock.AddGestureHandler<Gesture.OnPress>(HandleScrollPressed, includeHierarchy: false);
            Scroller.UIBlock.AddGestureHandler<Gesture.OnRelease>(HandleScrollReleased, includeHierarchy: false);
            Scroller.UIBlock.AddGestureHandler<Gesture.OnCancel>(HandleScrollCanceled, includeHierarchy: false);
        }

        private void OnDisable()
        {
            // Unsubscribe from gesture events
            Scroller.UIBlock.RemoveGestureHandler<Gesture.OnScroll>(HandleScroll);
            Scroller.UIBlock.RemoveGestureHandler<Gesture.OnPress>(HandleScrollPressed);
            Scroller.UIBlock.RemoveGestureHandler<Gesture.OnRelease>(HandleScrollReleased);
            Scroller.UIBlock.RemoveGestureHandler<Gesture.OnCancel>(HandleScrollCanceled);
        }

        private void HandleScrollPressed(Gesture.OnPress evt)
        {
            // Reset on press
            amountScrolled = 0;
        }

        private void HandleScroll(Gesture.OnScroll evt)
        {
            if (evt.ScrollType != ScrollType.Manual)
            {
                return;
            }

            // Update the total distance scrolled/dragged
            int axis = Scroller.UIBlock.AutoLayout.Axis.Index();
            amountScrolled += evt.ScrollDeltaLocalSpace[axis];
        }

        private void HandleScrollReleased(Gesture.OnRelease evt) => UpdatePageFromScrollAmount();

        private void HandleScrollCanceled(Gesture.OnCancel evt) => UpdatePageFromScrollAmount();

        /// <summary>
        /// Based on the accumulated amount scrolled, move to the "scrolled" page and update the page indicators accordingly.
        /// </summary>
        private void UpdatePageFromScrollAmount()
        {
            if (amountScrolled == 0)
            {
                return;
            }

            int axis = Scroller.UIBlock.AutoLayout.Axis.Index();
            float percentScrolled = amountScrolled / Scroller.UIBlock.CalculatedSize[axis].Value;

            if (Mathf.Abs(percentScrolled) < NextPageThresholdPercent)
            {
                Scroller.ScrollToIndex(currentPageIndex);
                return;
            }

            UIBlock previousIndicator = IndicatorRoot.GetChild(currentPageIndex);
            previousIndicator.Color = previousIndicator.Color.WithAlpha(UnselectedPageIndicatorAlpha);

            currentPageIndex = Mathf.Clamp(currentPageIndex - (int)Mathf.Sign(percentScrolled), 0, Scroller.ScrollableChildCount - 1);

            UIBlock currentIndicator = IndicatorRoot.GetChild(currentPageIndex);
            currentIndicator.Color = currentIndicator.Color.WithAlpha(SelectedPageIndicatorAlpha);

            Scroller.ScrollToIndex(currentPageIndex);

            amountScrolled = 0;
        }
    }
}
