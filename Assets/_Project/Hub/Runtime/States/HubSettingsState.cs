using Project.Core.Activity;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using Project.Hub.Settings;
using Project.Hub.Settings.Sequences;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Hub.States
{
    public sealed class HubSettingsState : IHubState
    {
        private readonly HubStateMachine _sm;

        private readonly ILocalizationService _loc;
        private readonly ISpeechService _speech;
        private readonly IRepeatService _repeat;
        private readonly IVisualModeService _visual;
        private readonly IVisualAssistService _va;

        private sealed class FolderFrame
        {
            public HubSettingsFolder Folder;
            public int IndexInParent;
        }

        private readonly Stack<FolderFrame> _stack = new();

        private List<HubSettingsItem> _items = new();
        private int _index = 0;

        private enum Mode
        {
            Browse,
            EditList,
            EditRange,
            ConfirmAction
        }

        private Mode _mode = Mode.Browse;

        private HubSettingsList _editingList;
        private IReadOnlyList<ListOption> _listOptions;
        private int _listIndex;

        private HubSettingsRange _editingRange;
        private float _rangeValue;

        private HubSettingsAction _pendingAction;

        private bool _isHelpPlaying;

        public string Name => "Hub.Settings";

        public HubSettingsState(HubStateMachine sm)
        {
            _sm = sm;
            _loc = Core.App.AppContext.Services.Resolve<ILocalizationService>();
            _speech = Core.App.AppContext.Services.Resolve<ISpeechService>();
            _repeat = Core.App.AppContext.Services.Resolve<IRepeatService>();
            _visual = Core.App.AppContext.Services.Resolve<IVisualModeService>();
            _va = Core.App.AppContext.Services.Resolve<IVisualAssistService>();
        }

        public void Enter()
        {
            BuildRoot();
            _index = ClampIndex(_index, _items);
            _mode = Mode.Browse;
            _isHelpPlaying = false;

            RefreshVa();
            PlayBrowsePrompt();
        }

        public void Exit() { }
        public void OnFocusGained() { RefreshVa(); PlayPrompt(); }
        public void OnRepeatRequested() { RefreshVa(); PlayPrompt(); }

        public void Handle(NavAction action)
        {
            switch (_mode)
            {
                case Mode.Browse:
                    HandleBrowse(action);
                    break;
                case Mode.EditList:
                    HandleEditList(action);
                    break;
                case Mode.EditRange:
                    HandleEditRange(action);
                    break;
                case Mode.ConfirmAction:
                    HandleConfirmAction(action);
                    break;
            }
        }

        private void RefreshVa()
        {
            _va?.SetHeaderKey("va.screen.settings");

            var sub = ResolveVaSubHeader();
            _va?.SetSubHeaderText(sub);

            _va?.SetIdleHintKey(ResolveHintKeyForCurrentMode(_sm.Settings.Current));

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
            if (_items == null || _items.Count == 0) return "";

            var it = _items[_index];
            if (it == null) return "";

            var text = ResolveItemDisplayText(it);

            if (_mode == Mode.EditList || _mode == Mode.EditRange || _mode == Mode.ConfirmAction)
                return _loc.Get("va.setting", text);

            return _loc.Get("va.current", text);
        }

        private static string ResolveHintKeyForCurrentMode(AppSettingsData settings)
        {
            var mode = ResolveEffectiveHintMode(settings);

            return mode == ControlHintMode.Touch
                ? "hint.settings.browse.touch"
                : "hint.settings.browse.keyboard";
        }

        private void HandleBrowse(NavAction action)
        {
            if (_items == null || _items.Count == 0)
                return;

            if (_isHelpPlaying && (action == NavAction.Next || action == NavAction.Previous || action == NavAction.Back || action == NavAction.Confirm))
            {
                _sm.UiAudio.CancelCurrent();
                _isHelpPlaying = false;
            }

            switch (action)
            {
                case NavAction.Next:
                    _index = (_index + 1) % _items.Count;

                    _va?.PulseListMove(VaListMoveDirection.Next);

                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Previous:
                    _index = (_index - 1 + _items.Count) % _items.Count;

                    _va?.PulseListMove(VaListMoveDirection.Previous);

                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Confirm:
                    ConfirmCurrent();
                    break;

                case NavAction.Back:
                    BackFromBrowse();
                    break;
            }
        }

        private void ConfirmCurrent()
        {
            var item = _items[_index];
            if (item == null) return;

            if (IsBackItem(item))
            {
                BackFromBrowse();
                return;
            }

            switch (item.Type)
            {
                case HubSettingsItemType.Folder:
                    EnterFolder((HubSettingsFolder)item);
                    break;

                case HubSettingsItemType.Toggle:
                    ToggleItem((HubSettingsToggle)item);
                    break;

                case HubSettingsItemType.List:
                    BeginEditList((HubSettingsList)item);
                    break;

                case HubSettingsItemType.Range:
                    BeginEditRange((HubSettingsRange)item);
                    break;

                case HubSettingsItemType.Action:
                    HandleActionConfirm((HubSettingsAction)item);
                    break;
            }
        }

        private void HandleActionConfirm(HubSettingsAction action)
        {
            if (action == null) return;

            if (IsControlsHelpItem(action))
            {
                PlayControlsHelpFor(action.LabelKey);
                return;
            }

            if (action.LabelKey == "settings.reset_defaults")
            {
                _pendingAction = action;
                _mode = Mode.ConfirmAction;

                _sm.UiAudio.CancelCurrent();
                RefreshVa();
                PlayConfirmActionPrompt(action);
                return;
            }

            action.Execute?.Invoke();

            SpeakSelectedThenQueueBrowsePrompt(
                selectedKey: "selected.value",
                selectedValueText: SafeGet(action.LabelKey),
                selectedNonInterruptible: true
            );

            RefreshVa();
        }

        private void BackFromBrowse()
        {
            if (_stack.Count > 0)
            {
                _va?.NotifyTransitioning();

                var frame = _stack.Pop();

                if (_stack.Count == 0)
                {
                    BuildRoot();
                }
                else
                {
                    var parent = _stack.Peek().Folder;
                    _items = parent.BuildChildren?.Invoke() ?? new List<HubSettingsItem>();
                    EnsureBackAtEnd();
                }

                _index = ClampIndex(frame.IndexInParent, _items);
                _mode = Mode.Browse;
                _isHelpPlaying = false;

                RefreshVa();
                PlayBrowsePrompt();
                return;
            }

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

        private void EnterFolder(HubSettingsFolder folder)
        {
            if (folder == null) return;

            _va?.NotifyTransitioning();

            _sm.UiAudio.PlayGated(
                UiAudioScope.Hub,
                "nav.to_settings_folder",
                stillTransitioning: () => _sm.Transitions.IsTransitioning,
                delaySeconds: 0.5f,
                priority: SpeechPriority.High,
                ResolveFolderName(folder)
            );

            _sm.Transitions.RunInstant(() =>
            {
                _stack.Push(new FolderFrame { Folder = folder, IndexInParent = _index });

                _items = folder.BuildChildren?.Invoke() ?? new List<HubSettingsItem>();
                EnsureBackAtEnd();

                _index = 0;
                _mode = Mode.Browse;
                _isHelpPlaying = false;
            });

            RefreshVa();
            PlayBrowsePrompt();
        }

        private void ToggleItem(HubSettingsToggle t)
        {
            if (t == null) return;

            if (t.LabelKey == "settings.visual_mode.enabled")
            {
                _sm.UiAudio.CancelCurrent();

                _visual.ToggleVisualAssist();
                _sm.Settings.SetVisualMode(_visual.Mode);

                bool enabled = _visual.Mode == VisualMode.VisualAssist;

                _sm.UiAudio.Play(
                    UiAudioScope.Hub,
                    ctx => ToggleVisualAssistSequence.Run(ctx, enabled),
                    SpeechPriority.High,
                    interruptible: false
                );

                RefreshVa();
                PlayBrowsePrompt();
                return;
            }

            bool now = !t.GetValue();
            t.SetValue(now);

            SpeakSelectedThenQueueBrowsePrompt(
                selectedKey: "selected.value",
                selectedValueText: ResolveToggleValueText(now),
                selectedNonInterruptible: true
            );

            RefreshVa();
        }

        private void BeginEditList(HubSettingsList list)
        {
            _editingList = list;
            _listOptions = list?.GetOptions?.Invoke();
            _listIndex = Math.Max(0, list != null ? list.GetIndex() : 0);

            _mode = Mode.EditList;

            RefreshVa();
            PlayEditPrompt();
        }

        private void HandleEditList(NavAction action)
        {
            if (_editingList == null || _listOptions == null || _listOptions.Count == 0)
            {
                _mode = Mode.Browse;
                _editingList = null;
                _listOptions = null;

                RefreshVa();
                PlayBrowsePrompt();
                return;
            }

            switch (action)
            {
                case NavAction.Next:
                    _listIndex = (_listIndex + 1) % _listOptions.Count;

                    RefreshVa();
                    PlayCurrentValue();
                    break;

                case NavAction.Previous:
                    _listIndex = (_listIndex - 1 + _listOptions.Count) % _listOptions.Count;

                    RefreshVa();
                    PlayCurrentValue();
                    break;

                case NavAction.Confirm:
                    _editingList.SetIndex?.Invoke(_listIndex);
                    string selectedText = ResolveListOptionText(_listOptions[_listIndex]);

                    _mode = Mode.Browse;
                    _editingList = null;
                    _listOptions = null;

                    SpeakSelectedThenQueueBrowsePrompt(
                        selectedKey: "selected.value",
                        selectedValueText: selectedText,
                        selectedNonInterruptible: true
                    );

                    RefreshVa();
                    break;

                case NavAction.Back:
                    _mode = Mode.Browse;
                    _editingList = null;
                    _listOptions = null;

                    RefreshVa();
                    PlayBrowsePrompt();
                    break;
            }
        }

        private void BeginEditRange(HubSettingsRange range)
        {
            _editingRange = range;
            _rangeValue = Clamp(range != null ? range.GetValue() : 0f, range?.Min ?? 0f, range?.Max ?? 1f);

            _mode = Mode.EditRange;

            RefreshVa();
            PlayEditPrompt();
        }

        private void HandleEditRange(NavAction action)
        {
            if (_editingRange == null)
            {
                _mode = Mode.Browse;
                RefreshVa();
                PlayBrowsePrompt();
                return;
            }

            switch (action)
            {
                case NavAction.Next:
                    _rangeValue = Clamp(_rangeValue + _editingRange.Step, _editingRange.Min, _editingRange.Max);
                    RefreshVa();
                    PlayCurrentValue();
                    break;

                case NavAction.Previous:
                    _rangeValue = Clamp(_rangeValue - _editingRange.Step, _editingRange.Min, _editingRange.Max);
                    RefreshVa();
                    PlayCurrentValue();
                    break;

                case NavAction.Confirm:
                    _editingRange.SetValue?.Invoke(_rangeValue);
                    string selectedText = FormatRangeValue(_editingRange, _rangeValue);

                    _mode = Mode.Browse;
                    _editingRange = null;

                    SpeakSelectedThenQueueBrowsePrompt(
                        selectedKey: "selected.value",
                        selectedValueText: selectedText,
                        selectedNonInterruptible: true
                    );

                    RefreshVa();
                    break;

                case NavAction.Back:
                    _mode = Mode.Browse;
                    _editingRange = null;

                    RefreshVa();
                    PlayBrowsePrompt();
                    break;
            }
        }

        private void HandleConfirmAction(NavAction action)
        {
            switch (action)
            {
                case NavAction.Confirm:
                    _sm.UiAudio.CancelCurrent();

                    _pendingAction?.Execute?.Invoke();
                    _pendingAction = null;
                    _mode = Mode.Browse;

                    BuildRoot();
                    _index = 0;

                    _sm.UiAudio.Play(
                        UiAudioScope.Hub,
                        ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.applied"),
                        SpeechPriority.High,
                        interruptible: false
                    );

                    RefreshVa();
                    PlayBrowsePrompt();
                    break;

                case NavAction.Back:
                    _sm.UiAudio.CancelCurrent();

                    _pendingAction = null;
                    _mode = Mode.Browse;

                    _sm.UiAudio.Play(
                        UiAudioScope.Hub,
                        ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.cancelled"),
                        SpeechPriority.High,
                        interruptible: false
                    );

                    RefreshVa();
                    PlayBrowsePrompt();
                    break;
            }
        }

        private void PlayPrompt()
        {
            switch (_mode)
            {
                case Mode.EditList:
                case Mode.EditRange:
                    PlayEditPrompt();
                    break;

                case Mode.ConfirmAction:
                    PlayConfirmActionPrompt(_pendingAction);
                    break;

                default:
                    PlayBrowsePrompt();
                    break;
            }
        }

        private void PlayBrowsePrompt()
        {
            var item = _items.Count > 0 ? _items[_index] : null;

            string currentKey = CurrentKeyForItem(item);
            string hintKey = ResolveBrowseHintKey(_sm.Settings.Current);
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
            var item = _items[_index];

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
            var item = _items[_index];

            string currentKey = "current.setting";
            string hintKey = _mode == Mode.EditList
                ? ResolveEditListHintKey(_sm.Settings.Current)
                : ResolveEditRangeHintKey(_sm.Settings.Current);

            string currentText = ResolveItemDisplayText(item);
            string valueText = GetCurrentValueText();
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
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "current.value", GetCurrentValueText()),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayConfirmActionPrompt(HubSettingsItem item)
        {
            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.confirm"),
                SpeechPriority.High,
                interruptible: false
            );

            string hintKey = ResolveConfirmActionHintKey(_sm.Settings.Current);
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

        private bool IsControlsHelpItem(HubSettingsAction action)
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

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, helpKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void BuildRoot()
        {
            _stack.Clear();

            var root = HubSettingsSchema.BuildRoot(
                data: _sm.Settings.Current,
                settings: _sm.Settings,
                loc: _loc,
                speech: _speech,
                repeat: _repeat,
                visual: _visual);

            _items = root ?? new List<HubSettingsItem>();
            EnsureBackAtEnd();
        }

        private void EnsureBackAtEnd()
        {
            if (_items == null || _items.Count == 0) return;

            int idx = _items.FindIndex(IsBackItem);
            if (idx >= 0 && idx != _items.Count - 1)
            {
                var back = _items[idx];
                _items.RemoveAt(idx);
                _items.Add(back);
            }
        }

        private bool IsBackItem(HubSettingsItem it) => it != null && it.LabelKey == "common.back";

        private static bool IsFolder(HubSettingsItem it) => it != null && it.Type == HubSettingsItemType.Folder;

        private static bool IsInformationalAction(HubSettingsItem it)
        {
            if (it == null) return false;
            if (it.Type != HubSettingsItemType.Action) return false;

            return it.LabelKey == "settings.controls_help.touch"
                || it.LabelKey == "settings.controls_help.keyboard"
                || it.LabelKey == "settings.controls_help.mouse";
        }

        private static bool IsRealSetting(HubSettingsItem it)
        {
            if (it == null) return false;

            if (it.Type == HubSettingsItemType.Toggle
                || it.Type == HubSettingsItemType.List
                || it.Type == HubSettingsItemType.Range)
                return true;

            if (it.Type == HubSettingsItemType.Action && it.LabelKey == "settings.reset_defaults")
                return true;

            return false;
        }

        private static string CurrentKeyForItem(HubSettingsItem it)
        {
            if (it == null) return "current.option";
            if (it.LabelKey == "common.back") return "current.option";
            if (IsFolder(it)) return "current.option";
            if (IsInformationalAction(it)) return "current.option";

            return IsRealSetting(it) ? "current.setting" : "current.option";
        }

        private string ResolveItemDisplayText(HubSettingsItem item)
        {
            if (item == null) return "Unknown";

            var label = SafeGet(item.LabelKey);

            switch (item.Type)
            {
                case HubSettingsItemType.Toggle:
                    {
                        var t = (HubSettingsToggle)item;
                        var v = ResolveToggleValueText(t.GetValue());
                        return $"{label}: {v}";
                    }
                case HubSettingsItemType.Range:
                    {
                        var r = (HubSettingsRange)item;
                        var v = FormatRangeValue(r, Clamp(r.GetValue(), r.Min, r.Max));
                        return $"{label}: {v}";
                    }
                case HubSettingsItemType.List:
                    {
                        var l = (HubSettingsList)item;
                        var opts = l.GetOptions?.Invoke();
                        if (opts == null || opts.Count == 0) return label;

                        var idx = ClampInt(l.GetIndex(), 0, opts.Count - 1);
                        return $"{label}: {ResolveListOptionText(opts[idx])}";
                    }
                default:
                    return label;
            }
        }

        private string ResolveFolderName(HubSettingsFolder folder)
            => folder == null ? "Folder" : SafeGet(folder.LabelKey);

        private string GetCurrentValueText()
        {
            if (_mode == Mode.EditList && _listOptions != null && _listOptions.Count > 0)
                return ResolveListOptionText(_listOptions[_listIndex]);

            if (_mode == Mode.EditRange && _editingRange != null)
                return FormatRangeValue(_editingRange, _rangeValue);

            return "—";
        }

        private string ResolveToggleValueText(bool value)
        {
            return value ? SafeGet("settings.on") : SafeGet("settings.off");
        }

        private string ResolveListOptionText(ListOption opt)
        {
            var s = SafeGet(opt.LabelKey);
            return string.IsNullOrWhiteSpace(s) || s == opt.LabelKey ? opt.Id : s;
        }

        private string FormatRangeValue(HubSettingsRange range, float value)
        {
            if (range.LabelKey == "settings.repeat.manual_delay"
                || range.LabelKey == "settings.repeat.auto_delay")
                return $"{(int)Math.Round(value)}s";

            if (range.LabelKey == "settings.sfx_volume")
                return $"{(int)Math.Round(value * 100f)}%";

            if (range.LabelKey == "settings.visual_mode.dimmer_strength")
                return $"{(int)Math.Round(value * 100f)}%";

            if (range.LabelKey == "settings.visual_mode.marquee_speed")
                return $"{value:0.0}x";

            return value.ToString("0.##");
        }

        private string ResolveDescriptionKey(HubSettingsItem item)
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

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        private static int ClampInt(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private static int ClampIndex(int index, List<HubSettingsItem> items)
        {
            if (items == null || items.Count == 0) return 0;
            if (index < 0) return 0;
            if (index >= items.Count) return items.Count - 1;
            return index;
        }

        private static ControlHintMode ResolveEffectiveHintMode(AppSettingsData settings)
        {
            var mode = settings.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();
            return mode;
        }

        private static string ResolveBrowseHintKey(AppSettingsData settings)
        {
            var mode = ResolveEffectiveHintMode(settings);
            return mode == ControlHintMode.Touch
                ? "hint.settings.browse.touch"
                : "hint.settings.browse.keyboard";
        }

        private static string ResolveEditListHintKey(AppSettingsData settings)
        {
            var mode = ResolveEffectiveHintMode(settings);
            return mode == ControlHintMode.Touch
                ? "hint.settings.edit_list.touch"
                : "hint.settings.edit_list.keyboard";
        }

        private static string ResolveEditRangeHintKey(AppSettingsData settings)
        {
            var mode = ResolveEffectiveHintMode(settings);
            return mode == ControlHintMode.Touch
                ? "hint.settings.edit_range.touch"
                : "hint.settings.edit_range.keyboard";
        }

        private static string ResolveConfirmActionHintKey(AppSettingsData settings)
        {
            var mode = ResolveEffectiveHintMode(settings);
            return mode == ControlHintMode.Touch
                ? "hint.settings.confirm_action.touch"
                : "hint.settings.confirm_action.keyboard";
        }
    }
}