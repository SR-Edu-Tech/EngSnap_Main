using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Section D — Finish it Right Game (Page 48 Pen the Word).
/// Picture + incomplete word (e.g. 📮 sta___, 🦨 sku___, 🪺 ne___, 🎁 gi___).
/// Tray holds ending blend tiles.
/// Child drags (or taps) the blend tile into the incomplete word card!
/// Completing Section D triggers the Grand Finale Book Completion Celebration!
/// </summary>
public class U10_A4_FinishItRightController : MonoBehaviour
{
    [Header("UI Elements")]
    public Image            pictureDisplayImage;   // Picture item
    public TextMeshProUGUI incompleteWordText;    // e.g. "sta___"
    public Transform        blendTrayContainer;    // Tray holding blend option tiles
    public GameObject       tilePrefab;            // Option tile prefab

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   instructionClip;       // u10_end_game: "Which blend ends the word? Drag it in!"
    public AudioClip   correctChime;
    public AudioClip   wrongShake;
    public AudioClip   completionClip;

    [Header("References")]
    public U10_Manager manager;

    private List<BlendWordData_Phonics_Junior> gameWordList = new List<BlendWordData_Phonics_Junior>();
    private int  currentWordIndex = 0;
    private bool isProcessing     = false;
    private List<GameObject> trayTileObjs = new List<GameObject>();

    private static readonly string[] EndBlendTrayKeys = { "nd", "nk", "nt", "ng", "mp", "st", "sk", "ft", "ct", "pt", "lt", "lk", "ld", "lf", "lp", "lm", "rm", "rn", "rp", "rt", "rd", "rf", "rk", "rl", "mb" };
    private static Sprite roundedTraySprite = null;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SetupActivity(List<BlendWordData_Phonics_Junior> wordList)
    {
        gameWordList     = wordList != null ? wordList : new List<BlendWordData_Phonics_Junior>();
        currentWordIndex = 0;
        isProcessing     = false;

        AutoFindUIElements();

        if (manager != null) manager.HideNextButton();

        if (gameWordList.Count > 0)
        {
            DisplayCurrentWord();
        }

        PlayClip(instructionClip);
    }

    private void DisplayCurrentWord()
    {
        isProcessing = false;
        if (currentWordIndex >= gameWordList.Count) return;

        BlendWordData_Phonics_Junior data = gameWordList[currentWordIndex];

        if (pictureDisplayImage != null)
        {
            if (data.pictureSprite != null)
            {
                pictureDisplayImage.sprite = data.pictureSprite;
                pictureDisplayImage.gameObject.SetActive(true);
            }
            else
            {
                pictureDisplayImage.gameObject.SetActive(false);
            }
        }

        if (incompleteWordText != null)
        {
            incompleteWordText.text = !string.IsNullOrEmpty(data.incompleteWordText)
                ? data.incompleteWordText
                : $"{data.wordText}___";
            incompleteWordText.color = new Color(0.08f, 0.15f, 0.35f, 1f);
            incompleteWordText.fontSize = 58;
        }

        PopulateBlendTray(data.targetBlend);
    }

    private void PopulateBlendTray(string targetBlendKey)
    {
        foreach (GameObject g in trayTileObjs)
            if (g != null) Destroy(g);
        trayTileObjs.Clear();

        Transform container = blendTrayContainer != null ? blendTrayContainer : transform;

        List<string> options = new List<string>();
        options.Add(targetBlendKey.ToLower());

        List<string> pool = new List<string>(EndBlendTrayKeys);
        pool.Remove(targetBlendKey.ToLower());

        while (options.Count < 6 && pool.Count > 0)
        {
            int r = Random.Range(0, pool.Count);
            options.Add(pool[r]);
            pool.RemoveAt(r);
        }

        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int r = Random.Range(i, options.Count);
            options[i] = options[r];
            options[r] = temp;
        }

        foreach (string blendKey in options)
        {
            string key = blendKey;
            GameObject tileObj = InstantiateTrayTile(key, container);
            trayTileObjs.Add(tileObj);
        }
    }

    private GameObject InstantiateTrayTile(string blendKey, Transform parent)
    {
        GameObject obj;
        if (tilePrefab != null)
        {
            obj = Instantiate(tilePrefab, parent);
        }
        else
        {
            obj = new GameObject($"TrayTile_{blendKey}");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<CanvasRenderer>();

            Image bg = obj.AddComponent<Image>();
            bg.sprite = GetOrCreateRoundedSprite();
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(1f, 0.95f, 0.95f, 1.0f);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = blendKey;
            tmp.fontSize = 50;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.15f, 0.15f, 1f);

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        // Attach Draggable Tile component for Drag-and-Drop functionality!
        U10_FinishItRightDraggableTile dragComp = obj.GetComponent<U10_FinishItRightDraggableTile>();
        if (dragComp == null) dragComp = obj.AddComponent<U10_FinishItRightDraggableTile>();

        dragComp.blendKey = blendKey;
        dragComp.targetDropArea = incompleteWordText != null ? incompleteWordText.rectTransform : (pictureDisplayImage != null ? pictureDisplayImage.rectTransform : null);
        dragComp.onTileSubmitted = OnTrayTileSubmitted;

        return obj;
    }

    private void OnTrayTileSubmitted(string selectedBlend, GameObject tileObj)
    {
        if (isProcessing || currentWordIndex >= gameWordList.Count) return;

        BlendWordData_Phonics_Junior data = gameWordList[currentWordIndex];
        bool isCorrect = selectedBlend.ToLower() == data.targetBlend.ToLower();

        if (isCorrect)
        {
            StartCoroutine(HandleCorrectPick(data));
        }
        else
        {
            StartCoroutine(HandleWrongPick(tileObj));
        }
    }

    private IEnumerator HandleCorrectPick(BlendWordData_Phonics_Junior data)
    {
        isProcessing = true;

        if (incompleteWordText != null)
        {
            string pre = data.wordText.Substring(0, Mathf.Max(0, data.wordText.Length - data.targetBlend.Length));
            incompleteWordText.text = $"<b>{pre}</b><color=#D62828><b>{data.targetBlend}</b></color>";
        }

        PlayClip(correctChime);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();

        yield return new WaitForSeconds(1.0f);

        PlayClip(data.wordAudio);

        yield return new WaitForSeconds(1.2f);

        currentWordIndex++;
        if (currentWordIndex < gameWordList.Count)
        {
            DisplayCurrentWord();
        }
        else
        {
            PlayClip(completionClip);
            if (manager != null) manager.ShowFinalBookReward();
        }
    }

    private IEnumerator HandleWrongPick(GameObject tileObj)
    {
        PlayClip(wrongShake);
        RectTransform rt = tileObj.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 origin = rt.anchoredPosition;
        float elapsed  = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            rt.anchoredPosition = origin + new Vector2(Mathf.Sin(elapsed * 50f) * 6f, 0f);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    private static Sprite GetOrCreateRoundedSprite()
    {
        if (roundedTraySprite != null) return roundedTraySprite;

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
        roundedTraySprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedTraySprite;
    }

    private void AutoFindUIElements()
    {
        if (blendTrayContainer == null)
        {
            Transform t = transform.Find("BlendTrayContainer");
            if (t == null) t = transform.Find("TrayContainer");
            if (t != null) blendTrayContainer = t;
            else blendTrayContainer = transform;
        }

        if (incompleteWordText == null)
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.name.Contains("Word") || tmp.name.Contains("Incomplete"))
                {
                    incompleteWordText = tmp;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (instructionClip == null)
            instructionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit 10/Which blend ends the word Drag it in.mp3");
        if (completionClip == null)
            completionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit 10/You finished the whole book You're a Reading Star.mp3");
#endif
        if (instructionClip == null) instructionClip = Resources.Load<AudioClip>("u10_end_game");
        if (correctChime    == null) correctChime    = Resources.Load<AudioClip>("u8_pop");
        if (wrongShake      == null) wrongShake      = Resources.Load<AudioClip>("u8_wrong");
        if (completionClip  == null) completionClip  = Resources.Load<AudioClip>("u10_book_done");
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

/// <summary>
/// Drag-and-Drop Tile Component for Section D.
/// Allows child to drag the tile onto the target word card or tap it!
/// </summary>
public class U10_FinishItRightDraggableTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public string blendKey;
    public RectTransform targetDropArea;
    public System.Action<string, GameObject> onTileSubmitted;

    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalWorldPosition;
    private Transform originalParent;
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup   = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        parentCanvas  = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalParent = transform.parent;
        originalWorldPosition = transform.position;

        if (parentCanvas != null)
            transform.SetParent(parentCanvas.transform, true);

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }
        else
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        bool droppedOnTarget = false;
        if (targetDropArea != null)
        {
            float dist = Vector2.Distance(rectTransform.position, targetDropArea.position);
            if (dist < 250f || RectTransformUtility.RectangleContainsScreenPoint(targetDropArea, eventData.position, eventData.pressEventCamera))
            {
                droppedOnTarget = true;
            }
        }

        if (droppedOnTarget)
        {
            transform.SetParent(originalParent, true);
            transform.position = originalWorldPosition;
            onTileSubmitted?.Invoke(blendKey, gameObject);
        }
        else
        {
            StartCoroutine(ReturnToOriginRoutine());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging)
        {
            onTileSubmitted?.Invoke(blendKey, gameObject);
        }
    }

    private IEnumerator ReturnToOriginRoutine()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, originalWorldPosition, elapsed / 0.25f);
            yield return null;
        }
        transform.SetParent(originalParent, true);
        transform.position = originalWorldPosition;
    }
}
