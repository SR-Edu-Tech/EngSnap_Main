using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Activity3_FamilySortController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI instructionTitleText;
    public Transform houseBasketsContainer;
    public Transform wordPillSpawnArea;
    public GameObject houseBasketPrefab;
    public GameObject wordPillPrefab;
    public AudioSource audioSource;
    public AudioClip chimeSFX;

    [Header("Pre-Placed Houses (Optional Direct Inspector Assignment)")]
    public List<GameObject> preplacedHouses = new List<GameObject>();
    public List<TextMeshProUGUI> houseChunkTexts = new List<TextMeshProUGUI>();

    private List<WordFamilyData> allLevelFamilies = new List<WordFamilyData>();
    private List<WordFamilyData> activeFamilies = new List<WordFamilyData>();
    private List<CVCWordData> allWords = new List<CVCWordData>();
    private int currentWordIndex = 0;
    private int currentRoundIndex = 0;
    private const int MAX_HOUSES_PER_ROUND = 3;
    private GameObject currentPillObj;

    public System.Action OnActivityComplete;

    public void Setup(Unit4LevelData levelData)
    {
        ConfigureLayout();
        if (instructionTitleText != null) instructionTitleText.text = "Drag each word into its matching Family House!";

        allLevelFamilies.Clear();
        if (levelData != null)
        {
            allLevelFamilies.AddRange(levelData.families);
        }

        currentRoundIndex = 0;
        StartRound(currentRoundIndex);
    }

    private void StartRound(int roundIndex)
    {
        activeFamilies.Clear();
        allWords.Clear();

        int startIndex = roundIndex * MAX_HOUSES_PER_ROUND;
        for (int i = startIndex; i < allLevelFamilies.Count && activeFamilies.Count < MAX_HOUSES_PER_ROUND; i++)
        {
            activeFamilies.Add(allLevelFamilies[i]);
            allWords.AddRange(allLevelFamilies[i].familyWords);
        }

        // Shuffle words for this round
        for (int i = 0; i < allWords.Count; i++)
        {
            var temp = allWords[i];
            int rand = Random.Range(i, allWords.Count);
            allWords[i] = allWords[rand];
            allWords[rand] = temp;
        }

        currentWordIndex = 0;
        BuildHouses();
        SpawnCurrentWordPill();
    }

    private void ConfigureLayout()
    {
        if (houseBasketsContainer != null)
        {
            HorizontalLayoutGroup layout = houseBasketsContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = houseBasketsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

            if (layout != null)
            {
                layout.spacing = 35f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }
    }

    private void BuildHouses()
    {
        // 1. Direct Inspector Assignment Mode (If preplacedHouses or houseChunkTexts are assigned in Inspector)
        if (preplacedHouses.Count > 0 || houseChunkTexts.Count > 0)
        {
            for (int i = 0; i < activeFamilies.Count; i++)
            {
                WordFamilyData family = activeFamilies[i];

                if (i < houseChunkTexts.Count && houseChunkTexts[i] != null)
                {
                    houseChunkTexts[i].text = family.chunkName;
                    houseChunkTexts[i].fontSize = 85;
                    houseChunkTexts[i].enableAutoSizing = true;
                    houseChunkTexts[i].fontSizeMin = 50;
                    houseChunkTexts[i].fontSizeMax = 95;
                    houseChunkTexts[i].fontStyle = FontStyles.Bold;
                    houseChunkTexts[i].color = new Color(0.15f, 0.1f, 0.05f, 1f);
                }

                if (i < preplacedHouses.Count && preplacedHouses[i] != null)
                {
                    GameObject houseObj = preplacedHouses[i];
                    houseObj.name = $"House_{family.chunkName}";

                    TextMeshProUGUI tmp = houseObj.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null)
                    {
                        tmp.gameObject.SetActive(true);
                        tmp.enabled = true;
                        tmp.text = family.chunkName;
                        tmp.fontSize = 85;
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMin = 50;
                        tmp.fontSizeMax = 95;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.color = new Color(0.15f, 0.1f, 0.05f, 1f);
                    }

                    Button btn = houseObj.GetComponent<Button>();
                    if (btn == null) btn = houseObj.GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        string targetChunk = family.chunkName;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnHouseClicked(targetChunk, houseObj));
                    }
                }
            }
            return;
        }

        if (houseBasketsContainer == null) return;

        // 2. Container Hierarchy Mode
        List<GameObject> houseObjects = new List<GameObject>();
        if (houseBasketsContainer.childCount >= activeFamilies.Count)
        {
            for (int i = 0; i < houseBasketsContainer.childCount; i++)
            {
                houseObjects.Add(houseBasketsContainer.GetChild(i).gameObject);
            }
        }
        else if (houseBasketPrefab != null)
        {
            foreach (Transform child in houseBasketsContainer) Destroy(child.gameObject);
            foreach (var family in activeFamilies)
            {
                GameObject houseObj = Instantiate(houseBasketPrefab, houseBasketsContainer);
                houseObj.transform.localScale = Vector3.one;

                RectTransform rect = houseObj.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(220f, 180f);

                LayoutElement layoutElement = houseObj.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = houseObj.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 220f;
                layoutElement.preferredHeight = 180f;
                layoutElement.minWidth = 200f;
                layoutElement.minHeight = 160f;

                houseObjects.Add(houseObj);
            }
        }

        // Configure houses with chunk labels and drop listeners
        for (int i = 0; i < activeFamilies.Count && i < houseObjects.Count; i++)
        {
            GameObject houseObj = houseObjects[i];
            WordFamilyData family = activeFamilies[i];
            houseObj.name = $"House_{family.chunkName}";

            // If house is using generated prefab with blue tile, make background transparent so wooden house artwork shows!
            Image img = houseObj.GetComponent<Image>();
            if (img != null && houseObj.transform.childCount > 0)
            {
                img.color = new Color(1f, 1f, 1f, 0f); // Make blue background box transparent
            }

            TextMeshProUGUI tmp = houseObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.gameObject.SetActive(true);
                tmp.enabled = true;
                tmp.text = family.chunkName;
                tmp.fontSize = 85;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 50;
                tmp.fontSizeMax = 95;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.15f, 0.1f, 0.05f, 1f); // Dark bold contrast text for wooden sign
            }

            Button btn = houseObj.GetComponent<Button>();
            if (btn == null) btn = houseObj.GetComponentInChildren<Button>();
            if (btn != null)
            {
                string targetChunk = family.chunkName;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnHouseClicked(targetChunk, houseObj));
            }
        }
    }

    private void SpawnCurrentWordPill()
    {
        if (wordPillSpawnArea == null || wordPillPrefab == null) return;
        foreach (Transform child in wordPillSpawnArea) Destroy(child.gameObject);

        if (currentWordIndex >= allWords.Count)
        {
            int nextRoundIndex = currentRoundIndex + 1;
            if (nextRoundIndex * MAX_HOUSES_PER_ROUND < allLevelFamilies.Count)
            {
                currentRoundIndex++;
                StartRound(currentRoundIndex);
            }
            else
            {
                OnActivityComplete?.Invoke();
            }
            return;
        }

        CVCWordData wordData = allWords[currentWordIndex];

        currentPillObj = Instantiate(wordPillPrefab, wordPillSpawnArea);
        currentPillObj.transform.localPosition = Vector3.zero;
        currentPillObj.transform.localScale = Vector3.one;

        RectTransform rect = currentPillObj.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector2(240f, 80f);

        LayoutElement layoutElement = currentPillObj.GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = currentPillObj.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 240f;
        layoutElement.preferredHeight = 80f;

        // Format text (supports TextMeshProUGUI, legacy UI Text, and creates fallback if missing)
        TextMeshProUGUI tmp = currentPillObj.GetComponentInChildren<TextMeshProUGUI>(true);
        Text legacyText = currentPillObj.GetComponentInChildren<Text>(true);

        string displayWord = (wordData != null && !string.IsNullOrEmpty(wordData.word)) ? wordData.word.ToLower() : "word";

        if (tmp != null)
        {
            tmp.gameObject.SetActive(true);
            tmp.enabled = true;
            tmp.text = displayWord;
            tmp.fontSize = 54;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark contrast text
            tmp.alignment = TextAlignmentOptions.Center;
        }
        else if (legacyText != null)
        {
            legacyText.gameObject.SetActive(true);
            legacyText.enabled = true;
            legacyText.text = displayWord;
            legacyText.fontSize = 42;
            legacyText.color = Color.black;
            legacyText.alignment = TextAnchor.MiddleCenter;
        }
        else
        {
            // Dynamically create TextMeshProUGUI component if prefab has no text child
            GameObject textObj = new GameObject("PillText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(currentPillObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = displayWord;
            tmp.fontSize = 54;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // Optional Picture Support if child image named "Pic" or "Icon" exists
        Image[] childImages = currentPillObj.GetComponentsInChildren<Image>(true);
        foreach (var img in childImages)
        {
            if (img.gameObject != currentPillObj && img.gameObject.name.ToLower().Contains("pic"))
            {
                if (wordData != null && wordData.wordPicture != null)
                {
                    img.sprite = wordData.wordPicture;
                    img.gameObject.SetActive(true);
                }
                else
                {
                    img.gameObject.SetActive(false);
                }
            }
        }

        // Add Drag-and-Drop script
        FamilySortDragPill dragHandler = currentPillObj.GetComponent<FamilySortDragPill>();
        if (dragHandler == null) dragHandler = currentPillObj.AddComponent<FamilySortDragPill>();
        dragHandler.controller = this;
        dragHandler.wordChunk = wordData.word.Length >= 2 ? wordData.word.Substring(1).ToLower() : "";

        // Button listener to replay sound cleanly on tap
        Button pillBtn = currentPillObj.GetComponent<Button>();
        if (pillBtn == null) pillBtn = currentPillObj.AddComponent<Button>();
        pillBtn.onClick.RemoveAllListeners();
        pillBtn.onClick.AddListener(() => {
            if (wordData != null) PlayAudioClean(wordData.fullWordAudio);
        });

        // Mascot says word aloud on spawn cleanly without overlap
        if (wordData != null && wordData.fullWordAudio != null)
        {
            PlayAudioClean(wordData.fullWordAudio);
        }
    }

    private string GetWordChunk(CVCWordData wordData)
    {
        if (wordData == null || string.IsNullOrEmpty(wordData.word)) return "";
        string w = wordData.word.ToLower().Trim().Replace("-", "");
        if (w.Length <= 2) return w;

        // Find index of first vowel ('a', 'e', 'i', 'o', 'u') to correctly extract ending chunk for words like "thus" (th-us)
        for (int i = 0; i < w.Length; i++)
        {
            char c = w[i];
            if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u')
            {
                return w.Substring(i);
            }
        }

        return w.Substring(1);
    }

    public AudioClip wrongSFX;

    public bool TryDropOnHouse(GameObject targetObj, GameObject pillObj, Vector3 startPos)
    {
        if (currentWordIndex >= allWords.Count) return false;
        CVCWordData currentWord = allWords[currentWordIndex];
        string wordChunk = GetWordChunk(currentWord).Replace("-", "").ToLower().Trim();

        GameObject hitHouse = null;
        string hitChunk = "";

        // 1. Check exact house raycasted
        Transform current = targetObj != null ? targetObj.transform : null;
        while (current != null)
        {
            // Stop searching upward if we reach the container parent!
            if (houseBasketsContainer != null && current.gameObject == houseBasketsContainer.gameObject)
            {
                break;
            }

            for (int i = 0; i < activeFamilies.Count; i++)
            {
                WordFamilyData family = activeFamilies[i];
                GameObject houseObj = (i < preplacedHouses.Count && preplacedHouses[i] != null) 
                    ? preplacedHouses[i] 
                    : (houseBasketsContainer != null && i < houseBasketsContainer.childCount ? houseBasketsContainer.GetChild(i).gameObject : null);

                if (houseObj != null && (current.gameObject == houseObj || current.gameObject.name == houseObj.name || current.gameObject.name.Contains(family.chunkName)))
                {
                    hitHouse = houseObj;
                    hitChunk = family.chunkName.Replace("-", "").ToLower().Trim();
                    break;
                }
            }
            if (hitHouse != null) break;
            current = current.parent;
        }

        // 2. Resolution-Independent Proximity & Rect Check for Mobile Touch Screens
        if (hitHouse == null && pillObj != null)
        {
            float closestDist = float.MaxValue;
            Vector2 pillPos = pillObj.transform.position;

            for (int i = 0; i < activeFamilies.Count; i++)
            {
                WordFamilyData family = activeFamilies[i];
                GameObject houseObj = (i < preplacedHouses.Count && preplacedHouses[i] != null) 
                    ? preplacedHouses[i] 
                    : (houseBasketsContainer != null && i < houseBasketsContainer.childCount ? houseBasketsContainer.GetChild(i).gameObject : null);

                if (houseObj != null)
                {
                    RectTransform houseRect = houseObj.GetComponent<RectTransform>();
                    bool isInsideRect = false;

                    if (houseRect != null)
                    {
                        Canvas canvas = houseObj.GetComponentInParent<Canvas>();
                        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                        isInsideRect = RectTransformUtility.RectangleContainsScreenPoint(houseRect, pillPos, cam);
                    }

                    if (isInsideRect)
                    {
                        hitHouse = houseObj;
                        hitChunk = family.chunkName.Replace("-", "").ToLower().Trim();
                        break;
                    }

                    float dist = Vector2.Distance(pillPos, (Vector2)houseObj.transform.position);
                    float threshold = Screen.width > 0 ? (Screen.width * 0.25f) : 250f;

                    if (dist < threshold && dist < closestDist)
                    {
                        closestDist = dist;
                        hitHouse = houseObj;
                        hitChunk = family.chunkName.Replace("-", "").ToLower().Trim();
                    }
                }
            }
        }

        // 3. Evaluate Drop Correctness
        if (hitHouse != null)
        {
            if (hitChunk == wordChunk)
            {
                // Correct Drop!
                StartCoroutine(CorrectDropRoutine(hitHouse));
                return true;
            }
            else
            {
                // Wrong Drop! Play "Try Again" SFX and reject drop so pill snaps back!
                PlayWrongDropFeedback();
                return false;
            }
        }

        return false;
    }

    public void PlayAudioClean(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void PlayWrongDropFeedback()
    {
        AudioClip clipToPlay = wrongSFX;
        if (clipToPlay == null)
        {
            clipToPlay = Resources.Load<AudioClip>("Audio/That is incorrect, Try again");
        }
#if UNITY_EDITOR
        if (clipToPlay == null)
        {
            clipToPlay = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/General/That is incorrect, Try again.mp3");
        }
#endif
        if (clipToPlay != null)
        {
            PlayAudioClean(clipToPlay);
        }

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayHiAnimation();
    }

    public void OnHouseClicked(string clickedChunk, GameObject houseObj = null)
    {
        if (currentWordIndex >= allWords.Count) return;
        CVCWordData currentWord = allWords[currentWordIndex];
        string wordChunk = GetWordChunk(currentWord);

        string cleanClickedChunk = clickedChunk.Replace("-", "").ToLower().Trim();
        string cleanWordChunk = wordChunk.Replace("-", "").ToLower().Trim();

        if (cleanClickedChunk == cleanWordChunk)
        {
            // Correct Family House!
            StartCoroutine(CorrectDropRoutine(houseObj));
        }
        else
        {
            // Wrong Family House
            PlayWrongDropFeedback();
        }
    }

    private IEnumerator CorrectDropRoutine(GameObject houseObj)
    {
        if (chimeSFX != null)
        {
            PlayAudioClean(chimeSFX);
        }

        // House Light Up Scale Pop
        if (houseObj != null)
        {
            StartCoroutine(LightUpHouseRoutine(houseObj));
        }

        // Tuck word pill into house
        if (currentPillObj != null && houseObj != null)
        {
            Vector3 startPillPos = currentPillObj.transform.position;
            Vector3 targetPillPos = houseObj.transform.position;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (currentPillObj != null)
                {
                    currentPillObj.transform.position = Vector3.Lerp(startPillPos, targetPillPos, t);
                    currentPillObj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                }
                yield return null;
            }
        }

        currentWordIndex++;
        float delay = (chimeSFX != null) ? Mathf.Max(0.5f, chimeSFX.length + 0.1f) : 0.5f;
        yield return new WaitForSeconds(delay);
        SpawnCurrentWordPill();
    }

    private IEnumerator LightUpHouseRoutine(GameObject houseObj)
    {
        Vector3 originalScale = houseObj.transform.localScale;
        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            houseObj.transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.15f, t);
            yield return null;
        }
        houseObj.transform.localScale = originalScale;
    }
}

public class FamilySortDragPill : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 startPosition;
    private Transform originalParent;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    public Activity3_FamilySortController controller;
    public string wordChunk = "";

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = transform.position;
        originalParent = transform.parent;

        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling(); // Render ON TOP of all houses and UI!
        }
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 worldPoint))
        {
            transform.position = worldPoint;
        }
        else
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        bool handled = false;

        if (target != null && controller != null)
        {
            handled = controller.TryDropOnHouse(target, gameObject, startPosition);
        }

        // Essential Mobile Fallback: pointerCurrentRaycast can be null on touch release on mobile screens!
        if (!handled && controller != null)
        {
            handled = controller.TryDropOnHouse(null, gameObject, startPosition);
        }

        if (!handled)
        {
            StartCoroutine(ReturnRoutine(startPosition));
        }
    }

    private IEnumerator ReturnRoutine(Vector3 targetPos)
    {
        Vector3 currentPos = transform.position;
        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(currentPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;
        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
        }
    }
}
