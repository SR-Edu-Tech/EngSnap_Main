using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U8_A2_BuzzWhisperController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI phonemeDisplayText;
    public Button buzzButton;       // Voiced button
    public Button whisperButton;    // Unvoiced button
    public RectTransform throatGraphic; // Optional throat graphic reference
    public TextMeshProUGUI feedbackText;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip mascotDemoAudio;
    public U8_Manager manager;

    private Unit8LevelData currentLevel;
    private int currentIndex = 0;
    private bool isAnimating = false;

    public System.Action OnActivityComplete;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SetupActivity(Unit8LevelData levelData)
    {
        if (levelData == null)
        {
            levelData = Resources.Load<Unit8LevelData>("Unit8Level_Main");
#if UNITY_EDITOR
            if (levelData == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Unit8Level_Main t:Unit8LevelData");
                if (guids.Length > 0)
                {
                    levelData = UnityEditor.AssetDatabase.LoadAssetAtPath<Unit8LevelData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        currentLevel = levelData;
        currentIndex = 0;

        AutoFindUIElements();

        if (buzzButton != null)
        {
            buzzButton.onClick.RemoveAllListeners();
            buzzButton.onClick.AddListener(() => OnChoiceSelected(true));
        }

        if (whisperButton != null)
        {
            whisperButton.onClick.RemoveAllListeners();
            whisperButton.onClick.AddListener(() => OnChoiceSelected(false));
        }

        // Auto-load mascot demo audio clip
        if (mascotDemoAudio == null)
        {
            mascotDemoAudio = Resources.Load<AudioClip>("u8_buzz");
            if (mascotDemoAudio == null) mascotDemoAudio = Resources.Load<AudioClip>("u8_intro");
#if UNITY_EDITOR
            if (mascotDemoAudio == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("u8_buzz t:AudioClip");
                if (guids.Length == 0) guids = UnityEditor.AssetDatabase.FindAssets("u8_intro t:AudioClip");
                if (guids.Length > 0)
                {
                    mascotDemoAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        StartCoroutine(ShowMascotDemoRoutine());
    }

    private IEnumerator ShowMascotDemoRoutine()
    {
        isAnimating = true;

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        float waitTime = 3.5f;
        if (mascotDemoAudio != null) waitTime = mascotDemoAudio.length + 0.5f;

        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayThroatTouchDemoAnimation(waitTime);
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Put your fingers on your throat and feel the sound!";
        }

        if (audioSource != null && mascotDemoAudio != null)
        {
            audioSource.Stop();
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.PlayOneShot(mascotDemoAudio);
        }

        yield return new WaitForSeconds(waitTime);

        isAnimating = false;
        DisplayCurrentPhoneme();
    }

    private void AutoFindUIElements()
    {
        if (buzzButton == null)
        {
            Transform t = transform.Find("Buzz_Button");
            if (t == null) t = transform.Find("BuzzButton");
            if (t == null) t = transform.Find("Buzz");
            if (t != null) buzzButton = t.GetComponent<Button>();
        }

        if (whisperButton == null)
        {
            Transform t = transform.Find("Whisper_Button");
            if (t == null) t = transform.Find("WhisperButton");
            if (t == null) t = transform.Find("Whisper");
            if (t != null) whisperButton = t.GetComponent<Button>();
        }

        if (throatGraphic == null)
        {
            Transform t = transform.Find("ThroatGraphic");
            if (t == null) t = transform.Find("Throat_Graphic");
            if (t == null) t = transform.Find("Throat");
            if (t == null) t = transform.Find("ThroatIcon");
            if (t == null) t = transform.Find("Vibration");
            if (t != null) throatGraphic = t.GetComponent<RectTransform>();
        }

        if (phonemeDisplayText == null)
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.name.Contains("Phoneme") || tmp.name.Contains("Letter") || tmp.name.Contains("Sound"))
                {
                    phonemeDisplayText = tmp;
                    break;
                }
            }
            if (phonemeDisplayText == null && tmps.Length > 0) phonemeDisplayText = tmps[0];
        }

        if (feedbackText == null)
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp != phonemeDisplayText && (tmp.name.Contains("Feedback") || tmp.name.Contains("Instruction") || tmp.name.Contains("Text")))
                {
                    feedbackText = tmp;
                    break;
                }
            }
            if (feedbackText == null && tmps.Length > 1) feedbackText = tmps[1];
        }

        // Auto-find Next Button under Unit_8_Sections or current panel
        if (nextButton == null)
        {
            Transform unitRoot = transform;
            while (unitRoot.parent != null && unitRoot.parent.name != "Canvas") unitRoot = unitRoot.parent;
            Transform secs = unitRoot.Find("Unit_8_Sections");
            if (secs == null) secs = transform.Find("Unit_8_Sections");
            Transform searchRoot = secs != null ? secs : unitRoot;

            string[] nextNames = { "Next_Button", "NextButton", "Next Button" };
            foreach (string n in nextNames)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = transform.Find(n);
                if (t != null) { nextButton = t.GetComponent<Button>(); break; }
            }
        }

        // Hide Next Button initially when Section B starts
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    private void DisplayCurrentPhoneme()
    {
        if (currentLevel == null || currentLevel.buzzWhisperList == null || currentIndex >= currentLevel.buzzWhisperList.Count)
        {
            if (feedbackText != null) feedbackText.text = "Great job feeling the sounds!";
            if (phonemeDisplayText != null) phonemeDisplayText.text = "Done!";

            // Reveal Next Button when Section B finishes!
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
            }

            MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
            if (mascot != null) mascot.PlayCelebrationAnimation();

            if (OnActivityComplete != null) OnActivityComplete.Invoke();
            return;
        }

        BuzzWhisperData item = currentLevel.buzzWhisperList[currentIndex];
        if (item == null) return;

        if (phonemeDisplayText != null) phonemeDisplayText.text = item.phonemeKey;
        if (feedbackText != null) feedbackText.text = "Feel your throat: Buzz or Whisper?";

        // Play item audio clip
        if (audioSource != null && item.soundAudio != null)
        {
            audioSource.Stop();
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.PlayOneShot(item.soundAudio);
        }
    }

    private void OnChoiceSelected(bool selectedBuzz)
    {
        if (isAnimating || currentLevel == null || currentLevel.buzzWhisperList == null || currentIndex >= currentLevel.buzzWhisperList.Count) return;

        BuzzWhisperData item = currentLevel.buzzWhisperList[currentIndex];
        if (item == null) return;

        bool isCorrect = (selectedBuzz == item.isVoiced);
        StartCoroutine(FeedbackRoutine(isCorrect, item));
    }

    private IEnumerator FeedbackRoutine(bool isCorrect, BuzzWhisperData item)
    {
        isAnimating = true;

        if (isCorrect)
        {
            if (feedbackText != null) feedbackText.text = item.isVoiced ? "Buzz! Feel the vibration!" : "Whisper! Quiet and soft!";

            // ONLY vibrate phonemeDisplayText (gentle, soft 6px vibration + 8% pulse)
            if (item.isVoiced && phonemeDisplayText != null)
            {
                RectTransform targetAnim = phonemeDisplayText.rectTransform;
                if (targetAnim != null)
                {
                    Vector2 origPos = targetAnim.anchoredPosition;
                    Vector3 origScale = targetAnim.localScale;

                    float elapsed = 0f;
                    float duration = 0.6f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;

                        // Gentle 6px shake & 8% pulse scale
                        targetAnim.anchoredPosition = origPos + new Vector2(Random.Range(-6f, 6f), Random.Range(-6f, 6f));
                        targetAnim.localScale = origScale * (1.0f + Mathf.Sin(elapsed * 35f) * 0.08f);

                        yield return null;
                    }

                    targetAnim.anchoredPosition = origPos;
                    targetAnim.localScale = origScale;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
            if (mascot != null) mascot.PlayCelebrationAnimation();

            currentIndex++;
            yield return new WaitForSeconds(0.4f);
            DisplayCurrentPhoneme();
        }
        else
        {
            if (feedbackText != null) feedbackText.text = "Almost! Feel your throat again.";
            if (audioSource != null && item.soundAudio != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(item.soundAudio);
            }
            yield return new WaitForSeconds(1.0f);
        }

        isAnimating = false;
    }

    private void OnNextButtonClicked()
    {
        if (manager != null)
        {
            manager.StartSectionC();
        }
        else
        {
            U8_Manager mgr = FindFirstObjectByType<U8_Manager>(FindObjectsInactive.Include);
            if (mgr != null) mgr.StartSectionC();
        }
    }
}
