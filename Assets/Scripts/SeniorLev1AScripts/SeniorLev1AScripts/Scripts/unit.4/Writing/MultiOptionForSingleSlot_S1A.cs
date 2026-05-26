using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiOptionForSingleSlot_S1A : MonoBehaviour
{
    [System.Serializable]
    public class BlankSlot
    {
        public Button button;
        public TMP_Text text;
        public string[] correctAnswers;
        public string[] slotOptions;

        [HideInInspector] public bool filled;
    }

    [System.Serializable]
    public class OptionItem
    {
        public Button button;
        public TMP_Text text;

        [HideInInspector] public bool used;
    }

    [Header("UI")]
    public BlankSlot[] slots;
    public OptionItem[] options;
    public GameObject nextButton;
    public Button resetButton;

    [Header("Containers")]
    public RectTransform title;
    public TMP_Text titleText;
    public RectTransform board;
    public RectTransform optionsContainer;

    [Header("Result Panel")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button retryButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip popClip;
    public AudioClip placeClip;
    public AudioClip finishClip;

    [Header("Colors")]
    public Color normalColor = Color.black;
    public Color correctColor = new Color(0.18f, 0.55f, 0.34f);
    public Color wrongColor = Color.red;
    public Color selectedSlotColor = new Color(1f, 0.7f, 0.2f);
    public Color usedOptionColor = Color.gray;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float slideSpeed = 3f;
    public float staggerDelay = 0.08f;
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private BlankSlot currentSlot;
    private bool canInteract = false;

    private Vector2 boardOrigPos;
    private Vector2 optionsOrigPos;

    void Awake()
    {
        if (titleText == null && title != null)
            titleText = title.GetComponentInChildren<TMP_Text>();

        boardOrigPos = board.anchoredPosition;
        optionsOrigPos = optionsContainer.anchoredPosition;
    }

    void OnEnable()
    {
        ResetUI();
        Setup();
        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource) audioSource.Stop();
    }

    void ResetUI()
    {
        currentSlot = null;
        canInteract = false;

        nextButton.SetActive(false);

        foreach (var s in slots)
        {
            s.filled = false;
            s.text.text = "[Tap To Select]";
            s.text.color = normalColor;
            s.button.interactable = true;
        }

        foreach (var o in options)
        {
            o.used = false;
            o.button.interactable = true;
            o.text.color = normalColor;
            o.button.gameObject.SetActive(false);

            if (o.text != null)
            {
                CanvasGroup cg = o.text.GetComponent<CanvasGroup>();
                if (cg == null) cg = o.text.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        if (titleText != null)
        {
            CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
            if (titleCG == null) titleCG = titleText.gameObject.AddComponent<CanvasGroup>();
            titleCG.alpha = 0f;
        }

        TMP_Text[] boardTexts = board.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in boardTexts)
        {
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        board.localScale = Vector3.one;
        optionsContainer.localScale = Vector3.one;

        board.anchoredPosition = boardOrigPos + new Vector2(-1500f, 0);
        optionsContainer.anchoredPosition = optionsOrigPos + new Vector2(1500f, 0);

        foreach (Transform t in optionsContainer)
            t.localScale = Vector3.zero;

        resultPanel.SetActive(false);
        board.gameObject.SetActive(true);
        optionsContainer.gameObject.SetActive(true);
    }

    void Setup()
    {
        foreach (var s in slots)
        {
            BlankSlot captured = s;
            s.button.onClick.RemoveAllListeners();
            s.button.onClick.AddListener(() => OnSlotSelected(captured));
        }

        foreach (var o in options)
        {
            OptionItem captured = o;
            o.button.onClick.RemoveAllListeners();
            o.button.onClick.AddListener(() => OnOptionSelected(captured));
        }

        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(OnReset);

        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(OnRetryFromPopup);
    }

    IEnumerator IntroFlow()
    {
        if (introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(TitleAnim());
        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(SlideIn(board, boardOrigPos, new Vector2(-1500f, 0)));

        TMP_Text[] boardTexts = board.GetComponentsInChildren<TMP_Text>();
        foreach (var t in boardTexts)
        {
            StartCoroutine(PopTextPerChar(t));
        }

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(SlideIn(optionsContainer, optionsOrigPos, new Vector2(1500f, 0)));

        yield return StartCoroutine(AnimateOptions());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canInteract = true;
    }

    void OnSlotSelected(BlankSlot slot)
    {
        if (!canInteract) return;

        currentSlot = slot;

        foreach (var s in slots)
        {
            if (!s.filled)
            {
                s.text.color = normalColor;

                if (s != slot)
                    s.text.text = "[Tap To Select]";
            }
        }

        slot.text.color = selectedSlotColor;

        if (!slot.filled)
            slot.text.text = "[Select an option]";

        UpdateOptionsForSlot(slot);

        foreach (var o in options)
        {
            if (o.button.gameObject.activeSelf)
            {
                StartCoroutine(BounceIn(o.button.transform));
            }
        }
    }

    void UpdateOptionsForSlot(BlankSlot slot)
    {
        if (slot.slotOptions == null) return;

        for (int i = 0; i < options.Length; i++)
        {
            if (i < slot.slotOptions.Length)
            {
                options[i].button.gameObject.SetActive(true);
                options[i].text.text = slot.slotOptions[i];

                if (slot.filled && slot.text.text == slot.slotOptions[i])
                {
                    options[i].used = true;
                    options[i].button.interactable = false;
                    options[i].text.color = usedOptionColor;
                }
                else
                {
                    options[i].used = false;
                    options[i].button.interactable = !slot.filled;
                    options[i].text.color = normalColor;
                }
            }
            else
            {
                options[i].button.gameObject.SetActive(false);
            }
        }
    }

    void OnOptionSelected(OptionItem option)
    {
        if (!canInteract || currentSlot == null) return;
        if (currentSlot.filled) return;
        if (option.used) return;

        currentSlot.text.text = option.text.text;
        currentSlot.filled = true;

        bool correct = false;
        if (currentSlot.correctAnswers != null)
        {
            foreach (string ans in currentSlot.correctAnswers)
            {
                if (option.text.text == ans)
                {
                    correct = true;
                    break;
                }
            }
        }
        currentSlot.text.color = correct ? correctColor : wrongColor;

        option.used = true;
        option.button.interactable = false;
        option.text.color = usedOptionColor;

        currentSlot = null;

        if (placeClip && audioSource)
        {
            audioSource.PlayOneShot(placeClip);
        }

        bool allFilled = true;
        foreach (var s in slots)
        {
            if (!s.filled)
            {
                allFilled = false;
                break;
            }
        }

        if (allFilled)
        {
            CheckCompletion();
        }
        else
        {
            foreach (var o in options)
            {
                o.button.gameObject.SetActive(false);
            }
        }
    }

    void CheckCompletion()
    {
        foreach (var s in slots)
        {
            if (!s.filled)
                return;
        }

        StartCoroutine(ShowResultWithDelay());
    }

    IEnumerator ShowResultWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        ShowResult();
    }

    void ShowResult()
    {
        if (finishClip && audioSource)
        {
            audioSource.PlayOneShot(finishClip);
        }

        int correctCount = 0;

        foreach (var s in slots)
        {
            bool isCorrect = false;
            if (s.correctAnswers != null)
            {
                foreach (string ans in s.correctAnswers)
                {
                    if (s.text.text == ans)
                    {
                        isCorrect = true;
                        break;
                    }
                }
            }
            if (isCorrect) correctCount++;
        }

        if (correctCount == slots.Length)
            resultText.text = "Perfect!";
        else
            resultText.text = "You got " + correctCount + " correct";

        board.gameObject.SetActive(false);
        optionsContainer.gameObject.SetActive(false);

        resultPanel.SetActive(true);
        StartCoroutine(BounceIn(resultPanel.transform));

        if (!nextButton.activeSelf)
        {
            nextButton.SetActive(true);
            StartCoroutine(PopButton(nextButton.transform));
        }
    }

    void OnReset()
    {
        ResetGameplay();
    }

    void ResetGameplay()
    {
        currentSlot = null;

        foreach (var s in slots)
        {
            s.filled = false;
            s.text.text = "[Tap To Select]";
            s.text.color = normalColor;
        }

        foreach (var o in options)
        {
            o.used = false;
            o.button.interactable = true;
            o.text.color = normalColor;
            o.button.gameObject.SetActive(false);
        }
    }

    void OnRetryFromPopup()
    {
        resultPanel.SetActive(false);

        board.gameObject.SetActive(true);
        optionsContainer.gameObject.SetActive(true);
        board.anchoredPosition = boardOrigPos;
        optionsContainer.anchoredPosition = optionsOrigPos;

        ResetGameplay();
    }

    IEnumerator TitleAnim()
    {
        if (titleText == null) yield break;

        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
        if (titleCG == null) titleCG = titleText.gameObject.AddComponent<CanvasGroup>();
        titleCG.alpha = 0f;

        yield return new WaitForEndOfFrame();
        yield return null;

        titleText.ForceMeshUpdate();
        yield return null;
        titleText.ForceMeshUpdate();

        string originalText = titleText.text;

        TMP_TextInfo textInfo = titleText.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0) yield break;

        titleText.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        bool revealed = false;
        float elapsed = 0f;

        float expectedTime = (charCount * titlePopStagger) + Mathf.Max(0.5f, 1f / titlePopFrequency);
        float totalDuration = Mathf.Max(titlePopDuration, expectedTime);

        while (elapsed < totalDuration)
        {
            if (titleText.text != originalText) break;

            elapsed += Time.deltaTime;
            textInfo = titleText.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIndex = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;
                Vector3 charMid = (vertices[vertIndex] + vertices[vertIndex + 2]) / 2f;

                float letterDelay = i * titlePopStagger;
                float localTime = elapsed - letterDelay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / titlePopFrequency);
                    float t = Mathf.Clamp01(localTime / letterDur);

                    float overshoot = 1.70158f * (1f + titlePopAmplitude);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIndex].vertices[vertIndex + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertIndex + v] = charMid + offset * scale; // oops, this should be vertIndex
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                titleText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }

            yield return null;

            if (!revealed && titleCG != null)
            {
                titleCG.alpha = 1f;
                revealed = true;
            }
        }

        if (titleText.text == originalText)
        {
            textInfo = titleText.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
                titleText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        titleText.maxVisibleCharacters = 99999;
    }

    IEnumerator SlideIn(RectTransform target, Vector2 endPos, Vector2 slideOffset)
    {
        target.localScale = Vector3.one;
        Vector2 startPos = endPos + slideOffset;
        target.anchoredPosition = startPos;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * slideSpeed;
            float clamped = Mathf.Clamp01(t);

            float overshoot = 1.2f;
            float c1 = overshoot;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(clamped - 1f, 3f) + c1 * Mathf.Pow(clamped - 1f, 2f);

            target.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, ease);
            yield return null;
        }

        target.anchoredPosition = endPos;
    }

    IEnumerator AnimateOptions()
    {
        foreach (var o in options)
        {
            if (o.button.gameObject.activeSelf)
            {
                if (popClip && audioSource)
                {
                    audioSource.PlayOneShot(popClip);
                }
                yield return StartCoroutine(BounceIn(o.button.transform));
                yield return new WaitForSeconds(staggerDelay);
            }
        }
    }

    IEnumerator BounceIn(Transform target)
    {
        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>();
        foreach (var txt in texts)
        {
            CanvasGroup cg = txt.GetComponent<CanvasGroup>();
            if (cg == null) cg = txt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(PopTextPerChar(txt, 1.2f, 0.04f, 0.6f, 4f));
        }

        target.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            float clamped = Mathf.Clamp01(t);

            float overshoot = 1.70158f;
            float c1 = overshoot + 1f;
            float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            target.localScale = Vector3.one * ease;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator PopTextPerChar(TMP_Text tmp, float popDur = 1.2f, float charStagger = 0.04f, float popAmp = 0.6f, float popFreq = 4f)
    {
        if (tmp == null) yield break;

        tmp.ForceMeshUpdate();
        yield return null;
        tmp.ForceMeshUpdate();

        string originalText = tmp.text;

        TMP_TextInfo textInfo = tmp.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0) yield break;

        tmp.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIdx = charInfo.materialReferenceIndex;
            int vertIdx = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
            Vector3 charMid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;

            for (int v = 0; v < 4; v++)
            {
                vertices[vertIdx + v] = charMid;
            }
        }
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
        }

        yield return null;

        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        float expectedTime = (charCount * charStagger) + Mathf.Max(0.5f, 1f / popFreq);
        float totalDuration = Mathf.Max(popDur, expectedTime);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (tmp.text != originalText) break;

            elapsed += Time.deltaTime;
            textInfo = tmp.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
                Vector3 charMid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;

                float delay = i * charStagger;
                float localTime = elapsed - delay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / popFreq);
                    float lt = Mathf.Clamp01(localTime / letterDur);

                    float overshoot = 1.70158f * (1f + popAmp);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(lt - 1f, 3f) + overshoot * Mathf.Pow(lt - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIdx].vertices[vertIdx + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertIdx + v] = charMid + offset * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }

            yield return null;
        }

        if (tmp.text == originalText)
        {
            textInfo = tmp.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        tmp.maxVisibleCharacters = 99999;
    }

    // -------------------------
    // BUTTON POP
    // -------------------------
    IEnumerator PopButton(Transform btn)
    {
        TMP_Text[] texts = btn.GetComponentsInChildren<TMP_Text>();
        foreach (var txt in texts)
        {
            CanvasGroup cg = txt.GetComponent<CanvasGroup>();
            if (cg == null) cg = txt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(PopTextPerChar(txt, 1.2f, 0.04f, 0.6f, 4f));
        }

        if (popClip && audioSource)
        {
            audioSource.PlayOneShot(popClip);
        }

        // Phase 1: Scale from 0 to 1.15 (overshoot)
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float clamped = Mathf.Clamp01(t);
            btn.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.15f, clamped);
            yield return null;
        }

        // Phase 2: Settle from 1.15 back to 1.0
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 10f;
            float clamped = Mathf.Clamp01(t);
            // Smooth ease-out
            float smooth = 1f - Mathf.Pow(1f - clamped, 2f);
            btn.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, smooth);
            yield return null;
        }
        btn.localScale = Vector3.one;
    }
}