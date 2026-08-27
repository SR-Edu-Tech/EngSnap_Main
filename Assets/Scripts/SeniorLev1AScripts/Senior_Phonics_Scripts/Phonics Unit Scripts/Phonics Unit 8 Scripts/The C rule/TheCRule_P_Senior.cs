using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class TheCRuleWord
{
    [Tooltip("The plain word text, e.g. 'cell'")]
    public string wordText;

    [Tooltip("The formatted word text with letter after 'c' highlighted, e.g. 'c<b><color=#FF3366><u>e</u></color></b>ll'")]
    public string highlightedWordText;

    [Tooltip("True if soft C sound /s/ (when followed by e, i, y), false if hard C sound /k/")]
    public bool isSoftC;

    [Tooltip("The letter following 'c' that determines the sound, e.g. 'e', 'i', 'y', 'a', 'o', 'u'")]
    public string ruleLetter;

    [Tooltip("Optional image sprite for the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for speaking the word")]
    public AudioClip wordAudio;
}

public class TheCRule_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    [Tooltip("Soft C Word List (/s/)")]
    public List<TheCRuleWord> softCWords = new List<TheCRuleWord>();

    [Tooltip("Hard C Word List (/k/)")]
    public List<TheCRuleWord> hardCWords = new List<TheCRuleWord>();

    [Tooltip("Highlight color for the letter after 'c'")]
    public Color highlightColor = new Color(1f, 0.2f, 0.4f); // Vibrant Pink/Red

    [Header("Grid Containers & Templates")]
    [Tooltip("Grid panel to contain Soft C word buttons")]
    public RectTransform softGridContainer;

    [Tooltip("Grid panel to contain Hard C word buttons")]
    public RectTransform hardGridContainer;

    [Tooltip("Template button to clone for word grid entries")]
    public GameObject wordButtonTemplate;

    [Header("UI Mascot References")]
    [Tooltip("Gentle Cindy Mascot RectTransform (Soft C /s/)")]
    public RectTransform cindyMascot;

    [Tooltip("Car Mascot RectTransform (Hard C /k/)")]
    public RectTransform carMascot;

    [Header("Central Word Display Card")]
    [Tooltip("Container panel for displaying the tapped word card")]
    public GameObject centralWordCard;

    [Tooltip("Label to show the selected word in large text")]
    public TextMeshProUGUI centralWordText;

    [Tooltip("Image to show the selected word sprite")]
    public Image centralWordImage;

    [Tooltip("Audio replay button on the central card")]
    public Button centralReplayButton;

    [Header("Progress Bar & Navigation")]
    public GameObject globalNextButton;

    [Header("Audio Sources")]
    public AudioSource sfxAudioSource;
    public AudioSource voiceAudioSource;

    [Header("Audio Clips")]
    [Tooltip("Optional audio clip to play when the teach screen opens")]
    public AudioClip introAudio;

    [Tooltip("Audio clip for Soft C rule sound (/s/)")]
    public AudioClip softCRuleAudio;

    [Tooltip("Audio clip for Hard C rule sound (/k/)")]
    public AudioClip hardCRuleAudio;

    public AudioClip popSFX;
    public AudioClip transitionSFX;

    [Header("Events & Completion")]
    public UnityEvent onTeachComplete;

    // Runtime state
    private bool _canTap = true;
    private TheCRuleWord _currentlySelectedWord;
    private Vector3 _origCindyScale = Vector3.one;
    private Vector3 _origCarScale = Vector3.one;
    private Vector3 _origWordCardScale = Vector3.one;

    private HashSet<string> _tappedSoftWords = new HashSet<string>();
    private HashSet<string> _tappedHardWords = new HashSet<string>();

    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (cindyMascot != null) _origCindyScale = cindyMascot.localScale;
        if (carMascot != null) _origCarScale = carMascot.localScale;
        if (centralWordCard != null) _origWordCardScale = centralWordCard.transform.localScale;

        if (centralReplayButton != null)
        {
            centralReplayButton.onClick.RemoveAllListeners();
            centralReplayButton.onClick.AddListener(ReplayCurrentWordAudio);
        }

        if (globalNextButton != null)
        {
            Button nextBtn = globalNextButton.GetComponent<Button>();
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(OnNextButtonClicked);
            }
        }
    }

    private void Start()
    {
        _canTap = true;
        _tappedSoftWords.Clear();
        _tappedHardWords.Clear();

        if (centralWordCard != null)
        {
            centralWordCard.SetActive(false);
        }

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(false);
        }

        PopulateGrids();
        StartCoroutine(IntroSequence());
    }

    private void PopulateGrids()
    {
        if (wordButtonTemplate == null) return;
        wordButtonTemplate.SetActive(false);

        // Clear any runtime generated buttons just in case
        foreach (Transform child in softGridContainer)
        {
            if (child.gameObject != wordButtonTemplate)
            {
                Destroy(child.gameObject);
            }
        }
        foreach (Transform child in hardGridContainer)
        {
            if (child.gameObject != wordButtonTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        // Instantiate Soft C buttons
        foreach (var word in softCWords)
        {
            GameObject btnObj = Instantiate(wordButtonTemplate, softGridContainer);
            btnObj.SetActive(true);

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = word.wordText;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                TheCRuleWord w = word;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectWord(w, btnObj.transform));
            }
        }

        // Instantiate Hard C buttons
        foreach (var word in hardCWords)
        {
            GameObject btnObj = Instantiate(wordButtonTemplate, hardGridContainer);
            btnObj.SetActive(true);

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = word.wordText;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                TheCRuleWord w = word;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectWord(w, btnObj.transform));
            }
        }
    }

    private IEnumerator IntroSequence()
    {
        _canTap = false;

        // Play intro audio if assigned
        if (introAudio != null && voiceAudioSource != null)
        {
            voiceAudioSource.clip = introAudio;
            voiceAudioSource.Play();
        }

        // Pop in the mascots
        if (cindyMascot != null)
        {
            cindyMascot.localScale = Vector3.zero;
            StartCoroutine(PopUI(cindyMascot));
        }

        if (carMascot != null)
        {
            carMascot.localScale = Vector3.zero;
            StartCoroutine(PopUI(carMascot));
        }

        if (introAudio != null && voiceAudioSource != null)
        {
            while (voiceAudioSource.isPlaying)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        _canTap = true;
    }

    /// <summary>
    /// Triggered when a word button is clicked in the left or right grids.
    /// </summary>
    public void SelectWord(TheCRuleWord word, Transform wordButtonTransform)
    {
        Debug.Log($"[TheCRule_P_Senior] SelectWord called - word: {word.wordText}, canTap: {_canTap}");
        if (!_canTap) return;
        StartCoroutine(PlayWordSequence(word, wordButtonTransform));
    }

    private IEnumerator PlayWordSequence(TheCRuleWord word, Transform wordButtonTransform)
    {
        Debug.Log($"[TheCRule_P_Senior] PlayWordSequence - Phase 1 (Start) for word: {word.wordText}");
        _canTap = false;
        _currentlySelectedWord = word;

        try
        {
            // 1. Mark word as explored
            if (word.isSoftC)
                _tappedSoftWords.Add(word.wordText);
            else
                _tappedHardWords.Add(word.wordText);

            // 2. Wiggle the button clicked
            if (wordButtonTransform != null)
            {
                Debug.Log("[TheCRule_P_Senior] PlayWordSequence - Phase 2 (Wiggle Button)");
                StartCoroutine(WiggleAnimation(wordButtonTransform, Vector3.one, Quaternion.identity, 0.6f));
            }

            // 3. Update and Pop central word card
            if (centralWordCard != null)
            {
                Debug.Log("[TheCRule_P_Senior] PlayWordSequence - Phase 3 (Pop Central Card)");
                centralWordCard.SetActive(true);
                centralWordCard.transform.localScale = Vector3.zero;

                if (centralWordText != null)
                {
                    centralWordText.text = word.wordText;
                }

                if (centralWordImage != null)
                {
                    if (word.wordSprite != null)
                    {
                        centralWordImage.sprite = word.wordSprite;
                        centralWordImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        centralWordImage.gameObject.SetActive(false);
                    }
                }

                if (popSFX != null && sfxAudioSource != null)
                {
                    sfxAudioSource.PlayOneShot(popSFX);
                }

                yield return StartCoroutine(PopUI(centralWordCard.GetComponent<RectTransform>()));
            }

            Debug.Log("[TheCRule_P_Senior] PlayWordSequence - Phase 4 (Highlight Letter)");
            yield return new WaitForSeconds(0.2f);

            // 4. Animate the letter right after 'c' lighting up
            string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
            if (centralWordText != null)
            {
                centralWordText.text = FormatWordWithHighlight(word.wordText, hexColor);
                StartCoroutine(PulseCentralText());
            }

            // 5. Bounce corresponding mascot and play C phoneme sound
            AudioClip ruleAudio = word.isSoftC ? softCRuleAudio : hardCRuleAudio;
            RectTransform mascot = word.isSoftC ? cindyMascot : carMascot;
            Vector3 origMascotScale = word.isSoftC ? _origCindyScale : _origCarScale;

            Debug.Log($"[TheCRule_P_Senior] PlayWordSequence - Phase 5 (Rule Audio: {(ruleAudio != null ? ruleAudio.name : "NULL")})");
            if (ruleAudio != null && voiceAudioSource != null)
            {
                voiceAudioSource.clip = ruleAudio;
                voiceAudioSource.Play();

                if (mascot != null)
                {
                    StartCoroutine(MascotBounce(mascot, origMascotScale, ruleAudio.length));
                }

                yield return new WaitForSeconds(ruleAudio.length + 0.1f);
            }
            else
            {
                if (mascot != null)
                {
                    yield return StartCoroutine(MascotBounce(mascot, origMascotScale, 0.6f));
                }
            }

            // 6. Play the example word audio clip
            Debug.Log($"[TheCRule_P_Senior] PlayWordSequence - Phase 6 (Word Audio: {(word.wordAudio != null ? word.wordAudio.name : "NULL")})");
            if (word.wordAudio != null && voiceAudioSource != null)
            {
                voiceAudioSource.clip = word.wordAudio;
                voiceAudioSource.Play();

                if (mascot != null)
                {
                    StartCoroutine(MascotBounce(mascot, origMascotScale, word.wordAudio.length));
                }

                yield return new WaitForSeconds(word.wordAudio.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            // 7. Check if we should reveal the Next button (after 5 words from each category)
            Debug.Log("[TheCRule_P_Senior] PlayWordSequence - Phase 7 (Check Next Button)");
            CheckNextButtonUnlock();
        }
        finally
        {
            _canTap = true;
            Debug.Log($"[TheCRule_P_Senior] PlayWordSequence finished for word: {word.wordText}. canTap set back to true.");
        }
    }

    private void ReplayCurrentWordAudio()
    {
        if (_currentlySelectedWord == null || _currentlySelectedWord.wordAudio == null || voiceAudioSource == null) return;
        voiceAudioSource.clip = _currentlySelectedWord.wordAudio;
        voiceAudioSource.Play();

        RectTransform mascot = _currentlySelectedWord.isSoftC ? cindyMascot : carMascot;
        Vector3 origMascotScale = _currentlySelectedWord.isSoftC ? _origCindyScale : _origCarScale;
        if (mascot != null)
        {
            StartCoroutine(MascotBounce(mascot, origMascotScale, _currentlySelectedWord.wordAudio.length));
        }
    }

    private void CheckNextButtonUnlock()
    {
        // Unlock after the child has explored at least 5 words on each side
        if (_tappedSoftWords.Count >= 5 && _tappedHardWords.Count >= 5)
        {
            if (globalNextButton != null && !globalNextButton.activeSelf)
            {
                if (unitCompleteAudio != null && voiceAudioSource != null) voiceAudioSource.PlayOneShot(unitCompleteAudio);
                globalNextButton.SetActive(true);
                globalNextButton.transform.localScale = Vector3.zero;

                if (popSFX != null && sfxAudioSource != null)
                {
                    sfxAudioSource.PlayOneShot(popSFX);
                }

                StartCoroutine(PopUI(globalNextButton.GetComponent<RectTransform>()));
            }
        }
    }

    private void OnNextButtonClicked()
    {
        if (transitionSFX != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(transitionSFX);
        }

        onTeachComplete?.Invoke();

        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
    }

    // --- ANIMATIONS & UTILS ---

    private IEnumerator PopUI(RectTransform target)
    {
        if (target == null) yield break;

        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1.1f, 1f - Mathf.Pow(1f - percent, 3f));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;
        duration = 0.15f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float scale = Mathf.Lerp(1.1f, 1.0f, percent);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private IEnumerator MascotBounce(RectTransform mascot, Vector3 baseScale, float duration)
    {
        float elapsed = 0f;
        float wiggleSpeed = 16f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (mascot == null) yield break;

            float pulseY = 1.0f + Mathf.Sin(elapsed * wiggleSpeed) * 0.08f;
            float pulseX = 1.0f - Mathf.Sin(elapsed * wiggleSpeed) * 0.04f;

            mascot.localScale = new Vector3(baseScale.x * pulseX, baseScale.y * pulseY, baseScale.z);
            yield return null;
        }

        if (mascot != null)
        {
            mascot.localScale = baseScale;
        }
    }

    private IEnumerator WiggleAnimation(Transform target, Vector3 origScale, Quaternion origRot, float duration)
    {
        float elapsed = 0f;
        float wiggleSpeed = 20f;
        float wiggleAngle = 8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (target == null) yield break;

            float angle = Mathf.Sin(elapsed * wiggleSpeed) * wiggleAngle;
            target.localRotation = origRot * Quaternion.Euler(0f, 0f, angle);

            float scalePulse = 1f + Mathf.Sin(elapsed * wiggleSpeed) * 0.05f;
            target.localScale = origScale * scalePulse;

            yield return null;
        }

        if (target != null)
        {
            target.localScale = origScale;
            target.localRotation = origRot;
        }
    }

    private IEnumerator PulseCentralText()
    {
        if (centralWordText == null) yield break;

        Transform trans = centralWordText.transform;
        Vector3 baseScale = Vector3.one;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 1.0f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.25f;
            trans.localScale = baseScale * scale;
            yield return null;
        }

        trans.localScale = baseScale;
    }

    /// <summary>
    /// Formats the word to highlight the letter right after 'c'.
    /// </summary>
    public static string FormatWordWithHighlight(string word, string colorHex = "FF3366")
    {
        if (string.IsNullOrEmpty(word)) return word;

        int cIndex = word.IndexOf('c', StringComparison.OrdinalIgnoreCase);
        if (cIndex >= 0)
        {
            if (cIndex < word.Length - 1)
            {
                string before = word.Substring(0, cIndex + 1);
                char letterAfter = word[cIndex + 1];
                string after = word.Substring(cIndex + 2);
                return $"{before}<b><color=#{colorHex}><u>{letterAfter}</u></color></b>{after}";
            }
            else
            {
                string before = word.Substring(0, cIndex);
                char cChar = word[cIndex];
                return $"{before}<b><color=#{colorHex}><u>{cChar}</u></color></b>";
            }
        }

        return word;
    }

#if UNITY_EDITOR
    [ContextMenu("Populate Default C Words and Assets")]
    public void PopulateDefaultCWordsAndAssets()
    {
        // Lists of default words from Book
        string[] softWords = new string[] { "cell", "celery", "cement", "cereal", "acid", "citrus", "citizen", "circus", "cycle", "fancy", "city", "face", "space", "circle", "juice", "cent", "rice", "icy", "police", "pencil", "centipede", "voice", "race", "ace", "mice" };
        string[] hardWords = new string[] { "car", "call", "catch", "cook", "cry", "cake", "come", "came", "case", "count", "corn", "cold", "candle", "customer", "comb", "cup", "camel", "camp", "castle", "cupcake", "camera", "cute" };

        softCWords.Clear();
        foreach (string w in softWords)
        {
            TheCRuleWord item = new TheCRuleWord();
            item.wordText = w;
            item.isSoftC = true;
            item.ruleLetter = GetRuleLetter(w);
            item.highlightedWordText = FormatWordWithHighlight(w, ColorUtility.ToHtmlStringRGB(highlightColor));
            item.wordSprite = ResolveSprite(w);
            item.wordAudio = ResolveAudio(w);
            softCWords.Add(item);
        }

        hardCWords.Clear();
        foreach (string w in hardWords)
        {
            TheCRuleWord item = new TheCRuleWord();
            item.wordText = w;
            item.isSoftC = false;
            item.ruleLetter = GetRuleLetter(w);
            item.highlightedWordText = FormatWordWithHighlight(w, ColorUtility.ToHtmlStringRGB(highlightColor));
            item.wordSprite = ResolveSprite(w);
            item.wordAudio = ResolveAudio(w);
            hardCWords.Add(item);
        }

        // Preload default SFX
        if (popSFX == null)
            popSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");
        if (transitionSFX == null)
            transitionSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");

        // Preload mascot sounds
        if (softCRuleAudio == null)
            softCRuleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/Unit 1 Phonics/Listening/Meet the Letters/Letter Sounds/s.mp3");
        if (hardCRuleAudio == null)
            hardCRuleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/Unit 1 Phonics/Listening/Meet the Letters/Letter Sounds/c.mp3"); // fallback or c.mp3

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("Successfully populated all default C words, sprites, and audio references!");
    }

    private string GetRuleLetter(string word)
    {
        int cIndex = word.IndexOf('c', StringComparison.OrdinalIgnoreCase);
        if (cIndex >= 0 && cIndex < word.Length - 1)
        {
            return word[cIndex + 1].ToString().ToLower();
        }
        return "";
    }

    private Sprite ResolveSprite(string word)
    {
        Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 8/" + word + ".png");
        if (sprite == null)
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 8/" + word + ".jpg");

        if (sprite == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(word + " t:Sprite");
            if (guids.Length > 0)
            {
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        return sprite;
    }

    private AudioClip ResolveAudio(string word)
    {
        AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/Unit 8 Phonics/" + word + ".mp3");
        if (clip == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(word + " t:AudioClip");
            if (guids.Length > 0)
            {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        return clip;
    }

    private void OnValidate()
    {
        AutoAssignUI();
    }

    [ContextMenu("Auto Assign UI References")]
    public void AutoAssignUI()
    {
        PopulateDefaultCWordsAndAssets();

        // 1. Scan children recursively to tolerate different nesting structures and trailing spaces
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (var child in allChildren)
        {
            string cleanName = child.gameObject.name.Trim().ToLower();

            // Mascots
            if (cleanName == "cindymascot" || cleanName == "giraffemascot")
            {
                cindyMascot = child.GetComponent<RectTransform>();
            }
            else if (cleanName == "carmascot" || cleanName == "frogmascot")
            {
                carMascot = child.GetComponent<RectTransform>();
            }

            // Containers & Template
            else if (cleanName == "centralwordcard" || cleanName == "central word card")
            {
                centralWordCard = child.gameObject;
            }
            else if (cleanName == "wordbuttontemplate" || cleanName == "word button template")
            {
                wordButtonTemplate = child.gameObject;
            }

            // Grid columns
            else if (cleanName == "softgridcontainer" || cleanName == "softgridcontainer(clone)" || cleanName == "soft grid container")
            {
                softGridContainer = child.GetComponent<RectTransform>();
            }
            else if (cleanName == "hardgridcontainer" || cleanName == "hardgridcontainer(clone)" || cleanName == "hard grid container")
            {
                hardGridContainer = child.GetComponent<RectTransform>();
            }
        }

        // 2. Scan specifically inside the central word card
        if (centralWordCard != null)
        {
            TextMeshProUGUI[] cardTexts = centralWordCard.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in cardTexts)
            {
                string cleanName = txt.gameObject.name.Trim().ToLower();
                if (cleanName == "wordtextlabel" || cleanName == "wordlabel" || cleanName == "word text label" || cleanName == "wordtext")
                {
                    centralWordText = txt;
                }
            }

            Image[] cardImages = centralWordCard.GetComponentsInChildren<Image>(true);
            foreach (var img in cardImages)
            {
                string cleanName = img.gameObject.name.Trim().ToLower();
                if (cleanName == "wordimage" || cleanName == "cardimage" || cleanName == "word image")
                {
                    centralWordImage = img;
                }
            }

            Button[] cardButtons = centralWordCard.GetComponentsInChildren<Button>(true);
            foreach (var btn in cardButtons)
            {
                string cleanName = btn.gameObject.name.Trim().ToLower();
                if (cleanName == "replaybutton" || cleanName == "replay button" || cleanName == "soundbutton" || cleanName.Contains("speaker"))
                {
                    centralReplayButton = btn;
                }
            }
        }

        // 3. Scan for audio sources
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        foreach (var src in sources)
        {
            string cleanName = src.gameObject.name.Trim().ToLower();
            if (cleanName.Contains("voice") || cleanName.Contains("mascot"))
            {
                voiceAudioSource = src;
            }
            else if (cleanName.Contains("sfx"))
            {
                sfxAudioSource = src;
            }
        }
        if (voiceAudioSource == null && sources.Length > 0) voiceAudioSource = sources[0];
        if (sfxAudioSource == null && sources.Length > 1) sfxAudioSource = sources[1];

        // 4. Global button
        GameObject nextBtn = GameObject.Find("GlobalNextButton");
        if (nextBtn == null) nextBtn = GameObject.Find("NextButton");
        if (nextBtn != null) globalNextButton = nextBtn;

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[TheCRule_P_Senior] AutoAssignUI complete! Wired all references recursively.");
    }
#endif
}
