using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CategorizingOptions_S2A : MonoBehaviour
{
    // =========================================================
    // PHRASE DATA
    // =========================================================

    [System.Serializable]
    public class PhraseData
    {
        [Header("PHRASE")]
        public GameObject phrase;

        public TMP_Text phraseText;

        [Header("PHRASE ID")]
        public int phraseID;

        [Header("CORRECT OPTION ID")]
        public int correctOptionID;

        [Header("QUESTION AUDIO")]
        public AudioClip questionAudio;
    }


    // =========================================================
    // OPTION DATA
    // =========================================================

    [System.Serializable]
    public class OptionData
    {
        [Header("OPTION BUTTON")]
        public Button button;

        [Header("OPTION TEXT")]
        public TMP_Text optionText;

        [Header("OPTION ID")]
        public int optionID;
    }


    // =========================================================
    // CHAT DATA
    // =========================================================

    [System.Serializable]
    public class ChatData
    {
        [Header("CHAT ROOT")]
        public GameObject chat;

        [Header("PHRASES")]
        public PhraseData[] phrases;

        [Header("SHARED OPTIONS")]
        public OptionData[] options;
    }


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    public TMP_Text titleText;

    [Header("CHATS")]
    public ChatData[] chats;

    [Header("NEXT BUTTON")]
    public Button nextButton;


    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip popClip;
    public AudioClip finishClip;


    // =========================================================
    // COLORS
    // =========================================================

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;


    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Animation")]
    public float phraseFadeSpeed = 0.25f;
    public float phraseTextSpeed = 0.25f;
    public float optionFadeSpeed = 0.25f;
    public float optionTextSpeed = 0.25f;

    public float answerDelay = 0.7f;

    [Range(1f, 1.2f)]
    public float textPopScale = 1.04f;


    // =========================================================
    // INTERNAL VARIABLES
    // =========================================================

    private int currentChat = 0;
    private int currentPhrase = 0;

    private bool canSelect = false;
    private bool isChecking = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Next Button starts inactive.
        // Its existing OnClick is NOT touched.

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

        HideEverything();

        SetupButtons();

        StartCoroutine(IntroSequence());
    }


    // =========================================================
    // SETUP BUTTONS
    // =========================================================

    private void SetupButtons()
    {
        for (int c = 0; c < chats.Length; c++)
        {
            ChatData chat = chats[c];

            int chatIndex = c;

            // -------------------------------------------------
            // OPTIONS
            // -------------------------------------------------

            for (int o = 0; o < chat.options.Length; o++)
            {
                int optionIndex = o;

                OptionData option = chat.options[o];

                if (option.button == null)
                    continue;

                option.button.onClick.RemoveAllListeners();

                option.button.onClick.AddListener(() =>
                {
                    SelectOption(chatIndex, optionIndex);
                });

                option.button.interactable = true;
            }
        }
    }


    // =========================================================
    // INTRO
    // =========================================================

    private IEnumerator IntroSequence()
    {
        // -------------------------------------------------
        // TITLE
        // -------------------------------------------------

        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.one;

            LeanTween.cancel(titleText.gameObject);

            LeanTween.scale(
                titleText.gameObject,
                Vector3.one,
                0.35f
            ).setEaseOutBack();
        }


        // -------------------------------------------------
        // INTRO AUDIO
        // -------------------------------------------------

        if (introClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(introClip);

            yield return new WaitForSeconds(introClip.length);
        }


        yield return new WaitForSeconds(0.1f);


        // -------------------------------------------------
        // START CHAT 1
        // -------------------------------------------------

        ShowChat(0);
    }


    // =========================================================
    // HIDE EVERYTHING
    // =========================================================

    private void HideEverything()
    {
        for (int c = 0; c < chats.Length; c++)
        {
            ChatData chat = chats[c];

            // -------------------------------------------------
            // CHAT
            // -------------------------------------------------

            if (chat.chat != null)
            {
                chat.chat.SetActive(false);
            }


            // -------------------------------------------------
            // PHRASES
            // -------------------------------------------------

            for (int p = 0; p < chat.phrases.Length; p++)
            {
                if (chat.phrases[p].phrase != null)
                {
                    chat.phrases[p].phrase.SetActive(false);
                }
            }


            // -------------------------------------------------
            // OPTIONS
            // -------------------------------------------------

            for (int o = 0; o < chat.options.Length; o++)
            {
                if (chat.options[o].button != null)
                {
                    chat.options[o].button.gameObject.SetActive(false);
                    chat.options[o].button.interactable = true;
                }
            }
        }
    }


    // =========================================================
    // SHOW CHAT
    // =========================================================

    private void ShowChat(int chatIndex)
    {
        if (chatIndex >= chats.Length)
        {
            FinishGame();
            return;
        }


        currentChat = chatIndex;
        currentPhrase = 0;

        canSelect = false;
        isChecking = false;


        ChatData chat = chats[currentChat];


        // -------------------------------------------------
        // SHOW CHAT
        // -------------------------------------------------

        if (chat.chat != null)
        {
            chat.chat.SetActive(true);
        }


        // -------------------------------------------------
        // HIDE ALL PHRASES
        // -------------------------------------------------

        for (int i = 0; i < chat.phrases.Length; i++)
        {
            if (chat.phrases[i].phrase != null)
            {
                chat.phrases[i].phrase.SetActive(false);
            }
        }


        // -------------------------------------------------
        // RESET OPTIONS
        // -------------------------------------------------

        ResetOptions(chat);


        // -------------------------------------------------
        // SHOW FIRST PHRASE
        // -------------------------------------------------

        StartCoroutine(ShowPhrase());
    }


    // =========================================================
    // SHOW PHRASE
    // =========================================================

    private IEnumerator ShowPhrase()
    {
        ChatData chat = chats[currentChat];

        canSelect = false;
        isChecking = false;


        PhraseData phrase =
            chat.phrases[currentPhrase];


        // -------------------------------------------------
        // HIDE OTHER PHRASES
        // -------------------------------------------------

        for (int i = 0; i < chat.phrases.Length; i++)
        {
            if (i != currentPhrase)
            {
                if (chat.phrases[i].phrase != null)
                {
                    chat.phrases[i].phrase.SetActive(false);
                }
            }
        }


        // -------------------------------------------------
        // SHOW CURRENT PHRASE
        // -------------------------------------------------

        if (phrase.phrase != null)
        {
            phrase.phrase.SetActive(true);

            // Prevent phrase UI from blocking
            // option buttons.

            DisableRaycastTargets(
                phrase.phrase
            );


            // Fade the phrase in.
            // IMPORTANT:
            // No scale change.

            CanvasGroup phraseGroup =
                GetOrAddCanvasGroup(
                    phrase.phrase
                );

            phraseGroup.alpha = 0f;

            LeanTween.cancel(
                phrase.phrase
            );

            LeanTween.alphaCanvas(
                phraseGroup,
                1f,
                phraseFadeSpeed
            ).setEaseOutQuad();
        }


        // -------------------------------------------------
        // PHRASE TEXT ANIMATION
        // -------------------------------------------------

        if (phrase.phraseText != null)
        {
            AnimateTextIn(
                phrase.phraseText,
                0f,
                phraseTextSpeed
            );
        }


        // -------------------------------------------------
        // QUESTION AUDIO
        // -------------------------------------------------

        if (phrase.questionAudio != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                phrase.questionAudio
            );

            // Wait until the phrase audio finishes.
            yield return new WaitForSeconds(
                phrase.questionAudio.length
            );
        }
        else
        {
            // If no audio is assigned,
            // continue normally.

            yield return new WaitForSeconds(
                0.15f
            );
        }


        // -------------------------------------------------
        // SHOW OPTIONS
        // -------------------------------------------------

        AnimateOptions(chat);


        // Give the options a moment to appear.

        yield return new WaitForSeconds(
            optionFadeSpeed + 0.08f
        );


        canSelect = true;
        isChecking = false;
    }


    // =========================================================
    // OPTION ANIMATION
    // =========================================================

    private void AnimateOptions(ChatData chat)
    {
        for (int i = 0; i < chat.options.Length; i++)
        {
            OptionData option =
                chat.options[i];


            if (option.button == null)
                continue;


            // -------------------------------------------------
            // BUTTON
            // -------------------------------------------------

            option.button.gameObject.SetActive(true);

            option.button.interactable = true;


            SetButtonColor(
                option.button,
                normalColor
            );


            // -------------------------------------------------
            // BUTTON FADE
            // -------------------------------------------------

            CanvasGroup buttonGroup =
                GetOrAddCanvasGroup(
                    option.button.gameObject
                );

            buttonGroup.alpha = 0f;

            LeanTween.cancel(
                option.button.gameObject
            );


            LeanTween.alphaCanvas(
                buttonGroup,
                1f,
                optionFadeSpeed
            )
            .setDelay(i * 0.05f)
            .setEaseOutQuad();


            // -------------------------------------------------
            // OPTION TEXT
            // -------------------------------------------------

            if (option.optionText != null)
            {
                AnimateTextIn(
                    option.optionText,
                    i * 0.05f,
                    optionTextSpeed
                );
            }
        }
    }


    // =========================================================
    // SELECT OPTION
    // =========================================================

    private void SelectOption(
        int chatIndex,
        int optionIndex)
    {
        if (!canSelect)
            return;

        if (isChecking)
            return;

        if (chatIndex != currentChat)
            return;


        ChatData chat =
            chats[currentChat];


        if (optionIndex < 0 ||
            optionIndex >= chat.options.Length)
            return;


        OptionData selectedOption =
            chat.options[optionIndex];


        if (selectedOption.button == null)
            return;


        isChecking = true;
        canSelect = false;


        // -------------------------------------------------
        // OPTION POP SOUND
        // -------------------------------------------------

        PlaySound(popClip);


        // -------------------------------------------------
        // GET IDs
        // -------------------------------------------------

        int selectedOptionID =
            selectedOption.optionID;

        int correctOptionID =
            chat.phrases[currentPhrase]
                .correctOptionID;


        // -------------------------------------------------
        // CHECK ANSWER
        // -------------------------------------------------

        if (selectedOptionID ==
            correctOptionID)
        {
            StartCoroutine(
                CorrectAnswer(
                    selectedOption.button
                )
            );
        }
        else
        {
            StartCoroutine(
                WrongAnswer(
                    selectedOption.button
                )
            );
        }
    }


    // =========================================================
    // CORRECT ANSWER
    // =========================================================

    private IEnumerator CorrectAnswer(
        Button selectedButton)
    {
        SetButtonColor(
            selectedButton,
            correctColor
        );


        PlaySound(correctSFX);


        // Small feedback animation.
        // Does NOT change button scale.

        PulseButtonAlpha(
            selectedButton
        );


        yield return new WaitForSeconds(
            answerDelay
        );


        ChatData chat =
            chats[currentChat];


        currentPhrase++;


        // -------------------------------------------------
        // CHAT COMPLETE?
        // -------------------------------------------------

        if (currentPhrase >=
            chat.phrases.Length)
        {
            StartCoroutine(
                CompleteChat()
            );

            yield break;
        }


        // -------------------------------------------------
        // NEXT PHRASE
        // -------------------------------------------------

        ResetOptions(chat);

        StartCoroutine(
            ShowPhrase()
        );
    }


    // =========================================================
    // WRONG ANSWER
    // =========================================================

    private IEnumerator WrongAnswer(
        Button selectedButton)
    {
        SetButtonColor(
            selectedButton,
            wrongColor
        );


        PlaySound(wrongSFX);


        PulseButtonAlpha(
            selectedButton
        );


        yield return new WaitForSeconds(
            answerDelay
        );


        // Return to normal.

        SetButtonColor(
            selectedButton,
            normalColor
        );


        isChecking = false;
        canSelect = true;
    }


    // =========================================================
    // BUTTON FEEDBACK
    // =========================================================

    private void PulseButtonAlpha(
        Button button)
    {
        if (button == null)
            return;


        CanvasGroup group =
            GetOrAddCanvasGroup(
                button.gameObject
            );


        LeanTween.cancel(
            button.gameObject
        );


        group.alpha = 1f;


        LeanTween.alphaCanvas(
            group,
            0.75f,
            0.08f
        )
        .setEaseOutQuad()
        .setOnComplete(() =>
        {
            LeanTween.alphaCanvas(
                group,
                1f,
                0.08f
            ).setEaseOutQuad();
        });
    }


    // =========================================================
    // TEXT ANIMATION
    // =========================================================

    private void AnimateTextIn(
        TMP_Text text,
        float delay = 0f,
        float animationSpeed = 0.25f)
    {
        if (text == null)
            return;


        LeanTween.cancel(
            text.gameObject
        );


        // Start only slightly smaller.

        text.transform.localScale =
            Vector3.one * 0.94f;


        // Fast, subtle pop.

        LeanTween.scale(
            text.gameObject,
            Vector3.one * textPopScale,
            animationSpeed
        )
        .setDelay(delay)
        .setEaseOutQuad()
        .setOnComplete(() =>
        {
            LeanTween.scale(
                text.gameObject,
                Vector3.one,
                0.08f
            ).setEaseOutQuad();
        });
    }


    // =========================================================
    // RESET OPTIONS
    // =========================================================

    private void ResetOptions(
        ChatData chat)
    {
        for (int i = 0;
             i < chat.options.Length;
             i++)
        {
            OptionData option =
                chat.options[i];


            if (option.button == null)
                continue;


            // Active.

            option.button.gameObject.SetActive(true);


            // Clickable.

            option.button.interactable = true;


            // Normal color.

            SetButtonColor(
                option.button,
                normalColor
            );


            // Full opacity.

            CanvasGroup buttonGroup =
                GetOrAddCanvasGroup(
                    option.button.gameObject
                );

            buttonGroup.alpha = 1f;


            // IMPORTANT:
            // Button scale is NEVER touched.

            if (option.optionText != null)
            {
                option.optionText.transform.localScale =
                    Vector3.one;
            }
        }
    }


    // =========================================================
    // COMPLETE CHAT
    // =========================================================

    private IEnumerator CompleteChat()
    {
        ChatData chat =
            chats[currentChat];


        canSelect = false;
        isChecking = true;


        yield return new WaitForSeconds(
            0.2f
        );


        // -------------------------------------------------
        // HIDE CHAT
        // -------------------------------------------------

        if (chat.chat != null)
        {
            CanvasGroup chatGroup =
                GetOrAddCanvasGroup(
                    chat.chat
                );


            LeanTween.cancel(
                chat.chat
            );


            LeanTween.alphaCanvas(
                chatGroup,
                0f,
                0.25f
            )
            .setEaseInQuad()
            .setOnComplete(() =>
            {
                chat.chat.SetActive(false);

                // Restore for safety.

                chatGroup.alpha = 1f;
            });
        }


        yield return new WaitForSeconds(
            0.3f
        );


        currentChat++;


        // -------------------------------------------------
        // MORE CHATS
        // -------------------------------------------------

        if (currentChat < chats.Length)
        {
            ShowChat(currentChat);
        }
        else
        {
            FinishGame();
        }
    }


    // =========================================================
    // FINISH GAME
    // =========================================================

    private void FinishGame()
    {
        canSelect = false;
        isChecking = true;


        // Finish sound.

        PlaySound(
            finishClip
        );


        // -------------------------------------------------
        // NEXT BUTTON
        // -------------------------------------------------

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);

            nextButton.interactable = true;


            // Keep original scale.
            // Only fade it in.

            CanvasGroup nextGroup =
                GetOrAddCanvasGroup(
                    nextButton.gameObject
                );


            nextGroup.alpha = 0f;


            LeanTween.cancel(
                nextButton.gameObject
            );


            LeanTween.alphaCanvas(
                nextGroup,
                1f,
                0.3f
            ).setEaseOutQuad();
        }
    }


    // =========================================================
    // BUTTON COLOR
    // =========================================================

    private void SetButtonColor(
        Button button,
        Color color)
    {
        if (button == null)
            return;


        ColorBlock colors =
            button.colors;


        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        colors.selectedColor = color;


        button.colors = colors;
    }


    // =========================================================
    // CANVAS GROUP
    // =========================================================

    private CanvasGroup GetOrAddCanvasGroup(
        GameObject obj)
    {
        CanvasGroup group =
            obj.GetComponent<CanvasGroup>();


        if (group == null)
        {
            group =
                obj.AddComponent<CanvasGroup>();
        }


        return group;
    }


    // =========================================================
    // DISABLE RAYCAST TARGETS
    // =========================================================

    private void DisableRaycastTargets(
        GameObject obj)
    {
        Graphic[] graphics =
            obj.GetComponentsInChildren<
                Graphic
            >(true);


        for (int i = 0;
             i < graphics.Length;
             i++)
        {
            graphics[i].raycastTarget = false;
        }
    }


    // =========================================================
    // AUDIO
    // =========================================================

    private void PlaySound(
        AudioClip clip)
    {
        if (clip != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                clip
            );
        }
    }
}