using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.IO;

[System.Serializable]
public class BlendVsDigraphItem
{
    [Header("Blend Configuration")]
    public string blendLetter1;
    public string blendLetter2;
    public string blendCombined;
    public string blendExampleWord;
    public Sprite blendExampleSprite;
    public AudioClip blendLetter1Audio;
    public AudioClip blendLetter2Audio;
    public AudioClip blendCombinedAudio;
    public AudioClip blendWordAudio;

    [Header("Digraph Configuration")]
    public string digraphLetters;
    public string digraphExampleWord;
    public Sprite digraphExampleSprite;
    public AudioClip digraphSoundAudio;
    public AudioClip digraphWordAudio;
    [Tooltip("Subtitle or helper text for digraph card (e.g. 'one sound')")]
    public string digraphSubtitle = "one sound";
}

public class TeachBlendVsDigraph_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Comparisons Configuration")]
    public List<BlendVsDigraphItem> comparisonsList = new List<BlendVsDigraphItem>();

    [Header("General UI Components")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionLabel;
    public RectTransform mascotCharacter;
    public GameObject nextCardButton;
    public GameObject prevButton;
    public GameObject globalNextButton;

    [Header("Left Card (Blend Builder)")]
    public Button leftCardButton;
    public RectTransform blendBuilderContainer;
    public TextMeshProUGUI blendLetter1Text;
    public TextMeshProUGUI blendPlusText;
    public TextMeshProUGUI blendLetter2Text;
    public TextMeshProUGUI blendArrow1Text;
    public TextMeshProUGUI blendCombinedText;
    public TextMeshProUGUI blendArrow2Text;
    public TextMeshProUGUI blendWordText;
    public Image blendWordImage;

    [Tooltip("The arrow symbol character used in the UI. Change this if your font does not display the default symbol.")]
    public string arrowSymbol = "➔";

    [Header("Right Card (Digraph)")]
    public Button rightCardButton;
    public RectTransform digraphContainer;
    public TextMeshProUGUI digraphText;
    public TextMeshProUGUI digraphSubtitleText;
    public TextMeshProUGUI digraphWordText;
    public Image digraphWordImage;

    [Header("Progress Bar Dotted")]
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = new Color32(255, 255, 255, 60);
    public Color dotFilledColor = new Color32(76, 175, 80, 255);
    private List<GameObject> _dotInstances = new List<GameObject>();

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip introScreenAudio;
    public AudioClip popSFX;
    public AudioClip transitionSFX;

    [Header("Events & Transitions")]
    public UnityEvent onTeachComplete;
    public float wordScaleHighlight = 1.15f;
    public float animationSpeed = 4f;

    // Runtime state
    private int _currentIndex = 0;
    private bool _canTap = false;
    private Coroutine _audioSeqCoroutine;
    private Vector3 _originalMascotScale = Vector3.one;

    // Cached scales/rotations for resets
    private Vector3 _origLeftCardScale = Vector3.one;
    private Quaternion _origLeftCardRot = Quaternion.identity;
    private Vector3 _origRightCardScale = Vector3.one;
    private Quaternion _origRightCardRot = Quaternion.identity;

    private GameFlowManager_Senior_Phonics _flowManager;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (comparisonsList == null || comparisonsList.Count == 0)
        {
            PopulateDefaultComparisons();
        }
    }
#endif

    private void Awake()
    {
        // Cache original values
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        if (leftCardButton != null)
        {
            _origLeftCardScale = leftCardButton.transform.localScale;
            _origLeftCardRot = leftCardButton.transform.localRotation;
        }

        if (rightCardButton != null)
        {
            _origRightCardScale = rightCardButton.transform.localScale;
            _origRightCardRot = rightCardButton.transform.localRotation;
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Dynamically find global next button under the unit panel
        if (globalNextButton == null)
        {
            Transform unitParent = transform.parent != null ? transform.parent.parent : null;
            if (unitParent != null)
            {
                Transform nextBtnTrans = unitParent.Find("NextButton");
                if (nextBtnTrans != null)
                {
                    globalNextButton = nextBtnTrans.gameObject;
                }
            }
        }

        // Populate default comparisons if empty
        if (comparisonsList == null || comparisonsList.Count == 0)
        {
            PopulateDefaultComparisons();
        }
    }

    private void Start()
    {
        // Hook button listeners
        if (leftCardButton != null)
        {
            leftCardButton.onClick.RemoveAllListeners();
            leftCardButton.onClick.AddListener(OnLeftCardTapped);
        }

        if (rightCardButton != null)
        {
            rightCardButton.onClick.RemoveAllListeners();
            rightCardButton.onClick.AddListener(OnRightCardTapped);
        }

        if (nextCardButton != null)
        {
            Button btn = nextCardButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextCardTapped);
            }
        }

        if (prevButton != null)
        {
            Button btn = prevButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnPrevTapped);
            }
        }

        SetupProgressDots();
        ResetUI();
    }

    private void OnEnable()
    {
        ResetUI();
        StartCoroutine(IntroFlow());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        _audioSeqCoroutine = null;
    }

    private void ResetUI()
    {
        // Set scales of pop-in objects to zero
        if (mascotCharacter != null) mascotCharacter.localScale = Vector3.zero;
        if (nextCardButton != null) nextCardButton.SetActive(false);
        if (prevButton != null) prevButton.SetActive(false);
        if (globalNextButton != null) globalNextButton.SetActive(false);

        if (leftCardButton != null) leftCardButton.gameObject.SetActive(false);
        if (rightCardButton != null) rightCardButton.gameObject.SetActive(false);

        ResetCardVisuals();

        _currentIndex = 0;
        _canTap = false;
        UpdateProgressDots();
    }

    private void ResetCardVisuals()
    {
        if (leftCardButton != null)
        {
            leftCardButton.transform.localScale = _origLeftCardScale;
            leftCardButton.transform.localRotation = _origLeftCardRot;
        }

        if (rightCardButton != null)
        {
            rightCardButton.transform.localScale = _origRightCardScale;
            rightCardButton.transform.localRotation = _origRightCardRot;
        }
    }

    private void PopulateDefaultComparisons()
    {
        comparisonsList = new List<BlendVsDigraphItem>();

        // 1. bl vs sh
        comparisonsList.Add(new BlendVsDigraphItem
        {
            blendLetter1 = "b",
            blendLetter2 = "l",
            blendCombined = "bl",
            blendExampleWord = "block",
            digraphLetters = "sh",
            digraphExampleWord = "shop",
            digraphSubtitle = "one sound"
        });

        // 2. fr vs ch
        comparisonsList.Add(new BlendVsDigraphItem
        {
            blendLetter1 = "f",
            blendLetter2 = "r",
            blendCombined = "fr",
            blendExampleWord = "frog",
            digraphLetters = "ch",
            digraphExampleWord = "chin",
            digraphSubtitle = "one sound"
        });

        // 3. st vs th
        comparisonsList.Add(new BlendVsDigraphItem
        {
            blendLetter1 = "s",
            blendLetter2 = "t",
            blendCombined = "st",
            blendExampleWord = "star",
            digraphLetters = "th",
            digraphExampleWord = "thin",
            digraphSubtitle = "one sound"
        });

        // 4. mp vs ck (ending blend vs ending digraph)
        comparisonsList.Add(new BlendVsDigraphItem
        {
            blendLetter1 = "m",
            blendLetter2 = "p",
            blendCombined = "mp",
            blendExampleWord = "camp",
            digraphLetters = "ck",
            digraphExampleWord = "duck",
            digraphSubtitle = "one sound"
        });
    }

    private IEnumerator IntroFlow()
    {
        // 1. Play general screen intro audio if configured
        if (audioSource != null && introScreenAudio != null)
        {
            audioSource.clip = introScreenAudio;
            audioSource.Play();
        }

        // 2. Pop in the Mascot character
        if (mascotCharacter != null)
        {
            yield return StartCoroutine(PopUI(mascotCharacter));
        }

        if (audioSource != null && introScreenAudio != null)
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Load the first item
        LoadComparison(0);
    }

    private void LoadComparison(int index)
    {
        if (comparisonsList == null || index < 0 || index >= comparisonsList.Count)
        {
            Debug.LogError("[TeachBlendVsDigraph] Comparison index out of range: " + index);
            return;
        }

        _currentIndex = index;
        _canTap = false;

        if (nextCardButton != null) nextCardButton.SetActive(false);
        if (prevButton != null) prevButton.SetActive(index > 0);

        if (_audioSeqCoroutine != null)
        {
            StopCoroutine(_audioSeqCoroutine);
            _audioSeqCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        ResetCardVisuals();
        UpdateProgressDots();

        BlendVsDigraphItem data = comparisonsList[index];

        // Populate Left (Blend) Text UI
        if (blendLetter1Text != null) blendLetter1Text.text = data.blendLetter1;
        if (blendPlusText != null) blendPlusText.text = "+";
        if (blendLetter2Text != null) blendLetter2Text.text = data.blendLetter2;
        if (blendArrow1Text != null) blendArrow1Text.text = arrowSymbol;
        if (blendCombinedText != null) blendCombinedText.text = data.blendCombined;
        if (blendArrow2Text != null) blendArrow2Text.text = arrowSymbol;
        if (blendWordText != null) blendWordText.text = data.blendExampleWord;
        if (blendWordImage != null)
        {
            if (data.blendExampleSprite != null)
            {
                blendWordImage.sprite = data.blendExampleSprite;
                blendWordImage.gameObject.SetActive(true);
            }
            else
            {
                blendWordImage.gameObject.SetActive(false);
            }
        }

        // Populate Right (Digraph) Text UI
        if (digraphText != null) digraphText.text = data.digraphLetters;
        if (digraphSubtitleText != null) digraphSubtitleText.text = data.digraphSubtitle;
        if (digraphWordText != null) digraphWordText.text = data.digraphExampleWord;
        if (digraphWordImage != null)
        {
            if (data.digraphExampleSprite != null)
            {
                digraphWordImage.sprite = data.digraphExampleSprite;
                digraphWordImage.gameObject.SetActive(true);
            }
            else
            {
                digraphWordImage.gameObject.SetActive(false);
            }
        }

        // Deactivate components of the left blend builder initially for sequential reveal
        SetBlendBuilderActiveStates(false);
        if (rightCardButton != null) rightCardButton.gameObject.SetActive(false);

        // Turn left card on and trigger pop-in
        if (leftCardButton != null)
        {
            leftCardButton.gameObject.SetActive(true);
            leftCardButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(leftCardButton.gameObject);
            LeanTween.scale(leftCardButton.gameObject, Vector3.one, 0.45f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    _audioSeqCoroutine = StartCoroutine(PlayFullSequence(data));
                });
        }
        else
        {
            _audioSeqCoroutine = StartCoroutine(PlayFullSequence(data));
        }
    }

    private void SetBlendBuilderActiveStates(bool active)
    {
        if (blendLetter1Text != null) blendLetter1Text.gameObject.SetActive(active);
        if (blendPlusText != null) blendPlusText.gameObject.SetActive(active);
        if (blendLetter2Text != null) blendLetter2Text.gameObject.SetActive(active);
        if (blendArrow1Text != null) blendArrow1Text.gameObject.SetActive(active);
        if (blendCombinedText != null) blendCombinedText.gameObject.SetActive(active);
        if (blendArrow2Text != null) blendArrow2Text.gameObject.SetActive(active);
        if (blendWordText != null) blendWordText.gameObject.SetActive(active);
        if (blendWordImage != null && blendWordImage.sprite != null) blendWordImage.gameObject.SetActive(active);
    }

    private IEnumerator PlayFullSequence(BlendVsDigraphItem data)
    {
        _canTap = false;
        yield return StartCoroutine(LoadAssetsIfNeeded(data));

        // --- PHASE 1: Blend Builder sequential reveal ---
        // 1. Reveal Letter 1
        if (blendLetter1Text != null)
        {
            blendLetter1Text.gameObject.SetActive(true);
            yield return StartCoroutine(PopUI(blendLetter1Text.GetComponent<RectTransform>()));
        }
        if (data.blendLetter1Audio != null && audioSource != null)
        {
            audioSource.clip = data.blendLetter1Audio;
            audioSource.Play();
            if (blendLetter1Text != null)
            {
                StartCoroutine(WiggleAnimation(blendLetter1Text.transform, Vector3.one, Quaternion.identity, data.blendLetter1Audio.length));
            }
            yield return StartCoroutine(MascotTalkAnimation(data.blendLetter1Audio.length));
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            if (blendLetter1Text != null)
            {
                yield return StartCoroutine(WiggleAnimation(blendLetter1Text.transform, Vector3.one, Quaternion.identity, 0.8f));
            }
        }

        // 2. Reveal Plus & Letter 2
        if (blendPlusText != null)
        {
            blendPlusText.gameObject.SetActive(true);
            StartCoroutine(PopUI(blendPlusText.GetComponent<RectTransform>()));
        }
        if (blendLetter2Text != null)
        {
            blendLetter2Text.gameObject.SetActive(true);
            yield return StartCoroutine(PopUI(blendLetter2Text.GetComponent<RectTransform>()));
        }
        if (data.blendLetter2Audio != null && audioSource != null)
        {
            audioSource.clip = data.blendLetter2Audio;
            audioSource.Play();
            if (blendLetter2Text != null)
            {
                StartCoroutine(WiggleAnimation(blendLetter2Text.transform, Vector3.one, Quaternion.identity, data.blendLetter2Audio.length));
            }
            yield return StartCoroutine(MascotTalkAnimation(data.blendLetter2Audio.length));
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            if (blendLetter2Text != null)
            {
                yield return StartCoroutine(WiggleAnimation(blendLetter2Text.transform, Vector3.one, Quaternion.identity, 0.8f));
            }
        }

        // 3. Reveal Arrow 1 & Combined Blend
        if (blendArrow1Text != null)
        {
            blendArrow1Text.gameObject.SetActive(true);
            StartCoroutine(PopUI(blendArrow1Text.GetComponent<RectTransform>()));
        }
        if (blendCombinedText != null)
        {
            blendCombinedText.gameObject.SetActive(true);
            yield return StartCoroutine(PopUI(blendCombinedText.GetComponent<RectTransform>()));
        }
        if (data.blendCombinedAudio != null && audioSource != null)
        {
            audioSource.clip = data.blendCombinedAudio;
            audioSource.Play();
            if (blendCombinedText != null)
            {
                StartCoroutine(WiggleAnimation(blendCombinedText.transform, Vector3.one, Quaternion.identity, data.blendCombinedAudio.length));
            }
            yield return StartCoroutine(MascotTalkAnimation(data.blendCombinedAudio.length));
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            if (blendCombinedText != null)
            {
                yield return StartCoroutine(WiggleAnimation(blendCombinedText.transform, Vector3.one, Quaternion.identity, 0.8f));
            }
        }

        // 4. Reveal Arrow 2, Word & Word Sprite
        if (blendArrow2Text != null)
        {
            blendArrow2Text.gameObject.SetActive(true);
            StartCoroutine(PopUI(blendArrow2Text.GetComponent<RectTransform>()));
        }
        if (blendWordText != null)
        {
            blendWordText.gameObject.SetActive(true);
            StartCoroutine(PopUI(blendWordText.GetComponent<RectTransform>()));
        }
        if (blendWordImage != null && data.blendExampleSprite != null)
        {
            blendWordImage.gameObject.SetActive(true);
            yield return StartCoroutine(PopUI(blendWordImage.GetComponent<RectTransform>()));
        }

        if (data.blendWordAudio != null && audioSource != null)
        {
            audioSource.clip = data.blendWordAudio;
            audioSource.Play();
            if (blendWordText != null)
            {
                StartCoroutine(WiggleAnimation(blendWordText.transform, Vector3.one, Quaternion.identity, data.blendWordAudio.length));
            }
            if (blendWordImage != null && blendWordImage.gameObject.activeSelf)
            {
                StartCoroutine(WiggleAnimation(blendWordImage.transform, Vector3.one, Quaternion.identity, data.blendWordAudio.length));
            }
            yield return StartCoroutine(MascotTalkAnimation(data.blendWordAudio.length));
            yield return new WaitForSeconds(0.4f);
        }
        else
        {
            if (blendWordText != null)
            {
                yield return StartCoroutine(WiggleAnimation(blendWordText.transform, Vector3.one, Quaternion.identity, 1.0f));
            }
        }

        // --- PHASE 2: Digraph Card Entry & Reveal ---
        if (rightCardButton != null)
        {
            rightCardButton.gameObject.SetActive(true);
            rightCardButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(rightCardButton.gameObject);
            yield return StartCoroutine(PopUI(rightCardButton.GetComponent<RectTransform>()));

            // A. Digraph Sound
            if (data.digraphSoundAudio != null && audioSource != null)
            {
                audioSource.clip = data.digraphSoundAudio;
                audioSource.Play();
                if (digraphText != null)
                {
                    StartCoroutine(WiggleAnimation(digraphText.transform, Vector3.one, Quaternion.identity, data.digraphSoundAudio.length));
                }
                yield return StartCoroutine(MascotTalkAnimation(data.digraphSoundAudio.length));
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                if (digraphText != null)
                {
                    yield return StartCoroutine(WiggleAnimation(digraphText.transform, Vector3.one, Quaternion.identity, 0.8f));
                }
            }

            // B. Digraph Word
            if (data.digraphWordAudio != null && audioSource != null)
            {
                audioSource.clip = data.digraphWordAudio;
                audioSource.Play();
                if (digraphWordText != null)
                {
                    StartCoroutine(WiggleAnimation(digraphWordText.transform, Vector3.one, Quaternion.identity, data.digraphWordAudio.length));
                }
                if (digraphWordImage != null && digraphWordImage.gameObject.activeSelf)
                {
                    StartCoroutine(WiggleAnimation(digraphWordImage.transform, Vector3.one, Quaternion.identity, data.digraphWordAudio.length));
                }
                yield return StartCoroutine(MascotTalkAnimation(data.digraphWordAudio.length));
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                if (digraphWordText != null)
                {
                    yield return StartCoroutine(WiggleAnimation(digraphWordText.transform, Vector3.one, Quaternion.identity, 0.8f));
                }
            }
        }

        _canTap = true;
        ShowNextCardButton();
    }

    private void ShowNextCardButton()
    {
        if (nextCardButton != null && !nextCardButton.activeSelf)
        {
            nextCardButton.SetActive(true);

            if (audioSource != null && popSFX != null)
            {
                audioSource.PlayOneShot(popSFX);
            }

            nextCardButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(nextCardButton);
            LeanTween.scale(nextCardButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void OnLeftCardTapped()
    {
        if (!_canTap) return;

        if (_audioSeqCoroutine != null) StopCoroutine(_audioSeqCoroutine);
        ResetCardVisuals();

        BlendVsDigraphItem data = comparisonsList[_currentIndex];
        _audioSeqCoroutine = StartCoroutine(PlayLeftOnly(data));
    }

    private void OnRightCardTapped()
    {
        if (!_canTap) return;

        if (_audioSeqCoroutine != null) StopCoroutine(_audioSeqCoroutine);
        ResetCardVisuals();

        BlendVsDigraphItem data = comparisonsList[_currentIndex];
        _audioSeqCoroutine = StartCoroutine(PlayRightOnly(data));
    }

    private IEnumerator PlayLeftOnly(BlendVsDigraphItem data)
    {
        _canTap = false;

        // Repeat the entire sequence for the blend building
        // Letter 1
        if (data.blendLetter1Audio != null && audioSource != null)
        {
            audioSource.clip = data.blendLetter1Audio;
            audioSource.Play();
            if (blendLetter1Text != null) StartCoroutine(WiggleAnimation(blendLetter1Text.transform, Vector3.one, Quaternion.identity, data.blendLetter1Audio.length));
            yield return StartCoroutine(MascotTalkAnimation(data.blendLetter1Audio.length));
            yield return new WaitForSeconds(0.2f);
        }

        // Letter 2
        if (data.blendLetter2Audio != null && audioSource != null)
        {
            audioSource.clip = data.blendLetter2Audio;
            audioSource.Play();
            if (blendLetter2Text != null) StartCoroutine(WiggleAnimation(blendLetter2Text.transform, Vector3.one, Quaternion.identity, data.blendLetter2Audio.length));
            yield return StartCoroutine(MascotTalkAnimation(data.blendLetter2Audio.length));
            yield return new WaitForSeconds(0.2f);
        }

        // Combined Blend
        if (data.blendCombinedAudio != null && audioSource != null)
        {
            audioSource.clip = data.blendCombinedAudio;
            audioSource.Play();
            if (blendCombinedText != null) StartCoroutine(WiggleAnimation(blendCombinedText.transform, Vector3.one, Quaternion.identity, data.blendCombinedAudio.length));
            yield return StartCoroutine(MascotTalkAnimation(data.blendCombinedAudio.length));
            yield return new WaitForSeconds(0.2f);
        }

        // Example Word
        if (data.blendWordAudio != null && audioSource != null)
        {
            audioSource.clip = data.blendWordAudio;
            audioSource.Play();
            if (blendWordText != null) StartCoroutine(WiggleAnimation(blendWordText.transform, Vector3.one, Quaternion.identity, data.blendWordAudio.length));
            if (blendWordImage != null && blendWordImage.gameObject.activeSelf) StartCoroutine(WiggleAnimation(blendWordImage.transform, Vector3.one, Quaternion.identity, data.blendWordAudio.length));
            yield return StartCoroutine(MascotTalkAnimation(data.blendWordAudio.length));
            yield return new WaitForSeconds(0.2f);
        }

        _canTap = true;
    }

    private IEnumerator PlayRightOnly(BlendVsDigraphItem data)
    {
        _canTap = false;

        // Repeat the digraph sound and word
        if (data.digraphSoundAudio != null && audioSource != null)
        {
            audioSource.clip = data.digraphSoundAudio;
            audioSource.Play();
            if (digraphText != null) StartCoroutine(WiggleAnimation(digraphText.transform, Vector3.one, Quaternion.identity, data.digraphSoundAudio.length));
            yield return StartCoroutine(MascotTalkAnimation(data.digraphSoundAudio.length));
            yield return new WaitForSeconds(0.2f);
        }

        if (data.digraphWordAudio != null && audioSource != null)
        {
            audioSource.clip = data.digraphWordAudio;
            audioSource.Play();
            if (digraphWordText != null) StartCoroutine(WiggleAnimation(digraphWordText.transform, Vector3.one, Quaternion.identity, data.digraphWordAudio.length));
            if (digraphWordImage != null && digraphWordImage.gameObject.activeSelf) StartCoroutine(WiggleAnimation(digraphWordImage.transform, Vector3.one, Quaternion.identity, data.digraphWordAudio.length));
            yield return StartCoroutine(MascotTalkAnimation(data.digraphWordAudio.length));
            yield return new WaitForSeconds(0.2f);
        }

        _canTap = true;
    }

    private void OnNextCardTapped()
    {
        if (transitionSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSFX);
        }

        int nextIndex = _currentIndex + 1;
        if (nextIndex < comparisonsList.Count)
        {
            LoadComparison(nextIndex);
        }
        else
        {
            StartCoroutine(DelayFinishTeachScreen());
        }
    }

    private IEnumerator DelayFinishTeachScreen()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        if (unitCompleteAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(unitCompleteAudio);
            yield return new WaitForSeconds(unitCompleteAudio.length + 0.5f);
        }

        // Hide digraph navigation buttons
        if (nextCardButton != null) nextCardButton.SetActive(false);
        if (prevButton != null) prevButton.SetActive(false);

        // Activate the global NextButton of the unit
        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
            globalNextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(globalNextButton);
            LeanTween.scale(globalNextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);
        }

        if (onTeachComplete != null)
        {
            onTeachComplete.Invoke();
        }
        else if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnPrevTapped()
    {
        if (transitionSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSFX);
        }

        int prevIndex = _currentIndex - 1;
        if (prevIndex >= 0)
        {
            LoadComparison(prevIndex);
        }
    }

    private IEnumerator LoadAssetsIfNeeded(BlendVsDigraphItem item)
    {
        // Try dynamic loading of audios if not pre-configured
        string basePath = Application.dataPath + "/Phonics/Audio/Unit 6 Phonics/";
        
        if (item.blendLetter1Audio == null)
        {
            AudioClip clip = null;
            string path = "file://" + basePath + item.blendLetter1 + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            item.blendLetter1Audio = clip;
        }

        if (item.blendLetter2Audio == null)
        {
            AudioClip clip = null;
            string path = "file://" + basePath + item.blendLetter2 + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            item.blendLetter2Audio = clip;
        }

        if (item.blendCombinedAudio == null)
        {
            AudioClip clip = null;
            string path = "file://" + basePath + item.blendCombined + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            item.blendCombinedAudio = clip;
        }

        if (item.blendWordAudio == null)
        {
            AudioClip clip = null;
            string path = "file://" + basePath + item.blendExampleWord + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            item.blendWordAudio = clip;
        }

        if (item.digraphSoundAudio == null)
        {
            AudioClip clip = null;
            string path = "file://" + basePath + item.digraphLetters + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            item.digraphSoundAudio = clip;
        }

        if (item.digraphWordAudio == null)
        {
            AudioClip clip = null;
            string path = "file://" + basePath + item.digraphExampleWord + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            item.digraphWordAudio = clip;
        }

        // Try load sprites from Unit 6 folder if not set
        if (item.blendExampleSprite == null)
        {
            string path = Application.dataPath + "/Phonics/Sprites/Unit 6/" + item.blendExampleWord + ".png";
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    item.blendExampleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        if (item.digraphExampleSprite == null)
        {
            string path = Application.dataPath + "/Phonics/Sprites/Unit 6/" + item.digraphExampleWord + ".png";
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    item.digraphExampleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
    }

    private IEnumerator LoadAudioClipFromUrl(string url, System.Action<AudioClip> callback)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(DownloadHandlerAudioClip.GetContent(www));
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }

    // Animation Helpers
    private IEnumerator WiggleAnimation(Transform target, Vector3 origScale, Quaternion origRot, float duration)
    {
        float elapsed = 0f;
        float wiggleSpeed = 24f;
        float wiggleAngle = 10f;

        while (target != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * wiggleSpeed) * wiggleAngle;
            target.localRotation = origRot * Quaternion.Euler(0f, 0f, angle);

            float scaleProgress = Mathf.Min(elapsed / 0.15f, 1f);
            float baseScaleMult = Mathf.Lerp(1.0f, wordScaleHighlight, scaleProgress);
            float scalePulseX = 1f + Mathf.Sin(elapsed * wiggleSpeed) * 0.06f;
            float scalePulseY = 1f - Mathf.Sin(elapsed * wiggleSpeed) * 0.06f;

            target.localScale = new Vector3(
                origScale.x * baseScaleMult * scalePulseX,
                origScale.y * baseScaleMult * scalePulseY,
                origScale.z
            );

            yield return null;
        }

        if (target != null)
        {
            float t = 0f;
            Vector3 currentScale = target.localScale;
            Quaternion currentRotation = target.localRotation;
            while (target != null && t < 1f)
            {
                t += Time.deltaTime * animationSpeed;
                target.localScale = Vector3.Lerp(currentScale, origScale, t);
                target.localRotation = Quaternion.Lerp(currentRotation, origRot, t);
                yield return null;
            }
            if (target != null)
            {
                target.localScale = origScale;
                target.localRotation = origRot;
            }
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

    private IEnumerator PopUI(RectTransform target)
    {
        if (target == null) yield break;

        float t = 0f;
        while (target != null && t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            float scale = Mathf.Lerp(0f, 1.15f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        t = 0f;
        while (target != null && t < 1f)
        {
            t += Time.deltaTime * animationSpeed * 2f;
            float scale = Mathf.Lerp(1.15f, 1f, Mathf.Clamp01(t));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        if (target != null) target.localScale = Vector3.one;
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        foreach (Transform child in progressDotsContainer)
        {
            if (progressDotPrefab == null || child.gameObject != progressDotPrefab)
            {
                Destroy(child.gameObject);
            }
        }
        _dotInstances.Clear();

        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        int count = comparisonsList != null ? comparisonsList.Count : 0;
        for (int i = 0; i < count; i++)
        {
            GameObject dotObj;
            if (progressDotPrefab != null)
            {
                dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
            }
            else
            {
                dotObj = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(progressDotsContainer, false);
                RectTransform rt = dotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(24f, 24f);

                Image img = dotObj.GetComponent<Image>();
                if (dotEmptySprite != null) img.sprite = dotEmptySprite;
            }
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
                bool isCompletedOrActive = i <= _currentIndex;
                if (isCompletedOrActive)
                {
                    if (dotFilledSprite != null) img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;

                    if (i == _currentIndex)
                    {
                        _dotInstances[i].transform.localScale = Vector3.one * 1.25f;
                    }
                    else
                    {
                        _dotInstances[i].transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    if (dotEmptySprite != null) img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                    _dotInstances[i].transform.localScale = Vector3.one;
                }
            }
        }
    }
}
