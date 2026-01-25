using System;
using System.Collections.Generic;
using Project.Core.Activity;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.Hub.Settings
{
    public enum HubSettingsItemType
    {
        Folder,
        Toggle,
        Action,
        List,
        Range
    }

    public abstract class HubSettingsItem
    {
        public readonly HubSettingsItemType Type;
        public readonly string LabelKey;
        public readonly string DescriptionKey;

        protected HubSettingsItem(HubSettingsItemType type, string labelKey, string descriptionKey = null)
        {
            Type = type;
            LabelKey = labelKey;
            DescriptionKey = descriptionKey;
        }
    }

    public sealed class HubSettingsFolder : HubSettingsItem
    {
        public readonly Func<List<HubSettingsItem>> BuildChildren;

        public HubSettingsFolder(string labelKey, Func<List<HubSettingsItem>> buildChildren, string descriptionKey = null)
            : base(HubSettingsItemType.Folder, labelKey, descriptionKey)
        {
            BuildChildren = buildChildren;
        }
    }

    public sealed class HubSettingsToggle : HubSettingsItem
    {
        public readonly Func<bool> GetValue;
        public readonly Action<bool> SetValue;

        public HubSettingsToggle(string labelKey, Func<bool> getValue, Action<bool> setValue, string descriptionKey = null)
            : base(HubSettingsItemType.Toggle, labelKey, descriptionKey)
        {
            GetValue = getValue;
            SetValue = setValue;
        }
    }

    public sealed class HubSettingsAction : HubSettingsItem
    {
        public readonly Action Execute;

        public HubSettingsAction(string labelKey, Action execute, string descriptionKey = null)
            : base(HubSettingsItemType.Action, labelKey, descriptionKey)
        {
            Execute = execute;
        }
    }

    public sealed class HubSettingsList : HubSettingsItem
    {
        public readonly Func<int> GetIndex;
        public readonly Action<int> SetIndex;
        public readonly Func<IReadOnlyList<ListOption>> GetOptions;

        public HubSettingsList(
            string labelKey,
            Func<IReadOnlyList<ListOption>> getOptions,
            Func<int> getIndex,
            Action<int> setIndex,
            string descriptionKey = null)
            : base(HubSettingsItemType.List, labelKey, descriptionKey)
        {
            GetOptions = getOptions;
            GetIndex = getIndex;
            SetIndex = setIndex;
        }
    }

    public sealed class HubSettingsRange : HubSettingsItem
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

        public HubSettingsRange(
            string labelKey,
            float min,
            float max,
            float step,
            Func<float> getValue,
            Action<float> setValue,
            string descriptionKey = null)
            : base(HubSettingsItemType.Range, labelKey, descriptionKey)
        {
            _min = min;
            _max = max;
            _minProvider = null;
            _maxProvider = null;

            Step = step;
            GetValue = getValue;
            SetValue = setValue;
        }

        public HubSettingsRange(
            string labelKey,
            Func<float> minProvider,
            Func<float> maxProvider,
            float step,
            Func<float> getValue,
            Action<float> setValue,
            string descriptionKey = null)
            : base(HubSettingsItemType.Range, labelKey, descriptionKey)
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

    public readonly struct ListOption
    {
        public readonly string Id;
        public readonly string LabelKey;

        public ListOption(string id, string labelKey)
        {
            Id = id;
            LabelKey = labelKey;
        }
    }

    public static class HubSettingsSchema
    {
        public static List<HubSettingsItem> BuildRoot(
            AppSettingsData data,
            ISettingsService settings,
            Project.Core.Localization.ILocalizationService loc,
            Project.Core.Speech.ISpeechService speech,
            IRepeatService repeat,
            IVisualModeService visual)
        {
            return new List<HubSettingsItem>
            {
                BuildLanguageItem(settings, loc, speech),

                BuildControlsFolder(settings),

                BuildAudioFolder(settings),

                BuildRepeatFolder(settings, repeat),

                BuildVisualModeFolder(settings, visual),

                BuildResetAction(settings, loc, speech, repeat, visual),

                new HubSettingsAction("common.back", execute: () => { })
            };
        }

        private static HubSettingsList BuildLanguageItem(
            ISettingsService settings,
            Project.Core.Localization.ILocalizationService loc,
            Project.Core.Speech.ISpeechService speech)
        {
            return new HubSettingsList(
                labelKey: "settings.language",
                descriptionKey: "settings.language.desc",
                getOptions: () =>
                {
                    return new List<ListOption>
                    {
                        new ListOption("en", "settings.lang.en"),
                        new ListOption("pl", "settings.lang.pl")
                    };
                },
                getIndex: () =>
                {
                    var code = settings.Current.languageCode ?? "en";
                    return string.Equals(code, "pl", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                },
                setIndex: idx =>
                {
                    var code = idx == 1 ? "pl" : "en";

                    settings.SetLanguage(code, userSelected: true);

                    loc.SetLanguage(code);
                    speech.SetLanguage(code);
                }
            );
        }

        private static HubSettingsFolder BuildVisualModeFolder(
            ISettingsService settings,
            IVisualModeService visual)
        {
            return new HubSettingsFolder(
                labelKey: "settings.visual_mode",
                descriptionKey: "settings.visual_mode.desc",
                buildChildren: () => new List<HubSettingsItem>
                {
                    BuildVisualModeEnabledToggle(settings, visual),

                    BuildVaTextSizeItem(settings),

                    BuildVaMarqueeSpeedItem(settings),

                    BuildVaDimmerStrengthItem(settings),

                    new HubSettingsAction("common.back", execute: () => { })
                }
            );
        }

        private static HubSettingsToggle BuildVisualModeEnabledToggle(
            ISettingsService settings,
            IVisualModeService visual)
        {
            return new HubSettingsToggle(
                labelKey: "settings.visual_mode.enabled",
                descriptionKey: "settings.visual_mode.enabled.desc",
                getValue: () => settings.Current.visualMode == VisualMode.VisualAssist,
                setValue: enabled =>
                {
                    var mode = enabled ? VisualMode.VisualAssist : VisualMode.AudioOnly;
                    settings.SetVisualMode(mode);
                    visual.SetMode(mode);
                }
            );
        }

        private static HubSettingsList BuildVaTextSizeItem(ISettingsService settings)
        {
            return new HubSettingsList(
                labelKey: "settings.visual_mode.text_size",
                descriptionKey: "settings.visual_mode.text_size.desc",
                getOptions: () => new List<ListOption>
                {
                    new ListOption("small", "settings.small"),
                    new ListOption("medium", "settings.medium"),
                    new ListOption("large", "settings.large"),
                    new ListOption("extra_large", "settings.extra_large"),
                },
                getIndex: () => settings.Current.vaTextSizePreset switch
                {
                    VisualAssistTextSizePreset.Small => 0,
                    VisualAssistTextSizePreset.Medium => 1,
                    VisualAssistTextSizePreset.Large => 2,
                    VisualAssistTextSizePreset.ExtraLarge => 3,
                    _ => 1
                },
                setIndex: idx =>
                {
                    var preset = idx switch
                    {
                        0 => VisualAssistTextSizePreset.Small,
                        2 => VisualAssistTextSizePreset.Large,
                        3 => VisualAssistTextSizePreset.ExtraLarge,
                        _ => VisualAssistTextSizePreset.Medium
                    };

                    settings.SetVaTextSizePreset(preset);
                }
            );
        }

        private static HubSettingsRange BuildVaMarqueeSpeedItem(ISettingsService settings)
        {
            return new HubSettingsRange(
                labelKey: "settings.visual_mode.marquee_speed",
                descriptionKey: "settings.visual_mode.marquee_speed.desc",
                min: 0.5f,
                max: 2.0f,
                step: 0.1f,
                getValue: () => settings.Current.vaMarqueeSpeedScale,
                setValue: v => settings.SetVaMarqueeSpeedScale(v)
            );
        }

        private static HubSettingsRange BuildVaDimmerStrengthItem(ISettingsService settings)
        {
            return new HubSettingsRange(
                labelKey: "settings.visual_mode.dimmer_strength",
                descriptionKey: "settings.visual_mode.dimmer_strength.desc",
                min: 0.0f,
                max: 1.0f,
                step: 0.1f,
                getValue: () => settings.Current.vaDimmerStrength01,
                setValue: v => settings.SetVaDimmerStrength01(v)
            );
        }

        private static HubSettingsFolder BuildControlsFolder(ISettingsService settings)
        {
            return new HubSettingsFolder(
                labelKey: "settings.controls",
                descriptionKey: "settings.controls.desc",
                buildChildren: () => new List<HubSettingsItem>
                {
                    BuildControlHintsItem(settings),
                    BuildControlsHelpFolder(),
                    new HubSettingsAction("common.back", execute: () => { })
                }
            );
        }

        private static HubSettingsList BuildControlHintsItem(ISettingsService settings)
        {
            return new HubSettingsList(
                labelKey: "settings.control_hints",
                descriptionKey: "settings.control_hints.desc",
                getOptions: () => new List<ListOption>
                {
                    new ListOption("auto", "settings.control_hints.auto"),
                    new ListOption("touch", "settings.control_hints.touch"),
                    new ListOption("keyboard", "settings.control_hints.keyboard"),
                },
                getIndex: () => settings.Current.controlHintMode switch
                {
                    ControlHintMode.Auto => 0,
                    ControlHintMode.Touch => 1,
                    ControlHintMode.KeyboardMouse => 2,
                    _ => 0
                },
                setIndex: idx =>
                {
                    var mode = idx switch
                    {
                        1 => ControlHintMode.Touch,
                        2 => ControlHintMode.KeyboardMouse,
                        _ => ControlHintMode.Auto
                    };

                    settings.SetControlHintMode(mode);
                }
            );
        }

        private static HubSettingsFolder BuildControlsHelpFolder()
        {
            return new HubSettingsFolder(
                labelKey: "settings.controls_help",
                descriptionKey: "settings.controls_help.desc",
                buildChildren: () => new List<HubSettingsItem>
                {
                    new HubSettingsAction("settings.controls_help.touch", execute: () => { }),
                    new HubSettingsAction("settings.controls_help.keyboard", execute: () => { }),
                    new HubSettingsAction("settings.controls_help.mouse", execute: () => { }),
                    new HubSettingsAction("common.back", execute: () => { })
                }
            );
        }

        private static HubSettingsFolder BuildRepeatFolder(ISettingsService settings, IRepeatService repeat)
        {
            return new HubSettingsFolder(
                labelKey: "settings.repeat",
                descriptionKey: "settings.repeat.desc",
                buildChildren: () => new List<HubSettingsItem>
                {
                    BuildRepeatManualDelayItem(settings, repeat),
                    BuildRepeatAutoEnabledItem(settings),
                    BuildRepeatAutoDelayItem(settings),

                    new HubSettingsAction("common.back", execute: () => { })
                }
            );
        }

        private static HubSettingsRange BuildRepeatManualDelayItem(ISettingsService settings, IRepeatService repeat)
        {
            return new HubSettingsRange(
                labelKey: "settings.repeat.manual_delay",
                descriptionKey: "settings.repeat.manual_delay.desc",
                min: 1f,
                max: 15f,
                step: 1f,
                getValue: () => settings.Current.repeatIdleSeconds,
                setValue: v =>
                {
                    var clamped = Mathf.Clamp(v, 1f, 15f);
                    settings.SetRepeatIdleSeconds(clamped);

                    repeat.IdleThresholdSeconds = clamped;

                    float minAuto = Mathf.Max(10f, clamped);
                    if (settings.Current.autoRepeatIdleSeconds < minAuto)
                        settings.SetAutoRepeatIdleSeconds(minAuto);
                }
            );
        }

        private static HubSettingsToggle BuildRepeatAutoEnabledItem(ISettingsService settings)
        {
            return new HubSettingsToggle(
                labelKey: "settings.repeat.auto_enabled",
                descriptionKey: "settings.repeat.auto_enabled.desc",
                getValue: () => settings.Current.autoRepeatEnabled,
                setValue: enabled =>
                {
                    settings.SetAutoRepeatEnabled(enabled);
                }
            );
        }

        private static HubSettingsRange BuildRepeatAutoDelayItem(ISettingsService settings)
        {
            float MinAuto() => Mathf.Max(10f, settings.Current.repeatIdleSeconds);
            float MaxAuto() => 30f;

            return new HubSettingsRange(
                labelKey: "settings.repeat.auto_delay",
                descriptionKey: "settings.repeat.auto_delay.desc",
                minProvider: MinAuto,
                maxProvider: MaxAuto,
                step: 1f,
                getValue: () => settings.Current.autoRepeatIdleSeconds,
                setValue: v =>
                {
                    var min = MinAuto();
                    var max = MaxAuto();

                    var clamped = Mathf.Clamp(v, min, max);
                    settings.SetAutoRepeatIdleSeconds(clamped);
                }
            );
        }

        private static HubSettingsFolder BuildAudioFolder(ISettingsService settings)
        {
            return new HubSettingsFolder(
                labelKey: "settings.audio",
                descriptionKey: "settings.audio.desc",
                buildChildren: () => new List<HubSettingsItem>
                {
                    new HubSettingsRange(
                        labelKey: "settings.sfx_volume",
                        descriptionKey: "settings.sfx_volume.desc",
                        min: 0f,
                        max: 1f,
                        step: 0.05f,
                        getValue: () => settings.Current.sfxVolume,
                        setValue: v => settings.SetSfxVolume01(v)
                    ),

                    new HubSettingsToggle(
                        labelKey: "settings.enable_cues",
                        descriptionKey: "settings.enable_cues.desc",
                        getValue: () => settings.Current.cuesEnabled,
                        setValue: enabled => settings.SetCuesEnabled(enabled)
                    ),

                    new HubSettingsAction("common.back", execute: () => { })
                }
            );
        }

        private static HubSettingsAction BuildResetAction(
            ISettingsService settings,
            Project.Core.Localization.ILocalizationService loc,
            Project.Core.Speech.ISpeechService speech,
            IRepeatService repeat,
            IVisualModeService visual)
        {
            return new HubSettingsAction(
                labelKey: "settings.reset_defaults",
                descriptionKey: "settings.reset_defaults.desc",
                execute: () =>
                {
                    settings.ResetToDefaults();

                    loc.SetLanguage(settings.Current.languageCode);
                    speech.SetLanguage(settings.Current.languageCode);

                    visual.SetMode(settings.Current.visualMode);
                    repeat.IdleThresholdSeconds = Mathf.Clamp(settings.Current.repeatIdleSeconds, 1f, 15f);
                }
            );
        }
    }
}