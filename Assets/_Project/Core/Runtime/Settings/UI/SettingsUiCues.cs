using Project.Core.AudioFx;
using Project.Core.Input;

namespace Project.Core.Settings.Ui
{
    public static class SettingsUiCues
    {
        public static UiCueId ForNavAction(NavAction a, bool isBackItem)
        {
            return a switch
            {
                NavAction.Next => UiCueId.NavigateNext,
                NavAction.Previous => UiCueId.NavigatePrevious,
                NavAction.Back => UiCueId.Back,
                NavAction.Confirm => isBackItem ? UiCueId.Back : UiCueId.Confirm,
                _ => UiCueId.Error
            };
        }

        public static UiCueId ForValueChange(SettingsItemType type, NavAction a)
        {
            return type switch
            {
                SettingsItemType.Toggle => UiCueId.Toggle,

                SettingsItemType.List => a switch
                {
                    NavAction.Next => UiCueId.NavigateNext,
                    NavAction.Previous => UiCueId.NavigatePrevious,
                    NavAction.Back => UiCueId.Back,
                    NavAction.Confirm => UiCueId.Confirm,
                    _ => UiCueId.Error
                },

                SettingsItemType.Range => a switch
                {
                    NavAction.Next => UiCueId.Increase,
                    NavAction.Previous => UiCueId.Decrease,
                    NavAction.Back => UiCueId.Back,
                    NavAction.Confirm => UiCueId.Confirm,
                    _ => UiCueId.Error
                },

                _ => UiCueId.Error
            };
        }
    }
}