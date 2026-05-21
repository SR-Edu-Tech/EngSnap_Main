using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer_LetsActGame : Graphic
{
    [HideInInspector] public float lineWidth = 8f;

    private Vector2 _localFrom;
    private Vector2 _localTo;
    private bool    _hasPoints;

    private Canvas _rootCanvas;
    private Camera _cam;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        RefreshCanvas();
    }

    public void RefreshCanvas()
    {
        Canvas[] canvases = GetComponentsInParent<Canvas>(true);
        _rootCanvas = null;
        foreach (var c in canvases)
            if (c.isRootCanvas) { _rootCanvas = c; break; }
        if (_rootCanvas == null && canvases.Length > 0)
            _rootCanvas = canvases[canvases.Length - 1];

        if (_rootCanvas != null)
            _cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;

        Debug.Log($"[UILineRenderer] RefreshCanvas → rootCanvas={(_rootCanvas ? _rootCanvas.name : "NULL")}  renderMode={(_rootCanvas ? _rootCanvas.renderMode.ToString() : "?")}  cam={(_cam ? _cam.name : "NULL")}");
    }

    // World pos (RectTransform.position) → both ends
    public void SetWorldPoints(Vector3 worldFrom, Vector3 worldTo)
    {
        _localFrom = WorldToLocal(worldFrom);
        _localTo   = WorldToLocal(worldTo);
        _hasPoints = true;
        Debug.Log($"[UILineRenderer] SetWorldPoints  localFrom={_localFrom}  localTo={_localTo}  rectSize={rectTransform.rect.size}");
        SetVerticesDirty();
    }

    // From = world pos, To = raw screen pos (eventData.position)
    public void SetMixedPoints(Vector3 worldFrom, Vector2 screenTo)
    {
        _localFrom = WorldToLocal(worldFrom);
        _localTo   = ScreenToLocal(screenTo);
        _hasPoints = true;
        SetVerticesDirty();
    }

    public void ClearPoints()
    {
        _hasPoints = false;
        SetVerticesDirty();
    }

    // Helper for LineDrawer debug log
    public string DebugCanvasInfo() =>
        $"canvas={(_rootCanvas ? _rootCanvas.name : "NULL")} cam={(_cam ? _cam.name : "NULL")}";

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (!_hasPoints) return;

        Vector2 from = _localFrom;
        Vector2 to   = _localTo;
        float sqDist = (to - from).sqrMagnitude;
        if (sqDist < 0.01f) return;

        Vector2 dir  = (to - from).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f);

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = from + perp; vh.AddVert(v);
        v.position = from - perp; vh.AddVert(v);
        v.position = to   - perp; vh.AddVert(v);
        v.position = to   + perp; vh.AddVert(v);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);

        Debug.Log($"[UILineRenderer] OnPopulateMesh  from={from}  to={to}  perp={perp}  lineWidth={lineWidth}  color={color}");
    }

    private Vector2 WorldToLocal(Vector3 worldPos)
    {
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(_cam, worldPos);
        return ScreenToLocal(screenPt);
    }

    private Vector2 ScreenToLocal(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, screenPos, _cam, out Vector2 local);
        return local;
    }
}