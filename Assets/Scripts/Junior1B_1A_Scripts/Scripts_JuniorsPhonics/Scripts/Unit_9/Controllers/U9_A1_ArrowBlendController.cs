using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Activity 1 — Build the Word (Arrow Blend, Pages 33–40).
/// Reused across Stage 1 (ch, sh), Stage 2 (th, wh), and Stage 3 (ck, nk, ng).
/// Cards have transparent backgrounds with large bold black text (and bold red text for digraphs).
/// Title text updates dynamically to show Stage 1, Stage 2, or Stage 3!
/// </summary>
public class U9_A1_ArrowBlendController : MonoBehaviour
{
    [Header("UI Layout & Containers")]
    public Transform chunkContainer;          // Row container holding chunk tiles & arrows
    public GameObject chunkTilePrefab;        // Single tile prefab (leave None for transparent text tiles)
    public Button    blendButton;             // "Blend" action button
    public TextMeshProUGUI resultWordText;    // Displays merged word (e.g. <color=red>ch</color>ick)
    public TextMeshProUGUI titleText;         // Section Title (e.g. "Build the Word — Stage 3 (ck, nk, ng)")
    public Image           wordPictureImage;  // Optional picture sprite

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   blendChime;
    public AudioClip   completionClip;

    [Header("References")]
    public U9_Manager manager;

    private List<DigraphWordData> currentWordList = new List<DigraphWordData>();
    private int  currentWordIndex = 0;
    private bool isWordBlended    = false;
    private List<GameObject> spawnedChunkObjs = new List<GameObject>();

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (blendButton != null)
        {
            blendButton.onClick.RemoveAllListeners();
            blendButton.onClick.AddListener(OnBlendButtonClicked);
        }
    }

    public void SetupActivity(List<DigraphWordData> wordList)
    {
        currentWordList  = wordList != null ? wordList : new List<DigraphWordData>();
        currentWordIndex = 0;
        isWordBlended    = false;

        AutoFindUIElements();

        // Update Title Text dynamically based on Stage words loaded
        if (titleText != null && currentWordList.Count > 0)
        {
            string firstDg = currentWordList[0].targetDigraph.ToLower();
            if (firstDg == "ch" || firstDg == "sh")
                titleText.text = "Build the Word — Stage 1 (ch, sh)";
            else if (firstDg == "th" || firstDg == "wh")
                titleText.text = "Build the Word — Stage 2 (th, wh)";
            else if (firstDg == "ck" || firstDg == "nk" || firstDg == "ng")
                titleText.text = "Build the Word — Stage 3 (ck, nk, ng)";
            else
                titleText.text = "Build the Word — Arrow Blend";

            titleText.color = new Color(0.05f, 0.15f, 0.35f, 1f);
        }

        if (manager != null) manager.HideNextButton();

        if (currentWordList.Count > 0)
        {
            DisplayWord(currentWordList[0]);
        }

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }
    }

    private void DisplayWord(DigraphWordData data)
    {
        isWordBlended = false;

        if (resultWordText != null)
        {
            resultWordText.text = "";
            resultWordText.color = Color.black;
            resultWordText.fontSize = 72;
        }

        if (wordPictureImage != null)
        {
            if (data.pictureSprite != null)
            {
                wordPictureImage.sprite = data.pictureSprite;
                wordPictureImage.gameObject.SetActive(true);
            }
            else
            {
                wordPictureImage.gameObject.SetActive(false);
            }
        }

        // Clear previous chunk row
        foreach (GameObject g in spawnedChunkObjs)
            if (g != null) Destroy(g);
        spawnedChunkObjs.Clear();

        Transform container = chunkContainer != null ? chunkContainer : transform;
        List<string> chunks = (data.arrowChunks != null && data.arrowChunks.Count > 0)
            ? data.arrowChunks
            : BreakWordIntoChunks(data.wordText, data.targetDigraph);

        for (int i = 0; i < chunks.Count; i++)
        {
            string chunk = chunks[i];
            bool isDigraph = chunk.ToLower() == data.targetDigraph.ToLower();

            // Spawn Chunk Tile
            GameObject tileObj = InstantiateChunkTile(chunk, isDigraph, container);
            spawnedChunkObjs.Add(tileObj);

            // Spawn Arrow Indicator -> between chunks
            if (i < chunks.Count - 1)
            {
                GameObject arrowObj = InstantiateArrow(container);
                spawnedChunkObjs.Add(arrowObj);
            }
        }

        if (blendButton != null) blendButton.interactable = true;
    }

    private GameObject InstantiateChunkTile(string chunk, bool isDigraph, Transform parent)
    {
        GameObject obj;
        if (chunkTilePrefab != null)
        {
            obj = Instantiate(chunkTilePrefab, parent);
        }
        else
        {
            // Transparent tile card with large bold black text (bold red for digraphs)
            obj = new GameObject($"Chunk_{chunk}");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<CanvasRenderer>();

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f); // 100% Transparent background

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110f, 120f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

            if (isDigraph)
            {
                tmp.text = $"<color=#D62828><b>{chunk}</b></color>"; // Bold Vibrant Red Digraph
            }
            else
            {
                tmp.text = $"<color=#000000><b>{chunk}</b></color>"; // Bold Clear Black Sound Letter
            }

            tmp.fontSize = 72;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            obj.AddComponent<Button>();
        }

        Button btn = obj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => PlayClip(blendChime));
        }

        return obj;
    }

    private GameObject InstantiateArrow(Transform parent)
    {
        GameObject obj = new GameObject("Arrow");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<CanvasRenderer>();

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = "→";
        tmp.fontSize = 64;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Vibrant Red Arrow

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 120f);
        return obj;
    }

    private void OnBlendButtonClicked()
    {
        if (isWordBlended || currentWordIndex >= currentWordList.Count) return;

        isWordBlended = true;
        DigraphWordData data = currentWordList[currentWordIndex];

        // Format result word with Digraph highlighted in RED and rest in BLACK!
        if (resultWordText != null)
        {
            string formatted = FormatWordWithRedDigraph(data.wordText, data.targetDigraph);
            resultWordText.text = formatted;
            resultWordText.color = Color.black;
            resultWordText.fontSize = 72;
        }

        PlayClip(data.wordAudio != null ? data.wordAudio : blendChime);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();

        StartCoroutine(AdvanceToNextWordRoutine());
    }

    private IEnumerator AdvanceToNextWordRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        currentWordIndex++;
        if (currentWordIndex < currentWordList.Count)
        {
            DisplayWord(currentWordList[currentWordIndex]);
        }
        else
        {
            // Completed all words in current stage!
            PlayClip(completionClip);
            if (manager != null) manager.ShowNextButton();
        }
    }

    private string FormatWordWithRedDigraph(string word, string digraph)
    {
        if (string.IsNullOrEmpty(word)) return "";
        if (string.IsNullOrEmpty(digraph)) return $"<color=#000000><b>{word}</b></color>";

        string lowerWord = word.ToLower();
        string lowerDg   = digraph.ToLower();
        int idx = lowerWord.IndexOf(lowerDg);

        if (idx >= 0)
        {
            string pre  = word.Substring(0, idx);
            string dg   = word.Substring(idx, digraph.Length);
            string post = word.Substring(idx + digraph.Length);
            return $"<color=#000000><b>{pre}</b></color><color=#D62828><b>{dg}</b></color><color=#000000><b>{post}</b></color>";
        }

        return $"<color=#000000><b>{word}</b></color>";
    }

    private List<string> BreakWordIntoChunks(string word, string digraph)
    {
        List<string> list = new List<string>();
        if (string.IsNullOrEmpty(word)) return list;

        string lowerWord = word.ToLower();
        string lowerDg   = digraph != null ? digraph.ToLower() : "";
        int idx = !string.IsNullOrEmpty(lowerDg) ? lowerWord.IndexOf(lowerDg) : -1;

        if (idx == 0)
        {
            list.Add(word.Substring(0, digraph.Length));
            for (int i = digraph.Length; i < word.Length; i++) list.Add(word[i].ToString());
        }
        else if (idx > 0)
        {
            for (int i = 0; i < idx; i++) list.Add(word[i].ToString());
            list.Add(word.Substring(idx, digraph.Length));
            int end = idx + digraph.Length;
            for (int i = end; i < word.Length; i++) list.Add(word[i].ToString());
        }
        else
        {
            for (int i = 0; i < word.Length; i++) list.Add(word[i].ToString());
        }

        return list;
    }

    private void AutoFindUIElements()
    {
        if (titleText == null)
        {
            Transform t = transform.Find("TitleText");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }

        if (chunkContainer == null)
        {
            Transform t = transform.Find("ChunkContainer");
            if (t == null) t = transform.Find("RowContainer");
            if (t == null) t = transform.Find("Container");
            if (t != null) chunkContainer = t;
            else chunkContainer = transform;
        }

        if (blendButton == null)
        {
            Transform t = transform.Find("BlendButton");
            if (t != null) blendButton = t.GetComponent<Button>();
        }

        if (resultWordText == null)
        {
            Transform t = transform.Find("ResultWordText");
            if (t != null) resultWordText = t.GetComponent<TextMeshProUGUI>();
        }

        if (wordPictureImage == null)
        {
            Transform t = transform.Find("WordPictureImage");
            if (t != null) wordPictureImage = t.GetComponent<Image>();
        }

        if (blendChime     == null) blendChime     = Resources.Load<AudioClip>("u8_pop");
        if (completionClip == null) completionClip = Resources.Load<AudioClip>("u8_fanfare");
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
