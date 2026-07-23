using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Unit 1: Polished Communication - Roleplay Lesson Two (RP02: Free Scene / Tell the Same News Two Ways).
/// Standalone Book 2A base controller that simulates a scene-based roleplay where the player selects a scene and forms two sequential tone-contrasting turns (Formal and Informal) from a bank of phrase buttons in response to or alongside an NPC/prompt.
/// </summary>
public class Masters_PolishedCommunication_Roleplay_LessonTwo : Masters_Lesson {

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
        [Tooltip("The jumbled phrase chips presented to the player to form their sentence.")]
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
    [SerializeField] protected SceneData[] scenes;
    [SerializeField] protected GameObject sceneSelectionPanel;
    [SerializeField] protected TextMeshProUGUI progressCountTMP;

    [Header("Roleplay UI Settings")]
    [SerializeField] protected GameObject roleplayPanel;
    [SerializeField] protected GameObject npcCloud;
    [SerializeField] protected TextMeshProUGUI npcDialogueTMP;
    [SerializeField] protected GameObject playerCloud;
    [SerializeField] protected TextMeshProUGUI playerDialogueTMP;
    
    [Header("Player Writing Settings")]
    [SerializeField] protected GameObject playerWritingContainer;
    [SerializeField] protected Button wordButtonReference;
    [SerializeField] protected Transform buttonsParentTransform;
    [SerializeField] protected Transform slateWordsParentTransform;
    [SerializeField] protected TextMeshProUGUI slateSentenceTMP;
    [SerializeField] protected Button undoButton;
    [SerializeField] protected Button checkButton;
    [SerializeField] protected Button retryButton;
    
    [Header("Colors & Timing")]
    [SerializeField] protected Color defaultColor = Color.white;
    [SerializeField] protected Color correctColor = Color.green;
    [SerializeField] protected Color incorrectColor = Color.red;
    [SerializeField] protected float timeBetweenEachAnimation = 0.05f;
    [SerializeField] protected float timeBetweenTurns = 2f;
    
    [Header("Next Lesson")]
    [SerializeField] protected Masters_LessonSO nextLessonSO;

    [Header("Hint Settings")]
    [SerializeField] protected GameObject hintPanel;
    [SerializeField] protected TextMeshProUGUI hintTMP;

    protected int currentTurnIndex;
    protected SceneData activeScene;
    protected HashSet<SceneData> completedScenes = new HashSet<SceneData>();
    protected List<Masters_ArrangeWordButton> activeSlateWords = new List<Masters_ArrangeWordButton>();

    protected bool canClickCheck;
    protected int wrongAttemptsOnCurrentTurn = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;

        if (scenes != null) {
            foreach (var scene in scenes) {
                SceneData sData = scene;
                if (scene.sceneButton != null) {
                    RectTransform btnRect = scene.sceneButton.GetComponent<RectTransform>();
                    scene.sceneButton.onClick.AddListener(() => OnSceneButtonClicked(btnRect, sData));
                }
            }
        }

        if (checkButton != null) {
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }
        if (retryButton != null) {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
        if (undoButton != null) {
            undoButton.onClick.AddListener(OnUndoButtonClicked);
        }
    }

    protected override void Start() {
        base.Start();

        if (roleplayPanel != null) roleplayPanel.SetActive(false);
        if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(true);
        if (playerWritingContainer != null) playerWritingContainer.SetActive(false);
        if (npcCloud != null) npcCloud.SetActive(false);
        if (playerCloud != null) playerCloud.SetActive(false);
        if (checkButton != null) checkButton.gameObject.SetActive(false);
        if (undoButton != null) undoButton.gameObject.SetActive(false);

        UpdateProgressCount();
        
        if (scenes != null) {
            foreach (var scene in scenes) {
                if (scene.sceneGameObject != null) {
                    scene.sceneGameObject.SetActive(false);
                }
            }
        }
    }

    protected virtual void UpdateProgressCount() {
        if (progressCountTMP != null && scenes != null) {
            progressCountTMP.text = $"{completedScenes.Count}/{scenes.Length}";
        }
    }

    protected virtual void OnSceneButtonClicked(RectTransform rectTransform, SceneData sceneData) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (rectTransform != null) {
            rectTransform.DOKill(true);
            rectTransform.localScale = Vector3.one;
            rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        }

        if (completedScenes.Contains(sceneData)) {
            return;
        }

        StartScene(sceneData);
    }

    protected virtual void StartScene(SceneData sceneData) {
        activeScene = sceneData;
        currentTurnIndex = 0;

        if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(false);
        if (roleplayPanel != null) roleplayPanel.SetActive(true);

        if (activeScene.sceneGameObject != null) {
            activeScene.sceneGameObject.SetActive(true);
        }

        if (npcCloud != null) npcCloud.SetActive(false);
        if (playerCloud != null) playerCloud.SetActive(false);
        if (playerWritingContainer != null) playerWritingContainer.SetActive(false);

        StartNextTurn();
    }

    protected virtual void StartNextTurn() {
        if (activeScene == null || activeScene.turns == null) return;

        if (currentTurnIndex >= activeScene.turns.Length) {
            CompleteScene();
            return;
        }

        RoleplayTurn turn = activeScene.turns[currentTurnIndex];
        
        if (playerCloud != null) playerCloud.SetActive(false);
        if (playerWritingContainer != null) playerWritingContainer.SetActive(false);

        if (turn.turnType == TurnType.NPCTurn) {
            if (npcCloud != null) npcCloud.SetActive(true);
            if (npcDialogueTMP != null) npcDialogueTMP.text = turn.npcDialogueText;
            
            float delay = timeBetweenTurns;
            if (turn.npcAudioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(turn.npcAudioClip);
                delay = turn.npcAudioClip.length;
            }

            currentTurnIndex++;
            Invoke(nameof(StartNextTurn), delay);

        } else if (turn.turnType == TurnType.PlayerTurn) {
            if (playerWritingContainer != null) playerWritingContainer.SetActive(true);
            wrongAttemptsOnCurrentTurn = 0;
            HideHint();
            ClearAndSetPuzzle(turn.wordBank);
        }
    }

    protected virtual void ClearAndSetPuzzle(string[] wordBank) {
        activeSlateWords.Clear();
        if (wrongAttemptsOnCurrentTurn == 0) {
            HideHint();
        }
        if (slateWordsParentTransform != null) {
            foreach (Transform child in slateWordsParentTransform) {
                if (child.name != "SentenceDisplay" && child.GetComponent<TextMeshProUGUI>() == null) {
                    Destroy(child.gameObject);
                }
            }
        }
        if (buttonsParentTransform != null) {
            foreach (Transform child in buttonsParentTransform) {
                Destroy(child.gameObject);
            }
        }

        if (checkButton != null) checkButton.gameObject.SetActive(false);
        if (undoButton != null) undoButton.gameObject.SetActive(false);
        if (slateWordsParentTransform != null) {
            Button slateBtn = slateWordsParentTransform.GetComponent<Button>();
            if (slateBtn != null) slateBtn.interactable = false;
        }
        canClickCheck = false;

        EnsureSlateSentenceTMP();
        if (slateSentenceTMP != null) {
            slateSentenceTMP.text = "";
            slateSentenceTMP.color = defaultColor == Color.white ? Color.black : defaultColor;
        }

        if (wordBank != null) {
            StartCoroutine(SpawnButtonCoroutine(wordBank));
        }
    }

    protected virtual void EnsureSlateSentenceTMP() {
        if (slateSentenceTMP == null && slateWordsParentTransform != null) {
            slateSentenceTMP = slateWordsParentTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (slateSentenceTMP == null) {
                GameObject tmpGo = new GameObject("SentenceDisplay");
                tmpGo.transform.SetParent(slateWordsParentTransform, false);
                slateSentenceTMP = tmpGo.AddComponent<TextMeshProUGUI>();
                slateSentenceTMP.color = Color.black;
                slateSentenceTMP.alignment = TextAlignmentOptions.Center;
                slateSentenceTMP.fontSize = 40;
                slateSentenceTMP.enableAutoSizing = true;
                slateSentenceTMP.fontSizeMin = 22;
                slateSentenceTMP.fontSizeMax = 44;
                RectTransform rt = slateSentenceTMP.GetComponent<RectTransform>();
                if (rt != null) {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }

        if (slateWordsParentTransform != null) {
            Button slateBtn = slateWordsParentTransform.GetComponent<Button>();
            if (slateBtn == null) slateBtn = slateWordsParentTransform.gameObject.AddComponent<Button>();
            slateBtn.onClick.RemoveAllListeners();
            slateBtn.onClick.AddListener(OnUndoButtonClicked);
            slateBtn.interactable = (activeSlateWords.Count > 0);
        }
        if (undoButton != null) {
            undoButton.onClick.RemoveAllListeners();
            undoButton.onClick.AddListener(OnUndoButtonClicked);
        }
    }

    protected virtual IEnumerator SpawnButtonCoroutine(string[] wordBank) {
        int length = wordBank.Length;
        HashSet<int> randomSpawnHashSet = new HashSet<int>();

        while (randomSpawnHashSet.Count != length) {
            int i = Random.Range(0, length);
            if (!randomSpawnHashSet.Contains(i)) {
                randomSpawnHashSet.Add(i);

                yield return new WaitForSeconds(timeBetweenEachAnimation);

                if (wordButtonReference != null && buttonsParentTransform != null) {
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
                        if (spawnedButton != null) {
                            spawnedButton.onClick.AddListener(() => {
                                OnArrangeWordButtonClicked(arrangeWordButton);
                            });
                        }
                    }
                }
            }
        }
        if (buttonsParentTransform is RectTransform buttonsRect) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsRect);
        }
        canClickCheck = true;
    }

    protected virtual void OnArrangeWordButtonClicked(Masters_ArrangeWordButton clickedWordButton) {
        if (!canClickCheck) return;
        if (clickedWordButton == null || activeSlateWords.Contains(clickedWordButton)) return;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        clickedWordButton.gameObject.SetActive(false);
        activeSlateWords.Add(clickedWordButton);

        UpdateSlateSentenceDisplay();
    }

    protected virtual void OnUndoButtonClicked() {
        if (!canClickCheck || activeSlateWords.Count == 0) return;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        int lastIndex = activeSlateWords.Count - 1;
        Masters_ArrangeWordButton lastButton = activeSlateWords[lastIndex];
        activeSlateWords.RemoveAt(lastIndex);

        if (lastButton != null) {
            lastButton.gameObject.SetActive(true);
            if (buttonsParentTransform is RectTransform buttonsRect) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsRect);
            }
        }

        UpdateSlateSentenceDisplay();
    }

    protected virtual void UpdateSlateSentenceDisplay() {
        EnsureSlateSentenceTMP();

        string currentSentence = "";
        for (int i = 0; i < activeSlateWords.Count; i++) {
            if (activeSlateWords[i] != null) {
                currentSentence += activeSlateWords[i].GetButtonString() + " ";
            }
        }
        currentSentence = currentSentence.Trim();

        if (slateSentenceTMP != null) {
            slateSentenceTMP.text = currentSentence;
        }

        bool hasWords = (activeSlateWords.Count > 0);
        if (checkButton != null) {
            checkButton.gameObject.SetActive(hasWords);
        }
        if (undoButton != null) {
            undoButton.gameObject.SetActive(hasWords);
        }
        if (slateWordsParentTransform != null) {
            Button slateBtn = slateWordsParentTransform.GetComponent<Button>();
            if (slateBtn != null) slateBtn.interactable = hasWords;
        }
    }

    protected virtual void OnCheckButtonClicked() {
        if (!canClickCheck) return;

        string currentSentence = "";
        for (int i = 0; i < activeSlateWords.Count; i++) {
            if (activeSlateWords[i] != null) {
                currentSentence += activeSlateWords[i].GetButtonString() + " ";
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
            HandleWrongAnswer(turn);
        }
    }

    protected virtual void HandleCorrectAnswer(string finalSentence, RoleplayTurn turn, int matchedIndex) {
        wrongAttemptsOnCurrentTurn = 0;
        HideHint();
        canClickCheck = false;
        if (checkButton != null) checkButton.gameObject.SetActive(false);
        if (undoButton != null) undoButton.gameObject.SetActive(false);
        if (slateWordsParentTransform != null) {
            Button slateBtn = slateWordsParentTransform.GetComponent<Button>();
            if (slateBtn != null) slateBtn.interactable = false;
        }

        if (slateSentenceTMP != null) {
            slateSentenceTMP.color = correctColor;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        float delay = timeBetweenTurns;
        if (turn.playerCorrectAudioClips != null && turn.playerCorrectAudioClips.Length > 0 && Masters_AudioManager.Instance != null) {
            AudioClip clip = (matchedIndex >= 0 && matchedIndex < turn.playerCorrectAudioClips.Length && turn.playerCorrectAudioClips[matchedIndex] != null)
                ? turn.playerCorrectAudioClips[matchedIndex]
                : turn.playerCorrectAudioClips[0];

            if (clip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
                delay = clip.length + 0.5f;
            }
        }

        if (playerWritingContainer != null) playerWritingContainer.SetActive(false);
        if (playerDialogueTMP != null) playerDialogueTMP.text = finalSentence;
        if (playerCloud != null) playerCloud.SetActive(true);

        currentTurnIndex++;
        Invoke(nameof(StartNextTurn), delay);
    }

    protected virtual void HandleWrongAnswer(RoleplayTurn turn = null) {
        wrongAttemptsOnCurrentTurn++;
        canClickCheck = false;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        if (slateSentenceTMP != null) {
            slateSentenceTMP.color = incorrectColor;
        }

        if (slateWordsParentTransform != null) {
            slateWordsParentTransform.DOKill(true);
            slateWordsParentTransform.DOShakePosition(0.5f, new Vector3(20, 0, 0), 10, 90, false, true).OnComplete(() => {
                if (slateSentenceTMP != null) {
                    slateSentenceTMP.color = defaultColor == Color.white ? Color.black : defaultColor;
                }
                canClickCheck = true;
            });
        } else {
            canClickCheck = true;
        }

        if (wrongAttemptsOnCurrentTurn >= 2) {
            if (turn == null && activeScene != null && activeScene.turns != null && currentTurnIndex < activeScene.turns.Length) {
                turn = activeScene.turns[currentTurnIndex];
            }
            ShowHint(turn);
        }
    }

    protected virtual void EnsureHintUI() {
        if (hintTMP == null) {
            Transform existingHint = (playerWritingContainer != null) ? playerWritingContainer.transform.Find("HintDisplay") : transform.Find("HintDisplay");
            if (existingHint != null) {
                hintTMP = existingHint.GetComponent<TextMeshProUGUI>();
            }

            if (hintTMP == null && slateWordsParentTransform != null && slateWordsParentTransform.parent != null) {
                GameObject hintGo = new GameObject("HintDisplay");
                hintGo.transform.SetParent(slateWordsParentTransform.parent, false);
                hintTMP = hintGo.AddComponent<TextMeshProUGUI>();
                hintTMP.color = new Color(1f, 0.85f, 0.2f);
                hintTMP.alignment = TextAlignmentOptions.Center;
                hintTMP.fontSize = 32;
                hintTMP.enableAutoSizing = true;
                hintTMP.fontSizeMin = 20;
                hintTMP.fontSizeMax = 36;

                TextMeshProUGUI refTmp = slateSentenceTMP != null ? slateSentenceTMP : GetComponentInChildren<TextMeshProUGUI>(true);
                if (refTmp != null && refTmp.font != null) {
                    hintTMP.font = refTmp.font;
                }

                RectTransform rt = hintTMP.GetComponent<RectTransform>();
                if (rt != null) {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.sizeDelta = new Vector2(0, 60);
                    if (slateWordsParentTransform is RectTransform slateRt) {
                        rt.anchoredPosition = new Vector2(0, slateRt.anchoredPosition.y + 15f);
                    } else {
                        rt.anchoredPosition = new Vector2(0, 100);
                    }
                }
                hintTMP.gameObject.SetActive(false);
            }
        }
    }

    protected virtual void ShowHint(RoleplayTurn turn) {
        if (turn == null || turn.validSentences == null || turn.validSentences.Length == 0) return;

        string correctSentence = turn.validSentences[0].Trim();
        string hintMessage = $"Hint: {correctSentence}";

        EnsureHintUI();

        if (hintTMP != null) {
            hintTMP.text = hintMessage;
            if (hintPanel != null) {
                hintPanel.SetActive(true);
            } else {
                hintTMP.gameObject.SetActive(true);
            }

            hintTMP.transform.DOKill(true);
            hintTMP.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 10, 1f);
        }
    }

    protected virtual void HideHint() {
        if (hintPanel != null) hintPanel.SetActive(false);
        if (hintTMP != null) hintTMP.gameObject.SetActive(false);
    }

    protected virtual void OnRetryButtonClicked() {
        if (activeScene != null && activeScene.turns != null && currentTurnIndex < activeScene.turns.Length) {
            ClearAndSetPuzzle(activeScene.turns[currentTurnIndex].wordBank);
        }
    }

    protected virtual void CompleteScene() {
        if (activeScene != null) {
            completedScenes.Add(activeScene);
            if (activeScene.sceneButton != null) {
                activeScene.sceneButton.interactable = false;
            }
        }
        UpdateProgressCount();

        if (roleplayPanel != null) roleplayPanel.SetActive(false);
        if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(true);
        
        if (scenes != null && completedScenes.Count >= scenes.Length) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (topic != Masters_Topic.None && Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
