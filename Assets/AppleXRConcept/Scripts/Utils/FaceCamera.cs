using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// Face the attached transform towards the camera
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        private Camera cameraToFollow = null;
        public Camera Camera
        {
            get
            {
                if (cameraToFollow == null)
                {
                    cameraToFollow = Camera.main;
                }

                return cameraToFollow;
            }
        }

        void Update()
        {
            transform.rotation = Quaternion.LookRotation((transform.position - Camera.transform.position).normalized, Vector3.up);
        }
    }
}
