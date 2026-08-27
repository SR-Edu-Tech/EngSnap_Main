using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SB_LetterSoundManager_Phonics_Junior : MonoBehaviour
{
    [Header("Letter Data")]
    [SerializeField] private LetterData_Phonics_Junior[] letters;
    [Header("Instruction")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private AudioClip instructionAudio;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private CanvasGroup instructionCanvas;
    [SerializeField] private RectTransform instructionRect;

    [Header("UI")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private TMP_Text objectNameText;
    [SerializeField] private Image letterImage;
    [SerializeField] private GameObject sectionBPanel;
    [SerializeField] private GameObject sectionSelectionPanel;

    private bool isAnimating;

    [Header("Buttons")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Progress")]
    [SerializeField] private Transform dotsContainer;
    [SerializeField] private GameObject dotPrefab;

    [Header("Completion")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private GameObject letterScreen;

    private Image[] dots;
    private bool[] visitedLetters;

    private int currentLetterIndex;
    [SerializeField] private CanvasGroup contentCanvas;
    [Header("Sound Match")]
    [SerializeField] private GameObject soundMatchPanel;
    [SerializeField] private SB_SoundMatchManager_Phonics_Junior soundMatchManager;
    [Header("Idle Reminder")]
    [SerializeField] private float idleTime = 7f;
    [SerializeField] private AudioClip idleReminderClip;
    private Coroutine idleCoroutine;
    private float lastInteractionTime;

    [SerializeField] private MascotController_Phonics_Junior mascotController;

    private void Awake()
    {
        EnsureInit();
    }

    private void OnEnable()
    {
        EnsureInit();
        currentLetterIndex = 0;

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
        if (sectionBPanel == null) sectionBPanel = gameObject;

        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();

        if (mascotController == null) mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);

        if (soundMatchManager == null) soundMatchManager = GetComponentInChildren<SB_SoundMatchManager_Phonics_Junior>(true);
        if (soundMatchManager == null) soundMatchManager = FindFirstObjectByType<SB_SoundMatchManager_Phonics_Junior>(FindObjectsInactive.Include);

        Transform searchRoot = transform;
        if (letterScreen == null)
        {
            Transform t = searchRoot.Find("Letter Screen");
            if (t == null) t = searchRoot.Find("Letter Screen Panel");
            if (t != null) letterScreen = t.gameObject;
        }

        if (instructionPanel == null)
        {
            Transform t = searchRoot.Find("Instruction Panel");
            if (t != null) instructionPanel = t.gameObject;
        }

        if (soundMatchPanel == null)
        {
            Transform t = searchRoot.Find("Sound Match Panel");
            if (t != null) soundMatchPanel = t.gameObject;
        }

        if (completionPanel == null)
        {
            Transform t = searchRoot.Find("Completion Panel");
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

        // Auto-heal scene hierarchy: ensure all section B panels are parented directly under Section_B
        if (letterScreen != null && letterScreen.transform.parent != transform)
        {
            letterScreen.transform.SetParent(transform, false);
        }
        if (instructionPanel != null && instructionPanel.transform.parent != transform)
        {
            instructionPanel.transform.SetParent(transform, false);
        }
        if (soundMatchPanel != null && soundMatchPanel.transform.parent != transform)
        {
            soundMatchPanel.transform.SetParent(transform, false);
        }
        if (completionPanel != null && completionPanel.transform.parent != transform)
        {
            completionPanel.transform.SetParent(transform, false);
        }
    }

    private void Start()
    {
        EnsureInit();
        if (soundMatchPanel != null) soundMatchPanel.SetActive(false);

        CreateDots();

        currentLetterIndex = 0;
        if (contentCanvas != null) contentCanvas.alpha = 1f;
    }

    public void StopIdleReminder()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        if (audioSource != null) audioSource.Stop();
        if (mascotController != null) mascotController.HideMascot();
    }


   private void RestartIdleReminder()
    {
        if (!gameObject.activeInHierarchy)
            return;

        lastInteractionTime = Time.time;

        if (idleCoroutine != null)
            StopCoroutine(idleCoroutine);

        mascotController.HideMascot();
        audioSource.Stop();

        idleCoroutine = StartCoroutine(IdleReminder());
    }

    private IEnumerator IdleReminder()
    {
        while (true)
        {
            // Exit immediately if this section is no longer active
            if (!sectionBPanel.activeInHierarchy)
                yield break;

            yield return new WaitForSeconds(0.25f);

            if (Time.time - lastInteractionTime < idleTime)
                continue;

            if (audioSource.isPlaying)
                continue;

            mascotController.ShowMascot();
            mascotController.PlayHiAnimation();

            audioSource.clip = idleReminderClip;
            audioSource.Play();

            while (audioSource.isPlaying)
            {
                if (!sectionBPanel.activeInHierarchy)
                {
                    audioSource.Stop();
                    mascotController.HideMascot();
                    yield break;
                }

                yield return null;
            }

            mascotController.HideMascot();

            lastInteractionTime = Time.time;
        }
    }

    private void CreateDots()
    {
        if (letters == null || dotsContainer == null)
            return;

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
                if (dots[i] != null)
                {
                    dots[i].color = Color.white;
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
                dots[i].color = Color.yellow;
                dots[i].transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            }
            else if (visitedLetters[i])
            {
                dots[i].color = Color.green;
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

    private void DisplayCurrentLetterData()
    {
        if (letters != null && currentLetterIndex >= 0 && currentLetterIndex < letters.Length)
        {
            LetterData_Phonics_Junior current = letters[currentLetterIndex];

            if (visitedLetters != null && currentLetterIndex < visitedLetters.Length)
                visitedLetters[currentLetterIndex] = true;

            if (letterText != null) letterText.text = current.CombinedText;
            if (objectNameText != null) objectNameText.text = current.objectName;
            if (letterImage != null) letterImage.sprite = current.letterImage;
        }

        UpdateDots();
        if (previousButton != null) previousButton.interactable = currentLetterIndex > 0;
    }

    private IEnumerator ShowInstruction()
    {
        HideSectionSelectionPanels();

        if (letterScreen != null) letterScreen.SetActive(true);
        if (contentCanvas != null) contentCanvas.alpha = 1f;
        if (soundMatchPanel != null) soundMatchPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);
        if (instructionPanel != null) instructionPanel.SetActive(true);
        if (instructionCanvas != null) instructionCanvas.alpha = 1f;
        if (instructionRect != null) instructionRect.localScale = Vector3.one;

        DisplayCurrentLetterData();

        if (mascotController != null) mascotController.ShowMascot();
        if (instructionText != null) instructionText.text = "";

        string message = "Listen to the sounds and remember the matching pictures.";

        if (instructionAudio != null && audioSource != null)
        {
            if (mascotController != null) mascotController.PlayHiAnimation();
            audioSource.PlayOneShot(instructionAudio);
        }

        if (instructionText != null)
        {
            foreach (char c in message)
            {
                instructionText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        float audioDuration = (instructionAudio != null) ? instructionAudio.length : 0f;

        yield return new WaitForSeconds(
            Mathf.Max(0f, audioDuration - (message.Length * typingSpeed))
        );

        yield return new WaitForSeconds(0.3f);

        // Animate Out
        float duration = 0.25f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            if (instructionCanvas != null) instructionCanvas.alpha = Mathf.Lerp(1f, 0f, t);
            if (instructionRect != null) instructionRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.05f,t);

            yield return null;
        }

        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (mascotController != null) mascotController.HideMascot();

        PlayLetterSound();

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }

        RestartIdleReminder();

        if (instructionCanvas != null) instructionCanvas.alpha = 1f;
        if (instructionRect != null) instructionRect.localScale = Vector3.one;
    }
    private void ShowLetter()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (letterScreen != null) letterScreen.SetActive(true);
        if (contentCanvas != null) contentCanvas.alpha = 1f;

        if (mascotController != null) mascotController.HideMascot();

        RestartIdleReminder();
        StartCoroutine(FadeToLetter());
    }
    public void NextLetter()
    {
        if (!gameObject.activeInHierarchy)
            return;
        
        RestartIdleReminder();

        if (currentLetterIndex < letters.Length - 1)
        {
            currentLetterIndex++;
            ShowLetter();
        }
        else
        {
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
                idleCoroutine = null;
            }
            mascotController.HideMascot();
            // Hide only the A-Z letter UI
            letterScreen.SetActive(false);

            // Show the Sound Match UI
            soundMatchPanel.SetActive(true);

            // Start the mini-game
            soundMatchManager.StartGame();
        }
    }
    public void PreviousLetter()
    {
        if (!gameObject.activeInHierarchy)
            return;

        RestartIdleReminder();

        if (currentLetterIndex > 0)
        {
            currentLetterIndex--;
            ShowLetter();
        }
    }

    public void PlayLetterSound()
    {
        RestartIdleReminder();
        mascotController.HideMascot();
        audioSource.Stop();

        if (letters[currentLetterIndex].letterSoundAudio != null)
        {
            audioSource.PlayOneShot(letters[currentLetterIndex].letterSoundAudio);
        }
    }
    public void ReplaySection()
    {
        completionPanel.SetActive(false);
        soundMatchPanel.SetActive(false);
        letterScreen.SetActive(true);

        currentLetterIndex = 0;

        for (int i = 0; i < visitedLetters.Length; i++)
        {
            visitedLetters[i] = false;
        }

        audioSource.Stop();
        mascotController.HideMascot();
        StopAllCoroutines();

        if (idleCoroutine != null)
        {

            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        mascotController.HideMascot();
        StartCoroutine(ShowInstruction());
    }

     private IEnumerator FadeToLetter()
    {
        float duration = 0.15f;
        float timer = 0f;

        // Fade Out
        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (contentCanvas != null) contentCanvas.alpha = Mathf.Lerp(1f, 0f, timer / duration);
            yield return null;
        }

        // Change Content
        if (letters != null && currentLetterIndex >= 0 && currentLetterIndex < letters.Length)
        {
            LetterData_Phonics_Junior current = letters[currentLetterIndex];

            if (visitedLetters != null && currentLetterIndex < visitedLetters.Length)
                visitedLetters[currentLetterIndex] = true;

            if (letterText != null) letterText.text = current.CombinedText;
            if (objectNameText != null) objectNameText.text = current.objectName;
            if (letterImage != null) letterImage.sprite = current.letterImage;
        }

        UpdateDots();
        PlayLetterSound();

        if (previousButton != null)
        {
            previousButton.interactable = currentLetterIndex > 0;
        }

        // Fade In
        timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (contentCanvas != null) contentCanvas.alpha = Mathf.Lerp(0f, 1f, timer / duration);
            yield return null;
        }

        if (contentCanvas != null) contentCanvas.alpha = 1f;
    }

    
    public void OpenSectionB()
    {
        OpenLetterSounds();
    }

    public void OpenLetterSounds()
    {
        EnsureInit();
        HideSectionSelectionPanels();

        // 1. Force activate entire parent chain up to Canvas so activeInHierarchy is true
        Transform curr = transform;
        while (curr != null && curr.gameObject.name != "Canvas")
        {
            if (!curr.gameObject.activeSelf)
            {
                curr.gameObject.SetActive(true);
            }
            curr = curr.parent;
        }

        // Activate Section_B GameObject directly so gameplay objects are guaranteed active
        gameObject.SetActive(true);

        if (sectionBPanel != null && sectionBPanel != gameObject && sectionBPanel.name != "Section B Panel")
        {
            sectionBPanel.SetActive(true);
        }

        if (contentCanvas != null) contentCanvas.alpha = 1f;
        if (letterScreen != null) letterScreen.SetActive(true);
        if (soundMatchPanel != null) soundMatchPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);

        currentLetterIndex = 0;

        if (visitedLetters != null)
        {
            for (int i = 0; i < visitedLetters.Length; i++)
                visitedLetters[i] = false;
        }

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        if (audioSource != null) audioSource.Stop();
        if (mascotController != null) mascotController.HideMascot();
        StopAllCoroutines();

        // 2. Guard coroutine execution
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ShowInstruction());
        }
    }


    public void ReplayLetterSound()
    {
        PlayLetterSound();
    }

    public void StopSection()
    {
        StopAllCoroutines();
        if (instructionPanel != null) instructionPanel.SetActive(false);

        if (audioSource != null && audioSource)
            audioSource.Stop();

        if (mascotController != null && mascotController)
            mascotController.HideMascot();

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
        if (soundMatchPanel != null) soundMatchPanel.SetActive(false);
        if (letterScreen != null) letterScreen.SetActive(false);

        if (sectionBPanel != null)
        {
            sectionBPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (audioSource != null && audioSource)
        {
            audioSource.Stop();
        }

        if (mascotController != null && mascotController)
        {
            mascotController.HideMascot();
        }
    }
}