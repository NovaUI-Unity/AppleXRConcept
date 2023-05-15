using UnityEngine;

namespace NovaSamples.AppleXRConcept
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns a copy of the given color, <paramref name="c"/> with alpha set to <paramref name="alpha"/>. RGB values are unchanged.
        /// </summary>
        public static Color WithAlpha(this Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        /// <summary>
        /// Returns a copy of the given color, <paramref name="c"/> with alpha set to 0. RGB values are unchanged.
        /// </summary>
        public static Color Transparent(this Color c) => c.WithAlpha(0);
    }
}