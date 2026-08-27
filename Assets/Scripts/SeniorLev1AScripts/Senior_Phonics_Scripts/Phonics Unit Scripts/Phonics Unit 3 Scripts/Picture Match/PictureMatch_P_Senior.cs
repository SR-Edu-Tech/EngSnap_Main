using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PictureMatchData
{
    [Tooltip("The vowel category (e.g., Short a, Long e)")]
    public string vowelCategory;

    [Tooltip("The sentence text. E.g. 'A bug is on the rug.'")]
    public string sentenceText;

    [Tooltip("The audio clip of the mascot reading the entire sentence")]
    public AudioClip sentenceAudio;

    [Tooltip("The three picture options")]
    public Sprite[] optionSprites = new Sprite[3];

    [Tooltip("The index of the correct option (0, 1, or 2)")]
    public int correctOptionIndex;
}

[System.Serializable]
public class PictureMatchOption
{
    [Tooltip("The button component of the option card")]
    public Button cardButton;

    [Tooltip("The Image component displaying the option picture")]
    public Image pictureImage;

    [Tooltip("The status indicator circle below the picture")]
    public Image statusCircleImage;

    [Tooltip("The parent/container RectTransform of the card (for scaling/shaking)")]
    public RectTransform containerRect;
}

public class PictureMatch_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;

    [Header("Mascot Intro Audio")]
    public AudioClip introAudio;
    [Header("Gameplay Config")]
    public List<PictureMatchData> questions = new List<PictureMatchData>();
    
    [Header("UI Components - Option Cards")]
    public PictureMatchOption[] options = new PictureMatchOption[3];

    [Header("UI Components - General")]
    public TextMeshProUGUI sentenceTextLabel;
    public TextMeshProUGUI promptLabel;
    public TextMeshProUGUI progressLabel;
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public GameObject continueButton;
    public Button replaySentenceButton;
    public Button listenAgainButton; // Under the vowel indicator
    public RectTransform mascotCharacter;
    public TextMeshProUGUI scoreLabel;

    [Header("Vowel Indicator UI")]
    public TextMeshProUGUI indicatorLetterLabel;
    public Image indicatorLetterImage;
    public Sprite[] indicatorVowelSprites;
    public TextMeshProUGUI indicatorNoteLabel;
    public TextMeshProUGUI vowelCategoryLabel;

    [Header("Audio Source")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip popSFX;
    public AudioClip cheerSFX;
    public AudioClip levelCompleteSFX;

    [Header("Option Card Colors / Status Colors")]
    public Color circleEmptyColor = Color.gray;
    public Color circleCorrectColor = Color.green;
    public Color circleWrongColor = Color.red;

    [Header("Progress Dot Styling")]
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("Vowel Indicator Colors")]
    public string vowelIndicatorRedColorHex = "#A03020";

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private bool _canTap = false;
    private int _score = 0;
    private List<GameObject> _dotInstances = new List<GameObject>();
    private Vector3 _originalMascotScale = Vector3.one;
    private Dictionary<int, Vector3> _originalOptionScales = new Dictionary<int, Vector3>();
    private Coroutine _audioCoroutine;
    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null && options[i].containerRect != null)
            {
                _originalOptionScales[i] = options[i].containerRect.localScale;
            }
            else if (options[i] != null && options[i].cardButton != null)
            {
                _originalOptionScales[i] = options[i].cardButton.transform.localScale;
            }
            else
            {
                _originalOptionScales[i] = Vector3.one;
            }
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (mascotAudioSource == null)
        {
            mascotAudioSource = GetComponent<AudioSource>();
            if (mascotAudioSource == null)
            {
                mascotAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void Start()
    {
        _started = true;

        if (continueButton != null)
        {
            var btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnContinueClicked);
            }
            continueButton.SetActive(false);
        }

        if (replaySentenceButton != null)
        {
            replaySentenceButton.onClick.RemoveAllListeners();
            replaySentenceButton.onClick.AddListener(OnReplaySentenceClicked);
        }

        if (listenAgainButton != null)
        {
            listenAgainButton.onClick.RemoveAllListeners();
            listenAgainButton.onClick.AddListener(OnReplaySentenceClicked);
        }

        for (int i = 0; i < options.Length; i++)
        {
            int index = i; // local copy for closure
            if (options[i] != null && options[i].cardButton != null)
            {
                options[i].cardButton.onClick.RemoveAllListeners();
                options[i].cardButton.onClick.AddListener(() => OnOptionSelected(index));
            }
        }

        ResetToStart();
    }

    private void OnEnable()
    {
        if (_started)
        {
            ResetToStart();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Spacebar bypass: Simulate selecting the correct option
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_canTap && questions != null && _currentIndex < questions.Count)
            {
                int correctIdx = questions[_currentIndex].correctOptionIndex;
                Debug.Log($"[PictureMatch Bypass] Spacebar pressed. Selecting correct option: {correctIdx}");
                OnOptionSelected(correctIdx);
            }
        }
    }
#endif

    public void ResetToStart()
    {
        _currentIndex = 0;
        _score = 0;
        UpdateScoreUI();
        SetupProgressDots();
        LoadQuestion(_currentIndex);
    }

    private void LoadQuestion(int index)
    {
        _currentIndex = index;

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[PictureMatch] No questions configured!");
            return;
        }

        if (index < 0 || index >= questions.Count)
        {
            OnCompletedAll();
            return;
        }

        var data = questions[index];

        // 1. Update vowel indicator panel
        string vowelLetter = GetVowelLetter(data.vowelCategory);
        if (indicatorLetterLabel != null)
        {
            indicatorLetterLabel.text = vowelLetter;
        }
        if (indicatorLetterImage != null && indicatorVowelSprites != null && indicatorVowelSprites.Length > 0)
        {
            int spriteIndex = GetVowelSpriteIndex(vowelLetter);
            if (spriteIndex >= 0 && spriteIndex < indicatorVowelSprites.Length)
            {
                indicatorLetterImage.sprite = indicatorVowelSprites[spriteIndex];
            }
        }
        if (indicatorNoteLabel != null)
        {
            string formattedCategory = FormatCategoryForNote(data.vowelCategory);
            indicatorNoteLabel.text = $"We are learning <color={vowelIndicatorRedColorHex}>{formattedCategory}</color> sound.";
        }

        if (vowelCategoryLabel != null)
        {
            vowelCategoryLabel.text = data.vowelCategory;
        }

        // 2. Set sentence text
        if (sentenceTextLabel != null)
        {
            sentenceTextLabel.text = data.sentenceText;
        }

        // 3. Reset card visual state and set images
        for (int i = 0; i < options.Length; i++)
        {
            var opt = options[i];
            if (opt == null) continue;

            // Set sprite
            if (opt.pictureImage != null && data.optionSprites != null && i < data.optionSprites.Length)
            {
                opt.pictureImage.sprite = data.optionSprites[i];
                opt.pictureImage.gameObject.SetActive(data.optionSprites[i] != null);
            }

            // Reset status circle color
            if (opt.statusCircleImage != null)
            {
                opt.statusCircleImage.color = circleEmptyColor;
            }

            // Reset scale/position
            Transform targetTransform = opt.containerRect != null ? opt.containerRect : (opt.cardButton != null ? opt.cardButton.transform : null);
            if (targetTransform != null)
            {
                LeanTween.cancel(targetTransform.gameObject);
                targetTransform.localScale = _originalOptionScales[i];
                targetTransform.localRotation = Quaternion.identity;
            }
        }

        // 5. Hide continue button first so that progress dots logic reads its inactive state
        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        // 4. Update progress labels and dots
        UpdateProgressLabel();
        UpdateProgressDots();

        // 6. Play pop sound
        if (sfxAudioSource != null && popSFX != null)
        {
            sfxAudioSource.PlayOneShot(popSFX);
        }

        // 7. Mascot entry and play sentence audio
        _canTap = true;
        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.45f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => StartCoroutine(IntroAndStartFlow(data)));
        }
        else
        {
            StartCoroutine(IntroAndStartFlow(data));
        }

        // 8. Card entry animation (scale up nicely with delays for polish)
        for (int i = 0; i < options.Length; i++)
        {
            var opt = options[i];
            if (opt == null) continue;
            Transform targetTransform = opt.containerRect != null ? opt.containerRect : (opt.cardButton != null ? opt.cardButton.transform : null);
            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.zero;
                float delay = 0.1f * i;
                Vector3 destScale = _originalOptionScales[i];
                LeanTween.scale(targetTransform.gameObject, destScale, 0.4f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setDelay(delay);
            }
        }
    }

    private void PlaySentenceReading(PictureMatchData data)
    {
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);

        if (data.sentenceAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = data.sentenceAudio;
            mascotAudioSource.Play();

            _audioCoroutine = StartCoroutine(MascotTalkAnimation(data.sentenceAudio.length));
        }
        else
        {
            _canTap = true;
        }
    }

    private IEnumerator MascotTalkAnimation(float duration)
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.05f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(duration / 0.5f));
        }

        yield return new WaitForSeconds(duration);

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

    private void OnOptionSelected(int index)
    {
        if (!_canTap) return;

        // Stop any running sentence/mascot audio if user selects an option
        if (mascotAudioSource != null && mascotAudioSource.isPlaying)
        {
            mascotAudioSource.Stop();
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            if (mascotCharacter != null)
            {
                LeanTween.cancel(mascotCharacter.gameObject);
                mascotCharacter.localScale = _originalMascotScale;
            }
        }

        var data = questions[_currentIndex];

        if (index == data.correctOptionIndex)
        {
            HandleCorrectChoice(index);
        }
        else
        {
            HandleIncorrectChoice(index);
        }
    }

    private void HandleCorrectChoice(int index)
    {
        _canTap = false;

        var data = questions[_currentIndex];
        var opt = options[index];

        // 1. Play correct SFX and cheering audio
        if (sfxAudioSource != null)
        {
            if (correctSFX != null) sfxAudioSource.PlayOneShot(correctSFX);
            if (cheerSFX != null) StartCoroutine(PlaySFXDelay(cheerSFX, 0.4f));
        }

        // 2. Zoom animation on correct card (scale up and then scale back down to its original state)
        Transform targetTransform = opt.containerRect != null ? opt.containerRect : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform != null)
        {
            LeanTween.cancel(targetTransform.gameObject);
            Vector3 targetScale = _originalOptionScales[index] * 1.15f;
            LeanTween.scale(targetTransform.gameObject, targetScale, 0.35f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(targetTransform.gameObject, _originalOptionScales[index], 0.2f)
                        .setEase(LeanTweenType.easeOutQuad)
                        .setDelay(0.2f); // Keep zoomed briefly during cheer
                });
        }

        // 3. Status circle to green
        if (opt.statusCircleImage != null)
        {
            opt.statusCircleImage.color = circleCorrectColor;
        }

        // 4. Update score (1 point per correct answer)
        _score++;
        UpdateScoreUI();

        // 6. Show continue button with pop-in animation first so progress dots logic reads its active state
        if (continueButton != null)
        {
            continueButton.SetActive(true);
            continueButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(continueButton);
            LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }

        // 5. Update progress dots
        UpdateProgressDots();
    }

    private IEnumerator PlaySFXDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    private void HandleIncorrectChoice(int index)
    {
        _canTap = false;

        var opt = options[index];

        // 1. Play wrong SFX
        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        // 2. Red flash on status circle
        if (opt.statusCircleImage != null)
        {
            opt.statusCircleImage.color = circleWrongColor;
        }

        // 3. Shake card animation
        Transform targetTransform = opt.containerRect != null ? opt.containerRect : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform != null)
        {
            LeanTween.cancel(targetTransform.gameObject);
            
            float shakeAmount = 15f;
            Vector3 origPos = targetTransform.localPosition;
            LeanTween.moveLocalX(targetTransform.gameObject, origPos.x + shakeAmount, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    targetTransform.localPosition = origPos;
                    StartCoroutine(ResetCircleColorAfterDelay(opt, 0.5f));
                    StartCoroutine(RepromptAfterDelay(1.0f));
                    _canTap = true; // Allow selection again after the shake completes
                });
        }
        else
        {
            StartCoroutine(ResetCircleColorAfterDelay(opt, 0.5f));
            StartCoroutine(RepromptAfterDelay(1.0f));
            _canTap = true; // Allow selection again
        }
    }

    private IEnumerator ResetCircleColorAfterDelay(PictureMatchOption opt, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (opt != null && opt.statusCircleImage != null)
        {
            opt.statusCircleImage.color = circleEmptyColor;
        }
    }

    private IEnumerator RepromptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        var data = questions[_currentIndex];
        PlaySentenceReading(data);
    }

    private void OnContinueClicked()
    {
        int nextIndex = _currentIndex + 1;
        if (nextIndex < questions.Count)
        {
            LoadQuestion(nextIndex);
        }
        else
        {
            OnCompletedAll();
        }
    }

    private void OnCompletedAll()
    {
        Debug.Log("[PictureMatch] Completed all questions!");
        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnReplaySentenceClicked()
    {
        if (questions == null || _currentIndex >= questions.Count) return;
        PlaySentenceReading(questions[_currentIndex]);
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
            
            // Pop animation on score change
            LeanTween.cancel(scoreLabel.gameObject);
            scoreLabel.transform.localScale = Vector3.one * 1.3f;
            LeanTween.scale(scoreLabel.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutQuad);
        }
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
        {
            progressLabel.text = $"Question {_currentIndex + 1} / {questions.Count}";
        }
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        List<GameObject> keptDots;
        GameObject activeDotTemplate = PrepareContainer(progressDotsContainer, progressDotPrefab, out keptDots);
        _dotInstances.Clear();

        if (activeDotTemplate == null)
        {
            Debug.LogError("[PictureMatch] No progress dot prefab or template found!");
            return;
        }

        for (int i = 0; i < questions.Count; i++)
        {
            GameObject dotObj = Instantiate(activeDotTemplate, progressDotsContainer);
            dotObj.SetActive(true);
            _dotInstances.Add(dotObj);
        }

        UpdateProgressDots();
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            Image img = _dotInstances[i].GetComponent<Image>();
            if (img == null) img = _dotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isCompleted = i < _currentIndex || (i == _currentIndex && continueButton != null && continueButton.activeSelf);
                if (isCompleted)
                {
                    img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;
                }
                else
                {
                    img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                }
            }
        }
    }

    private GameObject PrepareContainer(RectTransform container, GameObject prefab, out List<GameObject> keptObjects)
    {
        keptObjects = new List<GameObject>();
        if (container == null) return null;

        GameObject template = prefab;

        if (template != null)
        {
            foreach (Transform child in container)
            {
                if (child.gameObject != template)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                else
                {
                    keptObjects.Add(child.gameObject);
                }
            }
        }
        else
        {
            foreach (Transform child in container)
            {
                string nameLower = child.name.ToLower();
                if (nameLower != "bg" && nameLower != "background" && template == null)
                {
                    template = child.gameObject;
                    template.SetActive(false);
                    keptObjects.Add(template);
                }
                else if (nameLower == "bg" || nameLower == "background")
                {
                    keptObjects.Add(child.gameObject);
                }
                else
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            if (template == null && container.parent != null)
            {
                foreach (Transform sibling in container.parent)
                {
                    string nameLower = sibling.name.ToLower();
                    if (sibling.gameObject != container.gameObject && 
                        nameLower != "bg" && 
                        nameLower != "background" && 
                        template == null)
                    {
                        template = sibling.gameObject;
                        template.SetActive(false);
                        break;
                    }
                }
            }
        }

        return template;
    }

    private string GetVowelLetter(string category)
    {
        if (string.IsNullOrEmpty(category)) return "A";
        string lower = category.ToLowerInvariant().Trim();
        
        string[] parts = lower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            if (part == "a") return "A";
            if (part == "e") return "E";
            if (part == "i") return "I";
            if (part == "o") return "O";
            if (part == "u") return "U";
        }
        
        if (lower.Contains("short a") || lower.Contains("long a") || lower.EndsWith("a")) return "A";
        if (lower.Contains("short e") || lower.Contains("long e") || lower.EndsWith("e")) return "E";
        if (lower.Contains("short i") || lower.Contains("long i") || lower.EndsWith("i")) return "I";
        if (lower.Contains("short o") || lower.Contains("long o") || lower.EndsWith("o")) return "O";
        if (lower.Contains("short u") || lower.Contains("long u") || lower.EndsWith("u")) return "U";

        return "A";
    }

    private int GetVowelSpriteIndex(string vowelLetter)
    {
        switch (vowelLetter.ToUpperInvariant())
        {
            case "A": return 0;
            case "E": return 1;
            case "I": return 2;
            case "O": return 3;
            case "U": return 4;
            default: return 0;
        }
    }

    private string FormatCategoryForNote(string category)
    {
        if (string.IsNullOrEmpty(category)) return "";
        string[] parts = category.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                if (parts[i].Length == 1)
                {
                    parts[i] = parts[i].ToUpperInvariant();
                }
                else
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
                    if (i == parts.Length - 1)
                    {
                        parts[i] = parts[i].ToUpperInvariant();
                    }
                }
            }
        }
        return string.Join(" ", parts);
    }

    private IEnumerator IntroAndStartFlow(PictureMatchData data)
    {
        _canTap = false;
        if (_currentIndex == 0 && introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
        }
        PlaySentenceReading(data);
        _canTap = true;
    }

}