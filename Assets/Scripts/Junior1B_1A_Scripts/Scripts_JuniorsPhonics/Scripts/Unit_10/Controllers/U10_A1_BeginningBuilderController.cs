using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Section A — Beginning Blend Builder (Pages 42–44).
/// Builds words starting with a beginning consonant blend.
/// Shows the default image in wordPictureImage when letters are not blended.
/// Upon completion, displays the Beginning Blends Stage 1 Badge!
/// </summary>
public class U10_A1_BeginningBuilderController : MonoBehaviour
{
    [Header("UI Layout & Containers")]
    public Transform chunkContainer;          // Container holding letter/blend tiles
    public GameObject tilePrefab;             // Single tile prefab
    public Button    blendButton;             // "BLEND!" action button
    public TextMeshProUGUI resultWordText;    // Displays merged word (e.g. <color=#0055FF>bl</color>ue)
    public TextMeshProUGUI titleText;         // Stage Header
    public Image           wordPictureImage;  // Word picture graphic
    public Sprite          defaultPictureSprite; // Saved default image assigned to wordPictureImage

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   blendChime;
    public AudioClip   completionClip;

    [Header("References")]
    public U10_Manager manager;

    private List<BlendWordData_Phonics_Junior> currentWordList = new List<BlendWordData_Phonics_Junior>();
    private int  currentWordIndex = 0;
    private bool isWordBlended    = false;
    private List<GameObject> spawnedTileObjs = new List<GameObject>();

    private static Sprite roundedTileSprite = null;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (wordPictureImage != null && defaultPictureSprite == null)
        {
            defaultPictureSprite = wordPictureImage.sprite;
        }

        if (blendButton != null)
        {
            blendButton.onClick.RemoveAllListeners();
            blendButton.onClick.AddListener(OnBlendButtonClicked);
        }
    }

    public void SetupActivity(List<BlendWordData_Phonics_Junior> wordList)
    {
        currentWordList  = wordList != null ? wordList : new List<BlendWordData_Phonics_Junior>();
        currentWordIndex = 0;
        isWordBlended    = false;

        AutoFindUIElements();

        if (titleText != null)
        {
            titleText.text = "Beginning Blend Builder";
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

    private void DisplayWord(BlendWordData_Phonics_Junior data)
    {
        isWordBlended = false;

        if (resultWordText != null)
        {
            resultWordText.text = "???";
            resultWordText.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            resultWordText.fontSize = 72;
        }

        // Display default image assigned to wordPictureImage when letters are not blended
        if (wordPictureImage != null)
        {
            if (defaultPictureSprite != null)
            {
                wordPictureImage.sprite = defaultPictureSprite;
            }
            wordPictureImage.gameObject.SetActive(true);
        }

        // Clear previous tile row
        foreach (GameObject g in spawnedTileObjs)
            if (g != null) Destroy(g);
        spawnedTileObjs.Clear();

        Transform container = chunkContainer != null ? chunkContainer : transform;
        List<string> chunks = (data.blendChunks != null && data.blendChunks.Count > 0)
            ? data.blendChunks
            : BreakWordIntoChunks(data.wordText, data.targetBlend);

        for (int i = 0; i < chunks.Count; i++)
        {
            string chunk = chunks[i];
            bool isBlend = chunk.ToLower() == data.targetBlend.ToLower();

            GameObject tileObj = InstantiateTile(chunk, isBlend, container);
            spawnedTileObjs.Add(tileObj);

            if (i < chunks.Count - 1)
            {
                GameObject arrowObj = InstantiateArrow(container);
                spawnedTileObjs.Add(arrowObj);
            }
        }

        if (blendButton != null) blendButton.interactable = true;
    }

    private GameObject InstantiateTile(string chunk, bool isBlend, Transform parent)
    {
        GameObject obj;
        if (tilePrefab != null)
        {
            obj = Instantiate(tilePrefab, parent);
        }
        else
        {
            obj = new GameObject($"Tile_{chunk}");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<CanvasRenderer>();

            Image bg = obj.AddComponent<Image>();
            bg.sprite = GetOrCreateRoundedSprite();
            bg.type   = Image.Type.Sliced;
            bg.color  = isBlend ? new Color(0.85f, 0.93f, 1f, 1f) : new Color(0.96f, 0.96f, 0.96f, 1f);

            // Dynamic Tile Width based on character length!
            float dynamicWidth = Mathf.Max(100f, 40f + chunk.Length * 32f);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(dynamicWidth, 130f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

            tmp.enableWordWrapping = false; // PREVENT VERTICAL WRAPPING!
            tmp.overflowMode       = TextOverflowModes.Overflow;

            if (isBlend)
            {
                tmp.text = $"<color=#0055FF><b>{chunk}</b></color>";
            }
            else
            {
                tmp.text = $"<color=#000000><b>{chunk}</b></color>";
            }

            tmp.fontSize = 72;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            obj.AddComponent<Button>();
        }

        TextMeshProUGUI tileTmp = obj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tileTmp != null)
        {
            tileTmp.enableWordWrapping = false;
            tileTmp.overflowMode       = TextOverflowModes.Overflow;
            tileTmp.enableAutoSizing   = true;
            tileTmp.fontSizeMin        = 36;
            tileTmp.fontSizeMax        = 72;
        }

        RectTransform tileRt = obj.GetComponent<RectTransform>();
        if (tileRt != null)
        {
            float dynamicWidth = Mathf.Max(tileRt.sizeDelta.x, 40f + chunk.Length * 32f);
            tileRt.sizeDelta = new Vector2(dynamicWidth, tileRt.sizeDelta.y > 0 ? tileRt.sizeDelta.y : 130f);
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
        tmp.color = new Color(0.85f, 0.15f, 0.15f, 1f);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50f, 130f);
        return obj;
    }

    private void OnBlendButtonClicked()
    {
        if (isWordBlended || currentWordIndex >= currentWordList.Count) return;

        isWordBlended = true;
        BlendWordData_Phonics_Junior data = currentWordList[currentWordIndex];

        // 1. Reveal formatted blended word text
        if (resultWordText != null)
        {
            string formatted = FormatWordWithBlueBlend(data.wordText, data.targetBlend);
            resultWordText.text = formatted;
            resultWordText.fontSize = 72;
        }

        // 2. REVEAL REAL WORD IMAGE (Replaces default image!)
        if (wordPictureImage != null && data.pictureSprite != null)
        {
            wordPictureImage.sprite = data.pictureSprite;
            wordPictureImage.gameObject.SetActive(true);
        }

        PlayClip(data.wordAudio != null ? data.wordAudio : blendChime);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();

        StartCoroutine(AdvanceToNextWordRoutine());
    }

    private IEnumerator AdvanceToNextWordRoutine()
    {
        yield return new WaitForSeconds(1.8f);

        currentWordIndex++;
        if (currentWordIndex < currentWordList.Count)
        {
            // Reset to default image for next word!
            DisplayWord(currentWordList[currentWordIndex]);
        }
        else
        {
            PlayClip(completionClip);
            // Show Beginning Blends Stage 1 Badge 🏅!
            if (manager != null) manager.ShowStage1Reward();
        }
    }

    private string FormatWordWithBlueBlend(string word, string blend)
    {
        if (string.IsNullOrEmpty(word)) return "";
        if (string.IsNullOrEmpty(blend)) return $"<color=#000000><b>{word}</b></color>";

        string lowerWord = word.ToLower();
        string lowerBlend = blend.ToLower();
        int idx = lowerWord.IndexOf(lowerBlend);

        if (idx >= 0)
        {
            string pre  = word.Substring(0, idx);
            string bl   = word.Substring(idx, blend.Length);
            string post = word.Substring(idx + blend.Length);
            return $"<color=#000000><b>{pre}</b></color><color=#0055FF><b>{bl}</b></color><color=#000000><b>{post}</b></color>";
        }

        return $"<color=#000000><b>{word}</b></color>";
    }

    private List<string> BreakWordIntoChunks(string word, string blend)
    {
        List<string> list = new List<string>();
        if (string.IsNullOrEmpty(word)) return list;

        string lowerWord  = word.ToLower();
        string lowerBlend = blend != null ? blend.ToLower() : "";
        int idx = !string.IsNullOrEmpty(lowerBlend) ? lowerWord.IndexOf(lowerBlend) : -1;

        if (idx == 0)
        {
            list.Add(word.Substring(0, blend.Length));
            for (int i = blend.Length; i < word.Length; i++) list.Add(word[i].ToString());
        }
        else
        {
            for (int i = 0; i < word.Length; i++) list.Add(word[i].ToString());
        }

        return list;
    }

    private static Sprite GetOrCreateRoundedSprite()
    {
        if (roundedTileSprite != null) return roundedTileSprite;

        int width = 64, height = 64, radius = 18;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cornerX = Mathf.Min(x, width - 1 - x);
                int cornerY = Mathf.Min(y, height - 1 - y);

                if (cornerX < radius && cornerY < radius)
                {
                    float dx = radius - cornerX, dy = radius - cornerY;
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
        roundedTileSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedTileSprite;
    }

    private void AutoFindUIElements()
    {
        if (chunkContainer == null)
        {
            Transform t = transform.Find("ChunkContainer");
            if (t == null) t = transform.Find("TileContainer");
            if (t != null) chunkContainer = t;
        }

        if (blendButton == null)
        {
            Transform t = transform.Find("BlendButton");
            if (t == null) t = transform.Find("Blend_Button");
            if (t != null) blendButton = t.GetComponent<Button>();
        }

        if (resultWordText == null)
        {
            Transform t = transform.Find("ResultWordText");
            if (t == null) t = transform.Find("WordText");
            if (t != null) resultWordText = t.GetComponent<TextMeshProUGUI>();
        }

        if (wordPictureImage == null)
        {
            Transform t = transform.Find("WordPictureImage");
            if (t == null) t = transform.Find("PictureImage");
            if (t != null) wordPictureImage = t.GetComponent<Image>();
        }

        if (wordPictureImage != null && defaultPictureSprite == null)
        {
            defaultPictureSprite = wordPictureImage.sprite;
        }

        if (blendButton != null)
        {
            blendButton.onClick.RemoveAllListeners();
            blendButton.onClick.AddListener(OnBlendButtonClicked);
        }

#if UNITY_EDITOR
        if (completionClip == null)
            completionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit 10/Great blending You heard both sounds.mp3");
#endif
        if (blendChime     == null) blendChime     = Resources.Load<AudioClip>("u8_pop");
        if (completionClip == null) completionClip = Resources.Load<AudioClip>("u8_complete");
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
