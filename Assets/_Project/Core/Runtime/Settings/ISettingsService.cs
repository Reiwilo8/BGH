namespace Project.Core.Settings
{
    public interface ISettingsService
    {
        AppSettingsData Current { get; }
        AppSettingsData Defaults { get; }

        void Load();
        void Save();

        void ResetToDefaults();

        void SetLanguage(string languageCode, bool userSelected);
        void SetVisualMode(Project.Core.Visual.VisualMode mode);

        void SetPreferredControlScheme(Project.Core.Input.ControlScheme scheme, bool userSelected);
    }
}