using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class TheGRuleWord
{
    [Tooltip("The plain word text, e.g. 'gem'")]
    public string wordText;

    [Tooltip("The formatted word text with letter after 'g' highlighted, e.g. 'g<b><color=#FF3366><u>e</u></color></b>m'")]
    public string highlightedWordText;

    [Tooltip("True if soft G sound /j/ (when followed by e, i, y), false if hard G sound /g/")]
    public bool isSoftG;

    [Tooltip("The letter following 'g' that determines the sound, e.g. 'e', 'i', 'y', 'a', 'o', 'u'")]
    public string ruleLetter;

    [Tooltip("Optional image sprite for the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for speaking the word")]
    public AudioClip wordAudio;
}

public class TheGRule_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    [Tooltip("Soft G Word List (/j/)")]
    public List<TheGRuleWord> softGWords = new List<TheGRuleWord>();

    [Tooltip("Hard G Word List (/g/)")]
    public List<TheGRuleWord> hardGWords = new List<TheGRuleWord>();

    [Tooltip("Highlight color for the letter after 'g'")]
    public Color highlightColor = new Color(1f, 0.2f, 0.4f); // Vibrant Pink/Red

    [Header("Grid Containers & Templates")]
    [Tooltip("Grid panel to contain Soft G word buttons")]
    public RectTransform softGridContainer;

    [Tooltip("Grid panel to contain Hard G word buttons")]
    public RectTransform hardGridContainer;

    [Tooltip("Template button to clone for word grid entries")]
    public GameObject wordButtonTemplate;

    [Header("UI Mascot References")]
    [Tooltip("Giraffe Mascot RectTransform (Soft G /j/)")]
    public RectTransform giraffeMascot;

    [Tooltip("Frog Mascot RectTransform (Hard G /g/)")]
    public RectTransform frogMascot;

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
    [Tooltip("Audio clip to play when the teach screen opens")]
    public AudioClip introAudio;

    [Tooltip("Audio clip for Soft G rule sound (/j/)")]
    public AudioClip softGRuleAudio;

    [Tooltip("Audio clip for Hard G rule sound (/g/)")]
    public AudioClip hardGRuleAudio;

    public AudioClip popSFX;
    public AudioClip transitionSFX;

    [Header("Events & Completion")]
    public UnityEvent onTeachComplete;

    // Runtime state
    private bool _canTap = true;
    private TheGRuleWord _currentlySelectedWord;
    private Vector3 _origGiraffeScale = Vector3.one;
    private Vector3 _origFrogScale = Vector3.one;
    private Vector3 _origWordCardScale = Vector3.one;

    private HashSet<string> _tappedSoftWords = new HashSet<string>();
    private HashSet<string> _tappedHardWords = new HashSet<string>();

    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (giraffeMascot != null) _origGiraffeScale = giraffeMascot.localScale;
        if (frogMascot != null) _origFrogScale = frogMascot.localScale;
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

        // Instantiate Soft G buttons
        foreach (var word in softGWords)
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
                TheGRuleWord w = word;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectWord(w, btnObj.transform));
            }
        }

        // Instantiate Hard G buttons
        foreach (var word in hardGWords)
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
                TheGRuleWord w = word;
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
        if (giraffeMascot != null)
        {
            giraffeMascot.localScale = Vector3.zero;
            StartCoroutine(PopUI(giraffeMascot));
        }

        if (frogMascot != null)
        {
            frogMascot.localScale = Vector3.zero;
            StartCoroutine(PopUI(frogMascot));
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
    public void SelectWord(TheGRuleWord word, Transform wordButtonTransform)
    {
        if (!_canTap) return;
        StartCoroutine(PlayWordSequence(word, wordButtonTransform));
    }

    private IEnumerator PlayWordSequence(TheGRuleWord word, Transform wordButtonTransform)
    {
        _canTap = false;
        _currentlySelectedWord = word;

        // 1. Mark word as explored
        if (word.isSoftG)
            _tappedSoftWords.Add(word.wordText);
        else
            _tappedHardWords.Add(word.wordText);

        // 2. Wiggle the button clicked
        if (wordButtonTransform != null)
        {
            StartCoroutine(WiggleAnimation(wordButtonTransform, Vector3.one, Quaternion.identity, 0.6f));
        }

        // 3. Update and Pop central word card
        if (centralWordCard != null)
        {
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

        yield return new WaitForSeconds(0.2f);

        // 4. Animate the letter right after 'g' lighting up
        string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
        if (centralWordText != null)
        {
            centralWordText.text = FormatWordWithHighlight(word.wordText, hexColor);
            StartCoroutine(PulseCentralText());
        }

        // 5. Bounce corresponding mascot and play G phoneme sound
        AudioClip ruleAudio = word.isSoftG ? softGRuleAudio : hardGRuleAudio;
        RectTransform mascot = word.isSoftG ? giraffeMascot : frogMascot;
        Vector3 origMascotScale = word.isSoftG ? _origGiraffeScale : _origFrogScale;

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

        // 7. Check if we should reveal the Next button
        CheckNextButtonUnlock();

        _canTap = true;
    }

    private void ReplayCurrentWordAudio()
    {
        if (_currentlySelectedWord == null || _currentlySelectedWord.wordAudio == null || voiceAudioSource == null) return;
        voiceAudioSource.clip = _currentlySelectedWord.wordAudio;
        voiceAudioSource.Play();

        RectTransform mascot = _currentlySelectedWord.isSoftG ? giraffeMascot : frogMascot;
        Vector3 origMascotScale = _currentlySelectedWord.isSoftG ? _origGiraffeScale : _origFrogScale;
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
    /// Formats the word to highlight the letter right after 'g'.
    /// </summary>
    public static string FormatWordWithHighlight(string word, string colorHex = "FF3366")
    {
        if (string.IsNullOrEmpty(word)) return word;

        int gIndex = word.IndexOf('g', StringComparison.OrdinalIgnoreCase);
        if (gIndex >= 0)
        {
            if (gIndex < word.Length - 1)
            {
                string before = word.Substring(0, gIndex + 1);
                char letterAfter = word[gIndex + 1];
                string after = word.Substring(gIndex + 2);
                return $"{before}<b><color=#{colorHex}><u>{letterAfter}</u></color></b>{after}";
            }
            else
            {
                string before = word.Substring(0, gIndex);
                char gChar = word[gIndex];
                return $"{before}<b><color=#{colorHex}><u>{gChar}</u></color></b>";
            }
        }

        return word;
    }
}
