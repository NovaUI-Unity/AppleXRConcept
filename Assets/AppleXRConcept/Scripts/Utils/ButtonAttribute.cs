using System;

namespace NovaSamples.AppleXRConcept
{
    /// <summary>
    /// An attribute that adds a button to trigger the method from the editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ButtonAttribute : Attribute
    {
        public string label = null;

        public ButtonAttribute(string label)
        {
            this.label = label;
        }
    }
}