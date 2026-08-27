using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class Masters_ReadingR02DraggableTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public string partnerText;
    public CollocationHub correctHub;
    public Vector3 startPosition;
    public bool isPlaced = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Masters_Collocations_Reading_LessonTwo controller;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(string text, CollocationHub hub, Masters_Collocations_Reading_LessonTwo mainController) {
        partnerText = text;
        correctHub = hub;
        controller = mainController;
        isPlaced = false;

        TMP_Text tmp = GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) {
            tmp.gameObject.SetActive(true);
            tmp.text = partnerText;
            tmp.color = Color.white;
        }

        if (canvasGroup != null) {
            canvasGroup.alpha = 1.0f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void SetStartPosition(Vector3 pos) {
        startPosition = pos;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (isPlaced) return;

        if (canvasGroup != null) {
            canvasGroup.alpha = 0.85f;
            canvasGroup.blocksRaycasts = false;
        }

        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.12f, 0.2f);
    }

    public void OnDrag(PointerEventData eventData) {
        if (isPlaced || canvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (isPlaced) return;

        if (canvasGroup != null) {
            canvasGroup.alpha = 1.0f;
            canvasGroup.blocksRaycasts = true;
        }

        if (controller != null) {
            controller.OnTileDropped(this);
        }
    }

    public void ReturnToStart() {
        transform.DOKill();
        transform.DOMove(startPosition, 0.35f).SetEase(Ease.OutQuad);
    }

    public void ResetToStartPosition() {
        isPlaced = false;
        if (canvasGroup != null) {
            canvasGroup.alpha = 1.0f;
            canvasGroup.blocksRaycasts = true;
        }
        transform.DOKill();
        transform.position = startPosition;
    }

    public void LockInSlot(Vector3 slotWorldPos) {
        isPlaced = true;
        if (canvasGroup != null) {
            canvasGroup.blocksRaycasts = false;
        }
        transform.DOKill();
        transform.DOMove(slotWorldPos, 0.25f).SetEase(Ease.OutBack).OnComplete(() => {
            transform.DOPunchScale(Vector3.one * 0.18f, 0.25f);
        });
    }
}