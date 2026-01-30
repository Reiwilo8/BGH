using System;
using System.Collections.Generic;
using Project.Core.Activity;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Settings.Ui;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.Hub.Settings
{
    public sealed class HubSettingsRootBuilder : ISettingsRootBuilder
    {
        private readonly ISettingsService _settings;
        private readonly ILocalizationService _loc;
        private readonly ISpeechService _speech;
        private readonly IRepeatService _repeat;
        private readonly IVisualModeService _visual;

        public HubSettingsRootBuilder(
            ISettingsService settings,
            ILocalizationService loc,
            ISpeechService speech,
            IRepeatService repeat,
            IVisualModeService visual)
        {
            _settings = settings;
            _loc = loc;
            _speech = speech;
            _repeat = repeat;
            _visual = visual;
        }

        public List<SettingsItem> BuildRoot(SettingsBuildContext context)
        {
            return new List<SettingsItem>
            {
                BuildLanguageItem(),

                BuildControlsFolder(),

                BuildAudioFolder(),

                BuildHapticsFolder(),

                BuildRepeatFolder(),

                BuildVisualModeFolder(),

                BuildResetAction(),

                new SettingsAction("common.back", execute: () => { })
            };
        }

        private SettingsList BuildLanguageItem()
        {
            return new SettingsList(
                labelKey: "settings.language",
                descriptionKey: "settings.language.desc",
                getOptions: () => new List<SettingsListOption>
                {
                    new SettingsListOption("en", "settings.lang.en"),
                    new SettingsListOption("pl", "settings.lang.pl")
                },
                getIndex: () =>
                {
                    var code = _settings.Current.languageCode ?? "en";
                    return string.Equals(code, "pl", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                },
                setIndex: idx =>
                {
                    var code = idx == 1 ? "pl" : "en";

                    _settings.SetLanguage(code, userSelected: true);

                    _loc.SetLanguage(code);
                    _speech.SetLanguage(code);
                }
            );
        }

        private SettingsFolder BuildVisualModeFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.visual_mode",
                descriptionKey: "settings.visual_mode.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    BuildVisualModeEnabledToggle(),

                    BuildVaTextSizeItem(),

                    BuildVaMarqueeSpeedItem(),

                    BuildVaDimmerStrengthItem(),

                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsToggle BuildVisualModeEnabledToggle()
        {
            return new SettingsToggle(
                labelKey: "settings.visual_mode.enabled",
                descriptionKey: "settings.visual_mode.enabled.desc",
                getValue: () => _settings.Current.visualMode == VisualMode.VisualAssist,
                setValue: enabled =>
                {
                    var mode = enabled ? VisualMode.VisualAssist : VisualMode.AudioOnly;
                    _settings.SetVisualMode(mode);
                    _visual.SetMode(mode);
                }
            );
        }

        private SettingsList BuildVaTextSizeItem()
        {
            return new SettingsList(
                labelKey: "settings.visual_mode.text_size",
                descriptionKey: "settings.visual_mode.text_size.desc",
                getOptions: () => new List<SettingsListOption>
                {
                    new SettingsListOption("small", "settings.small"),
                    new SettingsListOption("medium", "settings.medium"),
                    new SettingsListOption("large", "settings.large"),
                    new SettingsListOption("extra_large", "settings.extra_large"),
                },
                getIndex: () => _settings.Current.vaTextSizePreset switch
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

                    _settings.SetVaTextSizePreset(preset);
                }
            );
        }

        private SettingsRange BuildVaMarqueeSpeedItem()
        {
            return new SettingsRange(
                labelKey: "settings.visual_mode.marquee_speed",
                descriptionKey: "settings.visual_mode.marquee_speed.desc",
                min: 0.5f,
                max: 2.0f,
                step: 0.1f,
                getValue: () => _settings.Current.vaMarqueeSpeedScale,
                setValue: v => _settings.SetVaMarqueeSpeedScale(v)
            );
        }

        private SettingsRange BuildVaDimmerStrengthItem()
        {
            return new SettingsRange(
                labelKey: "settings.visual_mode.dimmer_strength",
                descriptionKey: "settings.visual_mode.dimmer_strength.desc",
                min: 0.0f,
                max: 1.0f,
                step: 0.1f,
                getValue: () => _settings.Current.vaDimmerStrength01,
                setValue: v => _settings.SetVaDimmerStrength01(v)
            );
        }

        private SettingsFolder BuildControlsFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.controls",
                descriptionKey: "settings.controls.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    BuildControlHintsItem(),
                    BuildControlsHelpFolder(),
                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsList BuildControlHintsItem()
        {
            return new SettingsList(
                labelKey: "settings.control_hints",
                descriptionKey: "settings.control_hints.desc",
                getOptions: () => new List<SettingsListOption>
                {
                    new SettingsListOption("auto", "settings.control_hints.auto"),
                    new SettingsListOption("touch", "settings.control_hints.touch"),
                    new SettingsListOption("keyboard", "settings.control_hints.keyboard"),
                },
                getIndex: () => _settings.Current.controlHintMode switch
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

                    _settings.SetControlHintMode(mode);
                }
            );
        }

        private static SettingsFolder BuildControlsHelpFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.controls_help",
                descriptionKey: "settings.controls_help.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    new SettingsAction("settings.controls_help.touch", execute: () => { }),
                    new SettingsAction("settings.controls_help.keyboard", execute: () => { }),
                    new SettingsAction("settings.controls_help.mouse", execute: () => { }),
                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsFolder BuildRepeatFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.repeat",
                descriptionKey: "settings.repeat.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    BuildRepeatManualDelayItem(),
                    BuildRepeatAutoEnabledItem(),
                    BuildRepeatAutoDelayItem(),

                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsRange BuildRepeatManualDelayItem()
        {
            return new SettingsRange(
                labelKey: "settings.repeat.manual_delay",
                descriptionKey: "settings.repeat.manual_delay.desc",
                min: 1f,
                max: 15f,
                step: 1f,
                getValue: () => _settings.Current.repeatIdleSeconds,
                setValue: v =>
                {
                    var clamped = Mathf.Clamp(v, 1f, 15f);
                    _settings.SetRepeatIdleSeconds(clamped);

                    _repeat.IdleThresholdSeconds = clamped;

                    float minAuto = Mathf.Max(10f, clamped);
                    if (_settings.Current.autoRepeatIdleSeconds < minAuto)
                        _settings.SetAutoRepeatIdleSeconds(minAuto);
                }
            );
        }

        private SettingsToggle BuildRepeatAutoEnabledItem()
        {
            return new SettingsToggle(
                labelKey: "settings.repeat.auto_enabled",
                descriptionKey: "settings.repeat.auto_enabled.desc",
                getValue: () => _settings.Current.autoRepeatEnabled,
                setValue: enabled => _settings.SetAutoRepeatEnabled(enabled)
            );
        }

        private SettingsRange BuildRepeatAutoDelayItem()
        {
            float MinAuto() => Mathf.Max(10f, _settings.Current.repeatIdleSeconds);
            float MaxAuto() => 30f;

            return new SettingsRange(
                labelKey: "settings.repeat.auto_delay",
                descriptionKey: "settings.repeat.auto_delay.desc",
                minProvider: MinAuto,
                maxProvider: MaxAuto,
                step: 1f,
                getValue: () => _settings.Current.autoRepeatIdleSeconds,
                setValue: v =>
                {
                    var min = MinAuto();
                    var max = MaxAuto();

                    var clamped = Mathf.Clamp(v, min, max);
                    _settings.SetAutoRepeatIdleSeconds(clamped);
                }
            );
        }

        private SettingsFolder BuildAudioFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.audio",
                descriptionKey: "settings.audio.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    new SettingsToggle(
                        labelKey: "settings.enable_cues",
                        descriptionKey: "settings.enable_cues.desc",
                        getValue: () => _settings.Current.cuesEnabled,
                        setValue: enabled => _settings.SetCuesEnabled(enabled)
                    ),

                    new SettingsRange(
                        labelKey: "settings.cues_volume",
                        descriptionKey: "settings.cues_volume.desc",
                        min: 0f,
                        max: 1f,
                        step: 0.05f,
                        getValue: () => _settings.Current.cuesVolume,
                        setValue: v => _settings.SetCuesVolume01(v)
                    ),

                    new SettingsRange(
                        labelKey: "settings.game_volume",
                        descriptionKey: "settings.game_volume.desc",
                        min: 0f,
                        max: 1f,
                        step: 0.05f,
                        getValue: () => _settings.Current.gameVolume,
                        setValue: v => _settings.SetGameVolume01(v)
                    ),

                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsFolder BuildHapticsFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.haptics",
                descriptionKey: "settings.haptics.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    new SettingsToggle(
                        labelKey: "settings.haptics.enabled",
                        descriptionKey: "settings.haptics.enabled.desc",
                        getValue: () => _settings.Current.hapticsEnabled,
                        setValue: enabled => _settings.SetHapticsEnabled(enabled)
                    ),

                    new SettingsRange(
                        labelKey: "settings.haptics.strength",
                        descriptionKey: "settings.haptics.strength.desc",
                        min: 0f,
                        max: 1f,
                        step: 0.1f,
                        getValue: () => _settings.Current.hapticsStrengthScale01,
                        setValue: v => _settings.SetHapticsStrengthScale01(v)
                    ),

                    new SettingsToggle(
                        labelKey: "settings.haptics.audio_fallback",
                        descriptionKey: "settings.haptics.audio_fallback.desc",
                        getValue: () => _settings.Current.hapticsAudioFallbackEnabled,
                        setValue: enabled => _settings.SetHapticsAudioFallbackEnabled(enabled)
                    ),

                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsAction BuildResetAction()
        {
            return new SettingsAction(
                labelKey: "settings.reset_defaults",
                descriptionKey: "settings.reset_defaults.desc",
                execute: () =>
                {
                    _settings.ResetToDefaults();

                    _loc.SetLanguage(_settings.Current.languageCode);
                    _speech.SetLanguage(_settings.Current.languageCode);

                    _visual.SetMode(_settings.Current.visualMode);
                    _repeat.IdleThresholdSeconds = Mathf.Clamp(_settings.Current.repeatIdleSeconds, 1f, 15f);
                }
            );
        }
    }
}