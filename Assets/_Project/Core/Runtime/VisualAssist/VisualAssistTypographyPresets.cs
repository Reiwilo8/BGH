namespace Project.Core.VisualAssist
{
    public static class VisualAssistTypographyPresets
    {
        public static VisualAssistTypographyProfile Get(VisualAssistTextSizePreset preset)
        {
            switch (preset)
            {
                case VisualAssistTextSizePreset.Small:
                    return new VisualAssistTypographyProfile
                    {
                        headerFontSize = 34,
                        headerToSubHeaderRatio = 1.22f,
                        centerFontSizes = new[] { 40, 36, 32, 28 }
                    };

                case VisualAssistTextSizePreset.Medium:
                    return new VisualAssistTypographyProfile
                    {
                        headerFontSize = 44,
                        headerToSubHeaderRatio = 1.22f,
                        centerFontSizes = new[] { 56, 50, 44, 38 }
                    };

                case VisualAssistTextSizePreset.Large:
                    return new VisualAssistTypographyProfile
                    {
                        headerFontSize = 58,
                        headerToSubHeaderRatio = 1.22f,
                        centerFontSizes = new[] { 76, 68, 60, 52 }
                    };

                case VisualAssistTextSizePreset.ExtraLarge:
                    return new VisualAssistTypographyProfile
                    {
                        headerFontSize = 74,
                        headerToSubHeaderRatio = 1.22f,
                        centerFontSizes = new[] { 98, 88, 78, 68 }
                    };

                default:
                    return new VisualAssistTypographyProfile();
            }
        }
    }
}