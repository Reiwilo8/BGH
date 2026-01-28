using System;
using System.Collections.Generic;

namespace Project.Core.Settings.Ui
{
    public enum SettingsItemType
    {
        Folder,
        Toggle,
        Action,
        List,
        Range
    }

    public abstract class SettingsItem
    {
        public readonly SettingsItemType Type;
        public readonly string LabelKey;
        public readonly string DescriptionKey;

        protected SettingsItem(SettingsItemType type, string labelKey, string descriptionKey = null)
        {
            Type = type;
            LabelKey = labelKey;
            DescriptionKey = descriptionKey;
        }
    }

    public sealed class SettingsFolder : SettingsItem
    {
        public readonly Func<List<SettingsItem>> BuildChildren;

        public SettingsFolder(string labelKey, Func<List<SettingsItem>> buildChildren, string descriptionKey = null)
            : base(SettingsItemType.Folder, labelKey, descriptionKey)
        {
            BuildChildren = buildChildren;
        }
    }

    public sealed class SettingsToggle : SettingsItem
    {
        public readonly Func<bool> GetValue;
        public readonly Action<bool> SetValue;

        public SettingsToggle(string labelKey, Func<bool> getValue, Action<bool> setValue, string descriptionKey = null)
            : base(SettingsItemType.Toggle, labelKey, descriptionKey)
        {
            GetValue = getValue;
            SetValue = setValue;
        }
    }

    public sealed class SettingsAction : SettingsItem
    {
        public readonly Action Execute;

        public SettingsAction(string labelKey, Action execute, string descriptionKey = null)
            : base(SettingsItemType.Action, labelKey, descriptionKey)
        {
            Execute = execute;
        }
    }

    public sealed class SettingsList : SettingsItem
    {
        public readonly Func<int> GetIndex;
        public readonly Action<int> SetIndex;
        public readonly Func<IReadOnlyList<SettingsListOption>> GetOptions;

        public SettingsList(
            string labelKey,
            Func<IReadOnlyList<SettingsListOption>> getOptions,
            Func<int> getIndex,
            Action<int> setIndex,
            string descriptionKey = null)
            : base(SettingsItemType.List, labelKey, descriptionKey)
        {
            GetOptions = getOptions;
            GetIndex = getIndex;
            SetIndex = setIndex;
        }
    }

    public sealed class SettingsRange : SettingsItem
    {
        private readonly float _min;
        private readonly float _max;

        private readonly Func<float> _minProvider;
        private readonly Func<float> _maxProvider;

        public float Min => _minProvider != null ? _minProvider() : _min;
        public float Max => _maxProvider != null ? _maxProvider() : _max;

        public readonly float Step;

        public readonly Func<float> GetValue;
        public readonly Action<float> SetValue;

        public SettingsRange(
            string labelKey,
            float min,
            float max,
            float step,
            Func<float> getValue,
            Action<float> setValue,
            string descriptionKey = null)
            : base(SettingsItemType.Range, labelKey, descriptionKey)
        {
            _min = min;
            _max = max;
            _minProvider = null;
            _maxProvider = null;

            Step = step;
            GetValue = getValue;
            SetValue = setValue;
        }

        public SettingsRange(
            string labelKey,
            Func<float> minProvider,
            Func<float> maxProvider,
            float step,
            Func<float> getValue,
            Action<float> setValue,
            string descriptionKey = null)
            : base(SettingsItemType.Range, labelKey, descriptionKey)
        {
            _min = 0f;
            _max = 0f;
            _minProvider = minProvider;
            _maxProvider = maxProvider;

            Step = step;
            GetValue = getValue;
            SetValue = setValue;
        }
    }

    public readonly struct SettingsListOption
    {
        public readonly string Id;
        public readonly string LabelKey;

        public SettingsListOption(string id, string labelKey)
        {
            Id = id;
            LabelKey = labelKey;
        }
    }
}