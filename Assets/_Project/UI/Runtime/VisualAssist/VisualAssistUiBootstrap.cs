using Project.Core.App;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.UI.VisualAssist
{
    public sealed class VisualAssistUiBootstrap : MonoBehaviour
    {
        [Header("Prefab (optional)")]
        [SerializeField] private VisualAssistOverlayView overlayPrefab;

        [Header("Lifetime")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Canvas (optional override)")]
        [SerializeField] private int sortingOrder = 5000;

        [Header("Idle Hint Driver")]
        [SerializeField] private bool ensureIdleDriver = true;

        [SerializeField, Tooltip("How often to evaluate idle hint (seconds).")]
        private float idleCheckIntervalSeconds = 0.15f;

        private static bool _bootstrapped;

        private void Awake()
        {
            if (_bootstrapped)
            {
                Destroy(gameObject);
                return;
            }
            _bootstrapped = true;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            var services = AppContext.Services;
            var va = services.Resolve<IVisualAssistService>();
            if (va == null)
            {
                Debug.LogError("[VA.UI] IVisualAssistService not registered.");
                return;
            }

            EnsureSingleOverlay();

            if (ensureIdleDriver)
                EnsureIdleDriver();
        }

        private void EnsureSingleOverlay()
        {
#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<VisualAssistOverlayView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<VisualAssistOverlayView>(includeInactive: true);
#endif

            if (all != null && all.Length > 1)
            {
                Debug.LogError($"[VA.UI] Detected {all.Length} VisualAssistOverlayView instances. Keeping one, destroying duplicates.");

                for (int i = 1; i < all.Length; i++)
                {
                    if (all[i] != null)
                        Destroy(all[i].gameObject);
                }

                ApplyCanvasSorting(all[0]);
                return;
            }

            if (all != null && all.Length == 1)
            {
                ApplyCanvasSorting(all[0]);
                return;
            }

            if (overlayPrefab == null)
            {
                Debug.LogError("[VA.UI] No existing overlay found and overlayPrefab not assigned.");
                return;
            }

            var instance = Instantiate(overlayPrefab);
            instance.name = overlayPrefab.name;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(instance.gameObject);

            ApplyCanvasSorting(instance);
        }

        private void ApplyCanvasSorting(VisualAssistOverlayView view)
        {
            if (view == null) return;

            var canvas = view.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                canvas.sortingOrder = sortingOrder;
        }

        private void EnsureIdleDriver()
        {
#if UNITY_2023_1_OR_NEWER
            var existing = FindFirstObjectByType<VisualAssistIdleDriver>(FindObjectsInactive.Include);
#else
            var existing = FindObjectOfType<VisualAssistIdleDriver>(includeInactive: true);
#endif
            if (existing != null)
                return;

            var go = new GameObject("VisualAssistIdleDriver");
            go.transform.SetParent(transform, worldPositionStays: false);

            var driver = go.AddComponent<VisualAssistIdleDriver>();
            driver.Init(idleCheckIntervalSeconds);

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(go);
        }
    }
}