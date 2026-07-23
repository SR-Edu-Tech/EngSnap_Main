using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_AbbreviationsAndAcronyms_Reading_LessonThree : Masters_Lesson {

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
    [SerializeField] private float textDisplayDuration = 2.0f;
    
    [Header("Progression Settings")]
    [SerializeField] private TextMeshProUGUI progressionTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private List<SortingCardData> remainingCards = new List<SortingCardData>();
    private SortingCardData currentCard;
    private int totalCards;
    private bool isAnimating = false;
    private Vector2 originalCardPosition;

    protected override void Awake() {
        base.Awake();
        
        foreach (var container in containers) {
            SortingContainer c = container; // capture for closure
            if (c.containerButton != null) {
                c.containerButton.onClick.AddListener(() => OnContainerTapped(c));
            }
        }
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
        remainingCards.AddRange(cardsData);
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
        
        activeCardContainer.SetActive(true);
        activeCardRect.anchoredPosition = originalCardPosition;
        activeCardRect.localScale = Vector3.zero;
        activeCardRect.localEulerAngles = Vector3.zero;
        
        activeCardTMP.text = currentCard.frontText;
        
        activeCardRect.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        isAnimating = false;
    }

    private void OnContainerTapped(SortingContainer tappedContainer) {
        if (isAnimating || currentCard == null) return;
        
        isAnimating = true;

        if (currentCard.targetCategoryId == tappedContainer.categoryId) {
            // Correct Match
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            StartCoroutine(CorrectMatchRoutine(tappedContainer));
        } else {
            // Incorrect Match
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            activeCardRect.DOShakeAnchorPos(0.4f, 20f, 10, 90f, false, true).OnComplete(() => {
                isAnimating = false;
            });
        }
    }

    private IEnumerator CorrectMatchRoutine(SortingContainer targetContainer) {
        // 1. Move to the container
        Vector2 targetPos = targetContainer.containerRect.anchoredPosition;
        activeCardRect.DOAnchorPos(targetPos, animationSpeed).SetEase(Ease.InCubic);
        
        yield return new WaitForSeconds(animationSpeed);
        
        // 2. Flip and show back text
        activeCardRect.DORotate(new Vector3(0, 90, 0), animationSpeed / 2f).OnComplete(() => {
            activeCardTMP.text = currentCard.backText;
            activeCardRect.DORotate(Vector3.zero, animationSpeed / 2f);

            if (currentCard.voiceOverAudio != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(currentCard.voiceOverAudio);
            }
        });

        yield return new WaitForSeconds(animationSpeed + textDisplayDuration);
        
        // 3. Fade out / Shrink
        activeCardRect.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack);
        
        yield return new WaitForSeconds(animationSpeed);
        
        activeCardContainer.SetActive(false);
        
        // 4. Remove from list and load next
        remainingCards.RemoveAt(0);
        UpdateProgression();
        LoadNextCard();
    }

    private void GameWon() {
        nextButton.interactable = true;
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
