using UnityEngine;

namespace NovaSamples.AppleXRConcept.VisionOS
{
    public class LaunchNovaSite : NovaBehaviour
    {
        public void LaunchSite()
        {
            Application.OpenURL("http://novaui.io");
        }
    }
}
