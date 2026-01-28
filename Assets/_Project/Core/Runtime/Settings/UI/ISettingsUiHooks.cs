namespace Project.Core.Settings.Ui
{
    public interface ISettingsUiHooks
    {
        bool RequiresConfirmation(SettingsAction action);

        bool TryHandleToggle(SettingsToggle toggle);

        bool TryHandleAction(SettingsAction action);
    }
}