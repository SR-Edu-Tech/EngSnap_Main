using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U6_A2_PictureMatchController : MonoBehaviour
{
    [Header("Containers & Prefabs (Dynamic Setup)")]
    public Transform pictureContainer;
    public Transform wordContainer;
    public GameObject pictureCardPrefab;
    public GameObject wordCardPrefab;

    [Header("Card Sizing (Editor Adjustable)")]
    public bool overrideCardSizes = true;
    public Vector2 pictureCardSize = new Vector2(180f, 180f);
    public Vector2 wordCardSize = new Vector2(250f, 90f);

    [Header("UI Slots (Fallback Setup)")]
    public List<Transform> pictureSlots = new List<Transform>();
    public List<Transform> wordSlots = new List<Transform>();

    public AudioSource audioSource;
    public U6_Manager manager;

    private U6_LevelData currentLevel;
    private List<CVCWordData> currentBatchWords = new List<CVCWordData>();
    private CVCWordData selectedWordData;
    private GameObject selectedWordCardObj;
    private int currentRoundStartIndex = 0;
    private int roundMatchedCount = 0;
    private int targetRoundCount = 4;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SetupActivity(U6_LevelData levelData)
    {
        currentLevel = levelData;
        currentRoundStartIndex = 0;
        roundMatchedCount = 0;
        selectedWordData = null;
        selectedWordCardObj = null;
        currentBatchWords.Clear();

        if (levelData == null || levelData.teams == null) return;

        // Collect words across teams
        foreach (var team in levelData.teams)
        {
            if (team != null && team.teamWords != null)
            {
                currentBatchWords.AddRange(team.teamWords);
            }
        }

        // Shuffle word batch
        for (int i = 0; i < currentBatchWords.Count; i++)
        {
            CVCWordData temp = currentBatchWords[i];
            int randIndex = Random.Range(i, currentBatchWords.Count);
            currentBatchWords[i] = currentBatchWords[randIndex];
            currentBatchWords[randIndex] = temp;
        }

        LoadCurrentRound();
    }

    private void LoadCurrentRound()
    {
        selectedWordData = null;
        selectedWordCardObj = null;

        if (currentRoundStartIndex >= currentBatchWords.Count)
        {
            Debug.Log("[Activity 2] All rounds completed!");
            if (manager != null) manager.SetNextButtonState(true);
            return;
        }

        targetRoundCount = Mathf.Min(4, currentBatchWords.Count - currentRoundStartIndex);
        List<CVCWordData> roundWords = currentBatchWords.GetRange(currentRoundStartIndex, targetRoundCount);

        // Shuffle pictures and word positions independently
        List<CVCWordData> shuffledPictures = new List<CVCWordData>(roundWords);
        List<CVCWordData> shuffledWords = new List<CVCWordData>(roundWords);

        for (int i = 0; i < shuffledPictures.Count; i++)
        {
            int r1 = Random.Range(i, shuffledPictures.Count);
            CVCWordData t1 = shuffledPictures[i];
            shuffledPictures[i] = shuffledPictures[r1];
            shuffledPictures[r1] = t1;

            int r2 = Random.Range(i, shuffledWords.Count);
            CVCWordData t2 = shuffledWords[i];
            shuffledWords[i] = shuffledWords[r2];
            shuffledWords[r2] = t2;
        }

        roundMatchedCount = 0;

        // Setup Picture Targets
        SetupPictures(shuffledPictures);

        // Setup Word Cards
        SetupWords(shuffledWords);
    }

    private void SetupPictures(List<CVCWordData> pictureList)
    {
        if (pictureContainer != null && pictureCardPrefab != null)
        {
            foreach (Transform child in pictureContainer) Destroy(child.gameObject);

            foreach (var data in pictureList)
            {
                if (data == null) continue;
                GameObject card = Instantiate(pictureCardPrefab, pictureContainer);
                card.transform.localScale = Vector3.one;
                card.transform.localPosition = Vector3.zero;
                card.SetActive(true);

                // Enforce Editor-adjustable size via RectTransform & LayoutElement!
                if (overrideCardSizes)
                {
                    RectTransform rt = card.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.sizeDelta = pictureCardSize;
                    }

                    LayoutElement le = card.GetComponent<LayoutElement>();
                    if (le == null) le = card.AddComponent<LayoutElement>();
                    le.preferredWidth = pictureCardSize.x;
                    le.preferredHeight = pictureCardSize.y;
                    le.minWidth = pictureCardSize.x;
                    le.minHeight = pictureCardSize.y;
                }

                // Ensure root card tile frame background scales to pictureCardSize
                Image cardBgImg = card.GetComponent<Image>();
                if (cardBgImg != null)
                {
                    cardBgImg.type = Image.Type.Sliced;
                }

                // Hide text labels on picture cards so only the picture illustration displays!
                TextMeshProUGUI[] tmps = card.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps) tmp.gameObject.SetActive(false);

                Text[] uiTexts = card.GetComponentsInChildren<Text>(true);
                foreach (var uiText in uiTexts) uiText.gameObject.SetActive(false);

                // Assign picture sprite to WordPicture child or child Image
                Image img = null;
                Transform picTrans = card.transform.Find("WordPicture");
                if (picTrans == null) picTrans = card.transform.Find("WordPicture ");

                if (picTrans != null)
                {
                    picTrans.gameObject.SetActive(true);
                    img = picTrans.GetComponent<Image>();
                }
                else
                {
                    Image[] images = card.GetComponentsInChildren<Image>(true);
                    foreach (var i in images)
                    {
                        if (i.gameObject != card)
                        {
                            img = i;
                            break;
                        }
                    }
                    if (img == null) img = card.GetComponent<Image>();
                }

                if (img != null)
                {
                    img.gameObject.SetActive(true);
                    if (data.wordPicture != null)
                    {
                        img.sprite = data.wordPicture;
                    }
                    img.raycastTarget = true;
                    img.preserveAspect = true; // Keep picture illustration neatly proportioned!

                    // Set child picture image anchors with comfortable padding inside frame tile
                    RectTransform imgRt = img.GetComponent<RectTransform>();
                    if (imgRt != null && img.gameObject != card)
                    {
                        imgRt.anchorMin = new Vector2(0.15f, 0.15f);
                        imgRt.anchorMax = new Vector2(0.85f, 0.85f);
                        imgRt.offsetMin = Vector2.zero;
                        imgRt.offsetMax = Vector2.zero;
                    }
                }

                Button btn = card.GetComponent<Button>();
                if (btn == null) btn = card.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPictureClicked(data, card));
            }
        }
        else
        {
            // Fallback to inspector slot list
            for (int i = 0; i < pictureSlots.Count; i++)
            {
                Transform slot = pictureSlots[i];
                if (slot == null) continue;

                if (i < pictureList.Count)
                {
                    slot.gameObject.SetActive(true);
                    CVCWordData data = pictureList[i];
                    Image img = slot.GetComponentInChildren<Image>(true);
                    if (img != null && data.wordPicture != null) img.sprite = data.wordPicture;

                    Button btn = slot.GetComponent<Button>();
                    if (btn == null) btn = slot.gameObject.AddComponent<Button>();
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnPictureClicked(data, slot.gameObject));
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }
    }

    private void SetupWords(List<CVCWordData> wordList)
    {
        if (wordContainer != null && wordCardPrefab != null)
        {
            foreach (Transform child in wordContainer) Destroy(child.gameObject);

            foreach (var data in wordList)
            {
                if (data == null) continue;
                GameObject card = Instantiate(wordCardPrefab, wordContainer);
                card.transform.localScale = Vector3.one;
                card.transform.localPosition = Vector3.zero;
                card.SetActive(true);

                // Enforce Editor-adjustable size via RectTransform & LayoutElement!
                if (overrideCardSizes)
                {
                    RectTransform rt = card.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.sizeDelta = wordCardSize;
                    }

                    LayoutElement le = card.GetComponent<LayoutElement>();
                    if (le == null) le = card.AddComponent<LayoutElement>();
                    le.preferredWidth = wordCardSize.x;
                    le.preferredHeight = wordCardSize.y;
                    le.minWidth = wordCardSize.x;
                    le.minHeight = wordCardSize.y;
                }

                // Set text on all text components
                TextMeshProUGUI[] tmps = card.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    tmp.gameObject.SetActive(true);
                    tmp.text = data.word;
                    tmp.raycastTarget = false;
                }

                Text[] uiTexts = card.GetComponentsInChildren<Text>(true);
                foreach (var uiText in uiTexts)
                {
                    uiText.gameObject.SetActive(true);
                    uiText.text = data.word;
                    uiText.raycastTarget = false;
                }

                // Hide child Image/WordPicture components on word cards so only the background pill & text display!
                Transform picTrans = card.transform.Find("WordPicture");
                if (picTrans == null) picTrans = card.transform.Find("WordPicture ");
                if (picTrans == null) picTrans = card.transform.Find("Image");
                if (picTrans != null && picTrans.gameObject != card)
                {
                    picTrans.gameObject.SetActive(false);
                }

                Button btn = card.GetComponent<Button>();
                if (btn == null) btn = card.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnWordClicked(data, card));
            }
        }
        else
        {
            // Fallback to inspector slot list
            for (int i = 0; i < wordSlots.Count; i++)
            {
                Transform slot = wordSlots[i];
                if (slot == null) continue;

                if (i < wordList.Count)
                {
                    slot.gameObject.SetActive(true);
                    CVCWordData data = wordList[i];
                    TextMeshProUGUI label = slot.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null) label.text = data.word;

                    Button btn = slot.GetComponent<Button>();
                    if (btn == null) btn = slot.gameObject.AddComponent<Button>();
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnWordClicked(data, slot.gameObject));
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnWordClicked(CVCWordData data, GameObject cardObj)
    {
        if (data == null) return;
        selectedWordData = data;
        selectedWordCardObj = cardObj;

        // Play word audio
        if (data.fullWordAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(data.fullWordAudio);
        }

        // Pulse selected card
        if (cardObj != null) StartCoroutine(AnimatePop(cardObj));
    }

    private void OnPictureClicked(CVCWordData targetData, GameObject picObj)
    {
        if (selectedWordData == null)
        {
            // If no word selected, play target picture's word audio
            if (targetData != null && targetData.fullWordAudio != null && audioSource != null)
            {
                audioSource.PlayOneShot(targetData.fullWordAudio);
            }
            return;
        }

        if (selectedWordData == targetData)
        {
            // Correct Match!
            roundMatchedCount++;
            Debug.Log($"[Activity 2] Correct match: '{targetData.word}'! ({roundMatchedCount}/{targetRoundCount})");

            if (audioSource != null && targetData.fullWordAudio != null)
            {
                audioSource.PlayOneShot(targetData.fullWordAudio);
            }

            if (picObj != null) StartCoroutine(AnimateSuccess(picObj));
            if (selectedWordCardObj != null) selectedWordCardObj.SetActive(false);

            selectedWordData = null;
            selectedWordCardObj = null;

            if (roundMatchedCount >= targetRoundCount)
            {
                currentRoundStartIndex += targetRoundCount;
                Invoke(nameof(LoadCurrentRound), 1.2f);
            }

            MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
            if (mascot != null) mascot.PlayHiAnimation();
        }
        else
        {
            // Wrong Match Feedback
            if (selectedWordCardObj != null) StartCoroutine(AnimateShake(selectedWordCardObj));
            PlayWrongFeedback();
            selectedWordData = null;
            selectedWordCardObj = null;
        }
    }

    private void PlayWrongFeedback()
    {
        AudioClip wrongClip = Resources.Load<AudioClip>("Audio/That is incorrect, Try again");
#if UNITY_EDITOR
        if (wrongClip == null) wrongClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/General/That is incorrect, Try again.mp3");
#endif
        if (audioSource != null && wrongClip != null) audioSource.PlayOneShot(wrongClip);
    }

    private IEnumerator AnimatePop(GameObject obj)
    {
        Vector3 initialScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            obj.transform.localScale = initialScale * (1f + 0.15f * Mathf.Sin((elapsed / 0.2f) * Mathf.PI));
            yield return null;
        }
        obj.transform.localScale = initialScale;
    }

    private IEnumerator AnimateSuccess(GameObject obj)
    {
        Vector3 initialScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            obj.transform.localScale = initialScale * (1f + 0.25f * Mathf.Sin((elapsed / 0.35f) * Mathf.PI));
            yield return null;
        }
        obj.transform.localScale = initialScale;
    }

    private IEnumerator AnimateShake(GameObject obj)
    {
        Vector3 startPos = obj.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float xOffset = Mathf.Sin(elapsed * 40f) * 8f;
            obj.transform.localPosition = startPos + new Vector3(xOffset, 0, 0);
            yield return null;
        }
        obj.transform.localPosition = startPos;
    }
}
