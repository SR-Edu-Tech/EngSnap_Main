using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Subclass / Intro Controller for Unit 7 (Collocations): INTRO — The Word Magnet Lab.
/// Implements full 5-step cinematic intro flow:
/// 1. Scene Entry: Fade-in laboratory environment (CanvasGroup 0->1 over 1.0s) & ambient hum / chime.
/// 2. GET Magnet Demo: "a bus" tile floats to GET magnet -> repelled with soft buzz SFX & gentle bounce.
/// 3. CATCH Magnet Demo: "catch" & "a bus" tiles move to CATCH magnet -> SNAP together with click SFX, forming glowing "catch a bus" phrase, plays VO_INTRO_SNAP.
/// 4. ARIA Explanation: ARIA speaks lesson goal ("Some words love to hold hands! Today we learn which words go together — and which never do.") + VO_INTRO_ARIA.
/// 5. START Button: Prominent call-to-action button appears with pulse animation -> loads U7_Hub / completes Intro topic.
/// ARIA is tap-to-replay.
/// </summary>
public class Masters_Collocations_Intro_LessonOne : Masters_PolishedCommunication_Intro_LessonOne {

    [Header("Unit 7 Intro Audio Clips")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip snapAudio;
    [SerializeField] private AudioClip repelSFX;
    [SerializeField] private AudioClip snapSFX;
    [SerializeField] private AudioClip ambientAudio;

    [Header("Intro UI & Characters")]
    [SerializeField] private CanvasGroup introCanvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueTMP;
    [SerializeField] private GameObject dialogueBoxObj;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonTMP;
    [SerializeField] private Image ariaImage;

    [Header("Magnet Stations")]
    [SerializeField] private Transform magnetGet;
    [SerializeField] private Transform magnetCatch;
    [SerializeField] private Transform magnetIdea;
    [SerializeField] private Transform magnetSave;

    [Header("Word Tiles")]
    [SerializeField] private Transform tileABus;
    [SerializeField] private Transform tileCatch;
    [SerializeField] private TextMeshProUGUI tileABusTMP;
    [SerializeField] private TextMeshProUGUI tileCatchTMP;
    [SerializeField] private GameObject completedPhraseGlow;
    [SerializeField] private TextMeshProUGUI completedPhraseTMP;

    private Vector3 tileABusInitialPos;
    private Vector3 tileCatchInitialPos;
    private bool isAriaAudioPlaying = false;
    private bool isIntroCompleted = false;
    private bool isStartClicked = false;

    public bool IsIntroCompleted => isIntroCompleted;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Intro;
        narratorSpeech = null; // Clear base narrator audio to prevent double audio overlap
        FixCanvasAndEventSystem();
        EnsureUIReferencesInitialized();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Intro;
        FixCanvasAndEventSystem();
        EnsureUIReferencesInitialized();
        ApplyRoundedBoxStyle();
        UpdateTitleAndUIComponents();
        SetupAriaTapReplay();

        if (tileABus != null) tileABusInitialPos = tileABus.localPosition;
        if (tileCatch != null) tileCatchInitialPos = tileCatch.localPosition;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        StartCoroutine(PlayIntroCinematicSequence());
    }

    private void ApplyRoundedBoxStyle() {
        Sprite emptyButtonSprite = null;
        Sprite borderSprite = null;
        Sprite spriteGet = null;
        Sprite spriteCatch = null;
        Sprite spriteIdea = null;
        Sprite spriteSave = null;

#if UNITY_EDITOR
        emptyButtonSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/EmptyButton.png");
        borderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Border.png");
        spriteGet = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Unit7/magnet_station_get.png");
        spriteCatch = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Unit7/magnet_station_catch.png");
        spriteIdea = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Unit7/magnet_station_idea.png");
        spriteSave = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Unit7/magnet_station_save.png");
#endif

        // Apply 3D Magnet Station graphics to GET, CATCH, IDEA, and SAVE (preserve user custom sprites if set)
        Transform[] magnets = new Transform[] { magnetGet, magnetCatch, magnetIdea, magnetSave };
        Sprite[] stationSprites = new Sprite[] { spriteGet, spriteCatch, spriteIdea, spriteSave };

        for (int i = 0; i < magnets.Length; i++) {
            if (magnets[i] == null) continue;
            Image img = magnets[i].GetComponent<Image>();
            if (img != null) {
                img.preserveAspect = false;
                if (img.sprite == null && stationSprites[i] != null) {
                    img.sprite = stationSprites[i];
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                }
                if (img.sprite != null) {
                    TMP_Text labelTmp = magnets[i].GetComponentInChildren<TMP_Text>(true);
                    if (labelTmp != null) {
                        labelTmp.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Preserve user's custom tile assets (Yellow Bus sprite, Red Magnet sprite)
        Transform[] tiles = new Transform[] { tileABus, tileCatch };
        foreach (var t in tiles) {
            if (t == null) continue;
            Image img = t.GetComponent<Image>();
            if (img != null) {
                img.preserveAspect = false;
                if (img.sprite == null && emptyButtonSprite != null) {
                    img.sprite = emptyButtonSprite;
                    img.type = Image.Type.Sliced;
                }
            }
        }

        // Apply rounded box style to completed phrase glow box
        if (completedPhraseGlow != null) {
            Image img = completedPhraseGlow.GetComponent<Image>();
            if (img != null && emptyButtonSprite != null) {
                img.sprite = emptyButtonSprite;
                img.type = Image.Type.Sliced;
            }
        }

        // Apply rounded border box style to Dialogue box (like user image 2)
        if (dialogueBoxObj != null) {
            Image img = dialogueBoxObj.GetComponent<Image>();
            if (img != null) {
                if (borderSprite != null) {
                    img.sprite = borderSprite;
                    img.type = Image.Type.Sliced;
                } else if (emptyButtonSprite != null) {
                    img.sprite = emptyButtonSprite;
                    img.type = Image.Type.Sliced;
                }
            }
        }

        // Apply rounded box style to START button
        if (startButton != null) {
            Image img = startButton.GetComponent<Image>();
            if (img != null && emptyButtonSprite != null) {
                img.sprite = emptyButtonSprite;
                img.type = Image.Type.Sliced;
            }
        }
    }

    private void FixCanvasAndEventSystem() {
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            GameObject es = GameObject.Find("EventSystem");
            if (es == null) {
                es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        Canvas c = GetComponentInParent<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null) {
            c.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (introCanvasGroup == null) {
            introCanvasGroup = GetComponent<CanvasGroup>();
            if (introCanvasGroup == null) introCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void EnsureUIReferencesInitialized() {
        // Auto-load Audio Clips from Assets if unassigned in Inspector
        if (ariaIntroAudio == null) {
#if UNITY_EDITOR
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Intro/Some words love to hold hands Today we learn which words go together - and which never do.mp3");
            snapAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/7_Collocations/Intro/catch a bus.mp3");
            repelSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SelectNegative.mp3");
            snapSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
            ambientAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/JDSherbert - Ambiences Music Pack - Junction Jazz.ogg");
#endif
        }

        if (dialogueBoxObj == null) {
            Transform dTrans = transform.Find("DialogueBox") ?? transform.Find("SpeechBubble") ?? transform.Find("SubtitleBox");
            if (dTrans != null) dialogueBoxObj = dTrans.gameObject;
        }

        if (dialogueTMP == null && dialogueBoxObj != null) {
            dialogueTMP = dialogueBoxObj.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (dialogueTMP == null) {
            dialogueTMP = FindTMPByName("Dialogue") ?? FindTMPByName("Subtitle") ?? FindTMPByName("Speech");
        }

        if (startButton == null) {
            Transform sTrans = transform.Find("StartButton") ?? transform.Find("START") ?? transform.Find("NextButton");
            if (sTrans != null) startButton = sTrans.GetComponent<Button>();
        }
        if (startButton == null && nextButton != null) {
            startButton = nextButton;
        }

        if (startButton != null) {
            startButtonTMP = startButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (startButtonTMP != null) {
                startButtonTMP.text = "START";
            }
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        // Find ARIA character image
        if (ariaImage == null) {
            Transform ariaT = FindChildRecursiveCollocations(transform, "ARIA") ?? FindChildRecursiveCollocations(transform, "Owl") ?? FindChildRecursiveCollocations(transform, "Character");
            if (ariaT != null) ariaImage = ariaT.GetComponent<Image>();
        }

        // Find Magnet Stations
        if (magnetGet == null) magnetGet = FindChildRecursiveCollocations(transform, "Magnet_GET") ?? FindChildRecursiveCollocations(transform, "GET");
        if (magnetCatch == null) magnetCatch = FindChildRecursiveCollocations(transform, "Magnet_CATCH") ?? FindChildRecursiveCollocations(transform, "CATCH");
        if (magnetIdea == null) magnetIdea = FindChildRecursiveCollocations(transform, "Magnet_IDEA") ?? FindChildRecursiveCollocations(transform, "IDEA");
        if (magnetSave == null) magnetSave = FindChildRecursiveCollocations(transform, "Magnet_SAVE") ?? FindChildRecursiveCollocations(transform, "SAVE");

        // Find Word Tiles
        if (tileABus == null) tileABus = FindChildRecursiveCollocations(transform, "Tile_ABus") ?? FindChildRecursiveCollocations(transform, "Tile_a_bus") ?? FindChildRecursiveCollocations(transform, "a bus");
        if (tileCatch == null) tileCatch = FindChildRecursiveCollocations(transform, "Tile_Catch") ?? FindChildRecursiveCollocations(transform, "Tile_catch") ?? FindChildRecursiveCollocations(transform, "catch");

        if (tileABusTMP == null && tileABus != null) tileABusTMP = tileABus.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tileCatchTMP == null && tileCatch != null) tileCatchTMP = tileCatch.GetComponentInChildren<TextMeshProUGUI>(true);

        // Find Glow / Completed Phrase element
        if (completedPhraseGlow == null) {
            Transform glowT = FindChildRecursiveCollocations(transform, "CompletedPhraseGlow") ?? FindChildRecursiveCollocations(transform, "Glow");
            if (glowT != null) completedPhraseGlow = glowT.gameObject;
        }
        if (completedPhraseTMP == null && completedPhraseGlow != null) {
            completedPhraseTMP = completedPhraseGlow.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            if (tmp.GetComponentInParent<Button>() != null) continue;

            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName.Equals("lessontitletext") || lowerName.Equals("lessontitle") || lowerName.Equals("titletext")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "INTRO – The Word Magnet Lab";
                tmp.fontSize = 36;
            }
            else if (lowerName.Contains("heading") || textVal.Contains("MAGNET") || textVal.Contains("INTRO")) {
                tmp.text = "INTRO – The Word Magnet Lab";
            }
        }
    }

    private IEnumerator PlayIntroCinematicSequence() {
        isIntroCompleted = false;
        isStartClicked = false;

        if (startButton != null) {
            startButton.gameObject.SetActive(false);
        }

        if (completedPhraseGlow != null) {
            completedPhraseGlow.SetActive(false);
        }

        // Step 1: Scene Entry & Welcome Audio Voiceover
        if (introCanvasGroup != null) {
            introCanvasGroup.alpha = 0f;
            introCanvasGroup.DOFade(1f, 1.0f);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        // Start subtle idle floating on magnet stations
        AnimateMagnetStationPulse(magnetGet);
        AnimateMagnetStationPulse(magnetCatch);
        AnimateMagnetStationPulse(magnetIdea);
        AnimateMagnetStationPulse(magnetSave);

#if UNITY_EDITOR
        if (ariaIntroAudio == null) {
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Intro/Welcome to Unit 7 Collocations.mp3");
            if (ariaIntroAudio == null) {
                ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Intro/Some words love to hold hands Today we learn which words go together - and which never do.mp3");
            }
        }
#endif

        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            isAriaAudioPlaying = true;
            ShowDialogue("Welcome to Unit 7 Collocations! Some words love to hold hands. Today we learn which words go together, and which never do.");
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            yield return new WaitForSeconds(ariaIntroAudio.length + 0.3f);
            isAriaAudioPlaying = false;
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        // Step 2: GET Magnet Demonstration ("a bus" tile moves toward GET magnet and repels)
        ShowDialogue("'a bus' drifts toward GET...");
        if (tileABus != null && magnetGet != null) {
            Vector3 targetPos = magnetGet.localPosition + new Vector3(0f, -60f, 0f);
            tileABus.DOLocalMove(targetPos, 1.0f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(1.0f);

            // Repel bounce & SFX
            if (repelSFX != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(repelSFX);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (magnetGet != null) {
                magnetGet.DOKill();
                magnetGet.localScale = Vector3.one;
            }

            Vector3 repelPos = targetPos + new Vector3(-120f, -80f, 0f);
            tileABus.DOLocalMove(repelPos, 0.5f).SetEase(Ease.OutBounce);
            tileABus.DOPunchRotation(new Vector3(0f, 0f, 25f), 0.5f);
        }

        yield return new WaitForSeconds(1.5f);

        // Step 3: CATCH Magnet Demonstration ("catch" & "a bus" move to CATCH magnet and SNAP)
        ShowDialogue("Trying CATCH instead...");
        if (tileCatch != null && magnetCatch != null) {
            Vector3 catchTargetPos = magnetCatch.localPosition + new Vector3(-70f, -60f, 0f);
            tileCatch.DOLocalMove(catchTargetPos, 0.8f).SetEase(Ease.OutQuad);
        }

        if (tileABus != null && magnetCatch != null) {
            Vector3 busTargetPos = magnetCatch.localPosition + new Vector3(70f, -60f, 0f);
            tileABus.DOLocalMove(busTargetPos, 0.8f).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(0.8f);

        // Magnetic SNAP animation
        if (tileCatch != null && tileABus != null && magnetCatch != null) {
            Vector3 snapCenterPos = magnetCatch.localPosition + new Vector3(0f, -80f, 0f);
            tileCatch.DOLocalMove(snapCenterPos + new Vector3(-50f, 0f, 0f), 0.3f).SetEase(Ease.InQuad);
            tileABus.DOLocalMove(snapCenterPos + new Vector3(50f, 0f, 0f), 0.3f).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(0.3f);

        // Play Snap SFX & VO
        if (snapSFX != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(snapSFX);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        if (magnetCatch != null) {
            magnetCatch.DOKill();
            magnetCatch.localScale = Vector3.one;
        }

        ShowDialogue("catch a bus!");
        if (completedPhraseGlow != null) {
            completedPhraseGlow.SetActive(true);
            completedPhraseGlow.transform.localScale = Vector3.zero;
            completedPhraseGlow.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
        if (completedPhraseTMP != null) {
            completedPhraseTMP.text = "catch a bus";
        }

        if (snapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(snapAudio);
        }

        yield return new WaitForSeconds(2.0f);

        // Step 4: ARIA Explanation
        string ariaSpeechText = "Some words love to hold hands! Today we learn which words go together — and which never do.";
        ShowDialogue("ARIA: " + ariaSpeechText);

        if (ariaImage != null) {
            ariaImage.transform.DOKill();
            ariaImage.transform.DOPunchScale(Vector3.one * 0.18f, 0.5f);
        }

        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
        }

        yield return new WaitForSeconds(3.8f);

        // Step 5: START Button Appears
        isIntroCompleted = true;
        if (startButton != null) {
            startButton.gameObject.SetActive(true);
            if (startButtonTMP != null) startButtonTMP.text = "START";
            startButton.transform.localScale = Vector3.zero;
            startButton.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).OnComplete(() => {
                // Subtle idle pulse
                startButton.transform.DOScale(Vector3.one * 1.08f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            });
        }
    }

    private void AnimateMagnetStationPulse(Transform station) {
        if (station == null) return;
        station.DOKill();
        station.localScale = Vector3.one;
    }

    private void SetupAriaTapReplay() {
        if (ariaImage != null) {
            Button ariaBtn = ariaImage.GetComponent<Button>();
            if (ariaBtn == null) ariaBtn = ariaImage.gameObject.AddComponent<Button>();
            ariaBtn.interactable = true;
            ariaBtn.transition = Selectable.Transition.None;

            ariaBtn.onClick.RemoveAllListeners();
            ariaBtn.onClick.AddListener(ReplayAriaDialogue);
        }
    }

    public void ReplayAriaDialogue() {
        if (isAriaAudioPlaying) return;

        string ariaSpeechText = "Some words love to hold hands! Today we learn which words go together — and which never do.";
        ShowDialogue("ARIA: " + ariaSpeechText);

        if (ariaImage != null) {
            ariaImage.transform.DOKill();
            ariaImage.transform.DOPunchScale(Vector3.one * 0.18f, 0.4f);
        }

        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            isAriaAudioPlaying = true;
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            StartCoroutine(ResetAriaPlayingFlag(3.8f));
        }
    }

    private IEnumerator ResetAriaPlayingFlag(float delay) {
        yield return new WaitForSeconds(delay);
        isAriaAudioPlaying = false;
    }

    private void ShowDialogue(string text) {
        if (dialogueBoxObj != null) dialogueBoxObj.SetActive(true);
        if (dialogueTMP != null) {
            dialogueTMP.text = text;
        }
    }

    private void OnStartButtonClicked() {
        if (isStartClicked) return;
        isStartClicked = true;

        if (startButton != null) {
            startButton.interactable = false;
            startButton.transform.DOKill();
        }

        Debug.Log("[U7_Intro] START clicked -> Loading U7_Hub");
        PlayerPrefs.SetString("unitId", "M2A_U7");
        PlayerPrefs.Save();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        } else {
            try {
                SceneManager.LoadScene("U7_Hub");
            } catch {
                Debug.Log("[U7_Intro] U7_Hub scene loading via LevelManager");
            }
        }
    }

    private TextMeshProUGUI FindTMPByName(string nameSubstring) {
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tmps) {
            if (tmp == null) continue;
            if (tmp.name.IndexOf(nameSubstring, System.StringComparison.OrdinalIgnoreCase) >= 0 || (tmp.text != null && tmp.text.IndexOf(nameSubstring, System.StringComparison.OrdinalIgnoreCase) >= 0)) {
                return tmp as TextMeshProUGUI;
            }
        }
        return null;
    }

    private Transform FindChildRecursiveCollocations(Transform parent, string childName) {
        foreach (Transform child in parent) {
            if (child == null) continue;
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform result = FindChildRecursiveCollocations(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}