using UnityEngine;

namespace Project.Games.Definitions
{
    [CreateAssetMenu(menuName = "Project/Games/Game Definition")]
    public sealed class GameDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string gameId;

        [Header("Presentation (EN for now)")]
        public string displayName;
        [TextArea] public string description;

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