using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    [ExecuteAlways]
    public class HandInsideVolumeEffect : NovaBehaviour
    {
        public Material HandMaterial = null;
        public bool UseHierarchyBoundsAsVolume = false;

        private static int ExtentsID = Shader.PropertyToID("_Extents");
        private static int MatrixID = Shader.PropertyToID("_WorldToLocal");

        private void Update()
        {
            if (HandMaterial == null)
            {
                return;
            }

            if (UseHierarchyBoundsAsVolume)
            {
                Bounds hierarchyBounds = UIBlock.HierarchyBounds;
                HandMaterial.SetVector(ExtentsID, hierarchyBounds.extents);
                HandMaterial.SetMatrix(MatrixID, (transform.localToWorldMatrix * Matrix4x4.Translate(hierarchyBounds.center)).inverse);
            }
            else
            {
                HandMaterial.SetVector(ExtentsID, UIBlock.CalculatedSize.Value * 0.5f);
                HandMaterial.SetMatrix(MatrixID, transform.worldToLocalMatrix);
            }
        }
    }
}
