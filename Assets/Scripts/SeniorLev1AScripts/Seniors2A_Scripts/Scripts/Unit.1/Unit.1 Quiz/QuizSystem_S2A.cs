using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizSystem_S2A : MonoBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public string[] options;
        public int correctIndex;

        public AudioClip questionAudio;
    }

    [Header("Questions")]
    public QuestionData[] questions;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text questionText;
    public Button[] optionButtons;
    public TMP_Text[] optionTexts;
    public Button confirmButton; 
    public Button nextButton;

    [Header("Containers")]
    public RectTransform questionPanel;
    public RectTransform optionsParent;

    [Header("Colors")]
    public Color normalColor = Color.black;
    public Color selectedColor = Color.yellow; 
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip finishClip;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float staggerDelay = 0.1f;
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private int currentQuestion = 0;
    private bool canPlay = false;
    private bool isProcessing = false;
    private int selectedOptionIndex = -1;

    void OnEnable()
    {
        ResetUIState();
        ResetQuiz();
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

        if (questionText != null)
        {
            CanvasGroup cg = questionText.GetComponent<CanvasGroup>();
            if (cg == null) cg = questionText.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i] != null)
            {
                CanvasGroup cg = optionTexts[i].GetComponent<CanvasGroup>();
                if (cg == null) cg = optionTexts[i].gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        questionPanel.localScale = Vector3.zero;
        if (confirmButton != null) confirmButton.transform.localScale = Vector3.zero;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].transform.localScale = Vector3.zero;
        }
    }

    void ResetQuiz()
    {
        currentQuestion = 0;
        canPlay = false;
        isProcessing = false;
        selectedOptionIndex = -1;

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        optionsParent.localScale = Vector3.one;

        LoadQuestion();
    }

    void LoadQuestion()
    {
        var q = questions[currentQuestion];
        questionText.text = q.question;

        // PLAY QUESTION AUDIO
        if (audioSource &&
            q.questionAudio != null)
        {
            audioSource.Stop();

            audioSource.clip =
                q.questionAudio;

            audioSource.Play();
        
        }
        selectedOptionIndex = -1;
        
        if (confirmButton != null) confirmButton.interactable = false;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = true;
            optionButtons[i].transform.localScale = Vector3.zero;
            
            optionTexts[i].text = q.options[i];
            optionTexts[i].color = normalColor;

            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
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

        yield return StartCoroutine(BounceIn(questionPanel));
        if (questionText != null) StartCoroutine(PopTextPerChar(questionText));

        yield return new WaitForSeconds(0.2f);
        
        yield return StartCoroutine(AnimateOptions());

        if (confirmButton != null)
            StartCoroutine(BounceIn(confirmButton.transform));

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        // PLAY FIRST QUESTION AUDIO
        var q = questions[currentQuestion];

        if (audioSource &&
            q.questionAudio != null)
        {
            audioSource.clip =
                q.questionAudio;

            audioSource.Play();
        }

        canPlay = true;
    }

    void OnOptionSelected(int index)
    {
        if (!canPlay || isProcessing) return;

        selectedOptionIndex = index;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i == index)
            {
                optionTexts[i].color = selectedColor;
                optionButtons[i].transform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                optionTexts[i].color = normalColor;
                optionButtons[i].transform.localScale = Vector3.one;
            }
        }

        if (confirmButton != null) confirmButton.interactable = true;
    }

    void OnConfirmClicked()
    {
        if (!canPlay || isProcessing || selectedOptionIndex == -1) return;

        StartCoroutine(HandleAnswer(selectedOptionIndex));
    }

    IEnumerator HandleAnswer(int index)
    {
        isProcessing = true;
        if (confirmButton != null) confirmButton.interactable = false;

        var q = questions[currentQuestion];

        if (index == q.correctIndex)
        {
            optionTexts[index].color = correctColor;

            if (audioSource && correctSFX)
                audioSource.PlayOneShot(correctSFX);

            yield return StartCoroutine(Pulse(optionButtons[index].transform, 1.25f, 0.1f));

            currentQuestion++;

            if (currentQuestion >= questions.Length)
            {
                // PLAY FINISH AUDIO
                if (audioSource && finishClip)
                {
                    audioSource.PlayOneShot(
                    finishClip);

                    yield return new WaitForSeconds(
                    finishClip.length);
                }

                // SHOW NEXT BUTTON
                if (nextButton != null)
                {
                    nextButton.gameObject.SetActive(true);
                }

                // HIDE CONFIRM BUTTON
                if (confirmButton != null)
                {
                    yield return StartCoroutine(
                    PopOut(confirmButton.transform));

                    confirmButton.gameObject.SetActive(false);
                }
            }
            else
            {
                yield return StartCoroutine(PopOut(questionPanel));
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    StartCoroutine(PopOut(optionButtons[i].transform));
                }
                yield return new WaitForSeconds(0.3f);

                LoadQuestion();

                yield return StartCoroutine(BounceIn(questionPanel));
                if (questionText != null) StartCoroutine(PopTextPerChar(questionText));
                
                yield return new WaitForSeconds(0.2f);
                yield return StartCoroutine(AnimateOptions());
            }
        }
        else
        {
            optionTexts[index].color = wrongColor;

            if (audioSource && wrongSFX)
                audioSource.PlayOneShot(wrongSFX);

            yield return StartCoroutine(Shake(optionButtons[index].transform));

            optionTexts[index].color = normalColor;
            optionButtons[index].transform.localScale = Vector3.one; 
            selectedOptionIndex = -1; 
        }

        isProcessing = false;
        if (selectedOptionIndex == -1 && confirmButton != null) confirmButton.interactable = false;
        else if (selectedOptionIndex != -1 && confirmButton != null) confirmButton.interactable = true;
    }

    // ---------------------------
    // ANIMATIONS
    // ---------------------------

    IEnumerator AnimateOptions()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionTexts[i] != null) StartCoroutine(PopTextPerChar(optionTexts[i]));
            yield return StartCoroutine(BounceIn(optionButtons[i].transform));
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

    IEnumerator PopOut(Transform target)
    {
        float t = 0;
        Vector3 startScale = target.localScale;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        target.localScale = Vector3.zero;
    }

    IEnumerator Pulse(Transform target, float scale, float time)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            target.localScale = Vector3.Lerp(original, Vector3.one * scale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            target.localScale = Vector3.Lerp(Vector3.one * scale, original, t);
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