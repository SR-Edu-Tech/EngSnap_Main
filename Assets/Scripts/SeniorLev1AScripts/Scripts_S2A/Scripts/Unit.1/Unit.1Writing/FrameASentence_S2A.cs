using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class FrameASentence_S2A : MonoBehaviour
{
    [System.Serializable]
    public class SentenceData
    {
        public string[] words;
        public string[] correctOrder;
    }
    
    [Header("UI References")]
    public Transform titleContainer;
    public TMP_Text titleText;
    public Transform sentenceContainer;
    public Transform wordsContainer;

    public TMP_Text sentenceText;
    public Button[] wordButtons;

    public GameObject nextButton;
    public GameObject checkButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip popClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    [Header("Sentences")]
    public List<SentenceData> sentences;
    private int currentSentenceIndex = 0;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float staggerDelay = 0.08f;

    [Header("Title Pop Settings")]
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private List<int> selectedButtonIndices = new List<int>();
    private bool isChecked = false;
    private bool canPlay = false;

    private string[] words;
    private string[] correctOrder;

    void Awake()
    {
        if (titleText == null && titleContainer != null)
            titleText = titleContainer.GetComponentInChildren<TMP_Text>();

        EventTrigger trigger = sentenceText.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((data) => { OnSentenceClicked((PointerEventData)data); });
        trigger.triggers.Add(entry);

        if (checkButton != null)
        {
            Button btn = checkButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnCheckClicked);
            }
        }
    }

    void OnEnable()
    {
        ResetUIState();
        ResetGame();
        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void ResetUIState()
    {
        if (titleText != null)
        {
            CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
            if (titleCG == null) titleCG = titleText.gameObject.AddComponent<CanvasGroup>();
            titleCG.alpha = 0f;
        }

        if (sentenceText != null)
        {
            CanvasGroup sentenceCG = sentenceText.GetComponent<CanvasGroup>();
            if (sentenceCG == null) sentenceCG = sentenceText.gameObject.AddComponent<CanvasGroup>();
            sentenceCG.alpha = 0f;
        }

        if (sentenceContainer != null)
            sentenceContainer.localScale = Vector3.zero;

        if (wordsContainer != null)
        {
            wordsContainer.localScale = Vector3.one;
            foreach (Transform child in wordsContainer)
            {
                child.localScale = Vector3.zero;
            }
        }
    }

    void ResetGame()
    {
        selectedButtonIndices.Clear();
        isChecked = false;
        canPlay = false;

        if (checkButton != null) checkButton.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);
        wordsContainer.gameObject.SetActive(true);

        LoadCurrentSentence();
    }

    void LoadCurrentSentence()
    {
        if (currentSentenceIndex >= sentences.Count)
            return;

        var data = sentences[currentSentenceIndex];

        words = data.words;
        correctOrder = data.correctOrder;

        SetupButtons();
        UpdateSentence();
    }

    IEnumerator IntroFlow()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(TitleAnim());
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(PopIn(sentenceContainer));
        if (sentenceText != null) StartCoroutine(PopTextPerChar(sentenceText));
        yield return StartCoroutine(AnimateWords());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canPlay = true;
    }

    void SetupButtons()
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            if (i < words.Length)
            {
                wordButtons[i].gameObject.SetActive(true);
                int index = i;

                wordButtons[i].GetComponentInChildren<TMP_Text>().text = words[i];

                wordButtons[i].onClick.RemoveAllListeners();
                wordButtons[i].onClick.AddListener(() =>
                {
                    if (!canPlay) return;
                    OnWordClicked(index);
                });

                wordButtons[i].interactable = true;
            }
            else
            {
                wordButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnWordClicked(int buttonIndex)
    {
        if (!canPlay || isChecked) return;

        selectedButtonIndices.Add(buttonIndex);
        wordButtons[buttonIndex].interactable = false;

        UpdateSentence();

        if (selectedButtonIndices.Count >= correctOrder.Length)
        {
            wordsContainer.gameObject.SetActive(false);
            if (checkButton != null)
            {
                if (!checkButton.activeSelf)
                {
                    checkButton.SetActive(true);
                    if (popClip && audioSource) audioSource.PlayOneShot(popClip);
                    StartCoroutine(BounceIn(checkButton.transform));
                }
            }
        }
    }

    void OnCheckClicked()
    {
        if (selectedButtonIndices.Count < correctOrder.Length) return;

        isChecked = true;
        UpdateSentence();

        bool allCorrect = true;
        for (int i = 0; i < selectedButtonIndices.Count; i++)
        {
            if (words[selectedButtonIndices[i]] != correctOrder[i])
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            if (correctClip && audioSource)
            {
                audioSource.PlayOneShot(correctClip);
            }

            if (checkButton != null) checkButton.SetActive(false);
            canPlay = false;
            StartCoroutine(LoadNextAfterDelay());
        }
        else
        {
            if (wrongClip && audioSource)
            {
                audioSource.PlayOneShot(wrongClip);
            }
            StartCoroutine(ShakeSentence());
        }
    }

    void UpdateSentence()
    {
        string display = "";

        for (int i = 0; i < selectedButtonIndices.Count; i++)
        {
            int btnIndex = selectedButtonIndices[i];
            string word = words[btnIndex];

            if (!isChecked)
            {
                display += $"<link=\"{i}\"><color=#000000>{word}</color></link> ";
            }
            else
            {
                bool isCorrect = word == correctOrder[i];
                string color = isCorrect ? "#2E7D32" : "#FF0000";

                display += $"<link=\"{i}\"><color={color}>{word}</color></link> ";
            }
        }

        int remaining = correctOrder.Length - selectedButtonIndices.Count;
        for (int i = 0; i < remaining; i++)
        {
            display += "<color=#888888>_____</color> ";
        }

        sentenceText.text = display.Trim();
    }

    public void OnSentenceClicked(PointerEventData eventData)
    {
        if (!canPlay) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(sentenceText, eventData.position, eventData.pressEventCamera);
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = sentenceText.textInfo.linkInfo[linkIndex];
            if (int.TryParse(linkInfo.GetLinkID(), out int indexInList))
            {
                DeselectWordAt(indexInList);
            }
        }
    }

    void DeselectWordAt(int indexInList)
    {
        if (indexInList < 0 || indexInList >= selectedButtonIndices.Count) return;

        isChecked = false;

        int btnIndex = selectedButtonIndices[indexInList];
        selectedButtonIndices.RemoveAt(indexInList);

        wordButtons[btnIndex].interactable = true;

        wordsContainer.gameObject.SetActive(true);
        if (checkButton != null) checkButton.SetActive(false);

        UpdateSentence();
    }

    IEnumerator LoadNextAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        currentSentenceIndex++;

        if (currentSentenceIndex >= sentences.Count)
        {
            Debug.Log("All sentences completed");
            if (nextButton != null)
            {
                if (!nextButton.activeSelf)
                {
                    nextButton.SetActive(true);
                    StartCoroutine(PopButton(nextButton.transform));
                }
            }
            yield break;
        }

        ResetGame();
        StartCoroutine(NextSentenceFlow());
    }

    IEnumerator NextSentenceFlow()
    {
        yield return StartCoroutine(AnimateWords());
        canPlay = true;
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

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;

        // Phase 1: Scale from 0 to 1.15 with ease-out
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            float clamped = Mathf.Clamp01(t);
            float easeOut = 1f - Mathf.Pow(1f - clamped, 3f);
            target.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, easeOut);
            yield return null;
        }

        // Phase 2: Settle from 1.15 back to 1.0
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            float clamped = Mathf.Clamp01(t);
            float smooth = 1f - Mathf.Pow(1f - clamped, 2f);
            target.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, smooth);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    IEnumerator AnimateWords()
    {
        wordsContainer.localScale = Vector3.one;

        foreach (Transform child in wordsContainer)
        {
            child.localScale = Vector3.zero;
        }

        foreach (Transform child in wordsContainer)
        {
            if (popClip && audioSource)
            {
                audioSource.PlayOneShot(popClip);
            }

            yield return StartCoroutine(BounceIn(child));
            yield return new WaitForSeconds(staggerDelay);
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

        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();
        bool revealed = false;

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

            if (!revealed && cg != null)
            {
                cg.alpha = 1f;
                revealed = true;
            }
        }

        if (cg != null) cg.alpha = 1f;

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

    IEnumerator ShakeSentence()
    {
        Vector3 origPos = sentenceContainer.localPosition;
        float elapsed = 0f;
        float duration = 0.4f;
        float magnitude = 15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float xOffset = Mathf.Sin(elapsed * 40f) * magnitude * (1f - (elapsed / duration));
            sentenceContainer.localPosition = origPos + new Vector3(xOffset, 0, 0);
            yield return null;
        }

        sentenceContainer.localPosition = origPos;
    }

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