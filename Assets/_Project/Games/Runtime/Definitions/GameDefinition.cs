using UnityEngine;

namespace Project.Games.Definitions
{
    [CreateAssetMenu(menuName = "Project/Games/Game Definition")]
    public sealed class GameDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string gameId;

        [Header("Localization")]
        [Tooltip("String Table Collection name, e.g. 'Game_SteamRush'.")]
        public string localizationTable;

        [Tooltip("Key inside game's table. Recommended: 'name'.")]
        public string nameKey = "name";

        [Tooltip("Key inside game's table. Recommended: 'description'.")]
        public string descriptionKey = "description";

        [Header("Presentation (legacy fallback, EN)")]
        [Tooltip("Used only if localization key/table is missing.")]
        public string displayName;

        [TextArea]
        [Tooltip("Used only if localization key/table is missing.")]
        public string description;

        [Header("Scenes")]
        public string gameplaySceneName;

        [Header("Modes")]
        public GameModeDefinition[] modes;

        public GameModeDefinition GetMode(string modeId)
        {
            if (modes == null) return null;
            foreach (var m in modes)
                if (m != null && m.modeId == modeId) return m;
            return null;
        }
    }
}