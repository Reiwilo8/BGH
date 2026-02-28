/*using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Games.Fishing.Editor
{
    public static class FishingVisualOverlayBuilder
    {
        private const string CanvasName = "FishingOverlay";
        private const string RootName = "Root";

        private const float PanelWidth = 120f;

        [MenuItem("Tools/Project/Create Fishing Overlay")]
        public static void Create()
        {
            var canvasGO = GameObject.Find(CanvasName);
            if (canvasGO == null)
                canvasGO = CreateCanvas(CanvasName);

            var view = canvasGO.GetComponent<FishingVisualOverlayView>();
            if (view == null)
                view = canvasGO.AddComponent<FishingVisualOverlayView>();

            var root = FindOrCreateUI(canvasGO.transform, RootName);
            StretchFull(root.GetComponent<RectTransform>());

            var tensionRoot = FindOrCreateUI(root.transform, "TensionRoot");
            StretchFull(tensionRoot.GetComponent<RectTransform>());

            var boardRoot = FindOrCreateUI(root.transform, "BoardRoot");
            StretchFull(boardRoot.GetComponent<RectTransform>());
            SetOffsets(boardRoot.GetComponent<RectTransform>(), PanelWidth, PanelWidth, 0f, 0f);

            var grayL = FindOrCreateImage(tensionRoot.transform, "TensionGrayLeft");
            SetupSidePanel(grayL.rectTransform, left: true, PanelWidth);
            grayL.color = NewColor(0.26f, 0.26f, 0.26f, 1f);
            grayL.type = Image.Type.Simple;
            grayL.raycastTarget = false;

            var fillL = FindOrCreateImage(tensionRoot.transform, "TensionFillLeft");
            SetupSidePanel(fillL.rectTransform, left: true, PanelWidth);
            fillL.color = NewColor(0.78f, 0.70f, 0.30f, 1f);
            fillL.type = Image.Type.Filled;
            fillL.fillMethod = Image.FillMethod.Vertical;
            fillL.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillL.fillAmount = 0f;
            fillL.raycastTarget = false;

            var grayR = FindOrCreateImage(tensionRoot.transform, "TensionGrayRight");
            SetupSidePanel(grayR.rectTransform, left: false, PanelWidth);
            grayR.color = NewColor(0.26f, 0.26f, 0.26f, 1f);
            grayR.type = Image.Type.Simple;
            grayR.raycastTarget = false;

            var fillR = FindOrCreateImage(tensionRoot.transform, "TensionFillRight");
            SetupSidePanel(fillR.rectTransform, left: false, PanelWidth);
            fillR.color = NewColor(0.78f, 0.70f, 0.30f, 1f);
            fillR.type = Image.Type.Filled;
            fillR.fillMethod = Image.FillMethod.Vertical;
            fillR.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillR.fillAmount = 0f;
            fillR.raycastTarget = false;

            var floatImg = FindOrCreateImage(boardRoot.transform, "Float");
            SetupFloat(floatImg.rectTransform, 40f);
            floatImg.color = NewColor(0.78f, 0.30f, 0.26f, 1f);
            floatImg.type = Image.Type.Simple;
            floatImg.raycastTarget = false;
            floatImg.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvasGO);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(tensionRoot);
            EditorUtility.SetDirty(boardRoot);

            Selection.activeGameObject = canvasGO;
        }

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject FindOrCreateUI(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image FindOrCreateImage(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null)
            {
                var imgExisting = t.GetComponent<Image>();
                if (imgExisting != null) return imgExisting;
                return t.gameObject.AddComponent<Image>();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetOffsets(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void SetupSidePanel(RectTransform rt, bool left, float width)
        {
            if (left)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(width, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(width, 0f);
            }
        }

        private static void SetupFloat(RectTransform rt, float size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
        }

        private static Color NewColor(float r, float g, float b, float a)
        {
            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
        }
    }
}*/