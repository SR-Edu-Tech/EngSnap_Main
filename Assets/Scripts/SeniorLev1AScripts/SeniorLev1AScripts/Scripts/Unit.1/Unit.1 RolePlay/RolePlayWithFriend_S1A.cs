using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RolePlayWithFriend_S1A : MonoBehaviour
{
    [System.Serializable]
    public class OptionData
    {
        public int id;
        public string text;
    }

    [System.Serializable]
    public class DialogueStep
    {
        public string girlText;
        public AudioClip girlVoice;
        public int correctOptionID;
        public AudioClip correctVoice;
        public OptionData[] options;

        [Header("Step Behaviors")]
        public bool isAutomatic;
        public bool isEndStep;
        public bool isGirlOnly; // Girl speaks and immediately transitions to next step
    }

    [System.Serializable]
    public class OptionUI
    {
        public Button button;
        public TMP_Text label;
        public GameObject root;
    }

    [Header("Dialogue")]
    public DialogueStep[] steps;

    [Header("Option UI")]
    public OptionUI[] optionUIs;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text girlDialogueText;
    public TMP_Text boyDialogueText;
    public Button girlMessageButton;
    public GameObject girlMessageRoot;
    public GameObject boyMessageRoot;
    public Transform girlCharacter;
    public Transform boyCharacter;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip popClip;
    public AudioClip finishClip;

    [Header("Color Selection")]
    public Color correctAnswer;


    [Header("Animation Settings")]
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float optionSpeed = 5f;
    public float pulseSpeed = 8f;

    public float delayAfterTitle = 0.2f;
    public float delayBetweenOptions = 0.1f;
    public float delayAfterCorrect = 0.4f;
    public float delayAfterWrong = 0.4f;

    public float optionSlideDistance = 50f;
    public float titleDropDistance = 300f;

    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private int currentStep = 0;
    private bool canPlay = false;
    private bool isProcessing = false;

    void OnEnable()
    {
        ResetGame();
        StartCoroutine(IntroSequence());
    }

    void ResetGame()
    {
        currentStep = 0;
        canPlay = false;
        isProcessing = false;

        nextButton.gameObject.SetActive(false);
        boyDialogueText.text = "";

        if (titleText != null) SetupCanvasGroup(titleText);
        if (girlDialogueText != null) SetupCanvasGroup(girlDialogueText);
        if (boyDialogueText != null) SetupCanvasGroup(boyDialogueText);

        if (girlMessageRoot != null) girlMessageRoot.transform.localScale = Vector3.zero;
        if (boyMessageRoot != null) boyMessageRoot.transform.localScale = Vector3.zero;

        if (girlCharacter != null) girlCharacter.gameObject.SetActive(false);
        if (boyCharacter != null) boyCharacter.gameObject.SetActive(false);

        foreach (var ui in optionUIs)
        {
            ui.root.transform.localScale = Vector3.zero;
            if (ui.label != null) SetupCanvasGroup(ui.label);
        }

        SetupStep();
    }

    void SetupCanvasGroup(TMP_Text text)
    {
        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null) cg = text.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
    }

    IEnumerator IntroSequence()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        // Slide in characters
        if (girlCharacter != null) StartCoroutine(SlideIn(girlCharacter, new Vector3(-500, 0, 0)));
        if (boyCharacter != null) StartCoroutine(SlideIn(boyCharacter, new Vector3(500, 0, 0)));
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TitleAnim());
        yield return new WaitForSeconds(delayAfterTitle);

        if (popClip && audioSource) audioSource.PlayOneShot(popClip);
        Coroutine popGirl = null;
        Coroutine popBoy = null;

        if (girlMessageRoot != null) popGirl = StartCoroutine(PopIn(girlMessageRoot.transform));
        if (boyMessageRoot != null) popBoy = StartCoroutine(PopIn(boyMessageRoot.transform));

        if (popGirl != null) yield return popGirl;
        if (popBoy != null) yield return popBoy;

        if (girlDialogueText != null) StartCoroutine(PopTextPerChar(girlDialogueText));

        var firstStep = steps[currentStep];
        if (!firstStep.isAutomatic && !firstStep.isEndStep)
        {
            yield return StartCoroutine(AnimateOptions());
        }

        if (introClip)
            yield return new WaitForSeconds(Mathf.Max(0, introClip.length - delayAfterTitle));

        // Auto-play girl voice once when step first appears
        if (firstStep.girlVoice != null && audioSource)
        {
            audioSource.clip = firstStep.girlVoice;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        yield return StartCoroutine(HandleStepBehaviors(firstStep));
    }

    void SetupStep()
    {
        var step = steps[currentStep];
        girlDialogueText.text = step.girlText;

        if (girlMessageButton != null)
        {
            girlMessageButton.onClick.RemoveAllListeners();
            if (step.girlVoice != null)
            {
                girlMessageButton.onClick.AddListener(() =>
                {
                    if (audioSource.clip == step.girlVoice && audioSource.isPlaying)
                    {
                        audioSource.Stop();
                    }
                    else
                    {
                        audioSource.clip = step.girlVoice;
                        audioSource.Play();
                    }
                });
            }
        }

        if (step.isAutomatic || step.isEndStep || step.isGirlOnly)
        {
            for (int i = 0; i < optionUIs.Length; i++)
            {
                optionUIs[i].root.SetActive(false);
            }
            return;
        }

        for (int i = 0; i < optionUIs.Length; i++)
        {
            if (i < step.options.Length)
            {
                var data = step.options[i];
                var ui = optionUIs[i];

                ui.root.SetActive(true);
                ui.label.text = data.text;
                ui.label.color = Color.black;
                ui.button.interactable = true;

                int id = data.id;
                string text = data.text;
                OptionUI capturedUI = ui;

                ui.button.onClick.RemoveAllListeners();
                ui.button.onClick.AddListener(() =>
                    OnOptionClicked(id, text, capturedUI)
                );

                ui.root.transform.localScale = Vector3.zero;
            }
            else
            {
                optionUIs[i].root.SetActive(false);
            }
        }
    }

    void OnOptionClicked(int id, string text, OptionUI ui)
    {
        if (!canPlay || isProcessing) return;
        StartCoroutine(HandleSelection(id, text, ui));
    }

    IEnumerator HandleSelection(int id, string text, OptionUI ui)
    {
        isProcessing = true;

        boyDialogueText.text = text;
        StartCoroutine(PopTextPerChar(boyDialogueText));
        ui.label.color = Color.black;

        yield return new WaitForSeconds(0.4f);

        if (id == steps[currentStep].correctOptionID)
        {
            ui.label.color = correctAnswer;

            if (audioSource && correctSFX)
                audioSource.PlayOneShot(correctSFX);

            yield return StartCoroutine(Pulse(ui.root.transform, 1.2f));

            // Play the correct option's voice clip and wait for it
            AudioClip correctClip = steps[currentStep].correctVoice;
            if (correctClip != null && audioSource)
            {
                audioSource.clip = correctClip;
                audioSource.Play();
                yield return new WaitWhile(() => audioSource.isPlaying);
            }

            yield return new WaitForSeconds(delayAfterCorrect);

            yield return StartCoroutine(TransitionToNextStep());
        }
        else
        {
            ui.label.color = Color.red;

            if (audioSource && wrongSFX)
                audioSource.PlayOneShot(wrongSFX);

            yield return StartCoroutine(Shake(ui.root.transform));

            yield return new WaitForSeconds(delayAfterWrong);

            boyDialogueText.text = "";
            ui.label.color = Color.black;
        }

        isProcessing = false;
    }

    IEnumerator TransitionToNextStep()
    {
        currentStep++;

        if (currentStep >= steps.Length)
        {
            if (finishClip && audioSource) audioSource.PlayOneShot(finishClip);
            nextButton.gameObject.SetActive(true);
            StartCoroutine(PopButton(nextButton.transform));
        }
        else
        {
            boyDialogueText.text = "Select A Dialogue";
            if (girlMessageRoot != null) girlMessageRoot.transform.localScale = Vector3.zero;
            if (boyMessageRoot != null) boyMessageRoot.transform.localScale = Vector3.zero;

            yield return StartCoroutine(FadeOutOptions());
            SetupStep();

            if (popClip && audioSource) audioSource.PlayOneShot(popClip);
            Coroutine popGirl = null;
            Coroutine popBoy = null;

            if (girlMessageRoot != null) popGirl = StartCoroutine(PopIn(girlMessageRoot.transform));
            if (boyMessageRoot != null) popBoy = StartCoroutine(PopIn(boyMessageRoot.transform));

            if (popGirl != null) yield return popGirl;
            if (popBoy != null) yield return popBoy;

            if (girlDialogueText != null) yield return StartCoroutine(PopTextPerChar(girlDialogueText));

            var newStep = steps[currentStep];
            if (!newStep.isAutomatic && !newStep.isEndStep)
            {
                yield return StartCoroutine(AnimateOptions());
            }

            if (newStep.girlVoice != null && audioSource)
            {
                audioSource.clip = newStep.girlVoice;
                audioSource.Play();
                yield return new WaitWhile(() => audioSource.isPlaying);
            }

            yield return StartCoroutine(HandleStepBehaviors(newStep));
        }
    }

    IEnumerator HandleStepBehaviors(DialogueStep step)
    {
        if (step.isEndStep)
        {
            if (finishClip && audioSource) audioSource.PlayOneShot(finishClip);
            nextButton.gameObject.SetActive(true);
            StartCoroutine(PopButton(nextButton.transform));
        }
        else if (step.isGirlOnly)
        {
            yield return new WaitForSeconds(1f); // Brief pause before next step
            yield return StartCoroutine(TransitionToNextStep());
        }
        else if (step.isAutomatic)
        {
            yield return new WaitForSeconds(1f); // Wait a sec

            // Find correct text
            string boyText = "";
            foreach (var opt in step.options)
            {
                if (opt.id == step.correctOptionID)
                {
                    boyText = opt.text;
                    break;
                }
            }

            boyDialogueText.text = boyText;
            yield return StartCoroutine(PopTextPerChar(boyDialogueText));

            if (step.correctVoice != null && audioSource)
            {
                audioSource.clip = step.correctVoice;
                audioSource.Play();
                yield return new WaitWhile(() => audioSource.isPlaying);
            }

            yield return new WaitForSeconds(delayAfterCorrect);

            yield return StartCoroutine(TransitionToNextStep());
        }
        else
        {
            canPlay = true; // Wait for user interaction
        }
    }

    // ANIMATIONS

    IEnumerator SlideIn(Transform target, Vector3 offset)
    {
        target.gameObject.SetActive(true);
        Vector3 end = target.localPosition;
        Vector3 start = end + offset;
        target.localPosition = start;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 2f;
            target.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
    }

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            float scale = Mathf.Lerp(0, 1.1f, t);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    IEnumerator AnimateOptions()
    {
        var step = steps[currentStep];
        for (int i = 0; i < step.options.Length; i++)
        {
            if (popClip && audioSource) audioSource.PlayOneShot(popClip);
            StartCoroutine(PopTextPerChar(optionUIs[i].label));
            yield return StartCoroutine(SlidePop(optionUIs[i].root.transform));
            yield return new WaitForSeconds(delayBetweenOptions);
        }
    }

    IEnumerator SlidePop(Transform target)
    {
        Vector3 start = target.localPosition + Vector3.down * optionSlideDistance;
        Vector3 end = target.localPosition;

        target.localPosition = start;
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * optionSpeed;
            target.localPosition = Vector3.Lerp(start, end, t);
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
    }

    IEnumerator Pulse(Transform target, float scale)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * pulseSpeed;
            target.localScale = Vector3.Lerp(original, Vector3.one * scale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * pulseSpeed;
            target.localScale = Vector3.Lerp(Vector3.one * scale, original, t);
            yield return null;
        }
    }

    IEnumerator Shake(Transform target)
    {
        Vector3 original = target.localPosition;

        for (int i = 0; i < 10; i++)
        {
            target.localPosition = original + new Vector3(Random.Range(-10, 10), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }

        target.localPosition = original;
    }

    IEnumerator PopTextPerChar(TMP_Text tmp, float popDur = 1.2f, float charStagger = 0.03f, float popAmp = 0.5f, float popFreq = 4f)
    {
        if (tmp == null) yield break;
        tmp.maxVisibleCharacters = 99999;
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
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIdx = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIdx = textInfo.characterInfo[i].vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
            Vector3 mid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;
            for (int v = 0; v < 4; v++) vertices[vertIdx + v] = mid;
        }

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
            tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);

        yield return null;
        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        float totalDuration = Mathf.Max(popDur, (charCount * charStagger) + 0.5f);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (tmp.text != originalText) break;
            elapsed += Time.deltaTime;
            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
                Vector3 mid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;

                float delay = i * charStagger;
                float localTime = elapsed - delay;
                float scale = 0f;
                if (localTime > 0f)
                {
                    float lt = Mathf.Clamp01(localTime / 0.25f);
                    float overshoot = 1.70158f * (1f + popAmp);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(lt - 1f, 3f) + overshoot * Mathf.Pow(lt - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIdx].vertices[vertIdx + v];
                    vertices[vertIdx + v] = mid + (orig - mid) * scale;
                }
            }
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
                tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            yield return null;
        }
        tmp.maxVisibleCharacters = 9999;
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

    IEnumerator PopButton(Transform btn)
    {
        if (popClip && audioSource) audioSource.PlayOneShot(popClip);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            btn.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.15f, Mathf.Clamp01(t));
            yield return null;
        }
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 10f;
            float smooth = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
            btn.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, smooth);
            yield return null;
        }
        btn.localScale = Vector3.one;
    }

    IEnumerator FadeOutOptions()
    {
        foreach (var ui in optionUIs)
        {
            ui.root.transform.localScale = Vector3.zero;
        }
        yield return new WaitForSeconds(0.2f);
    }
}