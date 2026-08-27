using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Activity2_SwapSoundController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI chunkText;
    public Transform firstLetterContainer;
    public GameObject letterButtonPrefab;
    public Image resultPicture;
    public TextMeshProUGUI resultWordText;
    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    private List<CVCWordData> currentWords = new List<CVCWordData>();
    private List<char> usedLettersInFamily = new List<char>();
    private int currentWordIndex = 0;
    public System.Action OnActivityComplete;
    private Sprite placeholderSprite;

    private void Awake()
    {
        CapturePlaceholder();
    }

    private void CapturePlaceholder()
    {
        if (resultPicture != null && placeholderSprite == null && resultPicture.sprite != null)
        {
            placeholderSprite = resultPicture.sprite;
        }
    }

    public void Setup(Unit4LevelData levelData)
    {
        CapturePlaceholder();
        ConfigureLayout();
        currentWords.Clear();
        usedLettersInFamily.Clear();
        if (levelData != null)
        {
            foreach (var family in levelData.families)
            {
                currentWords.AddRange(family.familyWords);
            }
        }
        currentWordIndex = 0;
        LoadWord();
    }

    private void ConfigureLayout()
    {
        if (firstLetterContainer != null)
        {
            HorizontalLayoutGroup layout = firstLetterContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = firstLetterContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

            if (layout != null)
            {
                layout.spacing = 30f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }
    }

    private void LoadWord()
    {
        if (currentWordIndex >= currentWords.Count)
        {
            OnActivityComplete?.Invoke();
            return;
        }

        CVCWordData wordData = currentWords[currentWordIndex];
        string currentChunk = wordData.word.Length >= 2 ? wordData.word.Substring(1) : "";

        // Reset used letters if chunk changes (new family)
        if (currentWordIndex > 0)
        {
            CVCWordData prevWord = currentWords[currentWordIndex - 1];
            string prevChunk = prevWord.word.Length >= 2 ? prevWord.word.Substring(1) : "";
            if (prevChunk != currentChunk)
            {
                usedLettersInFamily.Clear();
            }
        }

        if (chunkText != null)
        {
            chunkText.text = "-" + currentChunk;
        }

        if (resultWordText != null) resultWordText.text = "";
        if (resultPicture != null)
        {
            CapturePlaceholder();
            resultPicture.gameObject.SetActive(true);
            if (placeholderSprite != null)
            {
                resultPicture.sprite = placeholderSprite;
                resultPicture.color = Color.white;
            }
            resultPicture.transform.localScale = Vector3.one;
        }

        // Spawn exactly 3 first-letter buttons (1 correct letter + 2 non-word forming distractors)
        if (firstLetterContainer != null && letterButtonPrefab != null)
        {
            foreach (Transform child in firstLetterContainer) Destroy(child.gameObject);

            List<char> options = new List<char> { wordData.Letter1 };

            // Comprehensive CVC word dictionary to prevent any distractor from forming a real word
            HashSet<string> validCvcWords = new HashSet<string> {
                // Short A
                "cat", "hat", "mat", "rat", "sat", "pat", "bat", "vat", "tat", "fat",
                "can", "man", "ran", "pan", "fan", "tan", "van", "ban",
                "had", "dad", "sad", "mad", "bad", "pad", "rad", "lad", "tad",
                "am", "sam", "ram", "jam", "dam", "ham", "yam",
                "cap", "map", "tap", "nap", "lap", "gap", "sap", "rap",
                "bag", "rag", "tag", "wag", "nag",
                // Short E
                "ten", "men", "hen", "pen", "den", "ben", "zen",
                "bed", "red", "led", "fed", "wed",
                "leg", "peg", "beg", "meg",
                "get", "let", "wet", "set", "net", "pet", "vet", "met",
                "yes"
            };

            char[] candidateDistractors = { 'z', 'v', 'x', 'q', 'j', 'k', 'g', 'w', 'f', 'y', 'b', 'p', 'm', 'n', 'l', 'r', 's', 't' };

            foreach (char d in candidateDistractors)
            {
                if (options.Count >= 3) break;
                if (d == wordData.Letter1) continue;

                string candidateWord = $"{d}{currentChunk}".ToLower();

                // Ensure the candidate letter does NOT form a valid word with this chunk!
                if (!validCvcWords.Contains(candidateWord))
                {
                    if (!options.Contains(d))
                    {
                        options.Add(d);
                    }
                }
            }

            // Shuffle options
            for (int i = 0; i < options.Count; i++)
            {
                char temp = options[i];
                int rand = Random.Range(i, options.Count);
                options[i] = options[rand];
                options[rand] = temp;
            }

            foreach (char c in options)
            {
                GameObject btnObj = Instantiate(letterButtonPrefab, firstLetterContainer);
                btnObj.transform.localScale = Vector3.one;

                TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = c.ToString().ToLower();
                    tmp.fontSize = 54;
                    tmp.fontStyle = FontStyles.Bold;
                }

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    char chosenChar = c;
                    btn.onClick.AddListener(() => OnLetterSelected(chosenChar, wordData, btnObj));
                }
            }
        }
    }

    private void OnLetterSelected(char chosenChar, CVCWordData wordData, GameObject btnObj)
    {
        Image btnImg = btnObj != null ? btnObj.GetComponent<Image>() : null;

        if (chosenChar == wordData.Letter1)
        {
            // Correct Answer: Green Highlight!
            if (btnImg != null) btnImg.color = new Color(0.2f, 0.85f, 0.3f, 1f);

            if (audioSource != null && correctSFX != null)
            {
                audioSource.PlayOneShot(correctSFX);
            }
            StartCoroutine(CorrectLetterRoutine(wordData));
        }
        else
        {
            // Wrong Answer: Red Flash Highlight!
            if (btnImg != null) StartCoroutine(WrongFlashRoutine(btnImg));

            if (audioSource != null && wrongSFX != null)
            {
                audioSource.PlayOneShot(wrongSFX);
            }
            MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
            if (mascot != null) mascot.PlayHiAnimation();
        }
    }

    private IEnumerator WrongFlashRoutine(Image btnImg)
    {
        if (btnImg == null) yield break;
        Color origColor = btnImg.color;
        btnImg.color = new Color(0.95f, 0.25f, 0.25f, 1f); // Vibrant Red Flash
        yield return new WaitForSeconds(0.45f);
        if (btnImg != null)
        {
            btnImg.color = origColor;
        }
    }

    private IEnumerator CorrectLetterRoutine(CVCWordData wordData)
    {
        if (!usedLettersInFamily.Contains(wordData.Letter1))
        {
            usedLettersInFamily.Add(wordData.Letter1);
        }

        // 1. Reveal combined word text (e.g. c + at = cat)
        if (resultWordText != null)
        {
            string chunkStr = wordData.word.Length >= 2 ? wordData.word.Substring(1) : "";
            resultWordText.text = $"{wordData.Letter1} + {chunkStr} = <color=#FFDD00>{wordData.word.ToLower()}</color>";
        }

        // 2. Delight Moment: Picture Scale-Pop Reveal!
        if (resultPicture != null && wordData.wordPicture != null)
        {
            resultPicture.sprite = wordData.wordPicture;
            resultPicture.gameObject.SetActive(true);

            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (resultPicture != null)
                {
                    resultPicture.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.15f, Mathf.PingPong(t * 2f, 1f));
                }
                yield return null;
            }
            if (resultPicture != null)
            {
                resultPicture.transform.localScale = Vector3.one;
            }
        }

        // 3. Audio sequence: c... at... cat!
        if (audioSource != null)
        {
            if (wordData.letter1Sound != null)
            {
                audioSource.PlayOneShot(wordData.letter1Sound);
                yield return new WaitForSeconds(wordData.letter1Sound.length + 0.15f);
            }

            if (wordData.fullWordAudio != null)
            {
                audioSource.PlayOneShot(wordData.fullWordAudio);
                yield return new WaitForSeconds(wordData.fullWordAudio.length + 0.3f);
            }
            else
            {
                yield return new WaitForSeconds(0.6f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.8f);
        }

        // Mascot cheer celebration
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayCelebrationAnimation();

        currentWordIndex++;
        yield return new WaitForSeconds(0.5f);
        LoadWord();
    }
}
