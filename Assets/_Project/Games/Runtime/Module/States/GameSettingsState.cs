using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Settings.Ui;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Games.Catalog;
using Project.Games.Definitions;
using Project.Games.Localization;
using Project.Games.Module.Settings;
using Project.Games.Module.Settings.Sequences;
using UnityEngine;

namespace Project.Games.Module.States
{
    public sealed class GameSettingsState : IGameModuleState
    {
        private readonly GameModuleStateMachine _sm;

        private readonly ILocalizationService _loc;
        private readonly IAudioFxService _audioFx;
        private readonly IVisualAssistService _va;

        private readonly AppSession _session;
        private readonly GameCatalog _catalog;

        private readonly SettingsUiSession _ui;
        private readonly ISettingsUiHooks _hooks;

        private bool _developerModeEnabled;
        private GameDefinition _game;

        public string Name => "GameModule.Settings";

        public GameSettingsState(GameModuleStateMachine sm)
        {
            _sm = sm;

            var services = AppContext.Services;

            _loc = services.Resolve<ILocalizationService>();
            _audioFx = services.Resolve<IAudioFxService>();
            _va = services.Resolve<IVisualAssistService>();

            _session = services.Resolve<AppSession>();
            _catalog = services.Resolve<GameCatalog>();

            var rootBuilder = new GameSettingsRootBuilder();
            _ui = new SettingsUiSession(rootBuilder);

            _hooks = new GameSettingsHooks();
        }

        public void Enter()
        {
            if (!LoadSelectedGameOrFail())
                return;

            _developerModeEnabled = false;

            _ui.SetBuildContext(new SettingsBuildContext(developerMode: false));
            _ui.Enter();

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
            RefreshVa();
            PlayPrompt();
        }

        public void Handle(NavAction action)
        {
            if (action == NavAction.ToggleVisualAssist)
            {
                ToggleDeveloperMode();
                return;
            }

            var modeBefore = _ui.Mode;
            string subBefore = ResolveVaSubHeader();

            PlayUiCueForAction(action);

            var result = _ui.Handle(action, hooks: _hooks);

            if (result.BackFromRoot)
            {
                ExitToGameMenu();
                return;
            }

            string subAfter = ResolveVaSubHeader();
            TryPulseVaListMove(action, subBefore, subAfter);

            RefreshVa();

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

        private void ToggleDeveloperMode()
        {
            _developerModeEnabled = !_developerModeEnabled;

            _sm.UiAudio.CancelCurrent();

            _audioFx?.PlayUiCue(_developerModeEnabled ? UiCueId.DeveloperModeOn : UiCueId.DeveloperModeOff);

            string key = _developerModeEnabled
                ? "settings.dev_mode.enabled"
                : "settings.dev_mode.disabled";

            _sm.UiAudio.Play(
                UiAudioScope.GameModule,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, key),
                SpeechPriority.Normal,
                interruptible: false
            );

            _ui.SetBuildContext(new SettingsBuildContext(developerMode: _developerModeEnabled));
            _ui.Enter();

            RefreshVa();
            PlayBrowsePrompt();
        }

        private bool LoadSelectedGameOrFail()
        {
            if (string.IsNullOrWhiteSpace(_session.SelectedGameId))
            {
                _ = _sm.Flow.ReturnToHubAsync();
                return false;
            }

            _game = _catalog.GetById(_session.SelectedGameId);
            if (_game == null)
            {
                _ = _sm.Flow.ReturnToHubAsync();
                return false;
            }

            return true;
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
                    _sm.UiAudio.Play(
                        UiAudioScope.GameModule,
                        ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "selected.value", ResolveCommittedValueText(result)),
                        SpeechPriority.High,
                        interruptible: false
                    );

                    RefreshVa();
                    PlayBrowsePrompt();
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
                        UiAudioScope.GameModule,
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

        private void SpeakActionCancelledNonInterruptible()
        {
            _sm.UiAudio.Play(
                UiAudioScope.GameModule,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.cancelled"),
                SpeechPriority.High,
                interruptible: false
            );
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

        private void RefreshVa()
        {
            if (_va == null) return;

            _va.SetHeaderKey("va.screen.game_settings", GameLocalization.GetGameName(_loc, _game));

            var sub = ResolveVaSubHeader();
            _va.SetSubHeaderText(sub);

            var hintKey = SettingsUiHintKeys.Resolve(
                _ui.Mode,
                ResolveEffectiveHintMode(_sm.Settings.Current));

            _va.SetIdleHintKey(hintKey);

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

        private System.Collections.IEnumerator ClearTransitioningNextFrame()
        {
            yield return null;
            _va?.ClearTransitioning();
        }

        private string ResolveVaSubHeader()
        {
            var it = _ui.CurrentItem;
            if (it == null) return "";

            var text = ResolveItemDisplayText(it);

            if (_ui.Mode == SettingsUiMode.EditList
                || _ui.Mode == SettingsUiMode.EditRange
                || _ui.Mode == SettingsUiMode.ConfirmAction)
                return GetCore("va.setting", text);

            return GetCore("va.current", text);
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
            if (item == null) return;

            string currentKey = CurrentKeyForItem(item);

            string hintKey = SettingsUiHintKeys.Resolve(
                SettingsUiMode.Browse,
                ResolveEffectiveHintMode(_sm.Settings.Current));

            string currentText = ResolveItemDisplayText(item);
            string descKey = ResolveDescriptionKey(item);

            _va?.SetIdleHintKey(hintKey);

            _sm.UiAudio.Play(
                UiAudioScope.GameModule,
                ctx => GameSettingsPromptSequence.Browse(
                    ctx,
                    GameLocalization.GetGameName(ctx.Localization, _game),
                    currentKey,
                    currentText,
                    hintKey,
                    descKey,
                    _game != null ? _game.localizationTable : null),
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
                UiAudioScope.GameModule,
                ctx => GameSettingsPromptSequence.Current(
                    ctx,
                    currentKey,
                    currentText,
                    descKey,
                    _game != null ? _game.localizationTable : null),
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
                UiAudioScope.GameModule,
                ctx => GameSettingsPromptSequence.Edit(
                    ctx,
                    currentKey,
                    currentText,
                    valueText,
                    hintKey,
                    descKey,
                    _game != null ? _game.localizationTable : null),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrentValue()
        {
            _sm.UiAudio.Play(
                UiAudioScope.GameModule,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "current.value", GetCurrentValueTextLive()),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private string ResolveCommittedValueText(SettingsUiResult r)
        {
            if (r.AffectedItemType == SettingsItemType.Toggle)
                return r.ToggleValue ? GetCore("settings.on") : GetCore("settings.off");

            if (r.AffectedItem != null && (r.AffectedItemType == SettingsItemType.Range || r.AffectedItemType == SettingsItemType.List))
                return ResolveItemDisplayText(r.AffectedItem);

            return GetCore("selected.value");
        }

        private string ResolveDescriptionKey(SettingsItem item)
        {
            if (item == null) return null;
            if (item.LabelKey == "common.back") return null;
            return item.DescriptionKey;
        }

        private static string CurrentKeyForItem(SettingsItem it)
        {
            if (it == null) return "current.option";
            if (it.LabelKey == "common.back") return "current.option";
            if (IsFolder(it)) return "current.option";

            return IsRealSetting(it) ? "current.setting" : "current.option";
        }

        private static bool IsFolder(SettingsItem it) => it != null && it.Type == SettingsItemType.Folder;

        private static bool IsRealSetting(SettingsItem it)
        {
            if (it == null) return false;

            if (it.Type == SettingsItemType.Toggle
                || it.Type == SettingsItemType.List
                || it.Type == SettingsItemType.Range)
                return true;

            return false;
        }

        private string ResolveItemDisplayText(SettingsItem item)
        {
            if (item == null) return "Unknown";

            var label = GetLabel(item.LabelKey);

            switch (item.Type)
            {
                case SettingsItemType.Toggle:
                    {
                        if (item is SettingsToggle t)
                        {
                            bool v = false;
                            try { v = t.GetValue(); } catch { }
                            var vt = v ? GetCore("settings.on") : GetCore("settings.off");
                            return $"{label}: {vt}";
                        }
                        return label;
                    }

                case SettingsItemType.Range:
                    {
                        if (item is SettingsRange r)
                        {
                            float v = 0f;
                            try { v = Mathf.Clamp(r.GetValue(), r.Min, r.Max); } catch { }

                            var formatted = FormatRangeValue(r, v);
                            return $"{label}: {formatted}";
                        }
                        return label;
                    }

                case SettingsItemType.List:
                    {
                        if (item is SettingsList l)
                        {
                            var opts = l.GetOptions?.Invoke();
                            if (opts == null || opts.Count == 0) return label;

                            int idx = ClampInt(l.GetIndex(), 0, opts.Count - 1);
                            return $"{label}: {ResolveListOptionText(opts[idx])}";
                        }
                        return label;
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

        private string FormatRangeValue(SettingsRange range, float value)
        {
            if (range != null && range.LabelKey == "settings.stats.recent_capacity")
            {
                int v = Mathf.RoundToInt(value);

                if (v < 1) v = 1;
                if (v > 10) v = 10;

                string suffixKey = ResolveRecentCapacitySuffixKey(v);

                string suffix = GetCore(suffixKey);
                if (string.IsNullOrWhiteSpace(suffix) || suffix == suffixKey)
                    suffix = "entries";

                return $"{v} {suffix}";
            }

            return SettingsUiValueFormat.Number(value, maxDecimals: 2);
        }

        private static string ResolveRecentCapacitySuffixKey(int v)
        {
            if (v == 1)
                return "settings.stats.recent_capacity.suffix.singular";

            int mod10 = v % 10;
            int mod100 = v % 100;

            bool few = (mod10 >= 2 && mod10 <= 4) && !(mod100 >= 12 && mod100 <= 14);

            return few
                ? "settings.stats.recent_capacity.suffix.plural_few"
                : "settings.stats.recent_capacity.suffix.plural";
        }

        private string ResolveListOptionText(SettingsListOption opt)
        {
            if (LooksLikeRawNumber(opt.LabelKey))
                return NormalizeRaw(opt.LabelKey);

            var s = GetLabel(opt.LabelKey);
            return string.IsNullOrWhiteSpace(s) || s == opt.LabelKey ? opt.Id : s;
        }

        private static bool LooksLikeRawNumber(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;

            int i = 0;
            if (s[0] == '-') i = 1;
            if (i >= s.Length) return false;

            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }

        private static string NormalizeRaw(string s) => s.Trim();

        private static int ClampInt(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private static ControlHintMode ResolveEffectiveHintMode(AppSettingsData settings)
        {
            var mode = settings.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();
            return mode;
        }

        private string GetCore(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key ?? "";

            if (_loc == null)
                return key;

            try
            {
                var s = (args == null || args.Length == 0)
                    ? _loc.Get(key)
                    : _loc.Get(key, args);

                return string.IsNullOrWhiteSpace(s) ? key : s;
            }
            catch
            {
                return key;
            }
        }

        private string GetLabel(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key ?? "";

            if (_loc == null)
                return key;

            string table = null;
            try { table = _game != null ? _game.localizationTable : null; }
            catch { table = null; }

            return ResolveTextCoreThenGame(key, table, args);
        }

        private string ResolveTextCoreThenGame(string key, string gameTable, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key ?? "";

            if (_loc == null)
                return key;

            try
            {
                var core = (args == null || args.Length == 0)
                    ? _loc.Get(key)
                    : _loc.Get(key, args);

                if (!IsMissingValue(core, key))
                    return core;
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(gameTable))
            {
                try
                {
                    var game = (args == null || args.Length == 0)
                        ? _loc.GetFromTable(gameTable, key)
                        : _loc.GetFromTable(gameTable, key, args);

                    if (!IsMissingValue(game, key))
                        return game;
                }
                catch { }
            }

            return key;
        }

        private static bool IsMissingValue(string value, string key)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (string.Equals(value, key, System.StringComparison.Ordinal))
                return true;

            var v = value.Trim();

            if (v.IndexOf(key, System.StringComparison.Ordinal) < 0 &&
                v.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            bool hasNotFound =
                v.IndexOf("not found", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("missing", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("no entry", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("no translation", System.StringComparison.OrdinalIgnoreCase) >= 0;

            bool hasInCoreOrTable =
                v.IndexOf("in core", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("in table", System.StringComparison.OrdinalIgnoreCase) >= 0;

            return hasNotFound || hasInCoreOrTable;
        }

        private sealed class GameSettingsHooks : ISettingsUiHooks
        {
            public bool RequiresConfirmation(SettingsAction action)
            {
                if (action == null) return false;

                return action.LabelKey == "settings.stats.reset"
                    || action.LabelKey == "settings.reset_defaults"
                    || action.LabelKey == "settings.run_params.seed_history.reset";
            }

            public bool TryHandleToggle(SettingsToggle toggle) => false;
            public bool TryHandleAction(SettingsAction action) => false;
        }

        private void PlayConfirmActionPrompt(SettingsItem item)
        {
            _sm.UiAudio.Play(
                UiAudioScope.GameModule,
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
                UiAudioScope.GameModule,
                ctx => GameSettingsPromptSequence.ConfirmAction(
                    ctx,
                    currentKey,
                    currentText,
                    hintKey,
                    descKey,
                    _game != null ? _game.localizationTable : null),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void ExitToGameMenu()
        {
            _sm.UiAudio.CancelCurrent();
            _va?.NotifyTransitioning();

            _sm.UiAudio.PlayGated(
                UiAudioScope.GameModule,
                "exit.to_game_menu",
                stillTransitioning: () => _sm.Transitions.IsTransitioning,
                delaySeconds: 0.5f,
                priority: SpeechPriority.High,
                GameLocalization.GetGameName(_loc, _game)
            );

            _sm.Transitions.RunInstant(() =>
            {
                _sm.SetState(new GameMenuState(_sm));
            });
        }
    }
}