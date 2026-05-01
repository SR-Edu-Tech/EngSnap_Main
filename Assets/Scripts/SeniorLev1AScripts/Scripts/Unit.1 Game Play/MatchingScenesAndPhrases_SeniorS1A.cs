using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchingScenesAndPhrases_SeniorS1A : MonoBehaviour
{
    [System.Serializable]
    public class Option
    {
        public int id;
        public string text;
        public Button button;
        public Image bg;
    }

    [System.Serializable]
    public class Slot
    {
        public int correctID;
        public Button button;
        public TMP_Text textField;
        public Image bg;
    }

    [Header("Data")]
    public Option[] options;
    public Slot[] slots;

    [Header("UI")]
    public TMP_Text titleText;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float staggerDelay = 0.1f;
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color usedColor = Color.gray;

    private Option selectedOption;
    private int correctCount = 0;
    private bool canPlay = false;

    void OnEnable()
    {
        ResetUIState();
        ResetGame();
        StartCoroutine(IntroSequence());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource) audioSource.Stop();
    }

    void ResetUIState()
    {
        if (titleText != null)
        {
            CanvasGroup cg = titleText.GetComponent<CanvasGroup>();
            if (cg == null) cg = titleText.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        foreach (var opt in options)
        {
            opt.button.transform.localScale = Vector3.zero;
            TMP_Text txt = opt.button.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                CanvasGroup cg = txt.GetComponent<CanvasGroup>();
                if (cg == null) cg = txt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        foreach (var slot in slots)
        {
            slot.button.transform.localScale = Vector3.zero;
            if (slot.textField != null)
            {
                CanvasGroup cg = slot.textField.GetComponent<CanvasGroup>();
                if (cg == null) cg = slot.textField.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }
    }

    void ResetGame()
    {
        correctCount = 0;
        selectedOption = null;
        canPlay = false;

        nextButton.gameObject.SetActive(false);

        foreach (var opt in options)
        {
            opt.bg.color = normalColor;
            opt.button.interactable = true;

            opt.button.onClick.RemoveAllListeners();
            opt.button.onClick.AddListener(() => SelectOption(opt));
        }

        foreach (var slot in slots)
        {
            slot.textField.text = "Describe";
            slot.bg.color = normalColor;
            slot.button.interactable = true;

            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => OnSlotClicked(slot));
        }
    }

    IEnumerator IntroSequence()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(TitleAnim());
        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(AnimateOptions());
        yield return StartCoroutine(AnimateSlots());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canPlay = true;
    }

    void SelectOption(Option opt)
    {
        if (!canPlay) return;
        if (!opt.button.interactable) return;

        selectedOption = opt;

        foreach (var o in options)
            o.bg.color = (o == opt) ? selectedColor : normalColor;
    }

    void OnSlotClicked(Slot slot)
    {
        if (!canPlay) return;
        if (selectedOption == null) return;
        if (!slot.button.interactable) return;

        slot.textField.text = selectedOption.text;

        if (selectedOption.id == slot.correctID)
        {
            slot.bg.color = correctColor;
            slot.button.interactable = false;

            selectedOption.bg.color = usedColor;
            selectedOption.button.interactable = false;

            if (audioSource && correctSFX)
                audioSource.PlayOneShot(correctSFX);

            StartCoroutine(Pulse(slot.button.transform, 1.15f));

            selectedOption = null;
            correctCount++;

            if (correctCount == slots.Length)
            {
                nextButton.gameObject.SetActive(true);
            }
        }
        else
        {
            slot.bg.color = wrongColor;

            if (audioSource && wrongSFX)
                audioSource.PlayOneShot(wrongSFX);

            StartCoroutine(Shake(slot.button.transform));
            StartCoroutine(ClearWrong(slot));
        }
    }

    IEnumerator ClearWrong(Slot slot)
    {
        yield return new WaitForSeconds(0.5f);

        slot.textField.text = "Describe";
        slot.bg.color = normalColor;
    }

    // -------------------------
    // ANIMATIONS
    // -------------------------

    IEnumerator AnimateOptions()
    {
        for (int i = 0; i < options.Length; i++)
        {
            TMP_Text txt = options[i].button.GetComponentInChildren<TMP_Text>();
            if (txt != null) StartCoroutine(PopTextPerChar(txt));

            yield return StartCoroutine(BounceIn(options[i].button.transform));
            yield return new WaitForSeconds(staggerDelay);
        }
    }

    IEnumerator AnimateSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].textField != null) StartCoroutine(PopTextPerChar(slots[i].textField));

            yield return StartCoroutine(BounceIn(slots[i].button.transform));
            yield return new WaitForSeconds(staggerDelay);
        }
    }

    IEnumerator BounceIn(Transform target)
    {
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

    IEnumerator Pulse(Transform target, float maxScale)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(original, Vector3.one * maxScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.one * maxScale, original, t);
            yield return null;
        }
    }

    IEnumerator Shake(Transform target)
    {
        Vector3 original = target.localPosition;

        for (int i = 0; i < 10; i++)
        {
            target.localPosition = original + new Vector3(Random.Range(-10f, 10f), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }

        target.localPosition = original;
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
                    vertices[vertIndex + v] = charMid + offset * scale;
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
                vertices[vertIdx + v] = charMid;
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
}