using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitAnimation_SB1 : MonoBehaviour
{
    public Image text;
    public float speed = 10f;
    public float bounceHeight = 40f;
    public float bounceSpeed = 8f;

    private Vector2 targetPos;

    void Start()
    {
        targetPos = text.rectTransform.anchoredPosition;
        text.rectTransform.anchoredPosition += Vector2.up * 400f;

        StartCoroutine(Drop());
    }

    IEnumerator Drop()
    {
        // DROP
        while (Vector2.Distance(text.rectTransform.anchoredPosition, targetPos) > 1f)
        {
            text.rectTransform.anchoredPosition = Vector2.Lerp(
                text.rectTransform.anchoredPosition,
                targetPos,
                Time.deltaTime * speed
            );
            yield return null;
        }

        text.rectTransform.anchoredPosition = targetPos;

        // BOUNCE UP
        Vector2 bounceTarget = targetPos + Vector2.up * bounceHeight;

        while (Vector2.Distance(text.rectTransform.anchoredPosition, bounceTarget) > 0.5f)
        {
            text.rectTransform.anchoredPosition = Vector2.Lerp(
                text.rectTransform.anchoredPosition,
                bounceTarget,
                Time.deltaTime * bounceSpeed
            );
            yield return null;
        }

        // BOUNCE DOWN
        while (Vector2.Distance(text.rectTransform.anchoredPosition, targetPos) > 0.5f)
        {
            text.rectTransform.anchoredPosition = Vector2.Lerp(
                text.rectTransform.anchoredPosition,
                targetPos,
                Time.deltaTime * bounceSpeed
            );
            yield return null;
        }

        text.rectTransform.anchoredPosition = targetPos;
    }
}
