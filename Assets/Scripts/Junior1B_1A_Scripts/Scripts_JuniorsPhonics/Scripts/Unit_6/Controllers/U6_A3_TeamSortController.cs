using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class U6_A3_TeamSortController : MonoBehaviour
{
    [Header("Team Bins")]
    public List<GameObject> preplacedBins = new List<GameObject>();
    public Transform binsContainer;

    [Header("Draggable Word Pill")]
    public GameObject wordPillPrefab;
    public Transform wordSpawnArea;

    [Header("Pill Sizing (Editor Adjustable)")]
    public bool overridePillSize = true;
    public Vector2 pillCardSize = new Vector2(220f, 160f);

    public AudioSource audioSource;
    public U6_Manager manager;

    private U6_LevelData currentLevel;
    private List<CVCWordData> allWords = new List<CVCWordData>();
    private int currentWordIndex = 0;
    private GameObject activePillObj;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SetupActivity(U6_LevelData levelData)
    {
        currentLevel = levelData;
        currentWordIndex = 0;
        allWords.Clear();

        if (levelData == null || levelData.teams == null) return;

        // Set Bin Labels
        for (int i = 0; i < levelData.teams.Count; i++)
        {
            GameObject binObj = i < preplacedBins.Count && preplacedBins[i] != null
                ? preplacedBins[i]
                : (binsContainer != null && i < binsContainer.childCount ? binsContainer.GetChild(i).gameObject : null);

            if (binObj != null)
            {
                binObj.SetActive(true);
                TextMeshProUGUI label = binObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = levelData.teams[i].teamSpelling;
            }

            if (levelData.teams[i].teamWords != null)
            {
                allWords.AddRange(levelData.teams[i].teamWords);
            }
        }

        // Shuffle words
        for (int i = 0; i < allWords.Count; i++)
        {
            CVCWordData temp = allWords[i];
            int randIndex = Random.Range(i, allWords.Count);
            allWords[i] = allWords[randIndex];
            allWords[randIndex] = temp;
        }

        SpawnNextWord();
    }

    private void SpawnNextWord()
    {
        if (activePillObj != null) Destroy(activePillObj);

        if (currentWordIndex >= allWords.Count)
        {
            Debug.Log("[Activity 3] All word sorting completed!");
            if (manager != null) manager.SetNextButtonState(true);
            return;
        }

        CVCWordData wordData = allWords[currentWordIndex];
        if (wordData == null || wordPillPrefab == null || wordSpawnArea == null) return;

        activePillObj = Instantiate(wordPillPrefab, wordSpawnArea);
        activePillObj.transform.localScale = Vector3.one;
        activePillObj.transform.localPosition = Vector3.zero;

        // Enforce Editor-adjustable size via RectTransform & LayoutElement!
        if (overridePillSize)
        {
            RectTransform rt = activePillObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = pillCardSize;
            }

            LayoutElement le = activePillObj.GetComponent<LayoutElement>();
            if (le == null) le = activePillObj.AddComponent<LayoutElement>();
            le.preferredWidth = pillCardSize.x;
            le.preferredHeight = pillCardSize.y;
            le.minWidth = pillCardSize.x;
            le.minHeight = pillCardSize.y;
        }

        // Find team spelling for letter highlighting
        string teamSpelling = GetTeamSpellingForWord(wordData);
        string formattedWord = HighlightTeamLetters(wordData.word, teamSpelling);

        TextMeshProUGUI[] tmps = activePillObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var label in tmps)
        {
            label.text = formattedWord;
        }

        Text[] uiTexts = activePillObj.GetComponentsInChildren<Text>(true);
        foreach (var uiText in uiTexts)
        {
            uiText.text = wordData.word;
        }

        // Set picture illustration & stretch child image to fill card tile
        Image img = null;
        Transform picTrans = activePillObj.transform.Find("WordPicture");
        if (picTrans == null) picTrans = activePillObj.transform.Find("WordPicture ");
        if (picTrans == null) picTrans = activePillObj.transform.Find("Image");

        if (picTrans != null)
        {
            picTrans.gameObject.SetActive(true);
            img = picTrans.GetComponent<Image>();
        }
        else
        {
            Image[] images = activePillObj.GetComponentsInChildren<Image>(true);
            foreach (var i in images)
            {
                if (i.gameObject != activePillObj)
                {
                    img = i;
                    break;
                }
            }
            if (img == null) img = activePillObj.GetComponent<Image>();
        }

        if (img != null && wordData.wordPicture != null)
        {
            img.gameObject.SetActive(true);
            img.sprite = wordData.wordPicture;
            img.preserveAspect = true;

            // Stretch child image so it fills the card nicely with border padding!
            RectTransform imgRt = img.GetComponent<RectTransform>();
            if (imgRt != null && img.gameObject != activePillObj)
            {
                imgRt.anchorMin = new Vector2(0.1f, 0.1f);
                imgRt.anchorMax = new Vector2(0.9f, 0.9f);
                imgRt.offsetMin = Vector2.zero;
                imgRt.offsetMax = Vector2.zero;
            }
        }

        U6_TeamSortDragPill dragScript = activePillObj.GetComponent<U6_TeamSortDragPill>();
        if (dragScript == null) dragScript = activePillObj.AddComponent<U6_TeamSortDragPill>();
        dragScript.controller = this;

        if (wordData.fullWordAudio != null)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.volume = 1f;
                audioSource.mute = false;
                audioSource.PlayOneShot(wordData.fullWordAudio);
            }
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(wordData.fullWordAudio, camPos);
        }
    }

    private string GetTeamSpellingForWord(CVCWordData wordData)
    {
        if (wordData == null || currentLevel == null || currentLevel.teams == null) return "";
        foreach (var team in currentLevel.teams)
        {
            if (team != null && team.teamWords != null && team.teamWords.Contains(wordData))
            {
                return team.teamSpelling;
            }
        }
        return "";
    }

    private string HighlightTeamLetters(string word, string teamSpelling)
    {
        if (string.IsNullOrEmpty(word)) return "";
        string cleanSpelling = teamSpelling.Replace("_", "").ToLower().Trim();

        if (teamSpelling == "a_e" || teamSpelling == "i_e" || teamSpelling == "o_e" || teamSpelling == "u_e")
        {
            if (word.Length >= 3 && word.EndsWith("e"))
            {
                char firstChar = word[0];
                char vowelChar = word[1];
                string middleChars = word.Substring(1, word.Length - 2);
                return $"{firstChar}<color=#FFD700>{vowelChar}</color>{middleChars.Substring(1)}<color=#FFD700>e</color>";
            }
        }

        if (word.Contains(cleanSpelling))
        {
            return word.Replace(cleanSpelling, $"<color=#FFD700>{cleanSpelling}</color>");
        }

        return word;
    }

    public bool TryDropOnBin(GameObject targetObj, GameObject pillObj, Vector3 startPos)
    {
        if (currentWordIndex >= allWords.Count || currentLevel == null) return false;
        CVCWordData currentWord = allWords[currentWordIndex];

        GameObject hitBin = null;
        string hitSpelling = "";

        // 1. Raycast Hit Check
        Transform current = targetObj != null ? targetObj.transform : null;
        while (current != null)
        {
            for (int i = 0; i < currentLevel.teams.Count; i++)
            {
                U6_LongVowelTeamData team = currentLevel.teams[i];
                GameObject binObj = i < preplacedBins.Count && preplacedBins[i] != null
                    ? preplacedBins[i]
                    : (binsContainer != null && i < binsContainer.childCount ? binsContainer.GetChild(i).gameObject : null);

                if (binObj != null && (current.gameObject == binObj || current.gameObject.name == binObj.name))
                {
                    hitBin = binObj;
                    hitSpelling = team.teamSpelling;
                    break;
                }
            }
            if (hitBin != null) break;
            current = current.parent;
        }

        // 2. Resolution-Independent Bounds Check for Mobile Touch
        if (hitBin == null && pillObj != null)
        {
            float closestDist = float.MaxValue;
            Vector2 pillPos = pillObj.transform.position;

            for (int i = 0; i < currentLevel.teams.Count; i++)
            {
                U6_LongVowelTeamData team = currentLevel.teams[i];
                GameObject binObj = i < preplacedBins.Count && preplacedBins[i] != null
                    ? preplacedBins[i]
                    : (binsContainer != null && i < binsContainer.childCount ? binsContainer.GetChild(i).gameObject : null);

                if (binObj != null)
                {
                    RectTransform binRect = binObj.GetComponent<RectTransform>();
                    if (binRect != null)
                    {
                        Canvas canvas = binObj.GetComponentInParent<Canvas>();
                        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                        if (RectTransformUtility.RectangleContainsScreenPoint(binRect, pillPos, cam))
                        {
                            hitBin = binObj;
                            hitSpelling = team.teamSpelling;
                            break;
                        }
                    }

                    float dist = Vector2.Distance(pillPos, (Vector2)binObj.transform.position);
                    float threshold = Screen.width > 0 ? (Screen.width * 0.25f) : 250f;
                    if (dist < threshold && dist < closestDist)
                    {
                        closestDist = dist;
                        hitBin = binObj;
                        hitSpelling = team.teamSpelling;
                    }
                }
            }
        }

        // 3. Evaluate Drop
        if (hitBin != null)
        {
            bool isCorrect = IsWordInTeam(currentWord, hitSpelling);
            if (isCorrect)
            {
                currentWordIndex++;
                if (audioSource != null && currentWord.fullWordAudio != null)
                {
                    audioSource.spatialBlend = 0f;
                    audioSource.PlayOneShot(currentWord.fullWordAudio);
                }
                MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
                if (mascot != null) mascot.PlayHiAnimation();

                Invoke(nameof(SpawnNextWord), 0.5f);
                return true;
            }
            else
            {
                PlayWrongFeedback();
                return false;
            }
        }

        return false;
    }

    private bool IsWordInTeam(CVCWordData wordData, string teamSpelling)
    {
        if (wordData == null || currentLevel == null) return false;
        foreach (var team in currentLevel.teams)
        {
            if (team != null && team.teamSpelling == teamSpelling)
            {
                if (team.teamWords != null && team.teamWords.Contains(wordData))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void PlayWrongFeedback()
    {
        AudioClip wrongClip = Resources.Load<AudioClip>("Audio/That is incorrect, Try again");
#if UNITY_EDITOR
        if (wrongClip == null) wrongClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/General/That is incorrect, Try again.mp3");
#endif
        if (wrongClip != null)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.volume = 1f;
                audioSource.PlayOneShot(wrongClip);
            }
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(wrongClip, camPos);
        }
    }
}

public class U6_TeamSortDragPill : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 startPosition;
    private Transform originalParent;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    public U6_A3_TeamSortController controller;

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
            transform.SetAsLastSibling();
        }
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Camera cam = eventData.pressEventCamera;
        if (cam == null && rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
        }

        if (rootCanvas != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            cam,
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
            handled = controller.TryDropOnBin(target, gameObject, startPosition);
        }

        if (!handled && controller != null)
        {
            handled = controller.TryDropOnBin(null, gameObject, startPosition);
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
        if (originalParent != null) transform.SetParent(originalParent, true);
    }
}
