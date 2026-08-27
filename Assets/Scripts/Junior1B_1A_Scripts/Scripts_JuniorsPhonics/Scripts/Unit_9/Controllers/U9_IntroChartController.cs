using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Page 32 — Consonant Digraph Chart (Intro Activity).
/// Matches the exact Page 32 textbook layout:
/// Goal: meet the digraphs and hear that two letters make one sound.
/// Tapping a card plays its single clean audio clip (which already includes the sound and example word, e.g. "/ch/... chain").
/// Sounds stop prior playback so audio NEVER overlaps between card taps.
/// Explored tiles glow. ONLY when ALL cards are clicked, the Next Button is revealed via Manager.
/// </summary>
public class U9_IntroChartController : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform gridContainer;           // Container for digraph chart tiles
    public GameObject digraphTilePrefab;      // Prefab: U9_TxtImgTxt_Prefab

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   introSpeechClip;       // "Sometimes two letters team up to make one brand-new sound Let's meet them.mp3"
    public AudioClip   tileTapChime;

    [Header("References")]
    public U9_Manager manager;

    private List<GameObject> spawnedTiles = new List<GameObject>();
    private HashSet<int>     exploredIndices = new HashSet<int>();
    private Unit9LevelData   currentLevelData;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SetupActivity(Unit9LevelData levelData)
    {
        currentLevelData = levelData;
        exploredIndices.Clear();

        AutoFindUIElements();

        if (manager != null) manager.HideNextButton();

        // Clear previous grid items
        foreach (GameObject g in spawnedTiles)
            if (g != null) Destroy(g);
        spawnedTiles.Clear();

        // Populate tiles
        List<DigraphTileData> list = (levelData != null && levelData.introDigraphs != null && levelData.introDigraphs.Count > 0)
            ? levelData.introDigraphs
            : GetDefaultIntroDigraphs();

        for (int i = 0; i < list.Count; i++)
        {
            int index = i;
            DigraphTileData data = list[i];
            GameObject tileObj = InstantiateTilePrefab(data, index);
            spawnedTiles.Add(tileObj);
        }

        // Mascot Intro
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }

        PlayClip(introSpeechClip);
    }

    private GameObject InstantiateTilePrefab(DigraphTileData data, int index)
    {
        Transform container = gridContainer != null ? gridContainer : transform;
        GameObject obj;

        if (digraphTilePrefab != null)
        {
            obj = Instantiate(digraphTilePrefab, container);
            PopulateAssignedPrefabTile(obj, data);
        }
        else
        {
            // Clean fallback UI tile card matching Page 32 textbook
            obj = new GameObject($"ChartTile_{data.digraphKey}");
            obj.transform.SetParent(container, false);
            obj.AddComponent<CanvasRenderer>();

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.95f, 0.96f, 1.0f, 1.0f);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 110f);

            HorizontalLayoutGroup hlg = obj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15, 15, 10, 10);
            hlg.spacing = 15f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // 1. Digraph Symbol Badge (e.g. "ch-")
            GameObject keyObj = new GameObject("DigraphKeyBadge");
            keyObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI keyTmp = keyObj.AddComponent<TextMeshProUGUI>();
            keyTmp.text = $"<b><color=#0D3B66>{data.digraphKey}</color></b>";
            keyTmp.fontSize = 32;
            keyTmp.alignment = TextAlignmentOptions.Center;
            RectTransform keyRt = keyObj.GetComponent<RectTransform>();
            keyRt.sizeDelta = new Vector2(75f, 60f);

            // 2. Picture Sprite Image
            GameObject imgObj = new GameObject("PictureImg");
            imgObj.transform.SetParent(obj.transform, false);
            Image img = imgObj.AddComponent<Image>();
            img.preserveAspect = true;
            if (data.startWordSprite != null) img.sprite = data.startWordSprite;
            else img.color = new Color(0.85f, 0.9f, 0.95f, 0.4f);
            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.sizeDelta = new Vector2(65f, 65f);

            // 3. Word Label (e.g. "chain")
            GameObject labelObj = new GameObject("WordLabel");
            labelObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = data.startWord;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.12f, 0.12f, 0.3f, 1f);
            RectTransform lblRt = labelObj.GetComponent<RectTransform>();
            lblRt.sizeDelta = new Vector2(110f, 40f);

            obj.AddComponent<Button>();
        }

        // Wire click handler on main object AND any child buttons
        Button[] btns = obj.GetComponentsInChildren<Button>(true);
        if (btns.Length == 0)
        {
            Button b = obj.AddComponent<Button>();
            btns = new Button[] { b };
        }

        foreach (Button btn in btns)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnTileTapped(data, index, obj));
            }
        }

        return obj;
    }

    private void PopulateAssignedPrefabTile(GameObject prefabObj, DigraphTileData data)
    {
        // Target exact child names from U9_TxtImgTxt_Prefab hierarchy
        Transform tLeft  = prefabObj.transform.Find("Text (TMP)");
        Transform tRight = prefabObj.transform.Find("Text (TMP) (1)");
        Transform tImage = prefabObj.transform.Find("Image");

        TextMeshProUGUI tmpLeft  = tLeft != null ? tLeft.GetComponent<TextMeshProUGUI>() : null;
        TextMeshProUGUI tmpRight = tRight != null ? tRight.GetComponent<TextMeshProUGUI>() : null;
        Image           centerImg = tImage != null ? tImage.GetComponent<Image>() : null;

        // Fallback search by type index if child names vary
        if (tmpLeft == null || tmpRight == null)
        {
            TextMeshProUGUI[] tmps = prefabObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length >= 1 && tmpLeft == null)  tmpLeft  = tmps[0];
            if (tmps.Length >= 2 && tmpRight == null) tmpRight = tmps[1];
        }

        if (centerImg == null)
        {
            Image[] imgs = prefabObj.GetComponentsInChildren<Image>(true);
            foreach (Image img in imgs)
            {
                if (img.gameObject != prefabObj) { centerImg = img; break; }
            }
        }

        // 1. Format Left Text (Digraph Symbol e.g. "ch-" or "-ch")
        if (tmpLeft != null)
        {
            tmpLeft.text = $"<b><color=#0D3B66>{data.digraphKey}</color></b>";
            tmpLeft.enableAutoSizing = true;
            tmpLeft.fontSizeMin = 18;
            tmpLeft.fontSizeMax = 32;
            tmpLeft.alignment = TextAlignmentOptions.Center;
        }

        // 2. Format Center Image (Picture Sprite)
        if (centerImg != null)
        {
            Sprite targetSprite = data.startWordSprite != null ? data.startWordSprite : data.digraphIcon;
            if (targetSprite != null)
            {
                centerImg.sprite = targetSprite;
                centerImg.color  = Color.white;
                centerImg.preserveAspect = true;
                centerImg.gameObject.SetActive(true);
            }
            else
            {
                centerImg.color = new Color(1f, 1f, 1f, 0.8f);
                centerImg.gameObject.SetActive(true);
            }
        }

        // 3. Format Right Text (Word Name e.g. "chain", "switch", "trash", "earth", "duck")
        if (tmpRight != null)
        {
            tmpRight.text = $"<b>{data.startWord}</b>";
            tmpRight.enableAutoSizing = true;
            tmpRight.fontSizeMin = 16;
            tmpRight.fontSizeMax = 28;
            tmpRight.alignment = TextAlignmentOptions.Center;
            tmpRight.color = new Color(0.15f, 0.15f, 0.35f, 1f);
        }
    }

    private void OnTileTapped(DigraphTileData data, int index, GameObject obj)
    {
        // 1. Stop any currently playing audio so sounds NEVER overlap
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 2. Mascot cheer animation
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayHiAnimation();

        // 3. Card glow visual feedback
        Image bg = obj.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.85f, 1f, 0.85f, 1f);

        // 4. Play single clean audio clip (digraph sound clip already includes sound + example word!)
        AudioClip clipToPlay = data.digraphSoundClip;
        if (clipToPlay == null) clipToPlay = data.wordAudioClip;
        if (clipToPlay == null && !string.IsNullOrEmpty(data.startWord))
        {
            clipToPlay = FindAudioClipForWord(data.startWord);
        }

        if (clipToPlay != null)
        {
            PlayClip(clipToPlay);
        }

        exploredIndices.Add(index);

        // Require ALL cards to be tapped before revealing Next Button!
        if (spawnedTiles.Count > 0 && exploredIndices.Count >= spawnedTiles.Count)
        {
            if (manager != null) manager.ShowNextButton();
        }
    }

    private AudioClip FindAudioClipForWord(string wordName)
    {
        if (string.IsNullOrEmpty(wordName)) return null;
        string cleanWord = wordName.ToLower().Trim();

        AudioClip clip = Resources.Load<AudioClip>($"Unit 9/sound clips/{cleanWord}");
        if (clip == null) clip = Resources.Load<AudioClip>($"Unit 9/Whole-word clips/{cleanWord}");
        if (clip == null) clip = Resources.Load<AudioClip>($"Unit 9/{cleanWord}");
        return clip;
    }

    private void AutoFindUIElements()
    {
        if (gridContainer == null)
        {
            Transform t = transform.Find("GridContainer");
            if (t == null) t = transform.Find("ChartGrid");
            if (t == null) t = transform.Find("Container");
            if (t != null) gridContainer = t;
            else gridContainer = transform;
        }

        if (introSpeechClip == null)
        {
            introSpeechClip = Resources.Load<AudioClip>("Sometimes two letters team up to make one brand-new sound Let's meet them");
            if (introSpeechClip == null) introSpeechClip = Resources.Load<AudioClip>("u9_intro");
        }
        if (tileTapChime == null) tileTapChime = Resources.Load<AudioClip>("u8_pop");
    }

    private List<DigraphTileData> GetDefaultIntroDigraphs()
    {
        List<DigraphTileData> list = new List<DigraphTileData>();
        var defs = new (string key, string word)[]
        {
            ("ch-", "chain"),  ("-ch", "switch"),
            ("sh-", "shark"),  ("-sh", "trash"),
            ("th-", "three"),  ("-th", "earth"),
            ("wh-", "wheel"),  ("-ck", "duck")
        };
        foreach (var d in defs)
        {
            DigraphTileData t = ScriptableObject.CreateInstance<DigraphTileData>();
            t.digraphKey = d.key;
            t.startWord = d.word;
            t.endWord   = d.word;
            list.Add(t);
        }
        return list;
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
