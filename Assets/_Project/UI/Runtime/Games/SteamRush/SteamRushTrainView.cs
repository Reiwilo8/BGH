using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Games.SteamRush
{
    public sealed class SteamRushTrainView : MonoBehaviour
    {
        [Header("Root (this)")]
        [SerializeField] private RectTransform root;

        [Header("Segments (5)")]
        [SerializeField] private RectTransform loco;
        [SerializeField] private RectTransform link1;
        [SerializeField] private RectTransform car1;
        [SerializeField] private RectTransform link2;
        [SerializeField] private RectTransform car2;

        [Header("Optional Images (phase visuals)")]
        [SerializeField] private Image locoImg;
        [SerializeField] private Image link1Img;
        [SerializeField] private Image car1Img;
        [SerializeField] private Image link2Img;
        [SerializeField] private Image car2Img;

        [Header("Shape (requested: longer train)")]
        [Tooltip("Car height relative to train width. Bigger = longer.")]
        [Range(0.90f, 1.80f)]
        [SerializeField] private float carHeightMul = 1.45f;

        [Tooltip("Link height relative to car height.")]
        [Range(0.08f, 0.28f)]
        [SerializeField] private float linkHeightMulOfCar = 0.12f;

        [Tooltip("Link width relative to train width.")]
        [Range(0.35f, 0.75f)]
        [SerializeField] private float linkWidthMulOfTrain = 0.50f;

        private Image[] _imgs;

        private float _lastTrainWidthPx = -1f;
        private float _totalHeightPx = 0f;

        public float TotalHeightPx => _totalHeightPx;
        public RectTransform Root => root;

        private void Awake()
        {
            if (root == null) root = transform as RectTransform;

            _imgs = new[] { locoImg, link1Img, car1Img, link2Img, car2Img };

            if (root != null)
                root.pivot = new Vector2(0.5f, 1.0f);

            SetActive(false);
        }

        public void SetActive(bool active)
        {
            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }

        public void ApplyLayout(float laneWidthPx, float trainWidthFactor)
        {
            if (root == null) return;

            float twf = Mathf.Clamp(trainWidthFactor, 0.35f, 0.65f);
            float trainWidthPx = Mathf.Max(8f, laneWidthPx * twf);

            if (Mathf.Abs(trainWidthPx - _lastTrainWidthPx) < 0.01f)
                return;

            _lastTrainWidthPx = trainWidthPx;

            float carH = Mathf.Max(8f, trainWidthPx * Mathf.Clamp(carHeightMul, 0.90f, 1.80f));
            float linkH = Mathf.Max(4f, carH * Mathf.Clamp(linkHeightMulOfCar, 0.08f, 0.28f));

            float totalH = (carH * 3f) + (linkH * 2f);
            _totalHeightPx = totalH;

            root.sizeDelta = new Vector2(trainWidthPx, totalH);

            SetSegSize(loco, trainWidthPx, carH);
            SetSegSize(car1, trainWidthPx, carH);
            SetSegSize(car2, trainWidthPx, carH);

            float linkW = Mathf.Max(6f, trainWidthPx * Mathf.Clamp(linkWidthMulOfTrain, 0.35f, 0.75f));
            SetSegSize(link1, linkW, linkH);
            SetSegSize(link2, linkW, linkH);

            SetPivotCenter(loco);
            SetPivotCenter(link1);
            SetPivotCenter(car1);
            SetPivotCenter(link2);
            SetPivotCenter(car2);

            float y = 0f;

            SetChildTopY(loco, y); y -= carH;
            SetChildTopY(link1, y); y -= linkH;
            SetChildTopY(car1, y); y -= carH;
            SetChildTopY(link2, y); y -= linkH;
            SetChildTopY(car2, y);
        }

        public void ApplyPhase(int phase, float phase01)
        {
            float a = 1f;
            if (phase == 1)
                a = Mathf.Clamp01(1f - (Mathf.Clamp01(phase01) * 0.15f));

            for (int i = 0; i < _imgs.Length; i++)
            {
                var img = _imgs[i];
                if (img == null) continue;
                var c = img.color;
                c.a = a;
                img.color = c;
            }
        }

        private static void SetSegSize(RectTransform rt, float w, float h)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 1.0f);
            rt.anchorMax = new Vector2(0.5f, 1.0f);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetPivotCenter(RectTransform rt)
        {
            if (rt == null) return;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetChildTopY(RectTransform rt, float topY)
        {
            if (rt == null) return;
            float h = rt.sizeDelta.y;
            rt.anchoredPosition = new Vector2(0f, topY - (h * 0.5f));
        }
    }
}