using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class SA_LetterManager_Phonics_Junior : MonoBehaviour
{
    [Header("Instruction")]
    [SerializeField] private CanvasGroup instructionCanvas;
    [SerializeField] private RectTransform instructionRect;
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private AudioClip instructionAudio;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private LetterData_Phonics_Junior[] letters;
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private AudioSource audioSource;
    private Coroutine currentAudioCoroutine;
    private Image[] dots;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private Transform dotsContainer;
    private bool[] visitedLetters;
    private int currentLetterIndex = 0;
    private bool sectionCompleted = false;
    public bool IsSectionCompleted => sectionCompleted;

    [Header("Main Section Panel")]
    [SerializeField] private GameObject sectionAPanel; // Assign Section_A GameObject
    [SerializeField] private GameObject letterScreen;

    [Header("Quiz")]
    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button option1Button;
    [SerializeField] private Button option2Button;
    [SerializeField] private Button option3Button;
    private bool shouldShowQuiz = false;
    private int correctAnswerIndex;
    private LetterData_Phonics_Junior[] currentQuizLetters;

    private RectTransform letterRect;
    private CanvasGroup letterCanvasGroup;
    private Vector2 defaultCenterPosition = Vector2.zero;
    private bool positionCaptured = false;
    private bool isTransitioning;

    [Header("Completion")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button replayLessonButton;

    [Header("Feedback Audio")]
    [SerializeField] private AudioClip[] correctFeedbackClips;
    [SerializeField] private AudioClip[] wrongFeedbackClips;

    [Header("Idle Reminder")]
    [SerializeField] private float idleTime = 7f;
    [SerializeField] private AudioClip idleReminderClip;
    private Coroutine idleCoroutine;

    [SerializeField] private MascotController_Phonics_Junior mascotController;

    private void Awake()
    {
        EnsureInit();
        if (instructionPanel != null) instructionPanel.SetActive(false);
    }

    private void OnEnable()
    {
        EnsureInit();
        currentLetterIndex = 0;
        sectionCompleted = false;
        shouldShowQuiz = false;
        isTransitioning = false;

        if (visitedLetters != null)
        {
            for (int i = 0; i < visitedLetters.Length; i++)
            {
                visitedLetters[i] = false;
            }
        }
    }

    private void EnsureInit()
    {
        if (sectionAPanel == null) sectionAPanel = gameObject;

        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();

        if (mascotController == null) mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);

        Transform searchRoot = transform;
        if (letterScreen == null)
        {
            Transform t = searchRoot.Find("Letter Screen");
            if (t == null) t = searchRoot.Find("Meet the Letters/Letter Screen");
            if (t != null) letterScreen = t.gameObject;
        }

        if (instructionPanel == null)
        {
            Transform t = searchRoot.Find("Instruction Panel");
            if (t == null) t = searchRoot.Find("Meet the Letters/Instruction Panel");
            if (t != null) instructionPanel = t.gameObject;
        }

        if (quizPanel == null)
        {
            Transform t = searchRoot.Find("Quiz Panel");
            if (t == null) t = searchRoot.Find("Meet the Letters/Quiz Panel");
            if (t != null) quizPanel = t.gameObject;
        }

        if (completionPanel == null)
        {
            Transform t = searchRoot.Find("Completion Panel");
            if (t == null) t = searchRoot.Find("Meet the Letters/Completion Panel");
            if (t != null) completionPanel = t.gameObject;
        }

        if (dotsContainer == null && letterScreen != null)
        {
            Transform t = letterScreen.transform.Find("Dot Container");
            if (t != null) dotsContainer = t;
        }

        if (letterText == null && letterScreen != null)
        {
            TMP_Text txt = letterScreen.GetComponentInChildren<TMP_Text>(true);
            if (txt != null) letterText = txt;
        }

        if (instructionText == null && instructionPanel != null)
        {
            TMP_Text txt = instructionPanel.GetComponentInChildren<TMP_Text>(true);
            if (txt != null) instructionText = txt;
        }

        if (letters != null && (visitedLetters == null || visitedLetters.Length != letters.Length))
        {
            visitedLetters = new bool[letters.Length];
        }

        if (dotsContainer != null && letters != null && (dots == null || dots.Length != letters.Length))
        {
            CreateDots();
        }

        if (letterText != null)
        {
            if (letterRect == null)
            {
                letterRect = letterText.GetComponent<RectTransform>();
            }

            if (!positionCaptured && letterRect != null)
            {
                defaultCenterPosition = letterRect.anchoredPosition;
                positionCaptured = true;
            }

            if (letterCanvasGroup == null)
            {
                letterCanvasGroup = letterText.GetComponent<CanvasGroup>();
                if (letterCanvasGroup == null)
                {
                    letterCanvasGroup = letterText.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        // Auto-heal scene hierarchy: ensure all section A panels are parented directly under Section_A
        if (letterScreen != null && letterScreen.transform.parent != transform)
        {
            letterScreen.transform.SetParent(transform, false);
        }
        if (instructionPanel != null && instructionPanel.transform.parent != transform)
        {
            instructionPanel.transform.SetParent(transform, false);
        }
        if (quizPanel != null && quizPanel.transform.parent != transform)
        {
            quizPanel.transform.SetParent(transform, false);
        }
        if (completionPanel != null && completionPanel.transform.parent != transform)
        {
            completionPanel.transform.SetParent(transform, false);
        }
    }

    private void Start()
    {
        EnsureInit();
    }

    private void CreateDots()
    {
        if (letters == null || dotsContainer == null)
            return;

        // If dotsContainer already has pre-placed children matching letters length, use existing scene objects!
        if (dotsContainer.childCount == letters.Length)
        {
            dots = new Image[letters.Length];
            for (int i = 0; i < letters.Length; i++)
            {
                Transform child = dotsContainer.GetChild(i);
                if (child != null)
                {
                    dots[i] = child.GetComponent<Image>();
                    if (dots[i] == null)
                    {
                        dots[i] = child.GetComponentInChildren<Image>(true);
                    }
                }
            }
            return;
        }

        if (dotPrefab == null)
            return;

        // Clean existing children so dots are never duplicated
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in dotsContainer)
        {
            children.Add(child.gameObject);
        }
        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(children[i]);
            else
                DestroyImmediate(children[i]);
        }

        dots = new Image[letters.Length];

        for (int i = 0; i < letters.Length; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsContainer);
            if (dot != null)
            {
                dots[i] = dot.GetComponent<Image>();
                if (dots[i] == null)
                {
                    dots[i] = dot.GetComponentInChildren<Image>(true);
                }
            }
        }
    }

    private void UpdateDots()
    {
        if (dots == null || visitedLetters == null)
            return;

        for (int i = 0; i < dots.Length && i < visitedLetters.Length; i++)
        {
            if (dots[i] == null) continue;

            if (i == currentLetterIndex)
            {
                // Current Active Letter Dot: Bright Sky Blue + Enlarged 1.35x
                dots[i].color = new Color(0.2f, 0.75f, 1f, 1f);
                dots[i].transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            }
            else if (visitedLetters[i])
            {
                // Visited/Completed Letter Dot: Bright Yellow
                dots[i].color = new Color(1f, 0.85f, 0f, 1f);
                dots[i].transform.localScale = Vector3.one;
            }
            else
            {
                // Unvisited Letter Dot: Clean Crisp White Color
                dots[i].color = new Color(1f, 1f, 1f, 0.85f);
                dots[i].transform.localScale = Vector3.one;
            }
        }
    }

    private void HideSectionSelectionPanels()
    {
        Unit_Selection_Panel_Phonics_Junior unitSel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
        if (unitSel != null)
        {
            unitSel.HideSelectionPanels();
        }

        GameObject sel1 = GameObject.Find("Unit_1_Section_Selection_Panels");
        if (sel1 != null) sel1.SetActive(false);

        GameObject sel2 = GameObject.Find("Unit_2_Section_Selection_Panels");
        if (sel2 != null) sel2.SetActive(false);
    }

    public void ResetSection()
    {
        EnsureInit();
        HideSectionSelectionPanels();
        currentLetterIndex = 0;
        sectionCompleted = false;
        shouldShowQuiz = false;

        if (letterScreen != null) letterScreen.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);

        if (visitedLetters != null)
        {
            for (int i = 0; i < visitedLetters.Length; i++)
            {
                visitedLetters[i] = false;
            }
        }

        if (dots != null)
        {
            UpdateDots();
        }

        StopAllCoroutines();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ShowInstruction());
        }
    }

    public void OpenMeetTheLetters()
    {
        HideSectionSelectionPanels();

        // Force activate entire parent chain up to Canvas so activeInHierarchy is true
        Transform curr = transform;
        while (curr != null && curr.gameObject.name != "Canvas")
        {
            if (!curr.gameObject.activeSelf)
            {
                curr.gameObject.SetActive(true);
            }
            curr = curr.parent;
        }

        gameObject.SetActive(true);

        if (sectionAPanel != null && sectionAPanel != gameObject && sectionAPanel.name != "Section A Panel")
        {
            sectionAPanel.SetActive(true);
        }

        ResetSection();
    }

    private IEnumerator ShowInstruction()
    {
        if (letterScreen != null) letterScreen.SetActive(false);
        if (instructionPanel != null) instructionPanel.SetActive(true);
        if (instructionCanvas != null) instructionCanvas.alpha = 1f;
        if (instructionRect != null) instructionRect.localScale = Vector3.one;

        if (mascotController != null) mascotController.ShowMascot();

        yield return null;

        instructionText.text = "";

        string message = "Let's learn the letter sounds together!";

        if (instructionAudio != null && audioSource != null && audioSource)
        {
            audioSource.Stop();
            if (mascotController != null) mascotController.PlayHiAnimation();
            audioSource.PlayOneShot(instructionAudio);
        }

        foreach (char c in message)
        {
            if (instructionText != null) instructionText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        float audioDuration = (instructionAudio != null) ? instructionAudio.length : 0f;

        yield return new WaitForSeconds(
            Mathf.Max(0f, audioDuration - (message.Length * typingSpeed))
        );

        yield return StartCoroutine(HideInstructionTransition());

        if (letterScreen != null) letterScreen.SetActive(true);

        ShowLetter(true);
    }

    private IEnumerator HideInstructionTransition()
    {
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * 0.9f;

        float startAlpha = 1f;
        float endAlpha = 0f;

        float timer = 0f;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;

            if (instructionCanvas != null) instructionCanvas.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            if (instructionRect != null) instructionRect.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        if (instructionCanvas != null) instructionCanvas.alpha = 1f;
        if (instructionRect != null) instructionRect.localScale = Vector3.one;

        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (mascotController != null) mascotController.HideMascot();
    }

    public void NextLetter()
    {
        if (isTransitioning)
            return;

        StartIdleTimer();

        if (shouldShowQuiz)
        {
            shouldShowQuiz = false;
            ShowQuiz();
            return;
        }

        currentLetterIndex++;

        if (currentLetterIndex >= letters.Length)
        {
            CompleteSection();
            return;
        }

        ShowLetter(true);

        if ((currentLetterIndex + 1) % 5 == 0)
        {
            shouldShowQuiz = true;
        }
    }

    public void PreviousLetter()
    {
        if (isTransitioning)
            return;

        StartIdleTimer();

        if (currentLetterIndex > 0)
        {
            currentLetterIndex--;
            ShowLetter(false);
        }
    }

    private void ShowLetter(bool isNext)
    {
        EnsureInit();

        if (letters == null || letters.Length == 0 || currentLetterIndex < 0 || currentLetterIndex >= letters.Length)
            return;

        if (previousButton != null)
        {
            previousButton.interactable = currentLetterIndex > 0;
        }

        LetterData_Phonics_Junior currentLetter = letters[currentLetterIndex];
        if (currentLetter == null) return;

        if (visitedLetters != null && currentLetterIndex < visitedLetters.Length)
        {
            visitedLetters[currentLetterIndex] = true;
        }

        if (letterText != null)
        {
            letterText.gameObject.SetActive(true);
        }

        if (letterCanvasGroup != null)
        {
            letterCanvasGroup.alpha = 1f;
        }

        UpdateDots();

        isTransitioning = false;

        StartCoroutine(
            SlideLetter(
                currentLetter.upperCase + currentLetter.lowerCase,
                isNext
            )
        );
    }

    public void PlayLetterAudio()
    {
        StartIdleTimer();
        if (currentAudioCoroutine != null)
        {
            StopCoroutine(currentAudioCoroutine);
        }

        currentAudioCoroutine = StartCoroutine(PlayAudioCoroutine());
    }

    private IEnumerator PlayAudioCoroutine()
    {
        LetterData_Phonics_Junior currentLetter = letters[currentLetterIndex];

        if (audioSource == null || currentLetter == null)
        {
            yield break;
        }

        audioSource.Stop();

        if (currentLetter.letterSoundAudio != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(currentLetter.letterSoundAudio);
        }

        currentAudioCoroutine = null;

        yield break;
    }

    private float lastInteractionTime;

    private void StartIdleTimer()
    {
        lastInteractionTime = Time.time;

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        if (gameObject.activeInHierarchy)
        {
            idleCoroutine = StartCoroutine(IdleReminder());
        }
    }

    private IEnumerator IdleReminder()
    {
        while (Time.time - lastInteractionTime < idleTime)
        {
            float waitRemaining = idleTime - (Time.time - lastInteractionTime);
            yield return new WaitForSeconds(Mathf.Max(0.1f, waitRemaining));
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            idleCoroutine = null;
            yield break;
        }

        if (mascotController != null)
        {
            mascotController.ShowMascot();
            mascotController.PlayHiAnimation();
        }

        if (audioSource != null && idleReminderClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(idleReminderClip);
            yield return new WaitForSeconds(idleReminderClip.length);
        }

        if (mascotController != null)
        {
            mascotController.HideMascot();
        }

        idleCoroutine = null;
    }

    private void ShowQuiz()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        if (currentAudioCoroutine != null)
        {
            StopCoroutine(currentAudioCoroutine);
            currentAudioCoroutine = null;
        }

        if (audioSource != null) audioSource.Stop();

        if (letterScreen != null) letterScreen.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);

        SetupQuiz();
        ReplayQuizSound();
    }

    private void HideQuiz()
    {
        if (currentAudioCoroutine != null)
        {
            StopCoroutine(currentAudioCoroutine);
            currentAudioCoroutine = null;
        }

        if (audioSource != null) audioSource.Stop();

        if (quizPanel != null) quizPanel.SetActive(false);
        if (letterScreen != null) letterScreen.SetActive(true);

        currentLetterIndex++;

        if (currentLetterIndex >= letters.Length)
        {
            CompleteSection();
            return;
        }

        ShowLetter(true);
    }

    private void SetupQuiz()
    {
        currentQuizLetters = new LetterData_Phonics_Junior[3];

        int startIndex = (currentLetterIndex / 5) * 5;
        int[] selectedIndices = new int[3];

        for (int i = 0; i < 3; i++)
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(startIndex, Mathf.Min(currentLetterIndex + 1, letters.Length));
            }
            while (
                (i > 0 && randomIndex == selectedIndices[0]) ||
                (i > 1 && randomIndex == selectedIndices[1])
            );

            selectedIndices[i] = randomIndex;
            currentQuizLetters[i] = letters[randomIndex];
        }

        if (option1Button != null) option1Button.GetComponentInChildren<TMP_Text>().text = currentQuizLetters[0].upperCase;
        if (option2Button != null) option2Button.GetComponentInChildren<TMP_Text>().text = currentQuizLetters[1].upperCase;
        if (option3Button != null) option3Button.GetComponentInChildren<TMP_Text>().text = currentQuizLetters[2].upperCase;

        correctAnswerIndex = Random.Range(0, 3);
    }

    private void CheckAnswer(int selectedIndex)
    {
        if (audioSource != null) audioSource.Stop();

        if (selectedIndex == correctAnswerIndex)
        {
            Button selectedButton = GetSelectedButton(selectedIndex);
            if (selectedButton != null)
            {
                StartCoroutine(FlashButton(selectedButton, new Color(0.75f, 1f, 0.75f)));
            }
            StartCoroutine(PlayCorrectFeedback());
        }
        else
        {
            Button selectedButton = GetSelectedButton(selectedIndex);
            if (selectedButton != null)
            {
                StartCoroutine(FlashButton(selectedButton, new Color(1f, 0.75f, 0.75f)));
                StartCoroutine(ShakeButton(selectedButton.transform));
            }
            StartCoroutine(PlayWrongFeedback());
        }
    }

    private Button GetSelectedButton(int index)
    {
        switch (index)
        {
            case 0: return option1Button;
            case 1: return option2Button;
            case 2: return option3Button;
        }
        return null;
    }

    private IEnumerator PlayCorrectFeedback()
    {
        if (correctFeedbackClips != null && correctFeedbackClips.Length > 0)
        {
            AudioClip clip = correctFeedbackClips[Random.Range(0, correctFeedbackClips.Length)];
            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }
            if (audioSource != null && clip != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length);
            }
            if (mascotController != null) mascotController.HideMascot();
        }

        yield return new WaitForSeconds(0.2f);
        HideQuiz();
    }

    private IEnumerator PlayWrongFeedback()
    {
        if (wrongFeedbackClips != null && wrongFeedbackClips.Length > 0)
        {
            AudioClip clip = wrongFeedbackClips[Random.Range(0, wrongFeedbackClips.Length)];
            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length);
            }
            if (mascotController != null) mascotController.HideMascot();
        }
    }

    public void ReplayQuizSound()
    {
        if (audioSource != null && currentQuizLetters != null && correctAnswerIndex < currentQuizLetters.Length && currentQuizLetters[correctAnswerIndex] != null)
        {
            audioSource.Stop();
            if (currentQuizLetters[correctAnswerIndex].letterSoundAudio != null)
            {
                audioSource.PlayOneShot(currentQuizLetters[correctAnswerIndex].letterSoundAudio);
            }
        }
    }

    public void SelectOption1() { CheckAnswer(0); }
    public void SelectOption2() { CheckAnswer(1); }
    public void SelectOption3() { CheckAnswer(2); }

    private void CompleteSection()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
        if (letterScreen != null) letterScreen.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);

        if (completionPanel != null) completionPanel.SetActive(true);

        sectionCompleted = true;
    }

    public void ReplayLesson()
    {
        currentLetterIndex = 0;
        sectionCompleted = false;

        if (completionPanel != null) completionPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (letterScreen != null) letterScreen.SetActive(true);

        if (visitedLetters != null)
        {
            for (int i = 0; i < visitedLetters.Length; i++)
            {
                visitedLetters[i] = false;
            }
        }

        StopAllCoroutines();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ShowInstruction());
        }
    }

    private IEnumerator SlideLetter(string newLetter, bool isNext)
    {
        isTransitioning = true;
        EnsureInit();

        if (letterText == null || letterRect == null || letterCanvasGroup == null)
        {
            if (letterText != null) letterText.text = newLetter;
            isTransitioning = false;
            yield break;
        }

        float duration = 0.25f;
        Vector2 center = defaultCenterPosition;

        Vector2 exitPosition = isNext ? center + Vector2.right * 400f : center + Vector2.left * 400f;
        Vector2 enterPosition = isNext ? center + Vector2.left * 400f : center + Vector2.right * 400f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            if (letterRect != null) letterRect.anchoredPosition = Vector2.Lerp(center, exitPosition, t);
            if (letterCanvasGroup != null) letterCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        if (letterText != null) letterText.text = newLetter;

        if (letterRect != null) letterRect.anchoredPosition = enterPosition;
        if (letterCanvasGroup != null) letterCanvasGroup.alpha = 0f;

        timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            if (letterRect != null) letterRect.anchoredPosition = Vector2.Lerp(enterPosition, center, t);
            if (letterCanvasGroup != null) letterCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        if (letterRect != null) letterRect.anchoredPosition = center;
        if (letterCanvasGroup != null) letterCanvasGroup.alpha = 1f;

        PlayLetterAudio();
        UpdateDots();

        isTransitioning = false;
    }

    public IEnumerator ShakeButton(Transform button)
    {
        if (button == null) yield break;

        Vector3 originalPos = button.localPosition;
        float duration = 0.2f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float offset = Mathf.Sin(timer * 60f) * 10f;
            button.localPosition = originalPos + Vector3.right * offset;
            yield return null;
        }

        button.localPosition = originalPos;
    }

    private IEnumerator FlashButton(Button button, Color flashColor)
    {
        if (button == null) yield break;
        Image image = button.GetComponent<Image>();
        if (image == null) yield break;

        Color originalColor = image.color;
        float duration = 0.15f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            image.color = Color.Lerp(originalColor, flashColor, timer / duration);
            yield return null;
        }

        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            image.color = Color.Lerp(flashColor, originalColor, timer / duration);
            yield return null;
        }

        image.color = originalColor;
    }

    public void StopSection()
    {
        StopAllCoroutines();
        if (instructionPanel != null) instructionPanel.SetActive(false);

        try
        {
            if (audioSource != null && audioSource)
                audioSource.Stop();
        }
        catch (System.Exception) { }

        try
        {
            if (mascotController != null && mascotController)
                mascotController.HideMascot();
        }
        catch (System.Exception) { }

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
    }

    public void CloseSection()
    {
        StopSection();
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (letterScreen != null) letterScreen.SetActive(false);

        if (sectionAPanel != null)
        {
            sectionAPanel.SetActive(false);
        }
    }

    public void OnCompletionNextButtonClicked()
    {
        CloseSection();
        Unit_Selection_Panel_Phonics_Junior unitPanel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
        if (unitPanel != null)
        {
            unitPanel.OpenSectionB();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isTransitioning = false;

        try
        {
            if (audioSource != null && audioSource)
                audioSource.Stop();
        }
        catch (System.Exception) { }

        try
        {
            if (mascotController != null && mascotController)
                mascotController.HideMascot();
        }
        catch (System.Exception) { }
    }
}