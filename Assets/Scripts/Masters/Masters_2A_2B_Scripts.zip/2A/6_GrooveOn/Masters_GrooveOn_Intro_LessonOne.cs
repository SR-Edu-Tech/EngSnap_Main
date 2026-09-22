using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Subclass / Intro Controller for Unit 6 (Groove On): INTRO — Festival Street Lights Up.
/// Implements full 5-step cinematic intro flow:
/// 1. Fade-in street environment (CanvasGroup 0->1 over 1.0s) & festive ambience.
/// 2. LEO Birthday Greeting ("Wish you a very happy birthday!") + balloon/confetti pop.
/// 3. LEO turns to Diwali side ("Wish you a Happy Diwali!") + sequential diya light-up.
/// 4. ARIA perches & previews goal ("Every celebration has its own words — today we learn to wish, greet and join the fun!").
/// 5. START button appears -> loads U6_Hub / completes Intro topic.
/// ARIA is tap-to-replay.
/// </summary>
public class Masters_GrooveOn_Intro_LessonOne : Masters_PolishedCommunication_Intro_LessonOne {

    [Header("Unit 6 Intro Audio Clips")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip leoBirthdayAudio;
    [SerializeField] private AudioClip leoDiwaliAudio;
    [SerializeField] private AudioClip ambientAudio;

    [Header("Intro UI & Characters")]
    [SerializeField] private CanvasGroup introCanvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueTMP;
    [SerializeField] private GameObject dialogueBoxObj;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonTMP;
    [SerializeField] private Image ariaImage;
    [SerializeField] private Image leoImage;
    [SerializeField] private Image[] diyaImages;
    [SerializeField] private GameObject confettiEffectObj;

    private bool isAriaAudioPlaying = false;
    private bool isIntroCompleted = false;

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
        UpdateTitleAndUIComponents();
        SetupAriaTapReplay();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        StartCoroutine(PlayIntroCinematicSequence());
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
        if (ariaIntroAudio == null) {
#if UNITY_EDITOR
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Intro/Welcome to Unit 6 Groove On Every celebration has its own words Today we learn to wish greet and join the fun.mp3");
            leoBirthdayAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Intro/Wish you a very happy birthday.mp3");
            leoDiwaliAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Intro/Wish you a Happy Diwali.mp3");
            ambientAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Game/Welcome to Greeting Dash Sort the celebration tiles into the correct categories.mp3");
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

        // Find LEO and ARIA character images
        if (ariaImage == null) {
            Transform ariaT = FindChildRecursiveGrooveOn(transform, "ARIA") ?? FindChildRecursiveGrooveOn(transform, "GrooveOn_owl") ?? FindChildRecursiveGrooveOn(transform, "Owl");
            if (ariaT != null) ariaImage = ariaT.GetComponent<Image>();
        }

        if (leoImage == null) {
            Transform leoT = FindChildRecursiveGrooveOn(transform, "LEO") ?? FindChildRecursiveGrooveOn(transform, "Leo") ?? FindChildRecursiveGrooveOn(transform, "Character");
            if (leoT != null) leoImage = leoT.GetComponent<Image>();
        }

        // Find Diyas for sequential lighting
        if (diyaImages == null || diyaImages.Length == 0) {
            List<Image> dList = new List<Image>();
            for (int i = 0; i < 5; i++) {
                Transform diyaT = FindChildRecursiveGrooveOn(transform, $"Diya_{i}") ?? FindChildRecursiveGrooveOn(transform, $"Diya_{i + 1}");
                if (diyaT != null && diyaT.GetComponent<Image>() != null) {
                    dList.Add(diyaT.GetComponent<Image>());
                }
            }
            diyaImages = dList.ToArray();
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
                tmp.text = "U6 Intro — Festival Street Lights Up";
            }
            else if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("INTRO")) {
                tmp.text = "U6 Intro — Festival Street Lights Up";
            }
        }
    }

    private IEnumerator PlayIntroCinematicSequence() {
        isIntroCompleted = false;

        if (startButton != null) {
            startButton.gameObject.SetActive(false);
        }

        // Step 1: Fade-in CanvasGroup & Start Ambient Audio
        if (introCanvasGroup != null) {
            introCanvasGroup.alpha = 0f;
            introCanvasGroup.DOFade(1f, 1.0f);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        yield return new WaitForSeconds(1.0f);

        // Step 2: LEO Birthday Greeting & Confetti Pop
        ShowDialogue("LEO: Wish you a very happy birthday!");
        if (leoImage != null) {
            leoImage.transform.DOKill();
            leoImage.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f);
        }

        if (leoBirthdayAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(leoBirthdayAudio);
        }

        if (confettiEffectObj != null) {
            confettiEffectObj.SetActive(true);
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        yield return new WaitForSeconds(2.8f);

        // Step 3: LEO Turns & Diwali Greeting with Diyas Lighting Up
        if (leoImage != null) {
            leoImage.transform.DOScaleX(-1f, 0.3f);
        }

        ShowDialogue("LEO: Wish you a Happy Diwali!");
        if (leoDiwaliAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(leoDiwaliAudio);
        }

        // Light up Diyas sequentially
        if (diyaImages != null && diyaImages.Length > 0) {
            foreach (var diya in diyaImages) {
                if (diya != null) {
                    diya.gameObject.SetActive(true);
                    diya.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }

            for (int i = 0; i < diyaImages.Length; i++) {
                if (diyaImages[i] != null) {
                    diyaImages[i].DOColor(new Color(1f, 0.92f, 0.35f, 1f), 0.3f);
                    diyaImages[i].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
                    }
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        yield return new WaitForSeconds(2.5f);

        // Step 4: ARIA Previews Goal
        ShowDialogue("ARIA: Every celebration has its own words — today we learn to wish, greet and join the fun!");
        if (ariaImage != null) {
            ariaImage.transform.DOKill();
            ariaImage.transform.DOPunchScale(Vector3.one * 0.18f, 0.5f);
        }

        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
        }

        yield return new WaitForSeconds(3.5f);

        // Step 5: START Button Appears
        isIntroCompleted = true;
        if (startButton != null) {
            startButton.gameObject.SetActive(true);
            if (startButtonTMP != null) startButtonTMP.text = "START";
            startButton.transform.localScale = Vector3.zero;
            startButton.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            NextButtonAnimation();
        }
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

        ShowDialogue("ARIA: Every celebration has its own words — today we learn to wish, greet and join the fun!");
        if (ariaImage != null) {
            ariaImage.transform.DOKill();
            ariaImage.transform.DOPunchScale(Vector3.one * 0.18f, 0.4f);
        }

        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            isAriaAudioPlaying = true;
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            StartCoroutine(ResetAriaPlayingFlag(3.2f));
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
        Debug.Log("[U6_Intro] START clicked -> Loading U6_Hub");
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        } else {
            try {
                SceneManager.LoadScene("U6_Hub");
            } catch {
                Debug.Log("[U6_Intro] U6_Hub scene loading via LevelManager");
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

    private Transform FindChildRecursiveGrooveOn(Transform parent, string childName) {
        foreach (Transform child in parent) {
            if (child == null) continue;
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform result = FindChildRecursiveGrooveOn(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}