using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer_BB2 : MonoBehaviour
{
    [Header("Canvas Layer")]
    [SerializeField] private RectTransform lineLayer;

    [Header("Appearance")]
    [SerializeField] private float lineWidth = 8f;

    [Header("Colours")]
    [SerializeField] private Color dragColor = new Color(0.25f, 0.65f, 1f, 0.9f);
    [SerializeField] private Color correctColor = new Color(0.18f, 0.85f, 0.25f, 1f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Wrong Line")]
    [SerializeField] private float wrongFadeTime = 0.45f;

    private UILineRenderer_BB2 currentDragLine;
    private RectTransform dragSource;

    private readonly List<UILineRenderer_BB2> permanentLines =
        new List<UILineRenderer_BB2>();

    //---------------------------------------------------------------------
    // Begin Drag
    //---------------------------------------------------------------------

    public void BeginDragLine(RectTransform from)
    {
        EndDragLine();

        dragSource = from;

        currentDragLine = CreateLine("Drag Line", dragColor);

        if (currentDragLine == null)
            return;

        currentDragLine.SetWorldPoints(from.position, from.position);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxLineDraw);
    }

    //---------------------------------------------------------------------
    // Update Drag
    //---------------------------------------------------------------------

    public void UpdateDragLine(Vector2 screenPosition)
    {
        if (currentDragLine == null)
            return;

        if (dragSource == null)
            return;

        currentDragLine.SetMixedPoints(
            dragSource.position,
            screenPosition);
    }

    //---------------------------------------------------------------------
    // Cancel Drag
    //---------------------------------------------------------------------

    public void EndDragLine()
    {
        if (currentDragLine != null)
        {
            Destroy(currentDragLine.gameObject);
            currentDragLine = null;
        }

        dragSource = null;
    }

    //---------------------------------------------------------------------
    // Final Line
    //---------------------------------------------------------------------

    public void CommitLine(
        RectTransform from,
        RectTransform to,
        bool correct)
    {
        EndDragLine();

        UILineRenderer_BB2 line =
            CreateLine(correct ? "Correct Line" : "Wrong Line",
            correct ? correctColor : wrongColor);

        if (line == null)
            return;

        line.SetWorldPoints(from.position, to.position);

        if (correct)
        {
            permanentLines.Add(line);
        }
        else
        {
            StartCoroutine(FadeAndDestroy(line));
        }
    }

    //---------------------------------------------------------------------
    // Clear
    //---------------------------------------------------------------------

    public void ClearAll()
    {
        EndDragLine();

        foreach (var line in permanentLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }

        permanentLines.Clear();
    }

    //---------------------------------------------------------------------
    // Create
    //---------------------------------------------------------------------

    private UILineRenderer_BB2 CreateLine(string objectName, Color colour)
    {
        if (lineLayer == null)
        {
            Debug.LogError("Line Layer Missing!");
            return null;
        }

        GameObject go = new GameObject(objectName);

        go.transform.SetParent(lineLayer, false);

        RectTransform rt = go.AddComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;

        UILineRenderer_BB2 line =
            go.AddComponent<UILineRenderer_BB2>();

        line.lineWidth = lineWidth;
        line.color = colour;
        line.raycastTarget = false;

        line.RefreshCanvas();

        return line;
    }

    //---------------------------------------------------------------------
    // Fade Wrong Line
    //---------------------------------------------------------------------

    private IEnumerator FadeAndDestroy(UILineRenderer_BB2 line)
    {
        Color start = line.color;

        float t = 0f;

        while (t < wrongFadeTime)
        {
            if (line == null)
                yield break;

            t += Time.deltaTime;

            Color c = start;
            c.a = Mathf.Lerp(1f, 0f, t / wrongFadeTime);

            line.color = c;

            yield return null;
        }

        if (line != null)
            Destroy(line.gameObject);
    }
}