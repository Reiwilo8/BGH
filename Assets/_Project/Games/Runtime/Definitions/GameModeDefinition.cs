using UnityEngine;

namespace Project.Games.Definitions
{
    public enum GameModeKind
    {
        Tutorial,
        Difficulty,
        Endless,
        Custom,
        Other
    }

    [System.Serializable]
    public sealed class GameModeDefinition
    {
        [Header("Identity")]
        public string modeId;
        public GameModeKind kind;

        [Header("Presentation (EN for now)")]
        public string displayName;
        [TextArea] public string description;

        [Header("Optional per-mode config (per-game)")]
        public ScriptableObject modeConfig;
    }
}