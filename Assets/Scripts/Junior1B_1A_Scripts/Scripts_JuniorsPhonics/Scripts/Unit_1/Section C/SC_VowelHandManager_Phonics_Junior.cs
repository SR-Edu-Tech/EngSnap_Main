using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SC_VowelHandManager_Phonics_Junior : MonoBehaviour
{
    [Header("Vowel Data")]
    [SerializeField] private VowelData_Phonics_Junior[] vowels;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI")]
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text messageText;

    [Header("Mascot")]
    [SerializeField] private AudioClip mascotClip;

    [Header("Panels")]
    [SerializeField] private GameObject sectionSelectionPanel;
    [SerializeField] private GameObject sectionCPanel;
    [SerializeField] private GameObject learnPanel;
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TMP_Text instructionText;

    [Header("Instruction")]
    [SerializeField, TextArea]
    private string instructionMessage =
    "Tap each vowel on the fingers to hear its sound.";
    [SerializeField] private SC_TextNarrator_Phonics_Junior narrator;
    private bool canClick = false;
    [SerializeField] private AudioClip instructionClip;
    private Coroutine instructionRoutine;
    [Header("Completion")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Button completionNextButton;
    [SerializeField] private AudioClip completionClip;
    [SerializeField] private SC_VowelQuizManager_Phonics_Junior quizManager;
    [Header("Idle Reminder")]
    [SerializeField] private float idleTime = 7f;
    [SerializeField] private AudioClip idleReminderClip;
    private float lastInteractionTime;
    private Coroutine idleCoroutine;
    [SerializeField] private MascotController_Phonics_Junior mascotController;
    [SerializeField] private SB_LetterSoundManager_Phonics_Junior sectionBManager;

    private void Awake()
    {
        EnsureInit();
    }

    private void OnEnable()
    {
        EnsureInit();
    }

    private void EnsureInit()
    {
        if (sectionCPanel == null) sectionCPanel = gameObject;

        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();

        if (mascotController == null) mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);

        if (narrator == null) narrator = GetComponentInChildren<SC_TextNarrator_Phonics_Junior>(true);
        if (narrator == null) narrator = FindFirstObjectByType<SC_TextNarrator_Phonics_Junior>(FindObjectsInactive.Include);

        if (quizManager == null) quizManager = GetComponentInChildren<SC_VowelQuizManager_Phonics_Junior>(true);
        if (quizManager == null) quizManager = FindFirstObjectByType<SC_VowelQuizManager_Phonics_Junior>(FindObjectsInactive.Include);

        if (audioSource != null)
        {
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f;
        }

        if (vowels != null)
        {
            for (int i = 0; i < vowels.Length; i++)
            {
                int idx = i;
                if (vowels[i] != null && vowels[i].button != null)
                {
                    vowels[i].button.onClick.RemoveAllListeners();
                    vowels[i].button.onClick.AddListener(() => PlayVowel(idx));
                }
            }
        }

        Transform searchRoot = transform;
        if (learnPanel == null)
        {
            Transform t = searchRoot.Find("Learn Panel");
            if (t == null) t = searchRoot.Find("Vowel Hand Panel");
            if (t != null) learnPanel = t.gameObject;
        }

        if (quizPanel == null)
        {
            Transform t = searchRoot.Find("Quiz Panel");
            if (t != null) quizPanel = t.gameObject;
        }

        if (completionPanel == null)
        {
            Transform t = searchRoot.Find("Completion Panel");
            if (t != null) completionPanel = t.gameObject;
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

    private void Start()
    {
        EnsureInit();
        ResetSection();
    }

    public void OpenVowelHand()
    {
        EnsureInit();

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

        gameObject.SetActive(true);
        if (sectionCPanel != null && sectionCPanel != gameObject) sectionCPanel.SetActive(true);

        HideSectionSelectionPanels();

        if (sectionBManager != null)
        {
            try { sectionBManager.StopIdleReminder(); } catch (System.Exception) { }
        }

        StopAllCoroutines();

        if (narrator != null) narrator.StopNarration();
        if (audioSource != null) audioSource.Stop();

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        if (mascotController != null) mascotController.HideMascot();

        ResetSection();

        canClick = false;

        if (instructionRoutine != null)
        {
            StopCoroutine(instructionRoutine);
            instructionRoutine = null;
        }

        if (gameObject.activeInHierarchy)
        {
            instructionRoutine = StartCoroutine(ShowInstruction());
        }
        else
        {
            canClick = true;
        }
    }

    private void StartIdleTimer()
    {
        if (idleCoroutine != null)
            StopCoroutine(idleCoroutine);

        idleCoroutine = StartCoroutine(IdleReminder());
    }

    private IEnumerator IdleReminder()
    {
        if (idleReminderClip == null)
            yield break;

        while (true)
        {
            yield return null;

            if (!canClick)
                continue;

            if (audioSource != null && audioSource.isPlaying)
                continue;

            if (Time.time - lastInteractionTime < idleTime)
                continue;

            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }

            if (audioSource != null)
            {
                audioSource.clip = idleReminderClip;
                audioSource.Play();

                while (audioSource.isPlaying)
                    yield return null;
            }

            if (mascotController != null) mascotController.HideMascot();

            lastInteractionTime = Time.time;
        }
    }

    private IEnumerator ShowInstruction()
    {
        if (audioSource != null) audioSource.Stop();
        if (mascotController != null) mascotController.HideMascot();
        canClick = false;

        if (narrator != null && instructionText != null && instructionClip != null)
        {
            yield return StartCoroutine(
                narrator.Play(
                    instructionText,
                    instructionMessage,
                    instructionClip
                )
            );
        }
        else if (instructionText != null)
        {
            instructionText.text = instructionMessage;
            if (instructionClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(instructionClip);
            }
        }

        instructionRoutine = null;
        canClick = true;

        lastInteractionTime = Time.time;
        StartIdleTimer();
    }

    private void ResetSection()
    {
        learnPanel.SetActive(true);
        quizPanel.SetActive(false);

        nextButton.interactable = false;

        instructionText.text = "";
        messageText.text = "";

        foreach (VowelData_Phonics_Junior vowel in vowels)
        {
            vowel.completed = false;

            if (vowel.button != null)
                vowel.button.image.color = Color.white;
        }
    }
    public void PlayVowel(int index)
    {
        if (!canClick)
            canClick = true;

        if (vowels == null || index < 0 || index >= vowels.Length)
            return;

        // Stop idle reminder coroutine
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        // Stop any reminder or previous audio
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f;
        }

        // Hide mascot immediately if it was giving a reminder
        if (mascotController != null)
        {
            mascotController.HideMascot();
        }

        VowelData_Phonics_Junior vowel = vowels[index];
        if (vowel == null) return;

        // Play the selected vowel sound
        if (vowel.sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(vowel.sound);
        }

        // Restart idle timer from now
        lastInteractionTime = Time.time;
        StartIdleTimer();

        // If already completed, don't check completion again
        if (vowel.completed)
            return;

        vowel.completed = true;

        if (vowel.button != null)
        {
            StartCoroutine(ChangeButtonColor(vowel.button.image));
        }

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (!canClick)
            return;

        foreach (VowelData_Phonics_Junior vowel in vowels)
        {
            if (!vowel.completed)
                return;
        }

        canClick = false;
        StartCoroutine(AllVowelsCompletedRoutine());
    }

    private IEnumerator ChangeButtonColor(Image buttonImage)
    {
        Color startColor = Color.white;
        Color targetColor = Color.yellow;

        float duration = 0.15f;
        float time = 0f;

        while (time < duration)
        {
            buttonImage.color = Color.Lerp(startColor, targetColor, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        buttonImage.color = targetColor;
    }


    private IEnumerator AllVowelsCompletedRoutine()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
        if (audioSource != null) audioSource.Stop();
        if (mascotController != null) mascotController.HideMascot();

        yield return new WaitForSeconds(0.5f);

        if (narrator != null)
        {
            yield return StartCoroutine(
                narrator.Play(
                    messageText,
                    "5 vowels! Everything else is a consonant.",
                    mascotClip
                )
            );
        }

        if (nextButton != null) nextButton.interactable = true;
    }

    public void OpenQuiz()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        audioSource.Stop();
        mascotController.HideMascot();

        learnPanel.SetActive(false);
        quizPanel.SetActive(true);

        quizManager.StartQuiz();
    }

    private IEnumerator ShowCompletion()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        audioSource.Stop();
        mascotController.HideMascot();

        quizPanel.SetActive(false);
        completionPanel.SetActive(true);

        completionNextButton.interactable = false;

        yield return StartCoroutine(
           narrator.Play(
              messageText,
              "Fantastic! You found all the vowels and completed the quiz. Great job!",
              completionClip
           )
        );

        completionNextButton.interactable = true;
    }
    public void OnSectionCNextButtonClicked()
    {
        StartCoroutine(ShowCompletion());
    }

    public void StopSection()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        if (instructionRoutine != null)
        {
            StopCoroutine(instructionRoutine);
            instructionRoutine = null;
        }

        StopAllCoroutines();

        audioSource.Stop();
        mascotController.HideMascot();

        canClick = false;
    }
    private void OnDisable()
    {
        Debug.Log("Section C Disabled");

        StopAllCoroutines();

        idleCoroutine = null;
        instructionRoutine = null;

        if (audioSource != null)
            audioSource.Stop();

        if (mascotController != null)
            mascotController.HideMascot();
    }
}