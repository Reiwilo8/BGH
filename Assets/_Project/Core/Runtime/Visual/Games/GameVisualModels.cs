using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Visual.Games
{
    public enum GameVisualElementKind
    {
        Unknown = 0,
        Card = 10,
        Player = 20,
        Train = 30,
        Gauge = 40,
        Marker2D = 50,
        Text = 60
    }

    [Serializable]
    public readonly struct GameVisualElement
    {
        public readonly string Id;
        public readonly GameVisualElementKind Kind;

        public readonly int State;
        public readonly int Int0;
        public readonly int Int1;

        public readonly float Value01;
        public readonly float Value02;

        public readonly Vector2 Position01;
        public readonly string Text;

        public GameVisualElement(
            string id,
            GameVisualElementKind kind,
            int state = 0,
            int int0 = 0,
            int int1 = 0,
            float value01 = 0f,
            float value02 = 0f,
            Vector2 position01 = default,
            string text = "")
        {
            Id = id ?? "";
            Kind = kind;

            State = state;
            Int0 = int0;
            Int1 = int1;

            Value01 = value01;
            Value02 = value02;

            Position01 = position01;
            Text = text ?? "";
        }
    }

    [Serializable]
    public sealed class GameVisualState
    {
        public string GameId;
        public int Revision;

        public List<GameVisualElement> Elements = new List<GameVisualElement>();

        public GameVisualState() { }

        public GameVisualState(string gameId, int revision, List<GameVisualElement> elements)
        {
            GameId = gameId ?? "";
            Revision = revision;
            Elements = elements ?? new List<GameVisualElement>();
        }
    }

    [Serializable]
    public readonly struct GameVisualEvent
    {
        public readonly string Name;
        public readonly string TargetId;

        public readonly int Int0;
        public readonly int Int1;

        public readonly float Float0;
        public readonly float Float1;

        public readonly string Text;
        public readonly bool Flag;

        public GameVisualEvent(
            string name,
            string targetId = "",
            int int0 = 0,
            int int1 = 0,
            float float0 = 0f,
            float float1 = 0f,
            string text = "",
            bool flag = false)
        {
            Name = name ?? "";
            TargetId = targetId ?? "";

            Int0 = int0;
            Int1 = int1;

            Float0 = float0;
            Float1 = float1;

            Text = text ?? "";
            Flag = flag;
        }
    }
}