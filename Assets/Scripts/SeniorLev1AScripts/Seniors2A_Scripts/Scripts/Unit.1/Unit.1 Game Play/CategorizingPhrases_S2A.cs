using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategorizingPhrases_S2A : MonoBehaviour
{
    [System.Serializable]
    public class PhraseData
    {
        public string text;
        public int correctCategory;
    }

    [System.Serializable]
    public class Pot
    {
        public int id;
        public RectTransform dropArea;

        public Image bg;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
    }

    [System.Serializable]
    public class DraggableItem
    {
        public RectTransform rect;
        public TMP_Text text;
        public DraggableItems_S2A dragHandler;
        public Image bg;

        [HideInInspector] public PhraseData data;
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public bool active;
    }

    [Header("Data")]
    public PhraseData[] allPhrases;

    [Header("UI")]
    public TMP_Text titleText;
    public DraggableItem[] draggableItems;
    public Pot[] pots;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip popClip;
    public AudioClip finishClip;

    [Header("Colors")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color itemHighlightColor = Color.yellow;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float returnSpeed = 6f;
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private Queue<PhraseData> phraseQueue;
    private bool canPlay = false;
    private Canvas canvas;

    public bool CanPlay()
    {
        return canPlay;
    }

    void OnEnable()
    {
        ResetUIState();
        SetupGame();
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

        foreach (var pot in pots)
        {
            pot.dropArea.localScale = Vector3.zero;
        }

        foreach (var item in draggableItems)
        {
            item.rect.localScale = Vector3.zero;
            if (item.text != null)
            {
                CanvasGroup cg = item.text.GetComponent<CanvasGroup>();
                if (cg == null) cg = item.text.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }
    }

    void SetupGame()
    {
        canvas = GetComponentInParent<Canvas>();

        nextButton.gameObject.SetActive(false);
        canPlay = false;

        phraseQueue = new Queue<PhraseData>(allPhrases);

        foreach (var item in draggableItems)
        {
            if (item.startPos == Vector3.zero) item.startPos = item.rect.localPosition;
            
            item.dragHandler.Setup(this, item);
            LoadIntoItem(item, false);

            item.rect.localScale = Vector3.zero;
        }

        foreach (var pot in pots)
        {
            pot.dropArea.localScale = Vector3.zero;
        }

        ResetAllPotHighlights(null);
    }

    void LoadIntoItem(DraggableItem item, bool animate)
    {
        if (phraseQueue.Count > 0)
        {
            item.data = phraseQueue.Dequeue();
            item.text.text = item.data.text;
            item.rect.localPosition = item.startPos;
            item.rect.gameObject.SetActive(true);
            item.active = true;

            if (animate)
            {
                if (popClip && audioSource) audioSource.PlayOneShot(popClip);
                StartCoroutine(PopTextPerChar(item.text));
                StartCoroutine(BounceIn(item.rect));
            }
        }
        else
        {
            item.rect.gameObject.SetActive(false);
            item.active = false;
            CheckCompletion();
        }
    }

    void CheckCompletion()
    {
        foreach (var item in draggableItems)
        {
            if (item.active) return;
        }

        if (!nextButton.gameObject.activeSelf)
        {
            if (finishClip && audioSource) audioSource.PlayOneShot(finishClip);
            nextButton.gameObject.SetActive(true);
            StartCoroutine(PopButton(nextButton.transform));
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
        yield return new WaitForSeconds(0.2f);

        foreach (var pot in pots)
        {
            if (popClip && audioSource) audioSource.PlayOneShot(popClip);
            yield return StartCoroutine(BounceIn(pot.dropArea));
        }

        foreach (var item in draggableItems)
        {
            if (item.active)
            {
                if (popClip && audioSource) audioSource.PlayOneShot(popClip);
                if (item.text != null) StartCoroutine(PopTextPerChar(item.text));
                yield return StartCoroutine(BounceIn(item.rect));
            }
        }

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canPlay = true;
    }

    public void HandleDragHover(DraggableItem item, Vector2 screenPos)
    {
        Camera cam = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        foreach (var pot in pots)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(pot.dropArea, screenPos, cam))
            {
                HighlightPot(pot);
                return;
            }
        }

        ResetAllPotHighlights(item);
    }

    void HighlightPot(Pot target)
    {
        foreach (var pot in pots)
        {
            if (pot.bg != null)
            {
                pot.bg.color = (pot == target) ? pot.highlightColor : pot.normalColor;

                if (pot.dropArea.localScale != Vector3.zero)
                    pot.dropArea.localScale = (pot == target) ? Vector3.one * 1.1f : Vector3.one;
            }
        }
    }

    public void ResetAllPotHighlights(DraggableItem item)
    {
        if (item != null && item.bg != null) item.bg.color = Color.white;

        foreach (var pot in pots)
        {
            if (pot.bg != null)
            {
                pot.bg.color = pot.normalColor;

                if (pot.dropArea.localScale != Vector3.zero)
                    pot.dropArea.localScale = Vector3.one;
            }
        }
    }

    public void HandleDrop(DraggableItem item, Vector2 screenPos)
    {
        if (!canPlay || !item.active) return;

        Camera cam = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        foreach (var pot in pots)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(pot.dropArea, screenPos, cam))
            {
                if (pot.id == item.data.correctCategory)
                {
                    StartCoroutine(HandleCorrect(item));
                    return;
                }
                else
                {
                    StartCoroutine(HandleWrong(item));
                    return;
                }
            }
        }

        StartCoroutine(ReturnToStart(item));
    }

    IEnumerator HandleCorrect(DraggableItem item)
    {
        if (audioSource && correctSFX)
            audioSource.PlayOneShot(correctSFX);

        StartCoroutine(FlashItemColor(item, correctColor));
        yield return StartCoroutine(Pulse(item.rect, 1.2f));
        yield return new WaitForSeconds(0.2f);

        item.rect.localScale = Vector3.zero;
        LoadIntoItem(item, true);
    }

    IEnumerator HandleWrong(DraggableItem item)
    {
        if (audioSource && wrongSFX)
            audioSource.PlayOneShot(wrongSFX);

        StartCoroutine(FlashItemColor(item, wrongColor));
        yield return StartCoroutine(Shake(item.rect));
        yield return StartCoroutine(ReturnToStart(item));
    }

    IEnumerator ReturnToStart(DraggableItem item)
    {
        Vector3 start = item.rect.localPosition;
        Vector3 end = item.startPos;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * returnSpeed;
            item.rect.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }

    // -------------------------
    // ANIMATIONS
    // -------------------------

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

    IEnumerator FlashItemColor(DraggableItem item, Color flashColor)
    {
        if (item.bg == null) yield break;

        Color original = Color.white; // Or get from a 'normalColor' field if added
        item.bg.color = flashColor;
        yield return new WaitForSeconds(0.5f);
        item.bg.color = original;
    }

    IEnumerator PopButton(Transform btn)
    {
        if (popClip && audioSource) audioSource.PlayOneShot(popClip);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float clamped = Mathf.Clamp01(t);
            btn.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.15f, clamped);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 10f;
            float clamped = Mathf.Clamp01(t);
            float smooth = 1f - Mathf.Pow(1f - clamped, 2f);
            btn.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, smooth);
            yield return null;
        }
        btn.localScale = Vector3.one;
    }
}