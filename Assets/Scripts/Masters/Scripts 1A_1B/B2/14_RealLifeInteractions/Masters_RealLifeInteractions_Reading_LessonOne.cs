using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reading Lesson 1 for Unit 14 Real Life Interactions.
/// Combines Scene Selection with Vertical Slate Sentence Ordering (Jumbled Words adaptation).
/// </summary>
public class Masters_RealLifeInteractions_Reading_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SentencePage {
        public string[] sentences;
        public AudioClip pageAudioClip;
    }

    [System.Serializable]
    public class SceneData {
        public string sceneName;
        public Button sceneButton;
        public SentencePage[] pages;
        [HideInInspector] public bool isCompleted;
    }

    [Header("Scene Selection")]
    [SerializeField] protected GameObject sceneSelectionPanel;
    [SerializeField] protected SceneData[] scenes;
    [SerializeField] protected TextMeshProUGUI sceneProgressTMP;

    [Header("Sentence Ordering UI")]
    [SerializeField] protected GameObject orderingPanel;
    [SerializeField] protected TextMeshProUGUI pageProgressTMP;
    [SerializeField] protected Masters_ArrangeWordButton wordButtonPrefab;
    [SerializeField] protected Transform buttonsParentTransform;
    [SerializeField] protected Transform slateWordsParentTransform;
    [SerializeField] protected Button checkButton;
    [SerializeField] protected Button retryButton;

    [Header("Colors & Timing")]
    [SerializeField] private Color defaultColor = Color.black;
    [SerializeField] protected Color correctColor = new Color(0.1f, 0.7f, 0.1f, 1f);
    [SerializeField] protected Color incorrectColor = Color.red;
    [SerializeField] protected float animationTime = 0.3f;

    [Header("Next Lesson")]
    [SerializeField] protected Masters_LessonSO nextLessonSO;

    protected int activeSceneIndex = -1;
    protected int activePageIndex = 0;
    protected SentencePage currentPage;
    protected bool canClickCheck;

    private Transform GetBankContainer() {
        if (buttonsParentTransform == null) return null;
        Transform border = buttonsParentTransform.Find("Border");
        return border != null ? border : buttonsParentTransform;
    }

    private Transform GetSlateContainer() {
        if (slateWordsParentTransform == null) return null;
        Transform border = slateWordsParentTransform.Find("Border");
        return border != null ? border : slateWordsParentTransform;
    }

    private int GetActiveSentenceCount(Transform container) {
        int count = 0;
        if (container == null) return 0;
        foreach (Transform child in container) {
            if (wordButtonPrefab != null && child == wordButtonPrefab.transform) continue;
            if (child.gameObject.activeSelf) count++;
        }
        return count;
    }

    protected override void Start() {
        base.Start();

        if (wordButtonPrefab != null) wordButtonPrefab.gameObject.SetActive(false);
        if (orderingPanel != null) orderingPanel.SetActive(false);
        if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(true);
        if (checkButton != null) checkButton.gameObject.SetActive(false);

        if (checkButton != null) checkButton.onClick.AddListener(OnCheckButtonClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryButtonClicked);

        for (int i = 0; i < scenes.Length; i++) {
            int index = i;
            if (scenes[i].sceneButton != null) {
                scenes[i].sceneButton.onClick.AddListener(() => OnSceneSelected(index));
            }
        }

        UpdateSceneProgress();
    }

    protected virtual void OnSceneSelected(int index) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        activeSceneIndex = index;
        activePageIndex = 0;

        if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(false);
        if (orderingPanel != null) orderingPanel.SetActive(true);

        LoadCurrentPage();
    }

    protected virtual void LoadCurrentPage() {
        ClearContainers();

        if (activeSceneIndex < 0 || activeSceneIndex >= scenes.Length) return;
        SceneData currentScene = scenes[activeSceneIndex];

        if (activePageIndex >= currentScene.pages.Length) {
            // Scene complete! Return to selector
            currentScene.isCompleted = true;
            if (currentScene.sceneButton != null) currentScene.sceneButton.interactable = false;

            if (orderingPanel != null) orderingPanel.SetActive(false);
            if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(true);

            UpdateSceneProgress();
            CheckAllScenesCompleted();
            return;
        }

        currentPage = currentScene.pages[activePageIndex];
        if (pageProgressTMP != null) {
            pageProgressTMP.text = $"{activePageIndex + 1}/{currentScene.pages.Length}";
        }

        if (checkButton != null) checkButton.gameObject.SetActive(false);
        StartCoroutine(SpawnSentenceButtonsCoroutine(currentPage.sentences));
    }

    private IEnumerator SpawnSentenceButtonsCoroutine(string[] targetSentences) {
        canClickCheck = false;
        List<string> shuffledList = new List<string>(targetSentences);
        
        // Shuffle sentences
        for (int i = 0; i < shuffledList.Count; i++) {
            string temp = shuffledList[i];
            int randomIndex = Random.Range(i, shuffledList.Count);
            shuffledList[i] = shuffledList[randomIndex];
            shuffledList[randomIndex] = temp;
        }

        Transform bankContainer = GetBankContainer();
        for (int i = 0; i < shuffledList.Count; i++) {
            yield return new WaitForSeconds(animationTime);
            if (wordButtonPrefab != null && bankContainer != null) {
                Masters_ArrangeWordButton btnObj = Instantiate(wordButtonPrefab, bankContainer);
                btnObj.gameObject.SetActive(true);
                btnObj.SetButtonTextAndStringTMP(shuffledList[i]);
                btnObj.SetButtonTextColor(defaultColor);
                btnObj.SetIsInBox(false);

                Masters_ArrangeWordButton btnRef = btnObj;
                Button uiBtn = btnObj.GetComponent<Button>();
                if (uiBtn != null) {
                    uiBtn.onClick.AddListener(() => OnSentenceButtonClicked(btnRef));
                }
            }
        }

        if (bankContainer != null && bankContainer.TryGetComponent(out LayoutGroup layoutGroup)) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(bankContainer.GetComponent<RectTransform>());
        }
        canClickCheck = true;
    }

    private void OnSentenceButtonClicked(Masters_ArrangeWordButton btn) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        Transform slateContainer = GetSlateContainer();
        Transform bankContainer = GetBankContainer();

        if (!btn.GetIsInBox()) {
            // Move from Bank to Slate
            if (slateContainer != null) btn.transform.SetParent(slateContainer, false);
            btn.SetIsInBox(true);

            if (bankContainer != null && GetActiveSentenceCount(bankContainer) == 0 && canClickCheck) {
                if (checkButton != null) checkButton.gameObject.SetActive(true);
            }
        } else {
            // Move from Slate back to Bank
            if (checkButton != null) checkButton.gameObject.SetActive(false);
            btn.SetButtonTextColor(defaultColor);
            if (bankContainer != null) btn.transform.SetParent(bankContainer, false);
            btn.SetIsInBox(false);
        }

        if (slateContainer != null && slateContainer.TryGetComponent(out LayoutGroup slateGroup)) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(slateContainer.GetComponent<RectTransform>());
        }
        if (bankContainer != null && bankContainer.TryGetComponent(out LayoutGroup bankGroup)) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(bankContainer.GetComponent<RectTransform>());
        }
    }

    private void OnCheckButtonClicked() {
        Transform slateContainer = GetSlateContainer();
        if (slateContainer == null || currentPage == null) return;

        List<Masters_ArrangeWordButton> placedButtons = new List<Masters_ArrangeWordButton>();
        for (int i = 0; i < slateContainer.childCount; i++) {
            Transform child = slateContainer.GetChild(i);
            if (wordButtonPrefab != null && child == wordButtonPrefab.transform) continue;
            if (child.TryGetComponent(out Masters_ArrangeWordButton btn) && child.gameObject.activeSelf) {
                placedButtons.Add(btn);
            }
        }

        if (placedButtons.Count == 0) return;

        int correctCount = 0;
        for (int i = 0; i < placedButtons.Count && i < currentPage.sentences.Length; i++) {
            if (placedButtons[i].GetButtonString() == currentPage.sentences[i]) {
                correctCount++;
                placedButtons[i].SetButtonTextColor(correctColor);
            } else {
                placedButtons[i].SetButtonTextColor(incorrectColor);
            }
        }

        if (correctCount == currentPage.sentences.Length && placedButtons.Count == currentPage.sentences.Length) {
            canClickCheck = false;
            if (checkButton != null) checkButton.gameObject.SetActive(false);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (currentPage.pageAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentPage.pageAudioClip);
                StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(OnPageSuccess));
            } else {
                StartCoroutine(DelayedSuccessRoutine());
            }
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private IEnumerator DelayedSuccessRoutine() {
        yield return new WaitForSeconds(1.2f);
        OnPageSuccess();
    }

    private void OnPageSuccess() {
        activePageIndex++;
        LoadCurrentPage();
    }

    private void OnRetryButtonClicked() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        LoadCurrentPage();
    }

    private void ClearContainers() {
        Transform slateContainer = GetSlateContainer();
        if (slateContainer != null) {
            for (int i = slateContainer.childCount - 1; i >= 0; i--) {
                Transform child = slateContainer.GetChild(i);
                if (wordButtonPrefab != null && (child == wordButtonPrefab.transform || child.IsChildOf(wordButtonPrefab.transform))) continue;
                Destroy(child.gameObject);
            }
        }
        Transform bankContainer = GetBankContainer();
        if (bankContainer != null) {
            for (int i = bankContainer.childCount - 1; i >= 0; i--) {
                Transform child = bankContainer.GetChild(i);
                if (wordButtonPrefab != null && (child == wordButtonPrefab.transform || child.IsChildOf(wordButtonPrefab.transform))) continue;
                Destroy(child.gameObject);
            }
        }
    }

    protected virtual void UpdateSceneProgress() {
        if (sceneProgressTMP == null) return;
        int completed = 0;
        for (int i = 0; i < scenes.Length; i++) {
            if (scenes[i].isCompleted) completed++;
        }
        sceneProgressTMP.text = $"{completed}/{scenes.Length}";
    }

    protected virtual void CheckAllScenesCompleted() {
        for (int i = 0; i < scenes.Length; i++) {
            if (!scenes[i].isCompleted) return;
        }

        // All scenes finished!
        if (nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
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
