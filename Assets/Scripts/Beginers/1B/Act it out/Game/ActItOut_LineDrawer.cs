using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beginners.ActItOut
{
    public class ActItOut_LineDrawer : MonoBehaviour
    {
        [Header("Canvas Layer (REQUIRED)")]
        public RectTransform lineLayer;

        [Header("Appearance")]
        [SerializeField] private float lineWidth = 8f;

        [Header("Colors")]
        [SerializeField] private Color dragColor    = new Color(0.3f,  0.7f,  1f,   0.85f);
        [SerializeField] private Color correctColor = new Color(0.15f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color wrongColor   = new Color(1f,    0.15f, 0.15f, 1f);

        [Header("Wrong-line fade (seconds)")]
        [SerializeField] private float wrongLineFadeDuration = 0.6f;

        private ActItOut_UILineRenderer               _activeDragLine;
        private RectTransform                            _activeDragSource;
        private readonly List<ActItOut_UILineRenderer> _permanentLines = new();

        void Awake()
        {
            ResolveLineLayer();
        }

        void Start()
        {
            ResolveLineLayer();
        }

        private void ResolveLineLayer()
        {
            if (lineLayer == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null) canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    lineLayer = canvas.GetComponent<RectTransform>();
                    Debug.Log($"[ActItOut_LineDrawer] lineLayer was null, auto-assigned to Canvas: {lineLayer.name}");
                }
                return;
            }

            // Check if lineLayer resides in a Prefab Asset (scene is invalid)
            if (!lineLayer.gameObject.scene.IsValid())
            {
                Debug.LogWarning($"[ActItOut_LineDrawer] lineLayer '{lineLayer.name}' is a Prefab Asset. Attempting to find scene instance...");
                
                // Traverse up to find the root of our local hierarchy
                Transform root = transform;
                while (root.parent != null)
                {
                    root = root.parent;
                }

                // Search for the GameObject named lineLayer.name inside our scene hierarchy
                Transform found = FindChildRecursive(root, lineLayer.name);
                if (found != null)
                {
                    lineLayer = found.GetComponent<RectTransform>();
                    Debug.Log($"[ActItOut_LineDrawer] Successfully resolved prefab reference to scene instance: {lineLayer.name}");
                }
                else
                {
                    // Fallback: search the entire active scene
                    GameObject sceneGo = GameObject.Find(lineLayer.name);
                    if (sceneGo != null && sceneGo.scene.IsValid())
                    {
                        lineLayer = sceneGo.GetComponent<RectTransform>();
                        Debug.Log($"[ActItOut_LineDrawer] Resolved prefab reference using GameObject.Find: {lineLayer.name}");
                    }
                    else
                    {
                        // Ultimate fallback: parent Canvas
                        Canvas canvas = GetComponentInParent<Canvas>();
                        if (canvas != null)
                        {
                            lineLayer = canvas.GetComponent<RectTransform>();
                            Debug.LogWarning($"[ActItOut_LineDrawer] Could not find scene instance for '{lineLayer.name}'. Falling back to Parent Canvas.");
                        }
                    }
                }
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public void BeginDragLine(RectTransform fromRect)
        {
            ResolveLineLayer();
            if (lineLayer == null) { Debug.LogError("[ActItOut_LineDrawer] lineLayer NOT assigned!"); return; }
            if (_activeDragLine != null) Destroy(_activeDragLine.gameObject);

            _activeDragSource = fromRect;
            _activeDragLine   = CreateUILine("DragLine", dragColor);

            if (_activeDragLine == null) { Debug.LogError("[ActItOut_LineDrawer] CreateUILine returned null!"); return; }

            _activeDragLine.SetWorldPoints(fromRect.position, fromRect.position);
            Debug.Log($"[ActItOut_LineDrawer] BeginDragLine from world pos {fromRect.position}");

            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxLineDraw);
        }

        public void UpdateDragLine(Vector2 screenPos)
        {
            if (_activeDragLine == null || _activeDragSource == null) return;
            _activeDragLine.SetMixedPoints(_activeDragSource.position, screenPos);
        }

        public void EndDragLine()
        {
            if (_activeDragLine != null) { Destroy(_activeDragLine.gameObject); _activeDragLine = null; }
            _activeDragSource = null;
        }

        public void CommitLine(RectTransform fromRect, RectTransform toRect, bool correct)
        {
            ResolveLineLayer();
            if (lineLayer == null) { Debug.LogError("[ActItOut_LineDrawer] lineLayer NOT assigned!"); return; }

            var lr = CreateUILine(correct ? "CorrectLine" : "WrongLine",
                                  correct ? correctColor  : wrongColor);
            lr.SetWorldPoints(fromRect.position, toRect.position);
            Debug.Log($"[ActItOut_LineDrawer] CommitLine correct={correct} from={fromRect.position} to={toRect.position}  lr.color={lr.color}  lineLayer={lineLayer.name}");

            if (correct) _permanentLines.Add(lr);
            else         StartCoroutine(FadeDestroy(lr, wrongLineFadeDuration));
        }

        public void ClearAll()
        {
            foreach (var l in _permanentLines) if (l != null) Destroy(l.gameObject);
            _permanentLines.Clear();
            EndDragLine();
        }

        private ActItOut_UILineRenderer CreateUILine(string goName, Color col)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(lineLayer, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var lr           = go.AddComponent<ActItOut_UILineRenderer>();
            lr.lineWidth     = lineWidth;
            lr.color         = col;
            lr.raycastTarget = false;
            lr.RefreshCanvas();

            Debug.Log($"[ActItOut_LineDrawer] Created '{goName}'  parent='{lineLayer.name}'  canvas={lr.DebugCanvasInfo()}  rt.rect={rt.rect}");
            return lr;
        }

        private IEnumerator FadeDestroy(ActItOut_UILineRenderer lr, float duration)
        {
            Color startCol = lr.color;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Color c = startCol; c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                lr.color = c;
                yield return null;
            }
            if (lr != null) Destroy(lr.gameObject);
        }
    }
}
