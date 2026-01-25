using UnityEngine;

namespace Project.Core.VisualAssist
{
    [System.Serializable]
    public sealed class VisualAssistTypographyProfile
    {
        [Header("Global layout")]
        [Range(0.1f, 0.3f)]
        public float topMaxHeightPercent = 0.20f;

        [Range(0.05f, 0.2f)]
        public float bottomReservedPercent = 0.12f;

        [Header("Header / SubHeader ratio")]
        [Tooltip("Header size relative to SubHeader (e.g. 1.2 = Header 20% bigger)")]
        [Range(1.0f, 1.4f)]
        public float headerToSubHeaderRatio = 1.2f;

        [Header("Header font size (base, before scaling)")]
        public int headerFontSize = 42;
        public int subHeaderFontSize = 34;

        [Header("Center font size presets (descending priority)")]
        public int[] centerFontSizes = new[]
        {
            48,
            42,
            36,
            30,
            26
        };

        [Header("Marquee tuning")]
        [Tooltip("Optional small boost for marquee text (applied only if readable)")]
        public int marqueeFontSizeBoost = 2;

        [Tooltip("Minimum visible characters required to allow marquee boost")]
        public int marqueeMinVisibleChars = 10;

        [Header("Vertical spacing")]
        public float headerVerticalPadding = 6f;
        public float subHeaderVerticalPadding = 4f;
    }
}