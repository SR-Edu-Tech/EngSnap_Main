using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Subclass Game Manager for Unit 7 (Collocations) Reward Lesson One (RWD Word Linker Badge Reward).
/// Celebrates completion of Unit 7 with ARIA announcement, 4 illuminated Word-Webs (GET, CATCH, IDEA, SAVE),
/// Word Linker badge entrance animation onto LEO, collocation summary card, and persistent badge collection into profile.
/// </summary>
public class Masters_Collocations_Rewards_LessonOne : Masters_Lesson {

    [Header("UI Header & Titles")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI mainTitleTMP;

    [Header("4 Word-Web Wall Indicators")]
    [SerializeField] private GameObject webGlow_GET;
    [SerializeField] private GameObject webGlow_CATCH;
    [SerializeField] private GameObject webGlow_IDEA;
    [SerializeField] private GameObject webGlow_SAVE;

    [Header("LEO & Word Linker Badge")]
    [SerializeField] private RectTransform leoAvatarRect;
    [SerializeField] private RectTransform wordLinkerBadgeRect;
    [SerializeField] private CanvasGroup badgeSparkleGroup;

    [Header("Summary Card Panel")]
    [SerializeField] private RectTransform summaryCardPanel;
    [SerializeField] private TextMeshProUGUI summaryTitleTMP;
    [SerializeField] private TextMeshProUGUI summaryContentTMP;

    [Header("Buttons")]
    [SerializeField] private Button collectButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI collectButtonTMP;

    [Header("Audio References")]
    [SerializeField] private AudioClip voRwdAria;
    [SerializeField] private AudioClip sfxConfetti;
    [SerializeField] private AudioClip musReward;
    [SerializeField] private AudioClip sfxBadge;

    private const string BADGE_KEY = "M2A_U7_WordLinker";
    private const string PROGRESS_KEY = "M2A_U7_progress";

    private bool isCollected = false;

    protected virtual void OnEnable() {
        // Prevent STT subscriptions
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Rewards;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);

        isCollected = (PlayerPrefs.GetInt(BADGE_KEY, 0) == 1);

        StartCoroutine(CelebrationSequenceRoutine());
    }

    private void DeactivateObsoleteBaseUI() {
        Transform skipTrans = transform.Find("SkipButton");
        if (skipTrans != null) skipTrans.gameObject.SetActive(false);

        Transform contTrans = transform.Find("Continue");
        if (contTrans != null) contTrans.gameObject.SetActive(false);

        Transform heading = transform.Find("Heading") ?? transform.Find("Header");
        if (heading != null) heading.gameObject.SetActive(false);
    }

    private void InitializeAudioReferences() {
#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2A/7_Collocations/Reward/RWD/";
        if (voRwdAria == null) voRwdAria = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "You're a true Word Linker now - you know exactly which words belong together.mp3");

        if (sfxConfetti == null) sfxConfetti = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
        if (sfxBadge == null) sfxBadge = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
        if (musReward == null) musReward = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
#endif
    }

    private IEnumerator CelebrationSequenceRoutine() {
        // Initial setup
        if (collectButton != null) collectButton.interactable = false;
        if (continueButton != null) continueButton.interactable = false;

        if (webGlow_GET != null) webGlow_GET.SetActive(false);
        if (webGlow_CATCH != null) webGlow_CATCH.SetActive(false);
        if (webGlow_IDEA != null) webGlow_IDEA.SetActive(false);
        if (webGlow_SAVE != null) webGlow_SAVE.SetActive(false);

        if (wordLinkerBadgeRect != null) {
            wordLinkerBadgeRect.localScale = Vector3.zero;
        }

        if (summaryCardPanel != null) {
            summaryCardPanel.anchoredPosition = new Vector2(0f, -600f);
        }

        yield return new WaitForSeconds(0.4f);

        // 1. Confetti SFX + Music
        if (sfxConfetti != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxConfetti);
        }

        // 2. Light up 4 Word-Webs on Lab Wall
        yield return StartCoroutine(LightUpWordWebsRoutine());

        // 3. Animate Word Linker Badge onto LEO
        if (wordLinkerBadgeRect != null) {
            if (sfxBadge != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxBadge);
            }
            wordLinkerBadgeRect.DOKill();
            wordLinkerBadgeRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(0.6f);
        }

        // 4. ARIA Announcement Audio
        if (voRwdAria != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(voRwdAria);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.2f);
        }

        // 5. Slide in Summary Card
        if (summaryCardPanel != null) {
            summaryCardPanel.DOKill();
            summaryCardPanel.DOAnchorPos(new Vector2(0f, -40f), 0.5f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.5f);
        }

        // 6. Enable Buttons
        if (isCollected) {
            if (collectButton != null) {
                collectButton.interactable = false;
                if (collectButtonTMP != null) collectButtonTMP.text = "COLLECTED!";
            }
            if (continueButton != null) continueButton.interactable = true;
        } else {
            if (collectButton != null) collectButton.interactable = true;
            if (continueButton != null) continueButton.interactable = false;
        }
    }

    private IEnumerator LightUpWordWebsRoutine() {
        GameObject[] webs = new GameObject[] { webGlow_GET, webGlow_CATCH, webGlow_IDEA, webGlow_SAVE };

        foreach (var web in webs) {
            if (web != null) {
                web.SetActive(true);
                web.transform.DOKill(true);
                web.transform.localScale = Vector3.zero;
                web.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    public void OnCollectButtonClicked() {
        if (isCollected) return;

        isCollected = true;

        // Persist Badge & 100% Progress into PlayerPrefs/Profile
        PlayerPrefs.SetInt(BADGE_KEY, 1);
        PlayerPrefs.SetFloat(PROGRESS_KEY, 100f);
        PlayerPrefs.Save();

        // Sound & Shine Animation
        if (sfxBadge != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxBadge);
        }

        if (wordLinkerBadgeRect != null) {
            wordLinkerBadgeRect.DOKill(true);
            wordLinkerBadgeRect.DOPunchScale(Vector3.one * 0.25f, 0.4f, 5, 0.5f);
        }

        if (collectButton != null) {
            collectButton.interactable = false;
            if (collectButtonTMP != null) collectButtonTMP.text = "COLLECTED!";
        }

        if (continueButton != null) {
            continueButton.interactable = true;
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Rewards);
        }
    }

    public void OnContinueButtonClicked() {
        ReturnToHub();
    }

    private void UpdateTitleAndUIComponents() {
        if (mainTitleTMP != null) {
            mainTitleTMP.gameObject.SetActive(true);
            mainTitleTMP.text = "Word Linker Badge";
            mainTitleTMP.color = Color.white;
            RectTransform rt = mainTitleTMP.GetComponent<RectTransform>();
            if (rt != null) {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(1000f, 50f);
                rt.anchoredPosition = new Vector2(0f, -40f);
            }
        }

        if (summaryTitleTMP != null) {
            summaryTitleTMP.text = "Unit 7 Complete!";
        }

        if (summaryContentTMP != null) {
            summaryContentTMP.text = "Badge earned: Word Linker.\nYou now know that some words go hand in hand, and you have built all four word-webs:\n" +
                "• GET (ready, permission, dressed, married, started, a job, upset, well soon)\n" +
                "• CATCH (a cold, the flu, a bus, a train, your breath, a thief, someone's attention)\n" +
                "• IDEA (bright, excellent, clever, original, innovative, outlandish, exciting, grand)\n" +
                "• SAVE (money, electricity, water, yourself, light, energy, time, someone a seat)\n" +
                "Best of all — you can use these pairs in your own sentences and stories!";
        }
    }

    private void SetupButtonListeners() {
        if (collectButton != null) {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(OnCollectButtonClicked);
        }

        if (continueButton != null) {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }

    protected override void OnNextButtonClicked() {
        ReturnToHub();
    }

    public void ReturnToHub() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Rewards);
        }
    }

    private void AutoFindUIReferences() {
        if (mainTitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) mainTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (webGlow_GET == null) {
            Transform t = transform.Find("WebGlow_GET");
            if (t != null) webGlow_GET = t.gameObject;
        }
        if (webGlow_CATCH == null) {
            Transform t = transform.Find("WebGlow_CATCH");
            if (t != null) webGlow_CATCH = t.gameObject;
        }
        if (webGlow_IDEA == null) {
            Transform t = transform.Find("WebGlow_IDEA");
            if (t != null) webGlow_IDEA = t.gameObject;
        }
        if (webGlow_SAVE == null) {
            Transform t = transform.Find("WebGlow_SAVE");
            if (t != null) webGlow_SAVE = t.gameObject;
        }

        if (leoAvatarRect == null) {
            Transform t = transform.Find("LeoAvatar");
            if (t != null) leoAvatarRect = t.GetComponent<RectTransform>();
        }

        if (wordLinkerBadgeRect == null) {
            Transform t = transform.Find("WordLinkerBadge");
            if (t != null) wordLinkerBadgeRect = t.GetComponent<RectTransform>();
        }

        if (summaryCardPanel == null) {
            Transform t = transform.Find("SummaryCardPanel");
            if (t != null) summaryCardPanel = t.GetComponent<RectTransform>();
        }

        if (summaryCardPanel != null) {
            if (summaryTitleTMP == null) {
                Transform t = summaryCardPanel.Find("SummaryTitleText");
                if (t != null) summaryTitleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (summaryContentTMP == null) {
                Transform t = summaryCardPanel.Find("SummaryContentText");
                if (t != null) summaryContentTMP = t.GetComponent<TextMeshProUGUI>();
            }
        }

        if (collectButton == null) {
            Transform t = transform.Find("CollectButton");
            if (t != null) {
                collectButton = t.GetComponent<Button>();
                collectButtonTMP = t.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (continueButton == null) {
            Transform t = transform.Find("ContinueButton");
            if (t != null) continueButton = t.GetComponent<Button>();
        }
    }
}