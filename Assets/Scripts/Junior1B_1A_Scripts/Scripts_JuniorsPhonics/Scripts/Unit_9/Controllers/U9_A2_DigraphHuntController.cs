using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Activity 2 — Digraph Hunt (Spot the Digraph, Pages 33–40).
/// Whole word appears as clickable character tiles (e.g. [s] [h] [e] [l] [l]).
/// Mascot speaks: "Tap the two letters that make one sound."
/// Learner taps the 2 letters forming the digraph.
/// Highlights use soft rounded rectangle borders!
/// Includes optional Check Answer Button, audio guidance, correct/incorrect feedback,
/// and automatic hint pulsing after 2 wrong tries!
/// </summary>
public class U9_A2_DigraphHuntController : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform wordContainer;           // Container holding character tiles
    public GameObject charTilePrefab;         // Single letter tile prefab (leave None for transparent text tiles)
    public TextMeshProUGUI instructionText;   // "Tap the two letters that make one sound!"
    public TextMeshProUGUI titleText;         // Stage Header (e.g. "Digraph Hunt — Stage 1 (ch, sh)")
    public Button    checkAnswerButton;       // Optional "Check Answer" button

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   instructionClip;       // u9_hunt: "Tap the two letters that make one sound."
    public AudioClip   correctSparkle;        // u9_great: "Great! Two letters, one sound!"
    public AudioClip   wrongShake;            // u9_try_again: "Almost! Listen to the sound again."
    public AudioClip   completionClip;

    [Header("References")]
    public U9_Manager manager;

    private List<DigraphWordData> currentWordList = new List<DigraphWordData>();
    private int  currentWordIndex = 0;
    private int  wrongAttempts = 0;
    private List<GameObject> letterTileObjs = new List<GameObject>();
    private HashSet<int> selectedCharIndices = new HashSet<int>();
    private bool isProcessing = false;

    private static Sprite roundedBoxSprite = null;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (checkAnswerButton != null)
        {
            checkAnswerButton.onClick.RemoveAllListeners();
            checkAnswerButton.onClick.AddListener(OnCheckButtonClicked);
        }
    }

    public void SetupActivity(List<DigraphWordData> wordList)
    {
        currentWordList  = wordList != null ? wordList : new List<DigraphWordData>();
        currentWordIndex = 0;
        wrongAttempts    = 0;
        isProcessing     = false;

        AutoFindUIElements();

        // Dynamic Title Header based on Stage
        if (titleText != null && currentWordList.Count > 0)
        {
            string firstDg = currentWordList[0].targetDigraph.ToLower();
            if (firstDg == "ch" || firstDg == "sh")
                titleText.text = "Digraph Hunt — Stage 1 (ch, sh)";
            else if (firstDg == "th" || firstDg == "wh")
                titleText.text = "Digraph Hunt — Stage 2 (th, wh)";
            else if (firstDg == "ck" || firstDg == "nk" || firstDg == "ng")
                titleText.text = "Digraph Hunt — Stage 3 (ck, nk, ng)";
            else
                titleText.text = "Digraph Hunt — Spot the Digraph";

            titleText.color = new Color(0.05f, 0.15f, 0.35f, 1f);
        }

        if (instructionText != null)
        {
            instructionText.text = "Tap the 2 letters that make ONE digraph sound!";
            instructionText.color = new Color(0.1f, 0.2f, 0.4f, 1f);
            instructionText.fontSize = 32;
        }

        if (currentWordList.Count > 0)
        {
            DisplayWord(currentWordList[0]);
        }

        // Mascot Speech & Animation
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }

        PlayClip(instructionClip != null ? instructionClip : Resources.Load<AudioClip>("Tap the two letters that make one sound"));
    }

    private void DisplayWord(DigraphWordData data)
    {
        selectedCharIndices.Clear();
        wrongAttempts = 0;
        isProcessing  = false;

        // Clear previous word tiles
        foreach (GameObject g in letterTileObjs)
            if (g != null) Destroy(g);
        letterTileObjs.Clear();

        Transform container = wordContainer != null ? wordContainer : transform;
        string word = data.wordText;

        for (int i = 0; i < word.Length; i++)
        {
            int charIndex = i;
            char c = word[i];
            GameObject tileObj = InstantiateCharTile(c, charIndex, container);
            letterTileObjs.Add(tileObj);
        }

        if (checkAnswerButton != null) checkAnswerButton.interactable = true;

        // Play word audio clip
        PlayClip(data.wordAudio);
    }

    private GameObject InstantiateCharTile(char c, int index, Transform parent)
    {
        GameObject obj;
        if (charTilePrefab != null)
        {
            obj = Instantiate(charTilePrefab, parent);
        }
        else
        {
            // Transparent tile with smooth rounded corner highlight support
            obj = new GameObject($"CharTile_{index}");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<CanvasRenderer>();

            Image bg = obj.AddComponent<Image>();
            bg.sprite = GetOrCreateRoundedSprite();
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(0f, 0f, 0f, 0f); // Transparent background when unselected

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 130f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = c.ToString();
            tmp.fontSize = 96;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0f, 0f, 0f, 1f); // Bold Black Text

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            obj.AddComponent<Button>();
        }

        Button btn = obj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnCharTapped(index, obj));
        }

        return obj;
    }

    private void OnCharTapped(int index, GameObject obj)
    {
        if (isProcessing || currentWordIndex >= currentWordList.Count) return;

        DigraphWordData data = currentWordList[currentWordIndex];
        string word = data.wordText.ToLower();
        string dg   = data.targetDigraph.ToLower();

        // Toggle selection
        if (selectedCharIndices.Contains(index))
        {
            selectedCharIndices.Remove(index);
            SetTileHighlight(obj, false);
        }
        else
        {
            // Maximum selected letters = length of digraph
            if (selectedCharIndices.Count >= dg.Length)
            {
                selectedCharIndices.Clear();
                foreach (GameObject g in letterTileObjs) if (g != null) SetTileHighlight(g, false);
            }

            selectedCharIndices.Add(index);
            SetTileHighlight(obj, true);
        }

        // Auto-check when child selects 2 letters
        if (selectedCharIndices.Count == dg.Length)
        {
            EvaluateSelection();
        }
    }

    private void OnCheckButtonClicked()
    {
        if (!isProcessing) EvaluateSelection();
    }

    private void EvaluateSelection()
    {
        if (currentWordIndex >= currentWordList.Count) return;

        DigraphWordData data = currentWordList[currentWordIndex];
        string word = data.wordText.ToLower();
        string dg   = data.targetDigraph.ToLower();
        int dgStart = word.IndexOf(dg);

        if (dgStart < 0) return;

        int dgEnd = dgStart + dg.Length - 1;

        // Check if selected indices match the exact digraph start & end
        bool isCorrect = true;
        for (int i = dgStart; i <= dgEnd; i++)
        {
            if (!selectedCharIndices.Contains(i)) { isCorrect = false; break; }
        }

        if (isCorrect && selectedCharIndices.Count == dg.Length)
        {
            StartCoroutine(HandleCorrectDigraphFound(dgStart, dgEnd));
        }
        else
        {
            StartCoroutine(HandleWrongSelection());
        }
    }

    private void SetTileHighlight(GameObject obj, bool isSelected)
    {
        TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = isSelected ? new Color(0.08f, 0.35f, 0.75f, 1f) : new Color(0f, 0f, 0f, 1f); // Soft Blue while choosing
        }

        Image bg = obj.GetComponent<Image>();
        if (bg != null)
        {
            // Smooth rounded soft sky blue background when selected
            bg.color = isSelected ? new Color(0.65f, 0.85f, 1f, 0.5f) : new Color(0f, 0f, 0f, 0f);
        }
    }

    private IEnumerator HandleCorrectDigraphFound(int dgStart, int dgEnd)
    {
        isProcessing = true;

        // Highlight selected digraph letters in bold vibrant GREEN with smooth rounded mint background
        for (int i = dgStart; i <= dgEnd; i++)
        {
            if (i >= 0 && i < letterTileObjs.Count && letterTileObjs[i] != null)
            {
                TextMeshProUGUI tmp = letterTileObjs[i].GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.color = new Color(0.18f, 0.65f, 0.22f, 1f); // Vibrant Emerald Green

                Image bg = letterTileObjs[i].GetComponent<Image>();
                if (bg != null) bg.color = new Color(0.35f, 0.88f, 0.45f, 0.5f); // Soft Mint Green Background
            }
        }

        // Play praise clip ("Great! Two letters, one sound!")
        AudioClip praise = correctSparkle != null ? correctSparkle : Resources.Load<AudioClip>("Great Two letters one sound");
        PlayClip(praise);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();

        yield return new WaitForSeconds(1.6f);

        currentWordIndex++;
        if (currentWordIndex < currentWordList.Count)
        {
            DisplayWord(currentWordList[currentWordIndex]);
        }
        else
        {
            // Completed Digraph Hunt stage!
            AudioClip stageDone = completionClip != null ? completionClip : Resources.Load<AudioClip>("Stage cleared Here's your badge");
            PlayClip(stageDone);

            if (manager != null) manager.ShowNextButton();
        }
    }

    private IEnumerator HandleWrongSelection()
    {
        isProcessing = true;
        wrongAttempts++;

        // Play gentle try again audio ("Almost! Listen to the sound again.")
        AudioClip tryAgain = wrongShake != null ? wrongShake : Resources.Load<AudioClip>("Almost Listen to the sound again");
        PlayClip(tryAgain);

        // Flash selection in vibrant RED for wrong answer
        foreach (int idx in selectedCharIndices)
        {
            if (idx >= 0 && idx < letterTileObjs.Count && letterTileObjs[idx] != null)
            {
                TextMeshProUGUI tmp = letterTileObjs[idx].GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Vibrant Red Text

                Image bg = letterTileObjs[idx].GetComponent<Image>();
                if (bg != null) bg.color = new Color(1f, 0.35f, 0.35f, 0.6f); // Soft Red Background
            }
        }

        yield return new WaitForSeconds(0.8f);

        // Reset selection
        selectedCharIndices.Clear();
        foreach (GameObject g in letterTileObjs)
        {
            if (g != null) SetTileHighlight(g, false);
        }

        // Hint: If 2 wrong tries, pulse correct digraph letters softly!
        if (wrongAttempts >= 2 && currentWordIndex < currentWordList.Count)
        {
            DigraphWordData data = currentWordList[currentWordIndex];
            string word = data.wordText.ToLower();
            string dg   = data.targetDigraph.ToLower();
            int dgStart = word.IndexOf(dg);

            if (dgStart >= 0)
            {
                int dgEnd = dgStart + dg.Length - 1;
                for (int i = dgStart; i <= dgEnd; i++)
                {
                    if (i >= 0 && i < letterTileObjs.Count && letterTileObjs[i] != null)
                    {
                        Image bg = letterTileObjs[i].GetComponent<Image>();
                        if (bg != null) bg.color = new Color(1f, 0.9f, 0.3f, 0.6f); // Pulse rounded yellow hint
                    }
                }
            }
        }

        isProcessing = false;
    }

    private static Sprite GetOrCreateRoundedSprite()
    {
        if (roundedBoxSprite != null) return roundedBoxSprite;

        int width = 64;
        int height = 64;
        int radius = 18;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cornerX = Mathf.Min(x, width - 1 - x);
                int cornerY = Mathf.Min(y, height - 1 - y);

                if (cornerX < radius && cornerY < radius)
                {
                    float dx = radius - cornerX;
                    float dy = radius - cornerY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    else if (dist > radius - 1.5f)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        continue;
                    }
                }
                tex.SetPixel(x, y, Color.white);
            }
        }
        tex.Apply();
        roundedBoxSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedBoxSprite;
    }

    private void AutoFindUIElements()
    {
        if (titleText == null)
        {
            Transform t = transform.Find("TitleText");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }

        if (wordContainer == null)
        {
            Transform t = transform.Find("WordContainer");
            if (t == null) t = transform.Find("RowContainer");
            if (t == null) t = transform.Find("Container");
            if (t != null) wordContainer = t;
            else wordContainer = transform;
        }

        if (instructionText == null)
        {
            Transform t = transform.Find("InstructionText");
            if (t != null) instructionText = t.GetComponent<TextMeshProUGUI>();
        }

        if (checkAnswerButton == null)
        {
            Transform t = transform.Find("CheckAnswerButton");
            if (t == null) t = transform.Find("CheckButton");
            if (t != null) checkAnswerButton = t.GetComponent<Button>();
        }

        if (instructionClip == null) instructionClip = Resources.Load<AudioClip>("Tap the two letters that make one sound");
        if (correctSparkle  == null) correctSparkle  = Resources.Load<AudioClip>("Great Two letters one sound");
        if (wrongShake      == null) wrongShake      = Resources.Load<AudioClip>("Almost Listen to the sound again");
        if (completionClip  == null) completionClip  = Resources.Load<AudioClip>("Stage cleared Here's your badge");
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.spatialBlend = 0f;
        audioSource.volume       = 1f;
        audioSource.PlayOneShot(clip);
    }
}
