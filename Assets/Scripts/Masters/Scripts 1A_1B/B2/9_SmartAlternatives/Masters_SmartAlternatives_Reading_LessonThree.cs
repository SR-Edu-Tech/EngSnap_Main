using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Masters_SmartAlternatives_Reading_LessonThree : Masters_Lesson, IBeginDragHandler, IDragHandler, IEndDragHandler {

    [System.Serializable]
    public class DropZoneRow {
        public string categoryId; 
        public RectTransform rowRect;
    }

    [System.Serializable]
    public class DragCardData {
        public string situationText;
        public string targetCategoryId;
        public AudioClip voiceOverAudio; 
    }

    [Header("Drag and Drop Settings")]
    [SerializeField] private DropZoneRow[] dropZones;
    [SerializeField] private List<DragCardData> cardsData;
    
    [Header("UI Setup")]
    [SerializeField] private GameObject activeCardContainer;
    [SerializeField] private RectTransform activeCardRect;
    [SerializeField] private TextMeshProUGUI activeCardTMP;
    
    [Header("Feedback Settings")]
    [SerializeField] private float animationSpeed = 0.3f;
    [SerializeField] private float bounceBackSpeed = 0.4f;
    [SerializeField] private TextMeshProUGUI progressionTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private List<DragCardData> remainingCards = new List<DragCardData>();
    private DragCardData currentCard;
    private int totalCards;
    private int completedCards = 0;

    private Vector3 originalCardPosition;
    private bool isDragging = false;
    private Canvas parentCanvas;

    protected override void Awake() {
        base.Awake();
        parentCanvas = GetComponentInParent<Canvas>();
        remainingCards.AddRange(cardsData);
        totalCards = remainingCards.Count;
        
        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
        
        UpdateProgression();
    }

    protected override void Start() {
        base.Start();
        if (remainingCards.Count > 0) {
            LoadNextCard();
        } else {
            GameWon();
        }
    }

    private void UpdateProgression() {
        if (progressionTMP != null) {
            progressionTMP.text = $"{completedCards}/{totalCards}";
        }
    }

    private void LoadNextCard() {
        if (remainingCards.Count == 0) {
            GameWon();
            return;
        }

        currentCard = remainingCards[0];
        activeCardTMP.text = currentCard.situationText;
        
        activeCardContainer.SetActive(true);
        activeCardRect.localScale = Vector3.zero;
        
        // Save position for bounce back
        activeCardRect.anchoredPosition = Vector2.zero;
        activeCardRect.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack).OnComplete(() => {
            originalCardPosition = activeCardRect.position;
        });
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (!activeCardContainer.activeSelf) return;

        // Check if we are grabbing the card
        if (eventData.pointerPress != null && eventData.pointerPress.transform.IsChildOf(activeCardRect.transform) || eventData.pointerPress == activeCardContainer) {
            isDragging = true;
            activeCardRect.DOKill();
            activeCardRect.SetAsLastSibling();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isDragging) return;

        Vector2 movePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)parentCanvas.transform,
            eventData.position,
            parentCanvas.worldCamera,
            out movePos);

        activeCardRect.position = parentCanvas.transform.TransformPoint(movePos);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!isDragging) return;
        isDragging = false;

        DropZoneRow matchedZone = null;

        // Find if we dropped over a valid row
        foreach (var zone in dropZones) {
            if (RectTransformUtility.RectangleContainsScreenPoint(zone.rowRect, eventData.position, parentCanvas.worldCamera)) {
                matchedZone = zone;
                break;
            }
        }

        if (matchedZone != null) {
            if (matchedZone.categoryId == currentCard.targetCategoryId) {
                // Correct Match
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                
                // Pin it to the row
                activeCardRect.DOMove(matchedZone.rowRect.position, animationSpeed).SetEase(Ease.OutQuad).OnComplete(() => {
                    if (currentCard.voiceOverAudio != null) {
                        Masters_AudioManager.Instance.StopVoiceOver();
                        Masters_AudioManager.Instance.PlayVoiceOver(currentCard.voiceOverAudio);
                    }
                    
                    activeCardRect.DOScale(Vector3.zero, animationSpeed).SetDelay(0.5f).OnComplete(() => {
                        activeCardContainer.SetActive(false);
                        completedCards++;
                        remainingCards.RemoveAt(0);
                        UpdateProgression();
                        LoadNextCard();
                    });
                });
            } else {
                // Wrong Match - Bounce back
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                // Can show a hint popup here if desired by GDD "bounce back with a hint"
                activeCardRect.DOMove(originalCardPosition, bounceBackSpeed).SetEase(Ease.OutBounce);
            }
        } else {
            // Dropped nowhere, just return
            activeCardRect.DOMove(originalCardPosition, bounceBackSpeed).SetEase(Ease.OutBounce);
        }
    }

    private void GameWon() {
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
        NextButtonAnimation();
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
