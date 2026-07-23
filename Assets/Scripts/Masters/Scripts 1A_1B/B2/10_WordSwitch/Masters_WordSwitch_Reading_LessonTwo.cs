using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Masters_WordSwitch_Reading_LessonTwo : Masters_Lesson, IBeginDragHandler, IDragHandler, IEndDragHandler {

    [System.Serializable]
    public class SortingContainer {
        public string categoryId; // Used for matching
        public Button containerButton;
        public RectTransform containerRect;
    }

    [System.Serializable]
    public class SortingCardData {
        public string frontText;
        public string backText;
        public AudioClip voiceOverAudio;
        public string targetCategoryId;
    }

    [Header("Sorting Game Settings")]
    [SerializeField] private SortingContainer[] containers;
    [SerializeField] private List<SortingCardData> cardsData;
    
    [Header("Card UI Setup")]
    [SerializeField] private GameObject activeCardContainer;
    [SerializeField] private RectTransform activeCardRect;
    [SerializeField] private TextMeshProUGUI activeCardTMP;
    
    [Header("Feedback Settings")]
    [SerializeField] private float animationSpeed = 0.5f;
    [SerializeField] private float bounceBackSpeed = 0.4f;
    [SerializeField] private float textDisplayDuration = 1.5f;
    
    [Header("Progression Settings")]
    [SerializeField] private TextMeshProUGUI progressionTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private List<SortingCardData> remainingCards = new List<SortingCardData>();
    private SortingCardData currentCard;
    private int totalCards;
    private bool isAnimating = false;
    private Vector2 originalCardPosition;
    
    private bool isDragging = false;
    private Canvas parentCanvas;

    protected override void Awake() {
        base.Awake();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    protected override void Start() {
        base.Start();

        if (activeCardRect != null) {
            originalCardPosition = activeCardRect.anchoredPosition;
        }

        StartGame();
    }

    private void StartGame() {
        remainingCards.Clear();
        if (cardsData != null) {
            remainingCards.AddRange(cardsData);
        }
        ShuffleList(remainingCards);
        totalCards = remainingCards.Count;
        
        UpdateProgression();
        LoadNextCard();
    }

    private void ShuffleList(List<SortingCardData> list) {
        for (int i = 0; i < list.Count; i++) {
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void UpdateProgression() {
        if (progressionTMP != null) {
            int solvedCount = totalCards - remainingCards.Count;
            progressionTMP.text = $"{solvedCount}/{totalCards}";
        }
    }

    private void LoadNextCard() {
        if (remainingCards.Count == 0) {
            GameWon();
            return;
        }

        currentCard = remainingCards[0];
        
        if (activeCardContainer != null) activeCardContainer.SetActive(true);
        if (activeCardRect != null) {
            activeCardRect.anchoredPosition = originalCardPosition;
            activeCardRect.localScale = Vector3.zero;
            activeCardRect.localEulerAngles = Vector3.zero;
            activeCardRect.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
        
        if (activeCardTMP != null) activeCardTMP.text = currentCard.frontText;
        
        isAnimating = false;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (isAnimating || currentCard == null || activeCardContainer == null || !activeCardContainer.activeSelf) return;

        if (eventData.pointerPress != null && activeCardRect != null && eventData.pointerPress.transform.IsChildOf(activeCardRect.transform) || eventData.pointerPress == activeCardContainer) {
            isDragging = true;
            activeCardRect.DOKill();
            activeCardRect.SetAsLastSibling();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isDragging || activeCardRect == null || parentCanvas == null) return;

        Vector2 movePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)parentCanvas.transform,
            eventData.position,
            parentCanvas.worldCamera,
            out movePos);

        activeCardRect.position = parentCanvas.transform.TransformPoint(movePos);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!isDragging || activeCardRect == null || parentCanvas == null) return;
        isDragging = false;

        SortingContainer matchedContainer = null;

        if (containers != null) {
            foreach (var container in containers) {
                if (container != null && container.containerRect != null && RectTransformUtility.RectangleContainsScreenPoint(container.containerRect, eventData.position, parentCanvas.worldCamera)) {
                    matchedContainer = container;
                    break;
                }
            }
        }

        if (matchedContainer != null) {
            isAnimating = true;

            if (currentCard.targetCategoryId == matchedContainer.categoryId) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                StartCoroutine(CorrectMatchRoutine(matchedContainer));
            } else {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                activeCardRect.DOMove(activeCardRect.parent.TransformPoint(originalCardPosition), bounceBackSpeed).SetEase(Ease.OutBounce).OnComplete(() => {
                    isAnimating = false;
                });
            }
        } else {
            activeCardRect.DOMove(activeCardRect.parent.TransformPoint(originalCardPosition), bounceBackSpeed).SetEase(Ease.OutBounce);
        }
    }

    private IEnumerator CorrectMatchRoutine(SortingContainer targetContainer) {
        // 1. Move to the target shelf container
        if (activeCardRect != null && targetContainer.containerRect != null) {
            activeCardRect.DOMove(targetContainer.containerRect.position, animationSpeed).SetEase(Ease.InCubic);
        }
        
        yield return new WaitForSeconds(animationSpeed);
        
        // 2. Stays on the same side (NO FLIPPING), speak audio pronunciation
        if (currentCard.voiceOverAudio != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(currentCard.voiceOverAudio);
        }

        yield return new WaitForSeconds(textDisplayDuration);
        
        // 3. Shrink and disappear
        if (activeCardRect != null) {
            activeCardRect.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack);
        }
        
        yield return new WaitForSeconds(animationSpeed);
        
        if (activeCardContainer != null) activeCardContainer.SetActive(false);
        
        // 4. Remove from deck and advance
        if (remainingCards.Count > 0) {
            remainingCards.RemoveAt(0);
        }
        UpdateProgression();
        LoadNextCard();
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
