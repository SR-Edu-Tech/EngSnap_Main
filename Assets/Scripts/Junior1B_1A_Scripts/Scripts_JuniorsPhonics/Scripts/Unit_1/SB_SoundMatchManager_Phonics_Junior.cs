using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class SB_SoundMatchManager_Phonics_Junior : MonoBehaviour
{
    [Header("Letter Data")]
    [SerializeField] private LetterData_Phonics_Junior[] letters;

    [Header("Option Buttons")]
    [SerializeField] private Button option1Button;
    [SerializeField] private Button option2Button;
    [SerializeField] private Button option3Button;

    [Header("Option Images")]
    [SerializeField] private Image option1Image;
    [SerializeField] private Image option2Image;
    [SerializeField] private Image option3Image;

    [Header("Replay")]
    [SerializeField] private Button replayButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    private LetterData_Phonics_Junior correctLetter;
    private LetterData_Phonics_Junior[] currentOptions = new LetterData_Phonics_Junior[3];

    [Header("Feedback Audio")]
    [SerializeField] private AudioClip[] correctFeedbackClips;
    [SerializeField] private AudioClip[] wrongFeedbackClips;

    [SerializeField] private int totalQuestions = 5;

    private int currentQuestion = 0;
    private List<LetterData_Phonics_Junior> availableLetters = new();

    [Header("Sound Match")]
    [SerializeField] private GameObject letterScreen;

    [SerializeField] private Transform dotsContainer;
    [SerializeField] private GameObject dotPrefab;

    [Header("Completion")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private GameObject soundMatchPanel;

    [SerializeField] private MascotController_Phonics_Junior mascotController;

    private bool isProcessingAnswer = false;

    private void Awake()
    {
        EnsureInit();
    }

    private void EnsureInit()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();

        if (mascotController == null) mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);

        if (soundMatchPanel == null) soundMatchPanel = gameObject;

        if (completionPanel == null && transform.parent != null)
        {
            Transform t = transform.parent.Find("Completion Panel");
            if (t == null) t = transform.parent.Find("CompletionPanel");
            if (t != null) completionPanel = t.gameObject;
        }
    }

    private void Start()
    {
        EnsureInit();

        if (completionPanel != null) completionPanel.SetActive(false);

        CreateDots();

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(PlayCurrentSound);
        }

        if (option1Button != null)
        {
            option1Button.onClick.RemoveAllListeners();
            option1Button.onClick.AddListener(() => CheckAnswer(currentOptions[0]));
        }

        if (option2Button != null)
        {
            option2Button.onClick.RemoveAllListeners();
            option2Button.onClick.AddListener(() => CheckAnswer(currentOptions[1]));
        }

        if (option3Button != null)
        {
            option3Button.onClick.RemoveAllListeners();
            option3Button.onClick.AddListener(() => CheckAnswer(currentOptions[2]));
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (option1Button != null) option1Button.interactable = interactable;
        if (option2Button != null) option2Button.interactable = interactable;
        if (option3Button != null) option3Button.interactable = interactable;
        if (replayButton  != null) replayButton.interactable  = interactable;
    }

    private void GenerateQuestion()
    {
        EnsureInit();

        if (availableLetters == null || availableLetters.Count == 0)
        {
            if (letters != null && letters.Length > 0)
            {
                availableLetters.AddRange(letters);
            }
            else
            {
                FinishGame();
                return;
            }
        }

        // Pick a random correct answer
        int randomIndex = Random.Range(0, availableLetters.Count);

        correctLetter = availableLetters[randomIndex];
        availableLetters.RemoveAt(randomIndex);

        // Store correct answer in first slot
        currentOptions[0] = correctLetter;

        // Fill remaining two slots with different random letters
        int count = 1;

        while (count < 3 && letters != null && letters.Length > 0)
        {
            LetterData_Phonics_Junior randomLetter = letters[Random.Range(0, letters.Length)];

            bool alreadyExists = false;

            for (int i = 0; i < count; i++)
            {
                if (currentOptions[i] == randomLetter)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                currentOptions[count] = randomLetter;
                count++;
            }
        }

        // Shuffle the three options
        for (int i = 0; i < currentOptions.Length; i++)
        {
            int shuffleIndex = Random.Range(i, currentOptions.Length);

            LetterData_Phonics_Junior temp = currentOptions[i];
            currentOptions[i] = currentOptions[shuffleIndex];
            currentOptions[shuffleIndex] = temp;
        }

        // Display images safely
        if (option1Image != null && currentOptions[0] != null) option1Image.sprite = currentOptions[0].letterImage;
        if (option2Image != null && currentOptions[1] != null) option2Image.sprite = currentOptions[1].letterImage;
        if (option3Image != null && currentOptions[2] != null) option3Image.sprite = currentOptions[2].letterImage;

        SetButtonsInteractable(true);

        // Play the target sound
        PlayCurrentSound();
    }

    public void StartGame()
    {
        EnsureInit();
        gameObject.SetActive(true);

        isProcessingAnswer = false;
        currentQuestion = 0;

        availableLetters.Clear();
        if (letters != null) availableLetters.AddRange(letters);

        UpdateDots();
        GenerateQuestion();
    }

    private void FinishGame()
    {
        isProcessingAnswer = false;
        if (mascotController != null) mascotController.HideMascot();

        if (soundMatchPanel != null) soundMatchPanel.SetActive(false);
        if (completionPanel  != null) completionPanel.SetActive(true);
    }

    private void PlayCurrentSound()
    {
        if (correctLetter == null || audioSource == null)
            return;

        audioSource.Stop();
        if (correctLetter.letterSoundAudio != null)
        {
            audioSource.clip = correctLetter.letterSoundAudio;
            audioSource.Play();
        }
    }

    private void CheckAnswer(LetterData_Phonics_Junior selectedLetter)
    {
        if (isProcessingAnswer) return;

        Button selectedButton = GetSelectedButton(selectedLetter);

        if (selectedLetter == correctLetter)
        {
            StartCoroutine(PlayCorrectFeedback(selectedButton));
        }
        else
        {
            StartCoroutine(PlayWrongFeedback(selectedButton));
        }
    }

    private Button GetSelectedButton(LetterData_Phonics_Junior selectedLetter)
    {
        if (selectedLetter == currentOptions[0])
            return option1Button;

        if (selectedLetter == currentOptions[1])
            return option2Button;

        return option3Button;
    }

    private IEnumerator PlayCorrectFeedback(Button button)
    {
        isProcessingAnswer = true;
        SetButtonsInteractable(false);

        if (button != null)
        {
            yield return StartCoroutine(FlashButton(button, new Color(0.75f, 1f, 0.75f)));
        }

        AudioClip clip = null;
        if (correctFeedbackClips != null && correctFeedbackClips.Length > 0)
        {
            clip = correctFeedbackClips[Random.Range(0, correctFeedbackClips.Length)];
        }

        if (mascotController != null)
        {
            mascotController.ShowMascot();
            mascotController.PlayHiAnimation();
        }

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        if (mascotController != null)
        {
            mascotController.HideMascot();
        }

        yield return new WaitForSeconds(0.2f);

        currentQuestion++;
        UpdateDots();

        isProcessingAnswer = false;

        if (currentQuestion >= totalQuestions || availableLetters == null || availableLetters.Count == 0)
        {
            FinishGame();
        }
        else
        {
            GenerateQuestion();
        }
    }

    private IEnumerator PlayWrongFeedback(Button button)
    {
        isProcessingAnswer = true;
        SetButtonsInteractable(false);

        if (button != null)
        {
            yield return StartCoroutine(FlashButton(button, new Color(1f, 0.75f, 0.75f)));
        }

        AudioClip clip = null;
        if (wrongFeedbackClips != null && wrongFeedbackClips.Length > 0)
        {
            clip = wrongFeedbackClips[Random.Range(0, wrongFeedbackClips.Length)];
        }

        if (mascotController != null)
        {
            mascotController.ShowMascot();
            mascotController.PlayHiAnimation();
        }

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        if (mascotController != null)
        {
            mascotController.HideMascot();
        }

        SetButtonsInteractable(true);
        isProcessingAnswer = false;
    }

    private void CreateDots()
    {
        if (dotsContainer == null) return;

        foreach (Transform child in dotsContainer)
        {
            if (child != null) Destroy(child.gameObject);
        }

        if (dotPrefab == null) return;

        for (int i = 0; i < totalQuestions; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsContainer);
            if (dot != null)
            {
                Image img = dot.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }
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
            if (image != null) image.color = Color.Lerp(originalColor, flashColor, timer / duration);
            yield return null;
        }

        timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (image != null) image.color = Color.Lerp(flashColor, originalColor, timer / duration);
            yield return null;
        }

        if (image != null) image.color = originalColor;
    }

    private void UpdateDots()
    {
        if (dotsContainer == null) return;

        for (int i = 0; i < dotsContainer.childCount; i++)
        {
            Transform child = dotsContainer.GetChild(i);
            if (child == null) continue;
            Image dot = child.GetComponent<Image>();
            if (dot == null) continue;

            if (i < currentQuestion)
                dot.color = Color.green;
            else if (i == currentQuestion)
                dot.color = Color.yellow;
            else
                dot.color = Color.white;
        }
    }
}