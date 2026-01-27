namespace Project.Core.Settings.Ui
{
    public readonly struct SettingsBuildContext
    {
        public readonly bool DeveloperMode;

        public SettingsBuildContext(bool developerMode = false)
        {
            DeveloperMode = developerMode;
        }

        public static SettingsBuildContext Default => new SettingsBuildContext(false);
    }
}