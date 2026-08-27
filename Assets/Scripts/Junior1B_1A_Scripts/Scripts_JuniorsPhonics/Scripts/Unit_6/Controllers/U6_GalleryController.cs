using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U6_GalleryController : MonoBehaviour
{
    [Header("Row Containers (a, e, i, o, u)")]
    public Transform contentA;
    public Transform contentE;
    public Transform contentI;
    public Transform contentO;
    public Transform contentU;

    [Header("Card Prefab")]
    public GameObject galleryCardPrefab;

    [Header("Audio & Navigation")]
    public AudioSource audioSource;
    public Button backButton;
    public U6_Manager manager;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    public void PopulateGallery(
        List<CVCWordData> longAWords,
        List<CVCWordData> longEWords,
        List<CVCWordData> longIWords,
        List<CVCWordData> longOWords,
        List<CVCWordData> longUWords)
    {
        PopulateRow(contentA, longAWords);
        PopulateRow(contentE, longEWords);
        PopulateRow(contentI, longIWords);
        PopulateRow(contentO, longOWords);
        PopulateRow(contentU, longUWords);
    }

    [Header("Page 23 Exact Row Words (a, e, i, o, u)")]
    public List<CVCWordData> rowAWords = new List<CVCWordData>();
    public List<CVCWordData> rowEWords = new List<CVCWordData>();
    public List<CVCWordData> rowIWords = new List<CVCWordData>();
    public List<CVCWordData> rowOWords = new List<CVCWordData>();
    public List<CVCWordData> rowUWords = new List<CVCWordData>();

    private void PopulateRow(Transform rowContainer, List<CVCWordData> wordList, int maxCount = 5)
    {
        if (rowContainer == null || galleryCardPrefab == null || wordList == null) return;

        // Clear previous cards
        foreach (Transform child in rowContainer)
        {
            Destroy(child.gameObject);
        }

        int count = Mathf.Min(maxCount, wordList.Count);

        // Spawn max 5 cards per row so it fits on screen matching page 23
        for (int i = 0; i < count; i++)
        {
            CVCWordData wordData = wordList[i];
            if (wordData == null) continue;

            GameObject cardObj = Instantiate(galleryCardPrefab, rowContainer);

            // Set Text label
            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = wordData.word;
                label.raycastTarget = false;
            }

            // Find child Image component for the word picture illustration (preserving root card frame background)
            Image img = null;
            Transform picTrans = cardObj.transform.Find("WordPicture");
            if (picTrans == null) picTrans = cardObj.transform.Find("WordPicture ");

            if (picTrans != null)
            {
                picTrans.gameObject.SetActive(true);
                img = picTrans.GetComponent<Image>();
            }
            else
            {
                // Find any child Image that is NOT the root cardObj Image
                Image[] images = cardObj.GetComponentsInChildren<Image>(true);
                foreach (var childImg in images)
                {
                    if (childImg.gameObject != cardObj)
                    {
                        img = childImg;
                        img.gameObject.SetActive(true);
                        break;
                    }
                }
            }

            if (img != null)
            {
                img.gameObject.SetActive(true);
                if (wordData.wordPicture != null)
                {
                    img.sprite = wordData.wordPicture;
                }
                img.raycastTarget = false; // Allow clicks to pass straight to parent card button!
            }

            // Disable raycastTarget ONLY on text components so labels don't block clicks!
            Graphic[] childGraphics = cardObj.GetComponentsInChildren<Graphic>(true);
            foreach (var g in childGraphics)
            {
                if (g is TextMeshProUGUI || g is Text)
                {
                    g.raycastTarget = false;
                }
            }

            // Bind OnItemClicked to all Button components inside cardObj
            Button[] allButtons = cardObj.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.targetGraphic != null)
                {
                    b.targetGraphic.raycastTarget = true;
                }
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnItemClicked(wordData));
            }

            Button rootBtn = cardObj.GetComponent<Button>();
            if (rootBtn == null) rootBtn = cardObj.AddComponent<Button>();
            Image rootImg = cardObj.GetComponent<Image>();
            if (rootImg != null) rootImg.raycastTarget = true;
            rootBtn.onClick.RemoveAllListeners();
            rootBtn.onClick.AddListener(() => OnItemClicked(wordData));
        }
    }

    public void OnItemClicked(CVCWordData wordData)
    {
        if (wordData == null) return;
        Debug.Log($"[Gallery] Card clicked: '{wordData.word}'!");

        // Ensure AudioSource is active with 2D sound settings
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f; // Force 2D sound for UI
            audioSource.volume = 1f;
            audioSource.mute = false;
        }

        // Play full word audio
        if (wordData.fullWordAudio != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(wordData.fullWordAudio);
            }
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(wordData.fullWordAudio, camPos);
        }
        else
        {
            Debug.LogWarning($"[Gallery] fullWordAudio clip is missing on '{wordData.word}'!");
        }

        // Trigger Mascot Animation
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayHiAnimation();
    }

    private List<CVCWordData> GetWordsFromLevel(U6_LevelData levelData)
    {
        List<CVCWordData> words = new List<CVCWordData>();
        if (levelData != null && levelData.teams != null)
        {
            foreach (var team in levelData.teams)
            {
                if (team != null && team.teamWords != null) words.AddRange(team.teamWords);
            }
        }
        return words;
    }

    public void SetupGallery(U6_LevelData levelData)
    {
        if (levelData == null || galleryCardPrefab == null) return;
        SetupGalleryAll(levelData, null);
    }

    public void SetupGalleryAll(U6_LevelData levelA, U6_LevelData levelE)
    {
        List<CVCWordData> listA = (rowAWords != null && rowAWords.Count > 0) ? rowAWords : GetWordsFromLevel(levelA);
        List<CVCWordData> listE = (rowEWords != null && rowEWords.Count > 0) ? rowEWords : GetWordsFromLevel(levelE);
        List<CVCWordData> listI = (rowIWords != null && rowIWords.Count > 0) ? rowIWords : new List<CVCWordData>();
        List<CVCWordData> listO = (rowOWords != null && rowOWords.Count > 0) ? rowOWords : new List<CVCWordData>();
        List<CVCWordData> listU = (rowUWords != null && rowUWords.Count > 0) ? rowUWords : new List<CVCWordData>();

        if (contentA != null && listA.Count > 0) PopulateRow(contentA, listA, 5);
        if (contentE != null && listE.Count > 0) PopulateRow(contentE, listE, 5);
        if (contentI != null && listI.Count > 0) PopulateRow(contentI, listI, 5);
        if (contentO != null && listO.Count > 0) PopulateRow(contentO, listO, 5);
        if (contentU != null && listU.Count > 0) PopulateRow(contentU, listU, 5);

        if (manager == null) manager = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
        if (manager != null) manager.SetNextButtonState(true);
    }


    private void OnBackClicked()
    {
        if (manager != null)
        {
            manager.ShowLevelSelection();
        }
    }
}
