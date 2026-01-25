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
                        subHeaderFontSize = 28,
                        centerFontSizes = new[] { 38, 34, 30, 26 }
                    };

                case VisualAssistTextSizePreset.Medium:
                    return new VisualAssistTypographyProfile
                    {
                        headerFontSize = 42,
                        subHeaderFontSize = 34,
                        centerFontSizes = new[] { 48, 42, 36, 30, 26 }
                    };

                case VisualAssistTextSizePreset.Large:
                    return new VisualAssistTypographyProfile
                    {
                        headerFontSize = 48,
                        subHeaderFontSize = 38,
                        centerFontSizes = new[] { 56, 48, 42, 36, 30 }
                    };

                case VisualAssistTextSizePreset.ExtraLarge:
                    return new VisualAssistTypographyProfile
                    {
                        headerFontSize = 56,
                        subHeaderFontSize = 44,
                        centerFontSizes = new[] { 64, 56, 48, 42, 36 }
                    };

                default:
                    return new VisualAssistTypographyProfile();
            }
        }
    }
}