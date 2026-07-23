using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer_BB2 : Graphic
{
    [HideInInspector]
    public float lineWidth = 8f;

    private Vector2 _localFrom;
    private Vector2 _localTo;
    private bool _hasPoints;

    private Canvas _rootCanvas;
    private Camera _camera;

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

        foreach (Canvas c in canvases)
        {
            if (c.isRootCanvas)
            {
                _rootCanvas = c;
                break;
            }
        }

        if (_rootCanvas == null && canvases.Length > 0)
            _rootCanvas = canvases[canvases.Length - 1];

        if (_rootCanvas != null)
        {
            _camera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _rootCanvas.worldCamera;
        }
    }

    public string DebugCanvasInfo()
    {
        return $"Canvas : {(_rootCanvas ? _rootCanvas.name : "NULL")}   Camera : {(_camera ? _camera.name : "NULL")}";
    }

    //====================================================
    // Public API
    //====================================================

    public void SetWorldPoints(Vector3 worldFrom, Vector3 worldTo)
    {
        _localFrom = WorldToLocal(worldFrom);
        _localTo = WorldToLocal(worldTo);

        _hasPoints = true;
        SetVerticesDirty();
    }

    public void SetMixedPoints(Vector3 worldFrom, Vector2 screenTo)
    {
        _localFrom = WorldToLocal(worldFrom);
        _localTo = ScreenToLocal(screenTo);

        _hasPoints = true;
        SetVerticesDirty();
    }

    public void ClearPoints()
    {
        _hasPoints = false;
        SetVerticesDirty();
    }

    //====================================================
    // Mesh
    //====================================================

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (!_hasPoints)
            return;

        Vector2 from = _localFrom;
        Vector2 to = _localTo;

        if ((to - from).sqrMagnitude < 0.01f)
            return;

        Vector2 direction = (to - from).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (lineWidth * 0.5f);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = from + perpendicular;
        vh.AddVert(vertex);

        vertex.position = from - perpendicular;
        vh.AddVert(vertex);

        vertex.position = to - perpendicular;
        vh.AddVert(vertex);

        vertex.position = to + perpendicular;
        vh.AddVert(vertex);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    //====================================================
    // Helpers
    //====================================================

    private Vector2 WorldToLocal(Vector3 worldPosition)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_camera, worldPosition);
        return ScreenToLocal(screenPoint);
    }

    private Vector2 ScreenToLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPoint,
            _camera,
            out Vector2 localPoint);

        return localPoint;
    }
}