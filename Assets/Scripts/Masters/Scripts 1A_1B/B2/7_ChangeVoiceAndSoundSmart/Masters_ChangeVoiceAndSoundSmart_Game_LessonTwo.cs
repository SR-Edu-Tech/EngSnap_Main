using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChangeVoiceAndSoundSmart_Game_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class ArrangeWordsPuzzle {
        [TextArea(2, 4)]
        [Tooltip("The active sentence to display to the player.")]
        public string activeSentenceText;
        
        [Tooltip("The words of the passive sentence in correct order.")]
        public string[] buttonTMPArray; 
        
        public AudioClip sentenceAudioClip;
    }

    [Header("Game Data")]
    [SerializeField]
    private float gameDuration = 60f;
    [SerializeField]
    private float bonusTimeOnSolve = 5f;
    [SerializeField]
    private int maxRounds = 6;
    [SerializeField]
    private ArrangeWordsPuzzle[] arrangeWordsPuzzleArray;
    
    [Header("UI Elements")]
    [SerializeField]
    private TextMeshProUGUI activeSentenceTMP; 
    [SerializeField]
    private TextMeshProUGUI timerTMP;
    [SerializeField]
    private Button wordButtonReference;
    [SerializeField]
    private Transform buttonsParentTransform;
    [SerializeField]
    private Transform slateWordsParentTransform;
    [SerializeField]
    private Button checkButton;
    [SerializeField]
    private Button retryButton;
    [SerializeField]
    private RectTransform slateRectTransform;
    [SerializeField]
    private Transform completedPanelRectTransform;
    [SerializeField]
    private TextMeshProUGUI progressCountTMP;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    [Header("Settings")]
    [SerializeField]
    private Color correctColor = Color.green;
    [SerializeField]
    private Color incorrectColor = Color.red;
    [SerializeField]
    private Color defaultColor = Color.white;
    [SerializeField]
    private float timeBetweenEachAnimation = 0.1f, animationTime = 0.5f;

    private int arrangeWordsPuzzleIndex;
    private ArrangeWordsPuzzle currentArrangeWordsPuzzle;
    private bool canClickCheck;
    
    private float timeRemaining;
    private bool isGameActive;
    private int roundsCompleted;
    private List<ArrangeWordsPuzzle> shuffledPuzzles;

    protected override void Awake() {
        base.Awake();

        if (checkButton != null) checkButton.onClick.AddListener(OnCheckButtonClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryButtonClicked);

        StartGame();
    }

    private void StartGame() {
        shuffledPuzzles = new List<ArrangeWordsPuzzle>(arrangeWordsPuzzleArray);
        for (int i = 0; i < shuffledPuzzles.Count; i++) {
            ArrangeWordsPuzzle temp = shuffledPuzzles[i];
            int randomIndex = Random.Range(i, shuffledPuzzles.Count);
            shuffledPuzzles[i] = shuffledPuzzles[randomIndex];
            shuffledPuzzles[randomIndex] = temp;
        }

        arrangeWordsPuzzleIndex = 0;
        roundsCompleted = 0;
        timeRemaining = gameDuration;
        isGameActive = true;

        UpdateTimerUI();

        if (nextButton != null) nextButton.interactable = false;
        if (retryButton != null) retryButton.gameObject.SetActive(false);

        ClearAndSetPuzzle();
    }

    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0) {
            timeRemaining = 0;
            GameOver();
        }
        UpdateTimerUI();
    }

    private void UpdateTimerUI() {
        if (timerTMP != null) {
            timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        }
    }

    private void OnRetryButtonClicked() {
        completedPanelRectTransform.DOScale(Vector3.zero, animationTime).SetEase(Ease.OutExpo).OnComplete(() => {
            slateRectTransform.gameObject.SetActive(true);
            completedPanelRectTransform.localScale = Vector2.one;
            completedPanelRectTransform.gameObject.SetActive(false);
            
            StartGame();
        });
    }

    private void OnCheckButtonClicked() {
        Masters_ArrangeWordButton[] arrangeWordButtonArray = slateWordsParentTransform.transform.
            GetComponentsInChildren<Masters_ArrangeWordButton>();

        if(arrangeWordButtonArray.Length == 0) {
            return;
        }

        int totalToProceed = currentArrangeWordsPuzzle.buttonTMPArray.Length;
        int currentCorrectAmount = 0;

        for(int i = 0; i < currentArrangeWordsPuzzle.buttonTMPArray.Length; i++) {
            if (arrangeWordButtonArray[i].GetButtonString() == currentArrangeWordsPuzzle.buttonTMPArray[i]) {
                // Correct
                currentCorrectAmount++;
                arrangeWordButtonArray[i].SetButtonTextColor(correctColor);
            } else {
                // Incorrect
                arrangeWordButtonArray[i].SetButtonTextColor(incorrectColor);
            }
        }

        if(currentCorrectAmount == totalToProceed) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            timeRemaining += bonusTimeOnSolve;
            UpdateTimerUI();
            if (timerTMP != null) {
                timerTMP.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 10, 1);
            }

            if (currentArrangeWordsPuzzle.sentenceAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentArrangeWordsPuzzle.sentenceAudioClip);
                StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(ClearAndSetNewPuzzle));
            } else {
                Invoke(nameof(ClearAndSetNewPuzzle), 1.5f);
            }
            canClickCheck = false;
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        checkButton.gameObject.SetActive(false);
    }

    private void ClearAndSetNewPuzzle() {
        Transform[] slateGameObjectArray = slateWordsParentTransform.GetComponentsInChildren<Transform>();

        for (int i = slateGameObjectArray.Length - 1; i > 0; i--) {
            Destroy(slateGameObjectArray[i].gameObject);
        }

        roundsCompleted++;
        ClearAndSetPuzzle();
    }

    private void GameOver() {
        isGameActive = false;

        Transform[] slateGameObjectArray = slateWordsParentTransform.GetComponentsInChildren<Transform>();
        for (int i = slateGameObjectArray.Length - 1; i > 0; i--) {
            Destroy(slateGameObjectArray[i].gameObject);
        }

        Transform[] bankGameObjectArray = buttonsParentTransform.GetComponentsInChildren<Transform>();
        for (int i = bankGameObjectArray.Length - 1; i > 0; i--) {
            Destroy(bankGameObjectArray[i].gameObject);
        }

        slateRectTransform.DOScale(Vector3.zero, animationTime).SetEase(Ease.OutExpo).OnComplete(() => {
            slateRectTransform.gameObject.SetActive(false);
            completedPanelRectTransform.gameObject.SetActive(true);
            if (retryButton != null) retryButton.gameObject.SetActive(true);
        });

        if (roundsCompleted >= maxRounds) {
            if (activeSentenceTMP != null) activeSentenceTMP.text = "You Won!";
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        } else {
            if (activeSentenceTMP != null) activeSentenceTMP.text = "Time's Up!";
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (nextButton != null) {
                nextButton.interactable = false;
            }
        }
    }

    private void ClearAndSetPuzzle() {
        if (roundsCompleted >= maxRounds || arrangeWordsPuzzleIndex >= shuffledPuzzles.Count) {
            GameOver();
            return;
        }

        currentArrangeWordsPuzzle = shuffledPuzzles[arrangeWordsPuzzleIndex++];
        
        if (activeSentenceTMP != null) {
            activeSentenceTMP.text = currentArrangeWordsPuzzle.activeSentenceText;
            activeSentenceTMP.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f);
        }

        if (progressCountTMP != null) {
            progressCountTMP.text = $"{roundsCompleted + 1}/{maxRounds}";
        }
        
        StartCoroutine(SpawnButtonCoroutine(currentArrangeWordsPuzzle.buttonTMPArray.Length));
    }

    private IEnumerator SpawnButtonCoroutine(int length) {
        HashSet<int> randomSpawnHashSet = new HashSet<int>();
        while (randomSpawnHashSet.Count != length) {
            int i = Random.Range(0, length);
            if (!randomSpawnHashSet.Contains(i)) {
                randomSpawnHashSet.Add(i);

                yield return new WaitForSeconds(timeBetweenEachAnimation);

                GameObject spawnedButtonGameObject = Instantiate(wordButtonReference.gameObject);
                spawnedButtonGameObject.transform.SetParent(buttonsParentTransform, false);
                spawnedButtonGameObject.SetActive(true);

                if (spawnedButtonGameObject.TryGetComponent(out Masters_ArrangeWordButton arrangeWordButton)) {
                    arrangeWordButton.SetButtonTextAndStringTMP(currentArrangeWordsPuzzle.buttonTMPArray[i]);
                    
                    LayoutRebuilder.ForceRebuildLayoutImmediate(spawnedButtonGameObject.GetComponent<RectTransform>());

                    Button spawnedButton = spawnedButtonGameObject.GetComponent<Button>();
                    spawnedButton.onClick.AddListener(() => {
                        OnArrangeWordButtonClicked(arrangeWordButton);
                    });
                }
            }
        }
        canClickCheck = true;
    }

    private void OnArrangeWordButtonClicked(Masters_ArrangeWordButton arrangeWordButton) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        HorizontalLayoutGroup slateWordsHorizontalLayoutGroup = slateWordsParentTransform.
                GetComponent<HorizontalLayoutGroup>();
        HorizontalLayoutGroup wordsHorizontalLayoutGroup = buttonsParentTransform.GetComponent<HorizontalLayoutGroup>();
        
        if (arrangeWordButton.GetIsInBox() == false) {
            // Is Down - move it up to the slate

            arrangeWordButton.transform.SetParent(slateWordsParentTransform, false);
            arrangeWordButton.SetIsInBox(true);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(arrangeWordButton.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(slateWordsParentTransform.GetComponent<RectTransform>());

            if(buttonsParentTransform.childCount == 0 && canClickCheck) {
                // Enable check button
                checkButton.gameObject.SetActive(true);
            }
        } else {
            // Is Up - move it back down to the bank

            checkButton.gameObject.SetActive(false);
            arrangeWordButton.SetButtonTextColor(defaultColor);
            arrangeWordButton.transform.SetParent(buttonsParentTransform, false);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(arrangeWordButton.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsParentTransform.GetComponent<RectTransform>());
            
            arrangeWordButton.SetIsInBox(false);
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
