using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FillInTheBlanksWithQuestionAndAnswer_S1A : MonoBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        [TextArea]
        public string sentence;

        public string[] options;
        public int correctIndex;
    }

    [Header("Questions")]
    public QuestionData[] questions;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text questionText;

    public Button[] optionButtons;
    public TMP_Text[] optionTexts;

    public RectTransform questionBox;
    public RectTransform[] optionRects;

    public GameObject nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;
    public AudioClip popSfx;

    [Header("Colors")]
    public Color normalColor = Color.black;
    public Color correctColor = new Color(0.2f, 0.7f, 0.3f);
    public Color wrongColor = new Color(0.9f, 0.2f, 0.2f);
    public Color selectedColor = new Color(1f, 0.8f, 0.25f);

    [Header("Animation")]
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titleFrequency = 4f;
    public float titleStagger = 0.05f;

    public float typewriterSpeed = 0.03f;

    public float questionBoxSlideSpeed = 1.5f;
    public float optionPopSpeed = 2.5f;
    public float optionPopScale = 1.1f;
    public float optionStaggerDelay = 0.15f;

    private int currentQuestionIndex;
    private bool canInteract;

    void OnEnable()
    {
        canInteract = false;
        if (nextButton) nextButton.SetActive(false);
        
        var tCg = titleText.GetComponent<CanvasGroup>();
        if (tCg == null) tCg = titleText.gameObject.AddComponent<CanvasGroup>();
        tCg.alpha = 0f;
        
        questionText.text = "";
        questionBox.anchoredPosition = new Vector2(-2500, 0);

        for (int i = 0; i < optionRects.Length; i++)
        {
            optionRects[i].localScale = Vector3.zero;
            optionTexts[i].color = normalColor;

            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => { if (canInteract) StartCoroutine(CheckAnswer(index)); });
        }

        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource) audioSource.Stop();
    }

    IEnumerator IntroFlow()
    {
        if (introClip) { audioSource.clip = introClip; audioSource.Play(); }
        
        // 1. Title animation first
        yield return StartCoroutine(PopTextPerChar(titleText, titlePopDuration, titleStagger, titlePopAmplitude, titleFrequency));
        
        // 2. Begin remaining animations immediately after title completes
        yield return StartCoroutine(LoadQuestion(0, true));
    }

    IEnumerator LoadQuestion(int questionIndex, bool isIntro = false)
    {
        canInteract = false;
        currentQuestionIndex = questionIndex;
        var q = questions[questionIndex];

        questionText.text = q.sentence;
        var qCg = questionText.GetComponent<CanvasGroup>();
        if (qCg == null) qCg = questionText.gameObject.AddComponent<CanvasGroup>();
        qCg.alpha = 0f;

        for (int i = 0; i < optionRects.Length; i++)
        {
            optionTexts[i].color = normalColor;
            optionRects[i].localScale = Vector3.zero;
            
            if (i < q.options.Length) optionTexts[i].text = q.options[i];
            else optionTexts[i].text = "";
            
            var cg = optionTexts[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = optionTexts[i].gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        // 2. All other animations start (Slide in parallel with options pop)
        if (isIntro)
        {
            StartCoroutine(SlideQuestionBox());
        }
        
        yield return StartCoroutine(ShowOptions());

        // 3. Text animation for all starts
        StartCoroutine(PopTextPerChar(questionText, 1.2f, typewriterSpeed, 0.6f, 4f));
        for (int i = 0; i < optionRects.Length; i++)
        {
            if (i < q.options.Length)
                StartCoroutine(PopTextPerChar(optionTexts[i], 1.2f, typewriterSpeed, 0.6f, 4f));
        }

        yield return new WaitForSeconds(1.5f);
        canInteract = true;
    }

    IEnumerator CheckAnswer(int selectedIndex)
    {
        canInteract = false;
        var q = questions[currentQuestionIndex];
        questionText.text = q.sentence.Replace("_____", $"<color=#000000>{q.options[selectedIndex]}</color>");

        bool correct = selectedIndex == q.correctIndex;
        optionTexts[selectedIndex].color = correct ? correctColor : wrongColor;

        if (correct && correctSfx) audioSource.PlayOneShot(correctSfx);
        else if (!correct && wrongSfx) audioSource.PlayOneShot(wrongSfx);

        if (correct)
        {
            yield return new WaitForSeconds(1f);
            if (++currentQuestionIndex < questions.Length)
                yield return StartCoroutine(LoadQuestion(currentQuestionIndex, false));
            else
            {
                Debug.Log("Quiz Complete");
                if (nextButton) nextButton.SetActive(true);
            }
        }
        else
        {
            yield return StartCoroutine(Shake(optionRects[selectedIndex]));
            optionTexts[selectedIndex].color = normalColor;
            questionText.text = q.sentence;
            canInteract = true;
        }
    }

    IEnumerator PopTextPerChar(TMP_Text tmp, float popDur = 1.2f, float charStagger = 0.04f, float popAmp = 0.6f, float popFreq = 4f)
    {
        if (tmp == null) yield break;

        tmp.maxVisibleCharacters = 99999;
        tmp.ForceMeshUpdate();
        yield return null;
        tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmp.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0) yield break;

        tmp.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();
        if (cg == null) cg = tmp.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        bool revealed = false;

        float expectedTime = (charCount * charStagger) + Mathf.Max(0.5f, 1f / popFreq);
        float totalDuration = Mathf.Max(popDur, expectedTime);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
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

            if (!revealed)
            {
                cg.alpha = 1f;
                revealed = true;
            }
        }

        cg.alpha = 1f;

        textInfo = tmp.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    IEnumerator SlideQuestionBox()
    {
        for (float time = 0; time < 1; time += Time.deltaTime * questionBoxSlideSpeed)
        {
            float clamped = Mathf.Clamp01(time);
            float overshoot = 1.70158f;
            float ease = 1f + (overshoot + 1f) * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            questionBox.anchoredPosition = Vector2.LerpUnclamped(new Vector2(-2000, 150), new Vector2(0, 150), ease);
            yield return null;
        }
        questionBox.anchoredPosition = new Vector2(0, 150);
    }

    IEnumerator ShowOptions()
    {
        for (int i = 0; i < optionRects.Length; i++)
        {
            StartCoroutine(PopOption(optionRects[i]));
            if (popSfx) audioSource.PlayOneShot(popSfx);
            yield return new WaitForSeconds(optionStaggerDelay);
        }
    }

    IEnumerator PopOption(RectTransform t)
    {
        for (float time = 0; time < 1; time += Time.deltaTime * optionPopSpeed)
        {
            float clamped = Mathf.Clamp01(time);
            float overshoot = 1.70158f;
            float ease = 1f + (overshoot + 1f) * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            t.localScale = Vector3.one * Mathf.LerpUnclamped(0f, optionPopScale, ease);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator Shake(RectTransform t)
    {
        Vector2 orig = t.anchoredPosition;
        for (float elapsed = 0; elapsed < 0.3f; elapsed += Time.deltaTime)
        {
            t.anchoredPosition = orig + new Vector2(Random.Range(-12f, 12f), 0);
            yield return null;
        }
        t.anchoredPosition = orig;
    }
}