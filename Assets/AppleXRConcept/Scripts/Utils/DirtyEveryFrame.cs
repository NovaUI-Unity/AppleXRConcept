namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Exists because we discovered a layout bug while making this example that we need to go investigate and fix.
    /// </summary>
    public class DirtyEveryFrame : NovaBehaviour
    {
        void Update()
        {
            UIBlock.CalculateLayout();
        }
    }
}
