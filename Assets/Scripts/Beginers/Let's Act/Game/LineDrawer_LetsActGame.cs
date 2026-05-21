using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
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

    private UILineRenderer_LetsActGame               _activeDragLine;
    private RectTransform                            _activeDragSource;
    private readonly List<UILineRenderer_LetsActGame> _permanentLines = new();

    public void BeginDragLine(RectTransform fromRect)
    {
        if (lineLayer == null) { Debug.LogError("[LineDrawer] lineLayer NOT assigned!"); return; }
        if (_activeDragLine != null) Destroy(_activeDragLine.gameObject);

        _activeDragSource = fromRect;
        _activeDragLine   = CreateUILine("DragLine", dragColor);

        if (_activeDragLine == null) { Debug.LogError("[LineDrawer] CreateUILine returned null!"); return; }

        _activeDragLine.SetWorldPoints(fromRect.position, fromRect.position);
        Debug.Log($"[LineDrawer] BeginDragLine from world pos {fromRect.position}");

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
        var lr = CreateUILine(correct ? "CorrectLine" : "WrongLine",
                              correct ? correctColor  : wrongColor);
        lr.SetWorldPoints(fromRect.position, toRect.position);
        Debug.Log($"[LineDrawer] CommitLine correct={correct} from={fromRect.position} to={toRect.position}  lr.color={lr.color}  lineLayer={lineLayer.name}");

        if (correct) _permanentLines.Add(lr);
        else         StartCoroutine(FadeDestroy(lr, wrongLineFadeDuration));
    }

    public void ClearAll()
    {
        foreach (var l in _permanentLines) if (l != null) Destroy(l.gameObject);
        _permanentLines.Clear();
        EndDragLine();
    }

    private UILineRenderer_LetsActGame CreateUILine(string goName, Color col)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(lineLayer, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var lr           = go.AddComponent<UILineRenderer_LetsActGame>();
        lr.lineWidth     = lineWidth;
        lr.color         = col;
        lr.raycastTarget = false;
        lr.RefreshCanvas();

        Debug.Log($"[LineDrawer] Created '{goName}'  parent='{lineLayer.name}'  canvas={lr.DebugCanvasInfo()}  rt.rect={rt.rect}");
        return lr;
    }

    private IEnumerator FadeDestroy(UILineRenderer_LetsActGame lr, float duration)
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