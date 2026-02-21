using Project.Core.Visual.Games.SteamRush;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Games.SteamRush
{
    public sealed class SteamRushVisualOverlayView : MonoBehaviour, ISteamRushVisualDriver
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Board")]
        [SerializeField] private RectTransform boardRoot;

        [Header("Optional Layer (recommended)")]
        [Tooltip("If set, train instances will be re-parented here and always kept on top of lanes/player.")]
        [SerializeField] private RectTransform trainsLayer;

        [Header("Lanes (3 Images)")]
        [SerializeField] private Image lane0;
        [SerializeField] private Image lane1;
        [SerializeField] private Image lane2;

        [Header("Player")]
        [SerializeField] private RectTransform player;

        [Header("Trains (pool, e.g. 6 instances)")]
        [SerializeField] private SteamRushTrainView[] trainPool;

        [Header("Auto-Collect Pool (if trainPool empty)")]
        [SerializeField] private bool autoCollectTrainPoolIfEmpty = true;

        [Header("Layout (requested: narrow lanes, small player, narrower trains)")]
        [Tooltip("Lane width = laneSpan * laneFillFactor. For ~1/7 board width lanes: laneFillFactor ~= (1/7)/(1/3)=3/7?0.4286")]
        [Range(0.30f, 0.55f)]
        [SerializeField] private float laneFillFactor = 0.43f;

        [Tooltip("Pack lanes closer to center (1=default spacing, smaller=more compact towards center).")]
        [Range(0.55f, 1.00f)]
        [SerializeField] private float laneCompactness = 0.90f;

        [Tooltip("Player size relative to lane width (square).")]
        [Range(0.08f, 0.40f)]
        [SerializeField] private float playerSizeFactor = 0.16f;

        [Header("Player Y (base)")]
        [Tooltip("Base player center Y in normalized board space (0 bottom, 1 top).")]
        [Range(0.08f, 0.55f)]
        [SerializeField] private float playerY01Base = 0.10f;

        [Tooltip("Optionally shift player slightly lower depending on ratio Approach:Pass to keep proportions readable.")]
        [Range(0.00f, 0.10f)]
        [SerializeField] private float playerAutoShiftMax01 = 0.04f;

        [Tooltip("Train width as fraction of lane width (narrower).")]
        [Range(0.35f, 0.65f)]
        [SerializeField] private float trainWidthFactor = 0.48f;

        [Header("Overscan (normalized board space)")]
        [Tooltip("Extra margin above top edge to ensure whole train is off-screen on spawn.")]
        [Range(0.00f, 0.20f)]
        [SerializeField] private float spawnMargin01 = 0.03f;

        [Tooltip("Extra margin below bottom edge to ensure whole train is off-screen on despawn.")]
        [Range(0.00f, 0.30f)]
        [SerializeField] private float despawnMargin01 = 0.06f;

        [Header("After-pass travel")]
        [Tooltip("Minimum time after passing ends that the train keeps moving before it can despawn (seconds).")]
        [Range(0.25f, 1.50f)]
        [SerializeField] private float afterPassMinSeconds = 0.50f;

        [Header("Debug")]
        [SerializeField] private bool logTrainActivationProblems = true;

        private RectTransform[] _laneRects;

        private float _lastBoardW = -999f;
        private float _lastBoardH = -999f;
        private float _laneWidthPx = -1f;

        private bool _visible;
        private bool _paused;

        private bool _pauseHidden;

        private int _playerLane = 1;

        private struct ActiveVisualTrain
        {
            public int id;
            public int lane;

            public float approachSec;
            public float passSec;

            public float startRealtime;
            public float pausedElapsed;

            public int poolIdx;

            public float startTopYPx;
            public float speedPxPerSec;
            public float despawnTopYPx;
            public float lifeSec;
        }

        private struct PendingSpawn
        {
            public int lane;
            public float approachSec;
            public float passSec;
        }

        private int _nextId = 1;

        private readonly List<ActiveVisualTrain> _active = new List<ActiveVisualTrain>(16);
        private readonly Dictionary<int, int> _idToIndex = new Dictionary<int, int>(32);
        private readonly Queue<PendingSpawn> _pending = new Queue<PendingSpawn>(32);

        private bool[] _used;

        private void Awake()
        {
            _laneRects = new[]
            {
                lane0 != null ? lane0.rectTransform : null,
                lane1 != null ? lane1.rectTransform : null,
                lane2 != null ? lane2.rectTransform : null
            };

            if (autoCollectTrainPoolIfEmpty && (trainPool == null || trainPool.Length == 0))
                AutoCollectTrainPool();

            if (trainPool == null)
                trainPool = new SteamRushTrainView[0];

            _used = new bool[trainPool.Length];

            for (int i = 0; i < trainPool.Length; i++)
            {
                if (trainPool[i] == null) continue;
                EnsureTrainParent(trainPool[i]);
                trainPool[i].SetActive(false);
            }

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;

            ApplyPauseVisibility();

            if (!visible)
                Reset();
        }

        public void Reset()
        {
            _paused = false;
            _pauseHidden = false;
            _nextId = 1;

            _idToIndex.Clear();
            _active.Clear();
            _pending.Clear();

            if (_used != null)
                for (int i = 0; i < _used.Length; i++)
                    _used[i] = false;

            if (trainPool != null)
                for (int i = 0; i < trainPool.Length; i++)
                    if (trainPool[i] != null)
                        trainPool[i].SetActive(false);

            ApplyPauseVisibility();
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;
            _paused = paused;

            // Hide visuals during pause, but keep state for resume.
            ApplyPauseVisibility();

            float now = Time.unscaledTime;

            for (int i = 0; i < _active.Count; i++)
            {
                var t = _active[i];

                if (paused)
                {
                    t.pausedElapsed = Mathf.Max(0f, now - t.startRealtime);
                }
                else
                {
                    t.startRealtime = now - Mathf.Max(0f, t.pausedElapsed);
                }

                _active[i] = t;
            }
        }

        public void SetPlayerLane(int playerLane)
        {
            _playerLane = Mathf.Clamp(playerLane, 0, 2);
            if (_visible) ApplyPlayer(_playerLane, approachSec: 1f, passSec: 1f);
        }

        public void SpawnTrain(int lane, float approachSeconds, float passSeconds)
        {
            if (!_visible) return;

            lane = Mathf.Clamp(lane, 0, 2);

            float a = Mathf.Max(0.001f, approachSeconds);
            float p = Mathf.Max(0.001f, passSeconds);

            if (!EnsureBoardReady())
            {
                _pending.Enqueue(new PendingSpawn { lane = lane, approachSec = a, passSec = p });
                return;
            }

            SpawnTrainInternal(lane, a, p);
        }

        private void Update()
        {
            if (!_visible) return;

            if (_pauseHidden) return;

            if (root != null && !root.activeInHierarchy) return;
            if (boardRoot == null) return;

            if (!EnsureBoardReady())
                return;

            EnsureLayout();

            ApplyPlayer(_playerLane, approachSec: 1f, passSec: 1f);

            while (_pending.Count > 0)
            {
                var s = _pending.Dequeue();
                SpawnTrainInternal(s.lane, s.approachSec, s.passSec);
            }

            if (_paused) return;

            float now = Time.unscaledTime;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var t = _active[i];

                float elapsed = Mathf.Max(0f, now - t.startRealtime);

                int poolIdx = t.poolIdx;
                if (poolIdx < 0 || poolIdx >= trainPool.Length || trainPool[poolIdx] == null)
                {
                    RemoveAt(i);
                    continue;
                }

                var view = trainPool[poolIdx];
                view.ApplyLayout(_laneWidthPx, trainWidthFactor);

                bool alive = PlaceAndStyleTrainPx(ref t, view, elapsed);

                if (!alive)
                {
                    view.SetActive(false);
                    RemoveAt(i);
                }
                else
                {
                    _active[i] = t;
                }

                if (logTrainActivationProblems && view.gameObject.activeSelf && !view.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning(
                        $"[SteamRushVisualOverlayView] Train activated but NOT visible (activeInHierarchy=false). " +
                        $"TrainGO='{view.gameObject.name}', Parent='{view.transform.parent?.name}'",
                        this
                    );
                }
            }
        }

        private void ApplyPauseVisibility()
        {
            bool wantActive = _visible && !_paused;

            _pauseHidden = _visible && _paused;

            SetActiveSafe(root, wantActive);

            if (trainsLayer != null)
                SetActiveSafe(trainsLayer.gameObject, wantActive);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf == active) return;
            go.SetActive(active);
        }

        private bool EnsureBoardReady()
        {
            if (boardRoot == null) return false;

            Rect r = boardRoot.rect;
            if (r.width > 2f && r.height > 2f)
                return true;

            Canvas.ForceUpdateCanvases();

            r = boardRoot.rect;
            return (r.width > 2f && r.height > 2f);
        }

        private void SpawnTrainInternal(int lane, float approachSeconds, float passSeconds)
        {
            if (boardRoot == null) return;
            if (trainPool == null || trainPool.Length == 0) return;

            EnsureLayout();

            ApplyPlayer(_playerLane, approachSeconds, passSeconds);

            int poolIdx = FindFreePoolSlot();
            if (poolIdx < 0) return;

            var view = trainPool[poolIdx];
            if (view == null) return;

            EnsureTrainParent(view);
            view.SetActive(true);
            BringTrainToFront(view);

            view.ApplyLayout(_laneWidthPx, trainWidthFactor);

            int id = _nextId++;

            Rect br = boardRoot.rect;
            float boardH = Mathf.Max(1f, br.height);

            float topEdgePx = +boardH * 0.5f;
            float bottomEdgePx = -boardH * 0.5f;

            float spawnMarginPx = Mathf.Max(0f, spawnMargin01) * boardH;
            float despawnMarginPx = Mathf.Max(0f, despawnMargin01) * boardH;

            float trainHpx = Mathf.Max(1f, view.TotalHeightPx);

            float playerHalf = 0f;
            if (player != null) playerHalf = player.sizeDelta.y * 0.5f;

            float playerCenterY = (player != null) ? player.anchoredPosition.y : Mathf.Lerp(bottomEdgePx, topEdgePx, 0.20f);
            float playerTopY = playerCenterY + playerHalf;
            float playerBottomY = playerCenterY - playerHalf;

            float A = Mathf.Max(0.001f, approachSeconds);
            float P = Mathf.Max(0.001f, passSeconds);

            float topAtApproachEnd = playerTopY + trainHpx;
            float topAtPassEnd = playerBottomY;

            float v = (topAtPassEnd - topAtApproachEnd) / P;

            if (Mathf.Abs(v) < 0.01f)
                v = -Mathf.Max(50f, boardH * 0.40f);

            float startTopY = topAtApproachEnd - v * A;

            float minStartTopY = topEdgePx + trainHpx + spawnMarginPx;
            if (startTopY < minStartTopY)
            {
                startTopY = minStartTopY;
            }

            float despawnTopY = bottomEdgePx - despawnMarginPx;

            float afterMin = Mathf.Max(0.25f, afterPassMinSeconds);

            float tToDespawn = 0f;
            if (v < -0.0001f)
            {
                tToDespawn = (despawnTopY - startTopY) / v;
            }
            else
            {
                tToDespawn = A + P + afterMin;
            }

            float life = Mathf.Max(A + P + afterMin, tToDespawn);

            var vt = new ActiveVisualTrain
            {
                id = id,
                lane = Mathf.Clamp(lane, 0, 2),

                approachSec = A,
                passSec = P,

                startRealtime = Time.unscaledTime,
                pausedElapsed = 0f,
                poolIdx = poolIdx,

                startTopYPx = startTopY,
                speedPxPerSec = v,
                despawnTopYPx = despawnTopY,
                lifeSec = life
            };

            _idToIndex[id] = _active.Count;
            _active.Add(vt);

            PlaceAndStyleTrainPx(ref vt, view, elapsed: 0f);
            _active[_active.Count - 1] = vt;
        }

        private bool PlaceAndStyleTrainPx(ref ActiveVisualTrain t, SteamRushTrainView view, float elapsed)
        {
            float e = Mathf.Max(0f, elapsed);

            float topYPx = t.startTopYPx + t.speedPxPerSec * e;

            int phase = (e < t.approachSec) ? 0 : 1;
            float phase01 =
                phase == 0 ? Mathf.Clamp01(e / Mathf.Max(0.0001f, t.approachSec))
                           : Mathf.Clamp01((e - t.approachSec) / Mathf.Max(0.0001f, t.passSec));

            view.ApplyPhase(phase, phase01);

            PlaceTrainPx(view.Root, t.lane, topYPx);

            if (e < t.lifeSec)
                return true;

            return topYPx > t.despawnTopYPx;
        }

        private void EnsureLayout()
        {
            Rect r = boardRoot.rect;

            if (Mathf.Abs(r.width - _lastBoardW) < 0.5f && Mathf.Abs(r.height - _lastBoardH) < 0.5f)
                return;

            _lastBoardW = r.width;
            _lastBoardH = r.height;

            float laneSpan = Mathf.Max(1f, r.width) / 3f;
            _laneWidthPx = laneSpan * Mathf.Clamp(laneFillFactor, 0.30f, 0.55f);

            for (int lane = 0; lane < 3; lane++)
            {
                var rt = _laneRects[lane];
                if (rt == null) continue;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                rt.sizeDelta = new Vector2(_laneWidthPx, r.height);
                rt.anchoredPosition = new Vector2(LaneCenterXPx(lane, r.width), 0f);
            }

            if (player != null)
            {
                float ps = Mathf.Max(8f, _laneWidthPx * Mathf.Clamp(playerSizeFactor, 0.08f, 0.40f));
                player.sizeDelta = new Vector2(ps, ps);
            }
        }

        private float LaneCenterXPx(int lane, float boardW)
        {
            lane = Mathf.Clamp(lane, 0, 2);

            float x01 = (lane + 0.5f) / 3f;
            float c = 0.5f;
            float k = Mathf.Clamp(laneCompactness, 0.55f, 1.0f);

            float x01c = c + (x01 - c) * k;

            return Mathf.Lerp(-boardW * 0.5f, boardW * 0.5f, x01c);
        }

        private void ApplyPlayer(int lane, float approachSec, float passSec)
        {
            if (player == null) return;

            Rect r = boardRoot.rect;

            float x = LaneCenterXPx(lane, r.width);

            float A = Mathf.Max(0.001f, approachSec);
            float P = Mathf.Max(0.001f, passSec);

            float ratio = Mathf.Clamp01((A - P) / Mathf.Max(0.001f, A + P));
            float shift = playerAutoShiftMax01 * ratio;

            float y01 = Mathf.Clamp01(playerY01Base - shift);

            float y = Mathf.Lerp(-r.height * 0.5f, r.height * 0.5f, y01);

            player.anchorMin = new Vector2(0.5f, 0.5f);
            player.anchorMax = new Vector2(0.5f, 0.5f);
            player.pivot = new Vector2(0.5f, 0.5f);

            player.anchoredPosition = new Vector2(x, y);
        }

        private void PlaceTrainPx(RectTransform trainRoot, int lane, float topYPx)
        {
            if (trainRoot == null) return;

            Rect br = boardRoot.rect;

            float x = LaneCenterXPx(lane, br.width);

            trainRoot.anchorMin = new Vector2(0.5f, 0.5f);
            trainRoot.anchorMax = new Vector2(0.5f, 0.5f);
            trainRoot.pivot = new Vector2(0.5f, 1.0f);

            trainRoot.anchoredPosition = new Vector2(x, topYPx);
        }

        private int FindFreePoolSlot()
        {
            if (_used == null || _used.Length != (trainPool?.Length ?? 0))
                _used = new bool[trainPool?.Length ?? 0];

            for (int i = 0; i < _used.Length; i++)
                _used[i] = false;

            for (int i = 0; i < _active.Count; i++)
            {
                int pi = _active[i].poolIdx;
                if (pi >= 0 && pi < _used.Length) _used[pi] = true;
            }

            for (int i = 0; i < trainPool.Length; i++)
            {
                if (trainPool[i] == null) continue;
                if (!_used[i]) return i;
            }

            return -1;
        }

        private void RemoveAt(int index)
        {
            if (index < 0 || index >= _active.Count) return;

            int id = _active[index].id;
            _idToIndex.Remove(id);

            int last = _active.Count - 1;
            if (index != last)
            {
                var moved = _active[last];
                _active[index] = moved;
                _idToIndex[moved.id] = index;
            }

            _active.RemoveAt(last);
        }

        private void BringTrainToFront(SteamRushTrainView view)
        {
            if (view == null) return;

            try { view.transform.SetAsLastSibling(); } catch { }

            if (trainsLayer != null)
            {
                try { trainsLayer.SetAsLastSibling(); } catch { }
            }
        }

        private void EnsureTrainParent(SteamRushTrainView view)
        {
            if (view == null) return;

            RectTransform targetParent = trainsLayer != null ? trainsLayer : boardRoot;
            if (targetParent == null) return;

            if (view.transform.parent != targetParent)
                view.transform.SetParent(targetParent, worldPositionStays: false);
        }

        private void AutoCollectTrainPool()
        {
            Transform scope =
                trainsLayer != null ? trainsLayer :
                boardRoot != null ? boardRoot :
                transform;

            var found = scope.GetComponentsInChildren<SteamRushTrainView>(includeInactive: true);
            trainPool = found ?? new SteamRushTrainView[0];

            if (logTrainActivationProblems)
                Debug.Log($"[SteamRushVisualOverlayView] Auto-collected trainPool: {trainPool.Length} instances.", this);
        }
    }
}