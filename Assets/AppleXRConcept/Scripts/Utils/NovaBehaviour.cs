using Nova;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    [RequireComponent(typeof(UIBlock2D))]
    public abstract class NovaBehaviour2D : NovaBehaviour<UIBlock2D> { }

    [RequireComponent(typeof(UIBlock))]
    public abstract class NovaBehaviour<T> : MonoBehaviour where T : UIBlock
    {
        private T uiBlock = null;
        public T UIBlock
        {
            get
            {
                if (uiBlock == null)
                {
                    uiBlock = GetComponent<T>();
                }

                return uiBlock;
            }
        }
    }

    [RequireComponent(typeof(UIBlock))]
    public abstract class NovaBehaviour : MonoBehaviour
    {
        [System.NonSerialized]
        private UIBlock uiBlock = null;
        public UIBlock UIBlock
        {
            get
            {
                if (uiBlock == null)
                {
                    uiBlock = GetComponent<UIBlock>();
                }

                return uiBlock;
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(MonoBehaviour), editorForChildClasses: true), UnityEditor.CanEditMultipleObjects]
    public class NovaBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying)
            {
                return;
            }

            if (target is NovaBehaviour)
            {
                if (GUILayout.Button("Click"))
                {
                    NovaBehaviour nb = target as NovaBehaviour;
                    nb.UIBlock.FireGestureEvent(new Gesture.OnClick());
                }
            }

            IEnumerable<MethodInfo> methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.GetParameters().Length == 0);

            foreach (MethodInfo method in methods)
            {
                ButtonAttribute buttonAttribute = Attribute.GetCustomAttribute(method, typeof(ButtonAttribute)) as ButtonAttribute;

                if (buttonAttribute != null)
                {
                    if (GUILayout.Button(buttonAttribute.label) == true)
                    {
                        foreach (UnityEngine.Object methodTarget in targets)
                            method.Invoke(methodTarget, null);
                    }
                }
            }
        }
    }
#endif
}