using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RiddleRevealSystem : MonoBehaviour
{
    // =========================================================
    // RIDDLE DATA
    // =========================================================

    [System.Serializable]
    public class RiddleData
    {
        [TextArea(2, 5)]
        public string riddle;

        [Header("RIDDLE AUDIO")]
        public AudioClip riddleAudio;

        [Header("ANSWER AUDIO")]
        public AudioClip answerAudio;
    }


    // =========================================================
    // RIDDLES
    // =========================================================

    [Header("RIDDLES")]

    public RiddleData[] riddles;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    public TMP_Text titleText;

    public TMP_Text riddleText;

    public Button nextButton;


    // =========================================================
    // TITLE
    // =========================================================

    [Header("TITLE")]

    public RectTransform titleBG;


    // =========================================================
    // RIDDLE
    // =========================================================

    [Header("RIDDLE")]

    public RectTransform riddleBG;


    // =========================================================
    // ANSWER OBJECTS
    // =========================================================
    //
    // Each element should be the WHOLE answer GameObject.
    //
    // Example:
    //
    // Element 0 → Answer Image.1
    //                  └── Answer Text
    //
    // Element 1 → Answer Image.2
    //                  └── Answer Text
    //
    // The entire GameObject will be activated.
    // =========================================================

    [Header("ANSWER OBJECTS")]

    public GameObject[] answerObjects;


    // =========================================================
    // AUDIO
    // =========================================================

    [Header("AUDIO")]

    public AudioSource audioSource;

    public AudioClip introClip;

    public AudioClip popSfx;


    // =========================================================
    // TIMING
    // =========================================================

    [Header("TIMING")]

    public float thinkingTime = 5f;

    public float answerDelay = 0.3f;

    public float nextRiddleDelay = 1.2f;


    // =========================================================
    // ANIMATION SETTINGS
    // =========================================================

    [Header("ANIMATION SETTINGS")]

    public float popSpeed = 5f;

    public float titlePopDuration = 1.75f;

    public float titlePopAmplitude = 0.75f;

    public float titlePopFrequency = 4f;

    public float titlePopStagger = 0.05f;

    public float answerPopDuration = 0.4f;

    public float answerPopScale = 1.08f;


    // =========================================================
    // PRIVATE
    // =========================================================

    private int currentRiddle = 0;

    private bool isPlaying = false;

    private Coroutine currentRoutine;


    // =========================================================
    // ON ENABLE
    // =========================================================

    void OnEnable()
    {
        ResetUIState();

        currentRiddle = 0;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(MainSequence());
    }


    // =========================================================
    // ON DISABLE
    // =========================================================

    void OnDisable()
    {
        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        isPlaying = false;
    }


    // =========================================================
    // RESET UI
    // =========================================================

    void ResetUIState()
    {
        isPlaying = false;

        currentRiddle = 0;


        // -----------------------------------------------------
        // NEXT BUTTON
        // -----------------------------------------------------

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);

            nextButton.interactable = false;

            nextButton.transform.localScale =
                Vector3.zero;
        }


        // -----------------------------------------------------
        // TITLE BG
        // -----------------------------------------------------

        if (titleBG != null)
        {
            titleBG.localScale =
                Vector3.zero;
        }


        // -----------------------------------------------------
        // TITLE TEXT
        // -----------------------------------------------------

        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);

            CanvasGroup cg =
                GetOrAddCanvasGroup(
                    titleText.gameObject
                );

            cg.alpha = 0f;
        }


        // -----------------------------------------------------
        // RIDDLE BG
        // -----------------------------------------------------

        if (riddleBG != null)
        {
            riddleBG.localScale =
                Vector3.zero;
        }


        // -----------------------------------------------------
        // RIDDLE TEXT
        // -----------------------------------------------------

        if (riddleText != null)
        {
            CanvasGroup cg =
                GetOrAddCanvasGroup(
                    riddleText.gameObject
                );

            cg.alpha = 0f;

            riddleText.text = "";
        }


        // -----------------------------------------------------
        // ANSWER OBJECTS
        // -----------------------------------------------------
        //
        // Hide the WHOLE answer GameObject.
        //
        // This automatically hides:
        //
        // Answer Image
        //      └── Answer Text
        //
        // and anything else inside it.
        // -----------------------------------------------------

        if (answerObjects != null)
        {
            for (int i = 0; i < answerObjects.Length; i++)
            {
                if (answerObjects[i] != null)
                {
                    answerObjects[i].SetActive(false);

                    answerObjects[i].transform.localScale =
                        Vector3.zero;
                }
            }
        }
    }


    // =========================================================
    // MAIN SEQUENCE
    // =========================================================

    IEnumerator MainSequence()
    {
        if (riddles == null || riddles.Length == 0)
        {
            Debug.LogWarning(
                "RiddleRevealSystem_S2A: No riddles assigned."
            );

            yield break;
        }

        isPlaying = true;


        // =====================================================
        // INTRO AUDIO
        // =====================================================

        if (audioSource != null && introClip != null)
        {
            audioSource.clip = introClip;

            audioSource.Play();
        }


        // =====================================================
        // TITLE BG
        // =====================================================

        if (titleBG != null)
        {
            yield return StartCoroutine(
                BounceIn(titleBG)
            );
        }


        // =====================================================
        // TITLE TEXT
        // =====================================================

        if (titleText != null)
        {
            yield return StartCoroutine(
                TitleAnim()
            );
        }


        // =====================================================
        // WAIT FOR INTRO AUDIO
        // =====================================================

        if (audioSource != null && introClip != null)
        {
            yield return new WaitWhile(
                () => audioSource.isPlaying
            );
        }


        // =====================================================
        // PLAY ALL RIDDLES
        // =====================================================

        for (
            currentRiddle = 0;
            currentRiddle < riddles.Length;
            currentRiddle++
        )
        {
            yield return StartCoroutine(
                PlayRiddle(currentRiddle)
            );


            // Small pause before next riddle
            if (
                currentRiddle <
                riddles.Length - 1
            )
            {
                yield return new WaitForSeconds(
                    nextRiddleDelay
                );
            }
        }


        // =====================================================
        // ALL RIDDLES FINISHED
        // =====================================================

        isPlaying = false;

        ShowNextButton();
    }


    // =========================================================
    // PLAY RIDDLE
    // =========================================================

    IEnumerator PlayRiddle(int index)
    {
        if (
            index < 0 ||
            index >= riddles.Length
        )
        {
            yield break;
        }

        RiddleData riddle =
            riddles[index];


        // =====================================================
        // HIDE PREVIOUS ANSWERS
        // =====================================================

        HideAllAnswers();


        // =====================================================
        // RESET RIDDLE BG
        // =====================================================

        if (riddleBG != null)
        {
            riddleBG.localScale =
                Vector3.zero;
        }


        // =====================================================
        // SET RIDDLE TEXT
        // =====================================================

        if (riddleText != null)
        {
            riddleText.text =
                riddle.riddle;

            CanvasGroup cg =
                GetOrAddCanvasGroup(
                    riddleText.gameObject
                );

            cg.alpha = 0f;
        }


        // =====================================================
        // RIDDLE BG POP
        // =====================================================

        if (riddleBG != null)
        {
            PlayPopSfx();

            yield return StartCoroutine(
                BounceIn(riddleBG)
            );
        }


        // =====================================================
        // RIDDLE TEXT POP
        // =====================================================

        if (riddleText != null)
        {
            yield return StartCoroutine(
                PopTextPerChar(riddleText)
            );
        }


        // =====================================================
        // RIDDLE AUDIO
        // =====================================================

        if (
            audioSource != null &&
            riddle.riddleAudio != null
        )
        {
            audioSource.clip =
                riddle.riddleAudio;

            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );
        }


        // =====================================================
        // THINKING TIME
        // =====================================================
        //
        // DEFAULT = 5 SECONDS
        // =====================================================

        yield return new WaitForSeconds(
            thinkingTime
        );


        // =====================================================
        // SMALL ANSWER DELAY
        // =====================================================

        yield return new WaitForSeconds(
            answerDelay
        );


        // =====================================================
        // ANSWER OBJECT
        // =====================================================

        if (
            answerObjects != null &&
            index < answerObjects.Length &&
            answerObjects[index] != null
        )
        {
            GameObject answer =
                answerObjects[index];


            // Activate the WHOLE GameObject
            answer.SetActive(true);


            // Start from zero for animation
            answer.transform.localScale =
                Vector3.zero;


            // Play pop sound
            PlayPopSfx();


            // Animate answer
            yield return StartCoroutine(
                BounceInAnswer(
                    answer.transform
                )
            );
        }
        else
        {
            Debug.LogWarning(
                "RiddleRevealSystem_S2A: No answer object assigned for riddle index "
                + index
            );
        }


        // =====================================================
        // ANSWER AUDIO
        // =====================================================

        if (
            audioSource != null &&
            riddle.answerAudio != null
        )
        {
            audioSource.clip =
                riddle.answerAudio;

            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );
        }
    }


    // =========================================================
    // HIDE ALL ANSWERS
    // =========================================================

    void HideAllAnswers()
    {
        if (answerObjects == null)
            return;


        for (
            int i = 0;
            i < answerObjects.Length;
            i++
        )
        {
            if (answerObjects[i] != null)
            {
                answerObjects[i].SetActive(false);

                answerObjects[i].transform.localScale =
                    Vector3.zero;
            }
        }
    }


    // =========================================================
    // POP SOUND
    // =========================================================

    void PlayPopSfx()
    {
        if (
            audioSource != null &&
            popSfx != null
        )
        {
            audioSource.PlayOneShot(popSfx);
        }
    }


    // =========================================================
    // NEXT BUTTON
    // =========================================================

    void ShowNextButton()
    {
        if (nextButton == null)
            return;


        nextButton.gameObject.SetActive(true);

        nextButton.interactable = true;


        StartCoroutine(
            PopButton(
                nextButton.transform
            )
        );
    }


    // =========================================================
    // BOUNCE IN
    // =========================================================

    IEnumerator BounceIn(
        RectTransform target
    )
    {
        if (target == null)
            yield break;


        target.localScale =
            Vector3.zero;


        float t = 0f;


        while (t < 1f)
        {
            t +=
                Time.deltaTime *
                popSpeed;


            float clamped =
                Mathf.Clamp01(t);


            float overshoot =
                1.70158f;


            float c1 =
                overshoot + 1f;


            float ease =
                1f
                + c1 *
                  Mathf.Pow(
                      clamped - 1f,
                      3f
                  )
                + overshoot *
                  Mathf.Pow(
                      clamped - 1f,
                      2f
                  );


            target.localScale =
                Vector3.one * ease;


            yield return null;
        }


        target.localScale =
            Vector3.one;
    }


    // =========================================================
    // ANSWER BOUNCE
    // =========================================================

    IEnumerator BounceInAnswer(
        Transform target
    )
    {
        if (target == null)
            yield break;


        target.localScale =
            Vector3.zero;


        float t = 0f;


        while (t < 1f)
        {
            t +=
                Time.deltaTime /
                Mathf.Max(
                    0.05f,
                    answerPopDuration
                );


            float clamped =
                Mathf.Clamp01(t);


            float overshoot =
                1.70158f *
                (answerPopScale - 1f);


            float c1 =
                overshoot + 1f;


            float ease =
                1f
                + c1 *
                  Mathf.Pow(
                      clamped - 1f,
                      3f
                  )
                + overshoot *
                  Mathf.Pow(
                      clamped - 1f,
                      2f
                  );


            target.localScale =
                Vector3.one * ease;


            yield return null;
        }


        target.localScale =
            Vector3.one;
    }


    // =========================================================
    // NEXT BUTTON POP
    // =========================================================

    IEnumerator PopButton(
        Transform target
    )
    {
        if (target == null)
            yield break;


        target.localScale =
            Vector3.zero;


        float t = 0f;


        while (t < 1f)
        {
            t +=
                Time.deltaTime *
                popSpeed;


            float clamped =
                Mathf.Clamp01(t);


            float overshoot =
                1.70158f;


            float c1 =
                overshoot + 1f;


            float ease =
                1f
                + c1 *
                  Mathf.Pow(
                      clamped - 1f,
                      3f
                  )
                + overshoot *
                  Mathf.Pow(
                      clamped - 1f,
                      2f
                  );


            target.localScale =
                Vector3.one * ease;


            yield return null;
        }


        target.localScale =
            Vector3.one;
    }


    // =========================================================
    // TITLE ANIMATION
    // =========================================================

    IEnumerator TitleAnim()
    {
        if (titleText == null)
            yield break;


        CanvasGroup titleCG =
            GetOrAddCanvasGroup(
                titleText.gameObject
            );


        titleCG.alpha = 0f;


        yield return new WaitForEndOfFrame();

        yield return null;


        titleText.ForceMeshUpdate();

        yield return null;

        titleText.ForceMeshUpdate();


        string originalText =
            titleText.text;


        TMP_TextInfo textInfo =
            titleText.textInfo;


        int charCount =
            textInfo.characterCount;


        if (charCount == 0)
            yield break;


        titleText.maxVisibleCharacters =
            charCount;


        TMP_MeshInfo[] cachedMeshInfo =
            textInfo.CopyMeshInfoVertexData();


        bool revealed = false;

        float elapsed = 0f;


        float expectedTime =
            (charCount *
             titlePopStagger)
            +
            Mathf.Max(
                0.5f,
                1f /
                titlePopFrequency
            );


        float totalDuration =
            Mathf.Max(
                titlePopDuration,
                expectedTime
            );


        while (
            elapsed <
            totalDuration
        )
        {
            if (
                titleText.text !=
                originalText
            )
            {
                break;
            }


            elapsed +=
                Time.deltaTime;


            textInfo =
                titleText.textInfo;


            for (
                int i = 0;
                i < charCount;
                i++
            )
            {
                TMP_CharacterInfo charInfo =
                    textInfo.characterInfo[i];


                if (!charInfo.isVisible)
                    continue;


                int matIndex =
                    charInfo.materialReferenceIndex;


                int vertIndex =
                    charInfo.vertexIndex;


                Vector3[] vertices =
                    textInfo
                        .meshInfo[matIndex]
                        .vertices;


                Vector3 charMid =
                    (
                        vertices[
                            vertIndex
                        ]
                        +
                        vertices[
                            vertIndex + 2
                        ]
                    ) / 2f;


                float letterDelay =
                    i *
                    titlePopStagger;


                float localTime =
                    elapsed -
                    letterDelay;


                float scale = 0f;


                if (localTime > 0f)
                {
                    float letterDur =
                        Mathf.Max(
                            0.1f,
                            1f /
                            titlePopFrequency
                        );


                    float t =
                        Mathf.Clamp01(
                            localTime /
                            letterDur
                        );


                    float overshoot =
                        1.70158f *
                        (
                            1f +
                            titlePopAmplitude
                        );


                    float c3 =
                        overshoot + 1f;


                    scale =
                        1f
                        +
                        c3 *
                        Mathf.Pow(
                            t - 1f,
                            3f
                        )
                        +
                        overshoot *
                        Mathf.Pow(
                            t - 1f,
                            2f
                        );
                }


                for (
                    int v = 0;
                    v < 4;
                    v++
                )
                {
                    Vector3 orig =
                        cachedMeshInfo[
                            matIndex
                        ].vertices[
                            vertIndex + v
                        ];


                    Vector3 offset =
                        orig - charMid;


                    vertices[
                        vertIndex + v
                    ] =
                        charMid
                        +
                        offset * scale;
                }
            }


            for (
                int m = 0;
                m <
                textInfo.meshInfo.Length;
                m++
            )
            {
                textInfo
                    .meshInfo[m]
                    .mesh.vertices =
                    textInfo
                        .meshInfo[m]
                        .vertices;


                titleText.UpdateGeometry(
                    textInfo
                        .meshInfo[m]
                        .mesh,
                    m
                );
            }


            yield return null;


            if (
                !revealed &&
                titleCG != null
            )
            {
                titleCG.alpha = 1f;

                revealed = true;
            }
        }


        if (
            titleText.text ==
            originalText
        )
        {
            textInfo =
                titleText.textInfo;


            for (
                int i = 0;
                i <
                textInfo.meshInfo.Length;
                i++
            )
            {
                textInfo
                    .meshInfo[i]
                    .mesh.vertices =
                    cachedMeshInfo[i]
                        .vertices;


                titleText.UpdateGeometry(
                    textInfo
                        .meshInfo[i]
                        .mesh,
                    i
                );
            }
        }


        titleText.maxVisibleCharacters =
            99999;
    }


    // =========================================================
    // RIDDLE TEXT POP
    // =========================================================

    IEnumerator PopTextPerChar(
        TMP_Text tmp,
        float popDur = 1.2f,
        float charStagger = 0.04f,
        float popAmp = 0.6f,
        float popFreq = 4f
    )
    {
        if (tmp == null)
            yield break;


        CanvasGroup cg =
            GetOrAddCanvasGroup(
                tmp.gameObject
            );


        cg.alpha = 1f;


        tmp.ForceMeshUpdate();

        yield return null;

        tmp.ForceMeshUpdate();


        string originalText =
            tmp.text;


        TMP_TextInfo textInfo =
            tmp.textInfo;


        int charCount =
            textInfo.characterCount;


        if (charCount == 0)
            yield break;


        tmp.maxVisibleCharacters =
            charCount;


        TMP_MeshInfo[] cachedMeshInfo =
            textInfo.CopyMeshInfoVertexData();


        // -----------------------------------------------------
        // START ALL CHARACTERS FROM THEIR CENTER
        // -----------------------------------------------------

        for (
            int i = 0;
            i < charCount;
            i++
        )
        {
            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];


            if (!charInfo.isVisible)
                continue;


            int matIdx =
                charInfo.materialReferenceIndex;


            int vertIdx =
                charInfo.vertexIndex;


            Vector3[] vertices =
                textInfo
                    .meshInfo[matIdx]
                    .vertices;


            Vector3 charMid =
                (
                    vertices[vertIdx]
                    +
                    vertices[vertIdx + 2]
                ) / 2f;


            for (
                int v = 0;
                v < 4;
                v++
            )
            {
                vertices[
                    vertIdx + v
                ] =
                    charMid;
            }
        }


        for (
            int m = 0;
            m <
            textInfo.meshInfo.Length;
            m++
        )
        {
            textInfo
                .meshInfo[m]
                .mesh.vertices =
                textInfo
                    .meshInfo[m]
                    .vertices;


            tmp.UpdateGeometry(
                textInfo
                    .meshInfo[m]
                    .mesh,
                m
            );
        }


        yield return null;


        float expectedTime =
            (charCount *
             charStagger)
            +
            Mathf.Max(
                0.5f,
                1f /
                popFreq
            );


        float totalDuration =
            Mathf.Max(
                popDur,
                expectedTime
            );


        float elapsed = 0f;


        while (
            elapsed <
            totalDuration
        )
        {
            if (
                tmp.text !=
                originalText
            )
            {
                break;
            }


            elapsed +=
                Time.deltaTime;


            textInfo =
                tmp.textInfo;


            for (
                int i = 0;
                i < charCount;
                i++
            )
            {
                TMP_CharacterInfo charInfo =
                    textInfo.characterInfo[i];


                if (!charInfo.isVisible)
                    continue;


                int matIdx =
                    charInfo.materialReferenceIndex;


                int vertIdx =
                    charInfo.vertexIndex;


                Vector3[] vertices =
                    textInfo
                        .meshInfo[matIdx]
                        .vertices;


                Vector3 charMid =
                    (
                        vertices[vertIdx]
                        +
                        vertices[vertIdx + 2]
                    ) / 2f;


                float delay =
                    i *
                    charStagger;


                float localTime =
                    elapsed -
                    delay;


                float scale = 0f;


                if (localTime > 0f)
                {
                    float letterDur =
                        Mathf.Max(
                            0.1f,
                            1f /
                            popFreq
                        );


                    float lt =
                        Mathf.Clamp01(
                            localTime /
                            letterDur
                        );


                    float overshoot =
                        1.70158f *
                        (
                            1f +
                            popAmp
                        );


                    float c3 =
                        overshoot + 1f;


                    scale =
                        1f
                        +
                        c3 *
                        Mathf.Pow(
                            lt - 1f,
                            3f
                        )
                        +
                        overshoot *
                        Mathf.Pow(
                            lt - 1f,
                            2f
                        );
                }


                for (
                    int v = 0;
                    v < 4;
                    v++
                )
                {
                    Vector3 orig =
                        cachedMeshInfo[
                            matIdx
                        ].vertices[
                            vertIdx + v
                        ];


                    Vector3 offset =
                        orig - charMid;


                    vertices[
                        vertIdx + v
                    ] =
                        charMid
                        +
                        offset * scale;
                }
            }


            for (
                int m = 0;
                m <
                textInfo.meshInfo.Length;
                m++
            )
            {
                textInfo
                    .meshInfo[m]
                    .mesh.vertices =
                    textInfo
                        .meshInfo[m]
                        .vertices;


                tmp.UpdateGeometry(
                    textInfo
                        .meshInfo[m]
                        .mesh,
                    m
                );
            }


            yield return null;
        }


        // -----------------------------------------------------
        // RESTORE ORIGINAL GEOMETRY
        // -----------------------------------------------------

        if (
            tmp.text ==
            originalText
        )
        {
            textInfo =
                tmp.textInfo;


            for (
                int i = 0;
                i <
                textInfo.meshInfo.Length;
                i++
            )
            {
                textInfo
                    .meshInfo[i]
                    .mesh.vertices =
                    cachedMeshInfo[i]
                        .vertices;


                tmp.UpdateGeometry(
                    textInfo
                        .meshInfo[i]
                        .mesh,
                    i
                );
            }
        }


        tmp.maxVisibleCharacters =
            99999;


        cg.alpha = 1f;
    }


    // =========================================================
    // CANVAS GROUP HELPER
    // =========================================================

    CanvasGroup GetOrAddCanvasGroup(
        GameObject obj
    )
    {
        if (obj == null)
            return null;


        CanvasGroup cg =
            obj.GetComponent<CanvasGroup>();


        if (cg == null)
        {
            cg =
                obj.AddComponent<CanvasGroup>();
        }


        return cg;
    }
}