using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_OfferingAHelpingHand_Roleplay_LessonTwo : Masters_Lesson {

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
        public string[] wordBank;
        public string[] validSentences;
        public AudioClip[] playerCorrectAudioClips;
    }

    [System.Serializable]
    public class SceneData {
        public Button sceneButton;
        [Tooltip("add the scene bg and other elements to a parent and add the parent here")]
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

        foreach (var scene in scenes) {
            SceneData sData = scene;
            RectTransform btnRect = scene.sceneButton.GetComponent<RectTransform>();
            scene.sceneButton.onClick.AddListener(() => OnSceneButtonClicked(btnRect, sData));
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

        roleplayPanel.SetActive(false);
        sceneSelectionPanel.SetActive(true);
        playerWritingContainer.SetActive(false);
        npcCloud.SetActive(false);
        playerCloud.SetActive(false);
        checkButton.gameObject.SetActive(false);

        UpdateProgressCount();
        
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
            playerWritingContainer.SetActive(true);
            ClearAndSetPuzzle(turn.wordBank);
        }
    }

    private void ClearAndSetPuzzle(string[] wordBank) {
        // Clear all existing words in slate and bank
        foreach (Transform child in slateWordsParentTransform) {
            Destroy(child.gameObject);
        }
        foreach (Transform child in buttonsParentTransform) {
            Destroy(child.gameObject);
        }

        checkButton.gameObject.SetActive(false);
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

        if (arrangeWordButton.GetIsInBox() == false) {
            // Move to slate
            arrangeWordButton.transform.SetParent(slateWordsParentTransform, false);
            arrangeWordButton.SetIsInBox(true);

            if (slateWordsParentTransform.childCount > 0 && canClickCheck) {
                checkButton.gameObject.SetActive(true);
            }
        } else {
            // Move back to bank
            checkButton.gameObject.SetActive(false);
            arrangeWordButton.SetButtonTextColor(defaultColor);
            arrangeWordButton.transform.SetParent(buttonsParentTransform, false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(arrangeWordButton.GetComponent<RectTransform>());
            arrangeWordButton.SetIsInBox(false);

            if (slateWordsParentTransform.childCount > 0 && canClickCheck) {
                checkButton.gameObject.SetActive(true);
            }
        }
    }

    private void OnRetryButtonClicked() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        Masters_ArrangeWordButton[] arrangeWordButtonArray = slateWordsParentTransform.GetComponentsInChildren<Masters_ArrangeWordButton>();
        foreach (var btn in arrangeWordButtonArray) {
            btn.SetButtonTextColor(defaultColor);
            btn.transform.SetParent(buttonsParentTransform, false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(btn.GetComponent<RectTransform>());
            btn.SetIsInBox(false);
        }
        checkButton.gameObject.SetActive(false);
    }

    private void OnCheckButtonClicked() {
        Masters_ArrangeWordButton[] arrangeWordButtonArray = slateWordsParentTransform.GetComponentsInChildren<Masters_ArrangeWordButton>();

        if (arrangeWordButtonArray.Length == 0) return;

        string playerSentence = "";
        for (int i = 0; i < arrangeWordButtonArray.Length; i++) {
            playerSentence += arrangeWordButtonArray[i].GetButtonString();
            if (i < arrangeWordButtonArray.Length - 1) playerSentence += " ";
        }

        string normalizedPlayerSentence = NormalizeSentence(playerSentence);
        bool isCorrect = false;
        int correctIndex = -1;
        
        for (int i = 0; i < activeScene.turns[currentTurnIndex].validSentences.Length; i++) {
            if (normalizedPlayerSentence == NormalizeSentence(activeScene.turns[currentTurnIndex].validSentences[i])) {
                isCorrect = true;
                correctIndex = i;
                break;
            }
        }

        if (isCorrect) {
            foreach (var btn in arrangeWordButtonArray) {
                btn.SetButtonTextColor(correctColor);
            }
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            float delay = timeBetweenTurns;
            if (activeScene.turns[currentTurnIndex].playerCorrectAudioClips != null && correctIndex >= 0 && correctIndex < activeScene.turns[currentTurnIndex].playerCorrectAudioClips.Length) {
                AudioClip correctClip = activeScene.turns[currentTurnIndex].playerCorrectAudioClips[correctIndex];
                if (correctClip != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(correctClip);
                    delay += correctClip.length;
                }
            }
            
            playerCloud.SetActive(true);
            playerDialogueTMP.text = playerSentence;
            playerWritingContainer.SetActive(false);
            
            canClickCheck = false;
            checkButton.gameObject.SetActive(false);

            currentTurnIndex++;
            Invoke(nameof(StartNextTurn), delay);
        } else {
            foreach (var btn in arrangeWordButtonArray) {
                btn.SetButtonTextColor(incorrectColor);
            }
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private string NormalizeSentence(string s) {
        return System.Text.RegularExpressions.Regex.Replace(s.Trim().ToLowerInvariant(), @"[^a-z0-9]", "");
    }

    private void CompleteScene() {
        completedScenes.Add(activeScene);
        UpdateProgressCount();
        
        if (activeScene.sceneGameObject != null) {
            activeScene.sceneGameObject.SetActive(false);
        }

        activeScene = null;
        roleplayPanel.SetActive(false);
        sceneSelectionPanel.SetActive(true);

        if (completedScenes.Count >= scenes.Length) {
            // All scenes complete
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
