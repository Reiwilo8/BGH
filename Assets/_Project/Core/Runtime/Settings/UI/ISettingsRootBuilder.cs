using System.Collections.Generic;

namespace Project.Core.Settings.Ui
{
    public interface ISettingsRootBuilder
    {
        List<SettingsItem> BuildRoot(SettingsBuildContext context);
    }
}