using Project.Core.Input;

namespace Project.Core.Settings.Ui
{
    public static class SettingsUiHintKeys
    {
        public static string Resolve(SettingsUiMode uiMode, ControlHintMode effectiveHintMode)
        {
            switch (uiMode)
            {
                case SettingsUiMode.EditList:
                    return effectiveHintMode == ControlHintMode.Touch
                        ? "hint.settings.edit_list.touch"
                        : "hint.settings.edit_list.keyboard";

                case SettingsUiMode.EditRange:
                    return effectiveHintMode == ControlHintMode.Touch
                        ? "hint.settings.edit_range.touch"
                        : "hint.settings.edit_range.keyboard";

                case SettingsUiMode.ConfirmAction:
                    return effectiveHintMode == ControlHintMode.Touch
                        ? "hint.settings.confirm_action.touch"
                        : "hint.settings.confirm_action.keyboard";

                default:
                    return effectiveHintMode == ControlHintMode.Touch
                        ? "hint.settings.browse.touch"
                        : "hint.settings.browse.keyboard";
            }
        }
    }
}