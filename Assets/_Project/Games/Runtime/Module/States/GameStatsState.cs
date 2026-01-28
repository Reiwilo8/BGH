using System;
using System.Collections.Generic;
using System.Text;
using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Games.Catalog;
using Project.Games.Definitions;
using Project.Games.Localization;
using Project.Games.Stats;
using UnityEngine;

namespace Project.Games.Module.States
{
    public sealed class GameStatsState : IGameModuleState
    {
        private readonly GameModuleStateMachine _sm;

        private readonly IUiAudioOrchestrator _uiAudio;
        private readonly IAppFlowService _flow;
        private readonly ISettingsService _settings;

        private readonly ILocalizationService _loc;
        private readonly IVisualAssistService _va;

        private readonly AppSession _session;
        private readonly GameCatalog _catalog;

        private readonly IGameStatsService _stats;
        private readonly IGameStatsPreferencesService _prefs;

        private GameDefinition _game;

        private int _index;
        private List<GameModeDefinition> _modesWithBack;

        private bool _showRecent;

        public string Name => "GameModule.Stats";

        public GameStatsState(GameModuleStateMachine sm)
        {
            _sm = sm;

            var services = Core.App.AppContext.Services;

            _uiAudio = services.Resolve<IUiAudioOrchestrator>();
            _flow = services.Resolve<IAppFlowService>();
            _settings = services.Resolve<ISettingsService>();

            _loc = services.Resolve<ILocalizationService>();
            _va = services.Resolve<IVisualAssistService>();

            _session = services.Resolve<AppSession>();
            _catalog = services.Resolve<GameCatalog>();

            _stats = services.Resolve<IGameStatsService>();
            _prefs = services.Resolve<IGameStatsPreferencesService>();
        }

        public void Enter()
        {
            if (!LoadSelectedGameOrFail())
                return;

            BuildModesWithBack();

            _index = 0;
            _showRecent = false;

            RefreshVa();
            PlayBrowsePrompt();
        }

        public void Exit() { }

        public void OnFocusGained()
        {
            RefreshVa();
            PlayBrowsePrompt();
        }

        public void OnRepeatRequested()
        {
            if (_flow.IsTransitioning) return;

            RefreshVa();
            PlayBrowsePrompt();
        }

        public void Handle(NavAction action)
        {
            if (_modesWithBack == null || _modesWithBack.Count == 0)
                return;

            switch (action)
            {
                case NavAction.Next:
                    _index = (_index + 1) % _modesWithBack.Count;
                    _va?.PulseListMove(VaListMoveDirection.Next);
                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Previous:
                    _index = (_index - 1 + _modesWithBack.Count) % _modesWithBack.Count;
                    _va?.PulseListMove(VaListMoveDirection.Previous);
                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Confirm:
                    if (IsBackSelected())
                    {
                        _va?.ClearTransitioning();
                        ExitToGameMenu();
                        return;
                    }

                    _showRecent = !_showRecent;

                    _va?.ClearTransitioning();
                    RefreshVa();
                    PlaySwitchedView();
                    break;

                case NavAction.Back:
                    _va?.ClearTransitioning();
                    ExitToGameMenu();
                    break;
            }
        }

        private bool LoadSelectedGameOrFail()
        {
            if (string.IsNullOrWhiteSpace(_session.SelectedGameId))
            {
                _ = _flow.ReturnToHubAsync();
                return false;
            }

            _game = _catalog.GetById(_session.SelectedGameId);
            if (_game == null)
            {
                _ = _flow.ReturnToHubAsync();
                return false;
            }

            return true;
        }

        private void BuildModesWithBack()
        {
            _modesWithBack = new List<GameModeDefinition>();

            var arr = _game != null ? _game.modes : null;
            if (arr != null)
            {
                foreach (var m in arr)
                {
                    if (m != null && !string.IsNullOrWhiteSpace(m.modeId))
                        _modesWithBack.Add(m);
                }
            }

            _modesWithBack.Add(null);

            if (_index < 0) _index = 0;
            if (_index >= _modesWithBack.Count) _index = _modesWithBack.Count - 1;
        }

        private bool IsBackSelected()
        {
            if (_modesWithBack == null || _modesWithBack.Count == 0) return true;
            return _index == _modesWithBack.Count - 1;
        }

        private void RefreshVa()
        {
            if (_va == null) return;

            _va.SetHeaderKey("va.screen.game_stats", GameLocalization.GetGameName(_loc, _game));

            string subHeader = IsBackSelected()
                ? SafeGet("common.back")
                : GameLocalization.GetModeName(_loc, _modesWithBack[_index]);

            _va.SetSubHeaderText(subHeader);
            _va.SetIdleHintKey(ResolveControlHintKey());

            var (descKey, descArgs) = BuildDescriptionKeyAndArgs();
            if (!string.IsNullOrWhiteSpace(descKey))
            {
                string center = SafeGet(descKey, descArgs);
                _va.SetCenterText(VaCenterLayer.PlannedSpeech, center);
            }
            else
            {
                _va.SetCenterText(VaCenterLayer.PlannedSpeech, "");
            }

            ScheduleClearTransitioning();
        }

        private void ScheduleClearTransitioning()
        {
            if (_va == null) return;
            if (!_va.IsTransitioning) return;

            if (_uiAudio is MonoBehaviour mb)
                mb.StartCoroutine(ClearTransitioningNextFrame());
            else
                _va.ClearTransitioning();
        }

        private System.Collections.IEnumerator ClearTransitioningNextFrame()
        {
            yield return null;
            _va?.ClearTransitioning();
        }

        private void PlayBrowsePrompt()
        {
            string hintKey = ResolveControlHintKey();
            string viewLabel = SafeGet(_showRecent ? "stats.view.recent" : "stats.view.overall");

            string currentKey;
            string currentText;

            if (IsBackSelected())
            {
                currentKey = "current.option";
                currentText = SafeGet("common.back");
            }
            else
            {
                currentKey = "current.mode";
                currentText = $"{GameLocalization.GetModeName(_loc, _modesWithBack[_index])} ({viewLabel})";
            }

            var (descKey, descArgs) = BuildDescriptionKeyAndArgs();

            _uiAudio.Play(
                UiAudioScope.GameModule,
                ctx => BrowseSequence(
                    ctx,
                    GameLocalization.GetGameName(_loc, _game),
                    currentKey,
                    currentText,
                    hintKey,
                    descKey,
                    descArgs),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrent()
        {
            string viewLabel = SafeGet(_showRecent ? "stats.view.recent" : "stats.view.overall");

            string currentKey;
            string currentText;

            if (IsBackSelected())
            {
                currentKey = "current.option";
                currentText = SafeGet("common.back");
            }
            else
            {
                currentKey = "current.mode";
                currentText = $"{GameLocalization.GetModeName(_loc, _modesWithBack[_index])} ({viewLabel})";
            }

            var (descKey, descArgs) = BuildDescriptionKeyAndArgs();

            _uiAudio.Play(
                UiAudioScope.GameModule,
                ctx => CurrentWithDescSequence(ctx, currentKey, currentText, descKey, descArgs),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlaySwitchedView()
        {
            if (IsBackSelected())
            {
                PlayCurrent();
                return;
            }

            string modeName = GameLocalization.GetModeName(_loc, _modesWithBack[_index]);
            string viewLabel = SafeGet(_showRecent ? "stats.view.recent" : "stats.view.overall");

            var (descKey, descArgs) = BuildDescriptionKeyAndArgs();

            _uiAudio.Play(
                UiAudioScope.GameModule,
                ctx => SwitchedSequence(ctx, viewLabel, modeName, descKey, descArgs),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private static System.Collections.IEnumerator BrowseSequence(
            UiAudioContext ctx,
            string gameName,
            string currentKey,
            string currentText,
            string hintKey,
            string descKey,
            object[] descArgs)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.game_stats", gameName);
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);

            if (!string.IsNullOrWhiteSpace(descKey))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, descKey, descArgs);
        }

        private static System.Collections.IEnumerator CurrentWithDescSequence(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string descKey,
            object[] descArgs)
        {
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);

            if (!string.IsNullOrWhiteSpace(descKey))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, descKey, descArgs);
        }

        private static System.Collections.IEnumerator SwitchedSequence(
            UiAudioContext ctx,
            string viewLabel,
            string modeName,
            string descKey,
            object[] descArgs)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "stats.switched", viewLabel, modeName);

            if (!string.IsNullOrWhiteSpace(descKey))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, descKey, descArgs);
        }

        private void ExitToGameMenu()
        {
            if (_flow.IsTransitioning)
                return;

            _uiAudio.CancelCurrent();
            _va?.NotifyTransitioning();

            _uiAudio.PlayGated(
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

        private (string key, object[] args) BuildDescriptionKeyAndArgs()
        {
            if (IsBackSelected())
                return (null, Array.Empty<object>());

            int cap = 5;
            try
            {
                cap = _prefs.GetRecentCapacity(_game != null ? _game.gameId : null);
            }
            catch
            {
                cap = 5;
            }

            if (cap < 1) cap = 1;
            if (cap > 10) cap = 10;

            var snapRaw = _stats.GetSnapshot(_game != null ? _game.gameId : null);
            var snap = GameStatsSnapshotLimiter.LimitRecent(snapRaw, cap);

            var mode = _modesWithBack[_index];
            string modeId = mode != null ? mode.modeId : null;

            ModeStatsSnapshot? ms = FindModeSnapshot(snap, modeId);

            if (!ms.HasValue)
                return ("stats.desc.no_data", Array.Empty<object>());

            if (_showRecent)
                return BuildRecentDesc(ms.Value);

            return BuildOverallDesc(mode, ms.Value);
        }

        private ModeStatsSnapshot? FindModeSnapshot(GameStatsSnapshot snap, string modeId)
        {
            if (snap.PerMode == null || string.IsNullOrWhiteSpace(modeId))
                return null;

            for (int i = 0; i < snap.PerMode.Count; i++)
            {
                if (snap.PerMode[i].ModeId == modeId)
                    return snap.PerMode[i];
            }

            return null;
        }

        private (string key, object[] args) BuildOverallDesc(GameModeDefinition modeDef, ModeStatsSnapshot ms)
        {
            var o = ms.Overall;

            string runs = o.Runs.ToString();

            bool endless = modeDef != null && modeDef.kind == GameModeKind.Endless;
            string completions = endless ? SafeGet("stats.na") : o.Completions.ToString();

            bool hasCompletion = o.Completions > 0;

            string bestLabel;
            string bestTime;

            if (hasCompletion && o.BestTime.HasValue)
            {
                bestLabel = endless
                    ? SafeGet("stats.best_time.longest")
                    : SafeGet("stats.best_time.shortest");

                bestTime = FormatDuration(o.BestTime.Value);
            }
            else
            {
                TimeSpan longest = TimeSpan.Zero;

                var rr = ms.RecentRuns;
                if (rr != null)
                {
                    for (int i = 0; i < rr.Count; i++)
                    {
                        if (rr[i].Duration > longest)
                            longest = rr[i].Duration;
                    }
                }

                bestLabel = SafeGet("stats.best_time.longest");
                bestTime = longest > TimeSpan.Zero ? FormatDuration(longest) : SafeGet("stats.na");
            }

            string lastPlayed = o.LastPlayedUtc.HasValue
                ? FormatUtcDate(o.LastPlayedUtc.Value)
                : SafeGet("stats.never");

            return ("stats.desc.overall", new object[] { runs, completions, bestLabel, bestTime, lastPlayed });
        }

        private (string key, object[] args) BuildRecentDesc(ModeStatsSnapshot ms)
        {
            var rr = ms.RecentRuns;

            if (rr == null || rr.Count == 0)
                return ("stats.desc.no_recent", Array.Empty<object>());

            int count = rr.Count;

            var sb = new StringBuilder();
            sb.Append(SafeGet("stats.recent.prefix"));
            sb.Append(' ');

            for (int i = 0; i < count; i++)
            {
                var r = rr[i];

                sb.Append(i + 1);
                sb.Append(") ");
                sb.Append(FormatDuration(r.Duration));

                if (r.Completed)
                {
                    sb.Append(" (");
                    sb.Append(SafeGet("stats.recent.completed"));
                    sb.Append(')');
                }

                if (i < count - 1)
                    sb.Append(", ");
            }

            return ("stats.desc.recent", new object[] { sb.ToString() });
        }

        private string ResolveControlHintKey()
        {
            var mode = _settings.Current.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == ControlHintMode.Touch
                ? "hint.game_stats.touch"
                : "hint.game_stats.keyboard";
        }

        private string SafeGet(string key, params object[] args)
        {
            if (_loc == null || string.IsNullOrWhiteSpace(key))
                return key ?? "";

            if (args == null || args.Length == 0)
            {
                var s0 = _loc.Get(key);
                return string.IsNullOrWhiteSpace(s0) ? key : s0;
            }

            try
            {
                var s = _loc.Get(key, args);
                return string.IsNullOrWhiteSpace(s) ? key : s;
            }
            catch
            {
                return key;
            }
        }

        private static string FormatDuration(TimeSpan t)
        {
            if (t <= TimeSpan.Zero) return "0s";

            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}h {t.Minutes}m";

            if (t.TotalMinutes >= 1)
                return $"{(int)t.TotalMinutes}m {t.Seconds}s";

            return $"{t.Seconds}s";
        }

        private static string FormatUtcDate(DateTime utc)
        {
            return utc.ToString("yyyy-MM-dd");
        }
    }
}