using Project.Core.Activity;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Settings.Ui;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using Project.Hub.Settings;
using Project.Hub.Settings.Sequences;
using System.Collections;
using UnityEngine;

namespace Project.Hub.States
{
    public sealed class HubSettingsState : IHubState
    {
        private readonly HubStateMachine _sm;

        private readonly ILocalizationService _loc;
        private readonly IAudioFxService _audioFx;
        private readonly IRepeatService _repeat;
        private readonly IVisualModeService _visual;
        private readonly IVisualAssistService _va;

        private readonly SettingsUiSession _ui;

        private bool _isHelpPlaying;

        private bool _developerModeEnabled;

        public string Name => "Hub.Settings";

        public HubSettingsState(HubStateMachine sm)
        {
            _sm = sm;

            _loc = Core.App.AppContext.Services.Resolve<ILocalizationService>();
            _audioFx = Core.App.AppContext.Services.Resolve<IAudioFxService>();
            _repeat = Core.App.AppContext.Services.Resolve<IRepeatService>();
            _visual = Core.App.AppContext.Services.Resolve<IVisualModeService>();
            _va = Core.App.AppContext.Services.Resolve<IVisualAssistService>();

            var rootBuilder = new HubSettingsRootBuilder(
                _sm.Settings,
                _loc,
                Core.App.AppContext.Services.Resolve<ISpeechService>(),
                _repeat,
                _visual
            );

            _ui = new SettingsUiSession(rootBuilder);
        }

        public void Enter()
        {
            _developerModeEnabled = false;

            _ui.SetBuildContext(new SettingsBuildContext(developerMode: false));
            _ui.Enter();

            _isHelpPlaying = false;

            RefreshVa();
            PlayBrowsePrompt();
        }

        public void Exit() { }

        public void OnFocusGained()
        {
            RefreshVa();
            PlayPrompt();
        }

        public void OnRepeatRequested()
        {
            _audioFx?.PlayUiCue(UiCueId.Repeat);
            RefreshVa();
            PlayPrompt();
        }

        public void Handle(NavAction action)
        {
            if (action == NavAction.ToggleVisualAssist)
            {
                //ToggleDeveloperMode();
                return;
            }

            if (_isHelpPlaying && IsInterruptingAction(action))
            {
                _sm.UiAudio.CancelCurrent();
                _isHelpPlaying = false;
            }

            var modeBefore = _ui.Mode;
            string subBefore = ResolveVaSubHeader();

            PlayUiCueForAction(action);

            var result = _ui.Handle(action, new HubHooks(this));

            if (result.BackFromRoot)
            {
                ExitToMainMenu();
                return;
            }

            if (!result.HandledByHooks
                && result.ValueChanged
                && result.AffectedItemType == SettingsItemType.Toggle)
            {
                _audioFx?.PlayUiCue(UiCueId.Toggle);
            }

            string subAfter = ResolveVaSubHeader();
            TryPulseVaListMove(action, subBefore, subAfter);

            RefreshVa();

            if (result.HandledByHooks)
                return;

            if (modeBefore == SettingsUiMode.ConfirmAction
                && _ui.Mode != SettingsUiMode.ConfirmAction
                && !result.ConfirmedAction)
            {
                SpeakActionCancelledNonInterruptible();
                PlayBrowsePrompt();
                return;
            }

            HandleUiResult(action, result);
        }

        private void TryPulseVaListMove(NavAction action, string subBefore, string subAfter)
        {
            if (_va == null)
                return;

            if (action != NavAction.Next && action != NavAction.Previous)
                return;

            if (_ui.Mode == SettingsUiMode.EditRange)
                return;

            if (string.Equals(subBefore ?? "", subAfter ?? "", System.StringComparison.Ordinal))
                return;

            var dir = action == NavAction.Next ? VaListMoveDirection.Next : VaListMoveDirection.Previous;
            _va.PulseListMove(dir);
        }

        private void SpeakActionCancelledNonInterruptible()
        {
            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.cancelled"),
                SpeechPriority.High,
                interruptible: false
            );
        }

        private void ToggleDeveloperMode()
        {
            _developerModeEnabled = !_developerModeEnabled;

            _sm.UiAudio.CancelCurrent();
            _audioFx?.PlayUiCue(_developerModeEnabled ? UiCueId.DeveloperModeOn : UiCueId.DeveloperModeOff);

            string key = _developerModeEnabled
                ? "settings.dev_mode.enabled"
                : "settings.dev_mode.disabled";

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, key),
                SpeechPriority.Normal,
                interruptible: false
            );

            _ui.SetBuildContext(
                new SettingsBuildContext(developerMode: _developerModeEnabled)
            );

            _ui.Enter();
            _isHelpPlaying = false;

            RefreshVa();
            PlayBrowsePrompt();
        }

        private void HandleUiResult(NavAction action, SettingsUiResult result)
        {
            if (action == NavAction.Next || action == NavAction.Previous)
            {
                if (_ui.Mode == SettingsUiMode.EditList || _ui.Mode == SettingsUiMode.EditRange)
                    PlayCurrentValue();
                else
                    PlayCurrent();
                return;
            }

            if (action == NavAction.Back)
            {
                PlayPrompt();
                return;
            }

            if (action == NavAction.Confirm)
            {
                if (result.EnteredFolder || result.ExitedFolderOrRoot)
                {
                    PlayBrowsePrompt();
                    return;
                }

                if (result.StartedEdit)
                {
                    PlayEditPrompt();
                    return;
                }

                if (result.CommittedEdit)
                {
                    SpeakSelectedThenQueueBrowsePrompt(
                        selectedKey: "selected.value",
                        selectedValueText: ResolveCommittedValueText(result),
                        selectedNonInterruptible: true
                    );
                    return;
                }

                if (result.CancelledEdit)
                {
                    PlayBrowsePrompt();
                    return;
                }

                if (result.StartedConfirmAction)
                {
                    PlayConfirmActionPrompt(_ui.PendingAction);
                    return;
                }

                if (result.ConfirmedAction)
                {
                    _sm.UiAudio.Play(
                        UiAudioScope.Hub,
                        ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.applied"),
                        SpeechPriority.High,
                        interruptible: false
                    );

                    RefreshVa();
                    PlayBrowsePrompt();
                    return;
                }
            }

            PlayPrompt();
        }

        private void PlayUiCueForAction(NavAction action)
        {
            if (action == NavAction.ToggleVisualAssist)
                return;

            var isBackItem = _ui.CurrentItem != null && _ui.CurrentItem.LabelKey == "common.back";

            if (_ui.Mode == SettingsUiMode.EditRange)
            {
                _audioFx?.PlayUiCue(SettingsUiCues.ForValueChange(SettingsItemType.Range, action));
                return;
            }

            if (_ui.Mode == SettingsUiMode.EditList)
            {
                _audioFx?.PlayUiCue(SettingsUiCues.ForValueChange(SettingsItemType.List, action));
                return;
            }

            _audioFx?.PlayUiCue(SettingsUiCues.ForNavAction(action, isBackItem));
        }

        private static bool IsInterruptingAction(NavAction a)
            => a == NavAction.Next || a == NavAction.Previous || a == NavAction.Back || a == NavAction.Confirm;

        private void RefreshVa()
        {
            _va?.SetHeaderKey("va.screen.settings");

            var sub = ResolveVaSubHeader();
            _va?.SetSubHeaderText(sub);

            var hintKey = SettingsUiHintKeys.Resolve(
                _ui.Mode,
                ResolveEffectiveHintMode(_sm.Settings.Current));

            _va?.SetIdleHintKey(hintKey);

            ScheduleClearTransitioning();
        }

        private void ScheduleClearTransitioning()
        {
            if (_va == null) return;
            if (!_va.IsTransitioning) return;

            if (_sm.UiAudio is MonoBehaviour mb)
                mb.StartCoroutine(ClearTransitioningNextFrame());
            else
                _va.ClearTransitioning();
        }

        private IEnumerator ClearTransitioningNextFrame()
        {
            yield return null;
            _va?.ClearTransitioning();
        }

        private string ResolveVaSubHeader()
        {
            var it = _ui.CurrentItem;
            if (it == null) return "";

            var text = ResolveItemDisplayText(it);

            if (_ui.Mode == SettingsUiMode.EditList || _ui.Mode == SettingsUiMode.EditRange || _ui.Mode == SettingsUiMode.ConfirmAction)
                return _loc.Get("va.setting", text);

            return _loc.Get("va.current", text);
        }

        private void PlayPrompt()
        {
            switch (_ui.Mode)
            {
                case SettingsUiMode.EditList:
                case SettingsUiMode.EditRange:
                    PlayEditPrompt();
                    break;

                case SettingsUiMode.ConfirmAction:
                    PlayConfirmActionPrompt(_ui.PendingAction);
                    break;

                default:
                    PlayBrowsePrompt();
                    break;
            }
        }

        private void PlayBrowsePrompt()
        {
            var item = _ui.CurrentItem;

            string currentKey = CurrentKeyForItem(item);
            string hintKey = SettingsUiHintKeys.Resolve(
                SettingsUiMode.Browse,
                ResolveEffectiveHintMode(_sm.Settings.Current));

            string currentText = ResolveItemDisplayText(item);
            string descKey = ResolveDescriptionKey(item);

            _va?.SetIdleHintKey(hintKey);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => SettingsPromptSequence.Browse(ctx, currentKey, currentText, hintKey, descKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrent()
        {
            var item = _ui.CurrentItem;
            if (item == null) return;

            string currentKey = CurrentKeyForItem(item);
            string currentText = ResolveItemDisplayText(item);
            string descKey = ResolveDescriptionKey(item);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => SettingsPromptSequence.Current(ctx, currentKey, currentText, descKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayEditPrompt()
        {
            var item = _ui.CurrentItem;
            if (item == null) return;

            string currentKey = "current.setting";
            string hintKey = SettingsUiHintKeys.Resolve(
                _ui.Mode,
                ResolveEffectiveHintMode(_sm.Settings.Current));

            string currentText = ResolveItemDisplayText(item);
            string valueText = GetCurrentValueTextLive();
            string descKey = ResolveDescriptionKey(item);

            _va?.SetIdleHintKey(hintKey);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => SettingsPromptSequence.Edit(ctx, currentKey, currentText, valueText, hintKey, descKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrentValue()
        {
            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "current.value", GetCurrentValueTextLive()),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayConfirmActionPrompt(SettingsItem item)
        {
            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.confirm"),
                SpeechPriority.High,
                interruptible: false
            );

            string hintKey = SettingsUiHintKeys.Resolve(
                SettingsUiMode.ConfirmAction,
                ResolveEffectiveHintMode(_sm.Settings.Current));

            string currentKey = "current.setting";
            string currentText = item != null ? ResolveItemDisplayText(item) : "—";
            string descKey = item != null ? ResolveDescriptionKey(item) : null;

            _va?.SetIdleHintKey(hintKey);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => SettingsPromptSequence.ConfirmAction(ctx, currentKey, currentText, hintKey, descKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void SpeakSelectedThenQueueBrowsePrompt(string selectedKey, string selectedValueText, bool selectedNonInterruptible)
        {
            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, selectedKey, selectedValueText),
                SpeechPriority.High,
                interruptible: !selectedNonInterruptible
            );

            PlayBrowsePrompt();
        }

        private void ExitToMainMenu()
        {
            _sm.UiAudio.CancelCurrent();
            _va?.NotifyTransitioning();

            _sm.UiAudio.PlayGated(
                UiAudioScope.Hub,
                "exit.to_main_menu",
                stillTransitioning: () => _sm.Transitions.IsTransitioning,
                delaySeconds: 0.5f,
                priority: SpeechPriority.High
            );

            _sm.Transitions.RunInstant(() =>
            {
                _sm.SetState(new HubMainState(_sm));
            });
        }

        private bool IsControlsHelpAction(SettingsAction action)
        {
            if (action == null) return false;
            return action.LabelKey == "settings.controls_help.touch"
                || action.LabelKey == "settings.controls_help.keyboard"
                || action.LabelKey == "settings.controls_help.mouse";
        }

        private void PlayControlsHelpFor(string labelKey)
        {
            string helpKey = labelKey switch
            {
                "settings.controls_help.touch" => "hint.actions.touch",
                "settings.controls_help.keyboard" => "hint.actions.keyboard",
                "settings.controls_help.mouse" => "hint.actions.mouse",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(helpKey))
                return;

            _isHelpPlaying = true;

            _sm.UiAudio.CancelCurrent();

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, helpKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private string ResolveItemDisplayText(SettingsItem item)
        {
            if (item == null) return "Unknown";

            var label = SafeGet(item.LabelKey);

            switch (item.Type)
            {
                case SettingsItemType.Toggle:
                    {
                        var t = (SettingsToggle)item;
                        var v = ResolveToggleValueText(t.GetValue());
                        return $"{label}: {v}";
                    }
                case SettingsItemType.Range:
                    {
                        var r = (SettingsRange)item;
                        var v = FormatRangeValue(r, Clamp(r.GetValue(), r.Min, r.Max));
                        return $"{label}: {v}";
                    }
                case SettingsItemType.List:
                    {
                        var l = (SettingsList)item;
                        var opts = l.GetOptions?.Invoke();
                        if (opts == null || opts.Count == 0) return label;

                        var idx = ClampInt(l.GetIndex(), 0, opts.Count - 1);
                        return $"{label}: {ResolveListOptionText(opts[idx])}";
                    }
                default:
                    return label;
            }
        }

        private string GetCurrentValueTextLive()
        {
            if (_ui.Mode == SettingsUiMode.EditList && _ui.ListOptions != null && _ui.ListOptions.Count > 0)
                return ResolveListOptionText(_ui.ListOptions[_ui.ListIndex]);

            if (_ui.Mode == SettingsUiMode.EditRange && _ui.EditingRange != null)
                return FormatRangeValue(_ui.EditingRange, _ui.RangeValue);

            return "—";
        }

        private string ResolveCommittedValueText(SettingsUiResult r)
        {
            if (r.AffectedItemType == SettingsItemType.Toggle)
                return ResolveToggleValueText(r.ToggleValue);

            if (r.AffectedItem != null && (r.AffectedItemType == SettingsItemType.List || r.AffectedItemType == SettingsItemType.Range))
                return ResolveItemDisplayText(r.AffectedItem);

            return SafeGet("selected.value");
        }

        private string ResolveToggleValueText(bool value)
        {
            return value ? SafeGet("settings.on") : SafeGet("settings.off");
        }

        private string ResolveListOptionText(SettingsListOption opt)
        {
            var s = SafeGet(opt.LabelKey);
            return string.IsNullOrWhiteSpace(s) || s == opt.LabelKey ? opt.Id : s;
        }

        private string FormatRangeValue(SettingsRange range, float value)
        {
            if (range.LabelKey == "settings.cues_volume")
                return SettingsUiValueFormat.Percent01(value);

            if (range.LabelKey == "settings.game_volume")
                return SettingsUiValueFormat.Percent01(value);

            if (range.LabelKey == "settings.repeat.manual_delay"
                || range.LabelKey == "settings.repeat.auto_delay")
                return SettingsUiValueFormat.Seconds(value);

            if (range.LabelKey == "settings.visual_mode.dimmer_strength")
                return SettingsUiValueFormat.Percent01(value);

            if (range.LabelKey == "settings.visual_mode.marquee_speed")
                return SettingsUiValueFormat.Multiplier(value, decimals: 1);

            return SettingsUiValueFormat.Number(value, maxDecimals: 2);
        }

        private string ResolveDescriptionKey(SettingsItem item)
        {
            if (item == null) return null;
            if (item.LabelKey == "common.back") return null;
            return item.DescriptionKey;
        }

        private string SafeGet(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";
            var s = _loc.Get(key);
            return string.IsNullOrWhiteSpace(s) ? key : s;
        }

        private static bool IsFolder(SettingsItem it) => it != null && it.Type == SettingsItemType.Folder;

        private static bool IsInformationalAction(SettingsItem it)
        {
            if (it == null) return false;
            if (it.Type != SettingsItemType.Action) return false;

            return it.LabelKey == "settings.controls_help.touch"
                || it.LabelKey == "settings.controls_help.keyboard"
                || it.LabelKey == "settings.controls_help.mouse";
        }

        private static bool IsRealSetting(SettingsItem it)
        {
            if (it == null) return false;

            if (it.Type == SettingsItemType.Toggle
                || it.Type == SettingsItemType.List
                || it.Type == SettingsItemType.Range)
                return true;

            if (it.Type == SettingsItemType.Action && it.LabelKey == "settings.reset_defaults")
                return true;

            return false;
        }

        private static string CurrentKeyForItem(SettingsItem it)
        {
            if (it == null) return "current.option";
            if (it.LabelKey == "common.back") return "current.option";
            if (IsFolder(it)) return "current.option";
            if (IsInformationalAction(it)) return "current.option";

            return IsRealSetting(it) ? "current.setting" : "current.option";
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        private static int ClampInt(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private static ControlHintMode ResolveEffectiveHintMode(AppSettingsData settings)
        {
            var mode = settings.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();
            return mode;
        }

        private sealed class HubHooks : ISettingsUiHooks
        {
            private readonly HubSettingsState _s;

            public HubHooks(HubSettingsState s) { _s = s; }

            public bool RequiresConfirmation(SettingsAction action)
            {
                return action != null && action.LabelKey == "settings.reset_defaults";
            }

            public bool TryHandleToggle(SettingsToggle toggle)
            {
                if (toggle == null) return false;

                if (toggle.LabelKey == "settings.visual_mode.enabled")
                {
                    _s._sm.UiAudio.CancelCurrent();

                    _s._visual.ToggleVisualAssist();
                    _s._sm.Settings.SetVisualMode(_s._visual.Mode);

                    bool enabled = _s._visual.Mode == VisualMode.VisualAssist;

                    _s._sm.UiAudio.Play(
                        UiAudioScope.Hub,
                        ctx => ToggleVisualAssistSequence.Run(ctx, enabled),
                        SpeechPriority.Normal,
                        interruptible: true
                    );

                    _s.RefreshVa();
                    _s.PlayBrowsePrompt();
                    return true;
                }

                return false;
            }

            public bool TryHandleAction(SettingsAction action)
            {
                if (action == null) return false;

                if (_s.IsControlsHelpAction(action))
                {
                    _s.PlayControlsHelpFor(action.LabelKey);
                    return true;
                }

                return false;
            }
        }
    }
}