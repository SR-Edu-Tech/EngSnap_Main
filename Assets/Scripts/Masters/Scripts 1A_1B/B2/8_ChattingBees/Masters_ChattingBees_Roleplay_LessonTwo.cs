using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Chatting Bees - Roleplay Lesson 2.
/// Simulates a scene-based roleplay where the player selects a scene and forms a reply
/// from a bank of phrase buttons in response to an NPC.
/// </summary>
public class Masters_ChattingBees_Roleplay_LessonTwo : Masters_Lesson {

    public enum TurnType {
        NPCTurn,
        PlayerTurn
    }

    [System.Serializable]
    public class RoleplayTurn {
        public TurnType turnType;
        
        [Header("NPC Settings (If NPC Turn)")]
        public string npcDialogueText;
        public AudioClip npcAudioClip;
        
        [Header("Player Settings (If Player Turn)")]
        [Tooltip("The jumbled phrases presented to the player to form their sentence.")]
        public string[] wordBank;
        [Tooltip("Acceptable sentence formats constructed from the word bank.")]
        public string[] validSentences;
        public AudioClip[] playerCorrectAudioClips;
    }

    [System.Serializable]
    public class SceneData {
        public Button sceneButton;
        [Tooltip("Add the scene bg and other elements to a parent and link it here to toggle visibility.")]
        public GameObject sceneGameObject; 
        public RoleplayTurn[] turns;
    }

    [Header("Scene Selection Settings")]
    [SerializeField] private SceneData[] scenes;
    [SerializeField] private GameObject sceneSelectionPanel;
    [SerializeField] private TextMeshProUGUI progressCountTMP;

    [Header("Roleplay UI Settings")]
    [SerializeField] private GameObject roleplayPanel;
    [SerializeField] private GameObject npcCloud;
    [SerializeField] private TextMeshProUGUI npcDialogueTMP;
    [SerializeField] private GameObject playerCloud;
    [SerializeField] private TextMeshProUGUI playerDialogueTMP;
    
    [Header("Player Writing Settings")]
    [SerializeField] private GameObject playerWritingContainer;
    [SerializeField] private Button wordButtonReference;
    [SerializeField] private Transform buttonsParentTransform;
    [SerializeField] private Transform slateWordsParentTransform;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button retryButton;
    
    [Header("Colors & Timing")]
    [SerializeField] private Color defaultColor = Color.black;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private float timeBetweenEachAnimation = 0.1f;
    [SerializeField] private float timeBetweenTurns = 2f;
    
    [Header("Next Lesson")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentTurnIndex;
    private SceneData activeScene;
    private HashSet<SceneData> completedScenes = new HashSet<SceneData>();

    private bool canClickCheck;

    protected override void Awake() {
        base.Awake();

        // Bind Scene Selection Buttons
        foreach (var scene in scenes) {
            SceneData sData = scene;
            if (scene.sceneButton != null) {
                RectTransform btnRect = scene.sceneButton.GetComponent<RectTransform>();
                scene.sceneButton.onClick.AddListener(() => OnSceneButtonClicked(btnRect, sData));
            }
        }

        if (checkButton != null) {
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }
        if (retryButton != null) {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
    }

    protected override void Start() {
        base.Start();

        // Initial UI State
        roleplayPanel.SetActive(false);
        sceneSelectionPanel.SetActive(true);
        playerWritingContainer.SetActive(false);
        npcCloud.SetActive(false);
        playerCloud.SetActive(false);
        if (checkButton != null) checkButton.gameObject.SetActive(false);

        UpdateProgressCount();
        
        // Hide all scene backgrounds
        foreach (var scene in scenes) {
            if (scene.sceneGameObject != null) {
                scene.sceneGameObject.SetActive(false);
            }
        }
    }

    private void UpdateProgressCount() {
        if (progressCountTMP != null) {
            progressCountTMP.text = $"{completedScenes.Count}/{scenes.Length}";
        }
    }

    /// <summary>
    /// Triggered when the user taps one of the initial Scene Selection buttons.
    /// </summary>
    private void OnSceneButtonClicked(RectTransform rectTransform, SceneData sceneData) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        rectTransform.DOKill(true);
        rectTransform.localScale = Vector3.one;
        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        if (completedScenes.Contains(sceneData)) {
            // Already completed this scene
            return;
        }

        StartScene(sceneData);
    }

    private void StartScene(SceneData sceneData) {
        activeScene = sceneData;
        currentTurnIndex = 0;

        sceneSelectionPanel.SetActive(false);
        roleplayPanel.SetActive(true);

        if (activeScene.sceneGameObject != null) {
            activeScene.sceneGameObject.SetActive(true);
        }

        npcCloud.SetActive(false);
        playerCloud.SetActive(false);
        playerWritingContainer.SetActive(false);

        StartNextTurn();
    }

    /// <summary>
    /// Processes whether the next turn belongs to the NPC (showing dialogue)
    /// or the Player (showing word buttons).
    /// </summary>
    private void StartNextTurn() {
        if (activeScene == null) return;

        if (currentTurnIndex >= activeScene.turns.Length) {
            // Scene complete
            CompleteScene();
            return;
        }

        RoleplayTurn turn = activeScene.turns[currentTurnIndex];
        
        playerCloud.SetActive(false);
        playerWritingContainer.SetActive(false);

        if (turn.turnType == TurnType.NPCTurn) {
            // NPC Turn
            npcCloud.SetActive(true);
            npcDialogueTMP.text = turn.npcDialogueText;
            
            float delay = timeBetweenTurns;
            if (turn.npcAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(turn.npcAudioClip);
                delay = turn.npcAudioClip.length;
            }

            currentTurnIndex++;
            Invoke(nameof(StartNextTurn), delay);

        } else if (turn.turnType == TurnType.PlayerTurn) {
            // Player Turn
            playerWritingContainer.SetActive(true);
            ClearAndSetPuzzle(turn.wordBank);
        }
    }

    /// <summary>
    /// Resets the drag-and-drop Word UI and spawns the new word buttons.
    /// </summary>
    private void ClearAndSetPuzzle(string[] wordBank) {
        foreach (Transform child in slateWordsParentTransform) {
            Destroy(child.gameObject);
        }
        foreach (Transform child in buttonsParentTransform) {
            Destroy(child.gameObject);
        }

        if (checkButton != null) checkButton.gameObject.SetActive(false);
        canClickCheck = false;

        StartCoroutine(SpawnButtonCoroutine(wordBank));
    }

    private IEnumerator SpawnButtonCoroutine(string[] wordBank) {
        int length = wordBank.Length;
        HashSet<int> randomSpawnHashSet = new HashSet<int>();

        while (randomSpawnHashSet.Count != length) {
            int i = Random.Range(0, length);
            if (!randomSpawnHashSet.Contains(i)) {
                randomSpawnHashSet.Add(i);

                yield return new WaitForSeconds(timeBetweenEachAnimation);

                GameObject spawnedButtonGameObject = Instantiate(wordButtonReference.gameObject);
                spawnedButtonGameObject.transform.SetParent(buttonsParentTransform, false);
                spawnedButtonGameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(spawnedButtonGameObject.GetComponent<RectTransform>());

                if (spawnedButtonGameObject.TryGetComponent(out Masters_ArrangeWordButton arrangeWordButton)) {
                    arrangeWordButton.SetButtonTextAndStringTMP(wordBank[i]);

                    TextMeshProUGUI tmp = spawnedButtonGameObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) {
                        tmp.enableAutoSizing = true;
                    }

                    Button spawnedButton = spawnedButtonGameObject.GetComponent<Button>();
                    spawnedButton.onClick.AddListener(() => {
                        OnArrangeWordButtonClicked(arrangeWordButton);
                    });
                }
            }
        }
        if (buttonsParentTransform is RectTransform buttonsRect) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsRect);
        }
        canClickCheck = true;
    }

    /// <summary>
    /// Toggles a word button between the "Word Bank" and the "Sentence Slate".
    /// </summary>
    private void OnArrangeWordButtonClicked(Masters_ArrangeWordButton clickedWordButton) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        
        RectTransform rectTransform = clickedWordButton.GetComponent<RectTransform>();
        bool movingToSlate = clickedWordButton.transform.parent == buttonsParentTransform;
        Transform targetParent = movingToSlate ? slateWordsParentTransform : buttonsParentTransform;
        
        clickedWordButton.transform.SetParent(targetParent, false);

        // Keep Auto Sizing enabled so phrases fit cleanly in both the slate and word bank boxes
        TextMeshProUGUI tmp = clickedWordButton.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) {
            tmp.enableAutoSizing = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        if (buttonsParentTransform is RectTransform buttonsRect) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsRect);
        }
        if (slateWordsParentTransform is RectTransform slateRect) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(slateRect);
        }

        // Check if there are any words in the slate to enable the Check Button
        if (checkButton != null) {
            checkButton.gameObject.SetActive(slateWordsParentTransform.childCount > 0);
        }
    }

    /// <summary>
    /// Validates the current slate sentence against the acceptable valid sentence structures.
    /// </summary>
    private void OnCheckButtonClicked() {
        if (!canClickCheck) return;

        string currentSentence = "";
        foreach (Transform child in slateWordsParentTransform) {
            if (child.TryGetComponent(out Masters_ArrangeWordButton arrangeWordButton)) {
                currentSentence += arrangeWordButton.GetButtonString() + " ";
            }
        }
        currentSentence = currentSentence.Trim();

        RoleplayTurn turn = activeScene.turns[currentTurnIndex];
        bool isCorrect = false;
        int matchedIndex = -1;

        if (turn.validSentences != null) {
            for (int i = 0; i < turn.validSentences.Length; i++) {
                if (currentSentence.Equals(turn.validSentences[i].Trim(), System.StringComparison.OrdinalIgnoreCase)) {
                    isCorrect = true;
                    matchedIndex = i;
                    break;
                }
            }
        }

        if (isCorrect) {
            HandleCorrectAnswer(currentSentence, turn, matchedIndex);
        } else {
            HandleWrongAnswer();
        }
    }

    private void HandleCorrectAnswer(string finalSentence, RoleplayTurn turn, int matchedIndex) {
        canClickCheck = false;
        if (checkButton != null) checkButton.gameObject.SetActive(false);

        foreach (Transform child in slateWordsParentTransform) {
            if (child.TryGetComponent(out Image buttonImage)) {
                buttonImage.color = correctColor;
            }
        }

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        // Calculate voiceover duration
        float delay = timeBetweenTurns;
        if (turn.playerCorrectAudioClips != null && turn.playerCorrectAudioClips.Length > 0) {
            AudioClip clip = (matchedIndex >= 0 && matchedIndex < turn.playerCorrectAudioClips.Length && turn.playerCorrectAudioClips[matchedIndex] != null)
                ? turn.playerCorrectAudioClips[matchedIndex]
                : turn.playerCorrectAudioClips[0];

            if (clip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
                delay = clip.length + 0.5f;
            }
        }

        // Show player dialogue bubble
        playerWritingContainer.SetActive(false);
        playerDialogueTMP.text = finalSentence;
        playerCloud.SetActive(true);

        currentTurnIndex++;
        Invoke(nameof(StartNextTurn), delay);
    }

    private void HandleWrongAnswer() {
        canClickCheck = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);

        foreach (Transform child in slateWordsParentTransform) {
            if (child.TryGetComponent(out Image buttonImage)) {
                buttonImage.color = incorrectColor;
            }
        }

        // Shake animation
        slateWordsParentTransform.DOKill(true);
        slateWordsParentTransform.DOShakePosition(0.5f, new Vector3(20, 0, 0), 10, 90, false, true).OnComplete(() => {
            // Reset colors
            foreach (Transform child in slateWordsParentTransform) {
                if (child.TryGetComponent(out Image buttonImage)) {
                    buttonImage.color = defaultColor;
                }
            }
            canClickCheck = true;
        });
    }

    private void OnRetryButtonClicked() {
        ClearAndSetPuzzle(activeScene.turns[currentTurnIndex].wordBank);
    }

    private void CompleteScene() {
        completedScenes.Add(activeScene);
        UpdateProgressCount();

        roleplayPanel.SetActive(false);
        sceneSelectionPanel.SetActive(true);
        
        // Darken or disable the scene button so they know it's done
        if (activeScene.sceneButton != null) {
            activeScene.sceneButton.interactable = false;
        }

        if (completedScenes.Count >= scenes.Length) {
            // All scenes complete
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
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
