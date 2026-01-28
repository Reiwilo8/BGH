using System.Collections.Generic;
using Project.Core.Settings.Ui;

namespace Project.Games.Module.Settings
{
    public sealed class GameSettingsRootBuilder : ISettingsRootBuilder
    {
        public List<SettingsItem> BuildRoot(SettingsBuildContext context)
        {
            var root = new List<SettingsItem>();

            root.Add(new SettingsAction("common.back", execute: () => { }));
            return root;
        }
    }
}