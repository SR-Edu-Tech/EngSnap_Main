using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class SpotSilentLetterQuestion
{
    [Tooltip("The word to display, e.g., 'knock'")]
    public string word;

    [Tooltip("The 0-based indices of the silent letters in the word (e.g., [0] for 'k' in 'knock', [2, 3] for 'g' and 'h' in 'light')")]
    public List<int> silentIndices = new List<int>();

    [Tooltip("The description of the silent letter category (e.g., 'b', 'k', 'w', 'h', 'gh', 't', 'l')")]
    public string category;

    [Tooltip("Audio clip for speaking the word normally")]
    public AudioClip wordAudioNormal;

    [Tooltip("Audio clip for speaking the word slowly")]
    public AudioClip wordAudioSlow;
}

public class SpotSilentLetter_Unit10_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("UI Hierarchy References")]
    [SerializeField] private RectTransform wordContainer;
    [SerializeField] private GameObject letterTileTemplate;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI instructionLabel;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI progressLabel;

    [Header("UI Panels & Containers")]
    [SerializeField] private RectTransform progressDotsContainer;
    [SerializeField] private GameObject progressDotPrefab;

    [Header("Mascot & Interaction Visuals")]
    [SerializeField] private RectTransform mascotCharacter;
    [SerializeField] private GameObject starEffectObject;

    [Header("Progress Dot Styling")]
    [SerializeField] private Sprite dotEmptySprite;
    [SerializeField] private Sprite dotFilledSprite;
    [SerializeField] private Color dotEmptyColor = Color.gray;
    [SerializeField] private Color dotFilledColor = Color.green;

    [Header("Styling Configuration")]
    [SerializeField] private Color letterNormalColor = new Color32(40, 40, 40, 255);
    [SerializeField] private Color letterIncorrectColor = new Color32(244, 67, 54, 255); // Red

    [Header("Audio Settings")]
    [SerializeField] private AudioSource mascotAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip correctSFX;
    [SerializeField] private AudioClip wrongSFX;
    [SerializeField] private AudioClip shushSFX;
    [SerializeField] private AudioClip cheerSFX;
    [SerializeField] private AudioClip levelCompleteSFX;
    [SerializeField] private AudioClip introAudio;

    [Header("Word Bank Configuration")]
    [SerializeField] private List<SpotSilentLetterQuestion> wordBank = new List<SpotSilentLetterQuestion>();

    [Header("Completion Events")]
    [SerializeField] private UnityEvent onLevelComplete;

    [Header("Global Next Button")]
    [SerializeField] private GameObject globalNextButton;

    [Header("Replay Button")]
    [SerializeField] private Button replayWordButton;

    // Runtime state
    private List<SpotSilentLetterQuestion> activeQuestions = new List<SpotSilentLetterQuestion>();
    private int currentIndex = 0;
    private int score = 0;
    private bool canTap = false;
    private List<GameObject> dotInstances = new List<GameObject>();
    private List<GameObject> activeTileObjects = new List<GameObject>();
    private Vector3 originalMascotScale = Vector3.one;
    private GameFlowManager_Senior_Phonics flowManager;
    private Coroutine audioCoroutine;

    private void Awake()
    {
        Debug.Log("[SpotSilentLetter] Awake called.");
        if (mascotCharacter != null)
        {
            originalMascotScale = mascotCharacter.localScale;
        }

        // Dynamically locate game flow manager and audio sources if not set
        flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        if (mascotAudioSource == null && sources.Length > 0) mascotAudioSource = sources[0];
        if (sfxAudioSource == null && sources.Length > 1) sfxAudioSource = sources[1];
        if (sfxAudioSource == null) sfxAudioSource = mascotAudioSource;

        if (wordBank == null || wordBank.Count == 0)
        {
            InitializeDefaultWordBank();
        }

        if (replayWordButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (name.Contains("replay") || name.Contains("speaker") || name.Contains("sound") || name.Contains("listen"))
                {
                    replayWordButton = b;
                    break;
                }
            }
        }

        if (replayWordButton != null)
        {
            replayWordButton.onClick.RemoveAllListeners();
            replayWordButton.onClick.AddListener(ReplayCurrentWordAudio);
        }
    }

    private void OnEnable()
    {
        Debug.Log("[SpotSilentLetter] OnEnable called.");
        if (globalNextButton != null)
        {
            globalNextButton.SetActive(false);
        }
        StartCoroutine(StartActivitySequence());
    }

    private void InitializeDefaultWordBank()
    {
        wordBank = new List<SpotSilentLetterQuestion>();

        // Exactly 10 default words covering categories: b, k, w, h, gh, t, l
        AddWordToBank("climb", new List<int> { 4 }, "b");
        AddWordToBank("thumb", new List<int> { 4 }, "b");
        AddWordToBank("knock", new List<int> { 0 }, "k");
        AddWordToBank("knife", new List<int> { 0 }, "k");
        AddWordToBank("write", new List<int> { 0 }, "w");
        AddWordToBank("wrist", new List<int> { 0 }, "w");
        AddWordToBank("ghost", new List<int> { 1 }, "h");
        AddWordToBank("light", new List<int> { 2, 3 }, "gh");
        AddWordToBank("listen", new List<int> { 3 }, "t");
        AddWordToBank("talk", new List<int> { 2 }, "l");
    }

    private void AddWordToBank(string word, List<int> indices, string category)
    {
        wordBank.Add(new SpotSilentLetterQuestion
        {
            word = word,
            silentIndices = indices,
            category = category
        });
    }

    [ContextMenu("Auto-Resolve Audio Assets")]
    public void AutoResolveAudio()
    {
#if UNITY_EDITOR
        if (wordBank == null || wordBank.Count == 0)
        {
            InitializeDefaultWordBank();
        }
        foreach (var q in wordBank)
        {
            if (q.wordAudioNormal == null)
            {
                q.wordAudioNormal = ResolveAudioAsset(q.word, false);
            }
            if (q.wordAudioSlow == null)
            {
                q.wordAudioSlow = ResolveAudioAsset(q.word, true);
            }
        }
#endif
    }

    private void Reset()
    {
        if (wordBank == null || wordBank.Count == 0)
        {
            InitializeDefaultWordBank();
        }
#if UNITY_EDITOR
        AutoResolveAudio();
#endif
    }

#if UNITY_EDITOR
    private AudioClip ResolveAudioAsset(string name, bool slow)
    {
        string suffix = slow ? "_slow" : "";
        string clipName = name + suffix;
        string[] guids = UnityEditor.AssetDatabase.FindAssets(clipName + " t:AudioClip");
        if (guids != null && guids.Length > 0)
        {
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Unit 10 Phonics") || path.Contains("Spot the Silent Letter"))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }
#endif

    private IEnumerator StartActivitySequence()
    {
        canTap = false;
        score = 0;
        currentIndex = 0;

        UpdateScoreUI();

        // 1. Choose exactly 10 questions across categories
        SelectActiveQuestions();

        // 2. Setup progress dots
        InitializeProgressDots();

        // 3. Intro transitions
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0f;
            LeanTween.cancel(mainCanvasGroup.gameObject);
            LeanTween.alphaCanvas(mainCanvasGroup, 1f, 0.4f);
        }

        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.zero;
            LeanTween.cancel(titleText.gameObject);
            LeanTween.scale(titleText.gameObject, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }

        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, originalMascotScale, 0.5f).setEase(LeanTweenType.easeOutBack);
        }

        // 4. Play general intro audio clip
        if (mascotAudioSource != null && introAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(introAudio.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 5. Load the first question
        LoadQuestion();
    }

    private void SelectActiveQuestions()
    {
        activeQuestions.Clear();
        activeQuestions.AddRange(wordBank);

        // Shuffle the active questions
        for (int i = 0; i < activeQuestions.Count; i++)
        {
            int rIndex = Random.Range(i, activeQuestions.Count);
            var temp = activeQuestions[i];
            activeQuestions[i] = activeQuestions[rIndex];
            activeQuestions[rIndex] = temp;
        }
    }

    private void LoadQuestion()
    {
        if (currentIndex < 0 || currentIndex >= activeQuestions.Count)
        {
            OnCompletedAllQuestions();
            return;
        }

        canTap = true;
        UpdateProgressUI();

        SpotSilentLetterQuestion current = activeQuestions[currentIndex];

        // 1. Clear previous letters
        foreach (var tile in activeTileObjects)
        {
            if (tile != null) Destroy(tile);
        }
        activeTileObjects.Clear();

        // 2. Spawn letter buttons
        if (wordContainer != null && letterTileTemplate != null)
        {
            letterTileTemplate.SetActive(false);

            for (int i = 0; i < current.word.Length; i++)
            {
                int charIndex = i;
                char character = current.word[i];

                GameObject newTile = Instantiate(letterTileTemplate, wordContainer);
                newTile.SetActive(true);
                newTile.name = $"Letter_{charIndex}_{character}";

                // Set text
                TMP_Text tmp = newTile.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = character.ToString();
                    tmp.color = letterNormalColor;
                }

                // Add button listeners
                Button btn = newTile.GetComponent<Button>();
                if (btn == null) btn = newTile.AddComponent<Button>();

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnLetterTileClicked(newTile, charIndex));

                activeTileObjects.Add(newTile);
            }
        }

        // 3. Play normal pronunciation
        if (audioCoroutine != null) StopCoroutine(audioCoroutine);
        audioCoroutine = StartCoroutine(PlayNormalAudioSequence(current));
    }

    private IEnumerator PlayNormalAudioSequence(SpotSilentLetterQuestion q)
    {
        if (mascotAudioSource != null && q.wordAudioNormal != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(q.wordAudioNormal);
            yield return new WaitForSeconds(q.wordAudioNormal.length);
        }
    }

    private void OnLetterTileClicked(GameObject clickedTile, int charIndex)
    {
        if (!canTap) return;

        SpotSilentLetterQuestion current = activeQuestions[currentIndex];

        if (current.silentIndices.Contains(charIndex))
        {
            // Correct silent letter clicked!
            StartCoroutine(HandleCorrectChoice(clickedTile));
        }
        else
        {
            // Wrong letter clicked!
            StartCoroutine(HandleIncorrectChoice(clickedTile));
        }
    }

    private IEnumerator HandleCorrectChoice(GameObject clickedTile)
    {
        canTap = false;
        score += 10;
        UpdateScoreUI();

        SpotSilentLetterQuestion current = activeQuestions[currentIndex];

        // 1. Play soft shush sound
        if (sfxAudioSource != null && shushSFX != null)
        {
            sfxAudioSource.PlayOneShot(shushSFX);
        }

        // 2. Fade out all silent letter tiles in the word
        List<Coroutine> fadeCoroutines = new List<Coroutine>();
        foreach (int index in current.silentIndices)
        {
            if (index >= 0 && index < activeTileObjects.Count)
            {
                GameObject tile = activeTileObjects[index];
                if (tile != null)
                {
                    fadeCoroutines.Add(StartCoroutine(FadeOutTileText(tile)));
                }
            }
        }

        // Wait briefly for the fade-out and shush to begin
        yield return new WaitForSeconds(0.4f);

        // 3. Play correct SFX (chime)
        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX);
        }

        // 4. Mascot bounce animation
        if (mascotCharacter != null)
        {
            StartCoroutine(AnimateMascotBounce());
        }

        // 5. Star effect
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
            StartCoroutine(HideStarAfterDelay(1.2f));
        }

        yield return new WaitForSeconds(0.3f);

        // 6. Speak word normally to confirm
        if (mascotAudioSource != null && current.wordAudioNormal != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(current.wordAudioNormal);
            yield return new WaitForSeconds(current.wordAudioNormal.length + 0.3f);
        }

        // Play mascot cheer
        if (cheerSFX != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(cheerSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Advance
        currentIndex++;
        LoadQuestion();
    }

    private IEnumerator FadeOutTileText(GameObject tile)
    {
        TMP_Text tmp = tile.GetComponentInChildren<TMP_Text>();
        if (tmp == null) yield break;

        Color startColor = tmp.color;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        tmp.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
    }

    private IEnumerator HandleIncorrectChoice(GameObject clickedTile)
    {
        canTap = false;

        SpotSilentLetterQuestion current = activeQuestions[currentIndex];

        // 1. Play wrong SFX
        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        // 2. Highlight clicked tile red briefly
        TMP_Text tmp = clickedTile.GetComponentInChildren<TMP_Text>();
        Color originalColor = letterNormalColor;
        if (tmp != null)
        {
            originalColor = tmp.color;
            tmp.color = letterIncorrectColor;
        }

        // 3. Shake the clicked tile
        yield return StartCoroutine(ShakeTile(clickedTile));

        // Reset tile color
        if (tmp != null)
        {
            tmp.color = originalColor;
        }

        // 4. Repeat the word slowly
        AudioClip slowClip = current.wordAudioSlow != null ? current.wordAudioSlow : current.wordAudioNormal;
        if (mascotAudioSource != null && slowClip != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(slowClip);
            yield return new WaitForSeconds(slowClip.length + 0.2f);
        }

        canTap = true;
    }

    private IEnumerator ShakeTile(GameObject tile)
    {
        Transform t = tile.transform;
        Vector3 originalPos = t.localPosition;
        float elapsed = 0f;
        float duration = 0.4f;
        float speed = 35f;
        float amount = 8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float xOffset = Mathf.Sin(elapsed * speed) * amount;
            t.localPosition = new Vector3(originalPos.x + xOffset, originalPos.y, originalPos.z);
            yield return null;
        }

        t.localPosition = originalPos;
    }

    private void ReplayCurrentWordAudio()
    {
        if (currentIndex < 0 || currentIndex >= activeQuestions.Count) return;
        SpotSilentLetterQuestion q = activeQuestions[currentIndex];

        if (mascotAudioSource != null && q.wordAudioNormal != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(q.wordAudioNormal);
        }
    }

    private IEnumerator AnimateMascotBounce()
    {
        if (mascotCharacter == null) yield break;

        Vector3 targetScale = originalMascotScale * 1.15f;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            mascotCharacter.localScale = Vector3.Lerp(originalMascotScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            mascotCharacter.localScale = Vector3.Lerp(targetScale, originalMascotScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mascotCharacter.localScale = originalMascotScale;
    }

    private IEnumerator HideStarAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }
    }

    private void InitializeProgressDots()
    {
        if (progressDotsContainer == null) return;

        foreach (var dot in dotInstances)
        {
            if (dot != null) Destroy(dot);
        }
        dotInstances.Clear();

        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        for (int i = 0; i < activeQuestions.Count; i++)
        {
            GameObject dotObj = null;
            if (progressDotPrefab != null)
            {
                dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
                dotObj.SetActive(true);
            }
            else
            {
                dotObj = new GameObject($"Dot_{i + 1}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(progressDotsContainer, false);
                RectTransform rt = dotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(18f, 18f);
                Image img = dotObj.GetComponent<Image>();
                img.color = dotEmptyColor;
            }
            dotInstances.Add(dotObj);
        }

        UpdateProgressDots();
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < dotInstances.Count; i++)
        {
            Image img = dotInstances[i].GetComponent<Image>();
            if (img == null) img = dotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                if (i < currentIndex)
                {
                    if (dotFilledSprite != null) img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;
                }
                else
                {
                    if (dotEmptySprite != null) img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                }
            }
        }
    }

    private void UpdateProgressUI()
    {
        UpdateProgressDots();

        if (scoreLabel != null)
        {
            scoreLabel.text = score.ToString();
        }

        if (progressLabel != null)
        {
            progressLabel.text = $"{currentIndex + 1} / {activeQuestions.Count}";
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = score.ToString();
        }
    }

    private void OnCompletedAllQuestions()
    {
        canTap = false;

        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Activity Complete!";
        }

        onLevelComplete?.Invoke();

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
            globalNextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(globalNextButton);
            LeanTween.scale(globalNextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);

            var btn = globalNextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    if (flowManager != null)
                    {
                        flowManager.NextGameplay();
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                });
            }
        }
        else
        {
            StartCoroutine(AutoAdvanceFlow());
        }
    }

    private IEnumerator AutoAdvanceFlow()
    {
        float delay = 2.0f;
        if (unitCompleteAudio != null) { delay = Mathf.Max(delay, unitCompleteAudio.length + 0.5f); }
        yield return new WaitForSeconds(delay);
        if (flowManager != null)
        {
            flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SetupReplayButton(Button replayBtn)
    {
        replayWordButton = replayBtn;
        if (replayBtn != null)
        {
            replayBtn.onClick.RemoveAllListeners();
            replayBtn.onClick.AddListener(ReplayCurrentWordAudio);
        }
    }
}
