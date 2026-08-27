using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U7_GalleryController : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform contentI;
    public Transform contentO;
    public Transform contentU;

    [Header("Card Prefab")]
    public GameObject galleryCardPrefab;

    [Header("Level Data (Fallback)")]
    public U7_LevelData levelLongI;
    public U7_LevelData levelLongO;
    public U7_LevelData levelLongU;

    [Header("Explicit Word Lists (Inspector Override)")]
    public List<CVCWordData> rowIWords = new List<CVCWordData>();
    public List<CVCWordData> rowOWords = new List<CVCWordData>();
    public List<CVCWordData> rowUWords = new List<CVCWordData>();

    public AudioSource audioSource;
    public U7_Manager manager;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SetupGalleryAll(levelLongI, levelLongO, levelLongU);
    }

    private void PopulateRow(Transform container, List<CVCWordData> words, int maxCount = 5)
    {
        if (container == null || words == null || galleryCardPrefab == null) return;

        // Clear existing dynamic children
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        int count = Mathf.Min(words.Count, maxCount);
        for (int i = 0; i < count; i++)
        {
            CVCWordData wordData = words[i];
            if (wordData == null) continue;

            GameObject cardObj = Instantiate(galleryCardPrefab, container);
            cardObj.transform.localScale = Vector3.one;
            cardObj.transform.localPosition = Vector3.zero;

            // Set Label
            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = wordData.word;

            // Set Image
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
                img.raycastTarget = false;
            }

            Graphic[] childGraphics = cardObj.GetComponentsInChildren<Graphic>(true);
            foreach (var g in childGraphics)
            {
                if (g is TextMeshProUGUI || g is Text)
                {
                    g.raycastTarget = false;
                }
            }

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
        Debug.Log($"[Unit 7 Gallery] Card clicked: '{wordData.word}'!");

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
        }

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
            Debug.LogWarning($"[Unit 7 Gallery] fullWordAudio clip is missing on '{wordData.word}'!");
        }

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayHiAnimation();
    }

    private List<CVCWordData> GetWordsFromLevel(U7_LevelData levelData)
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

    public void SetupGalleryAll(U7_LevelData levelI, U7_LevelData levelO, U7_LevelData levelU)
    {
        List<CVCWordData> listI = (rowIWords != null && rowIWords.Count > 0) ? rowIWords : GetWordsFromLevel(levelI);
        List<CVCWordData> listO = (rowOWords != null && rowOWords.Count > 0) ? rowOWords : GetWordsFromLevel(levelO);
        List<CVCWordData> listU = (rowUWords != null && rowUWords.Count > 0) ? rowUWords : GetWordsFromLevel(levelU);

        if (contentI != null && listI.Count > 0) PopulateRow(contentI, listI, 5);
        if (contentO != null && listO.Count > 0) PopulateRow(contentO, listO, 5);
        if (contentU != null && listU.Count > 0) PopulateRow(contentU, listU, 5);
    }
}
