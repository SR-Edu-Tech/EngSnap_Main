using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U8_A1_SoundWallController : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform tilesContainer;
    public GameObject consonantTilePrefab;
    public GameObject instructionPanel;
    public GameObject letterScreen;
    public Button nextButton;

    [Header("Card Scale & Grid Spacing (Uniform Control)")]
    [Range(0.5f, 3.0f)] public float cardScale = 1.25f;
    public Vector2 gridSpacing = new Vector2(10f, 10f);
    public int columnCount = 7;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introAudioClip;
    public U8_Manager manager;

    private Unit8LevelData currentLevel;
    private HashSet<string> exploredTiles = new HashSet<string>();
    private static readonly Vector2 BaseCardSize = new Vector2(200f, 220f);

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && tilesContainer != null)
            {
                ApplyInspectorSizing();
            }
        };
#endif
    }

    public void ApplyInspectorSizing()
    {
        if (tilesContainer == null) return;

        // Apply grid scale and spacing consistently using 200x220 base size
        GridLayoutGroup grid = tilesContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = tilesContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        if (grid != null)
        {
            grid.cellSize = new Vector2(BaseCardSize.x * cardScale, BaseCardSize.y * cardScale);
            grid.spacing = gridSpacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnCount;
        }

        // Apply localScale consistently to all card items
        for (int i = 0; i < tilesContainer.childCount; i++)
        {
            Transform item = tilesContainer.GetChild(i);
            if (item != null)
            {
                RectTransform rt = item.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = new Vector3(cardScale, cardScale, 1f);
                }
            }
        }

        if (Application.isPlaying)
        {
            Canvas.ForceUpdateCanvases();
        }
    }

    public void SetupActivity(Unit8LevelData levelData)
    {
        exploredTiles.Clear();

        if (levelData == null)
        {
            levelData = Resources.Load<Unit8LevelData>("Unit8Level_Main");
#if UNITY_EDITOR
            if (levelData == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Unit8Level_Main t:Unit8LevelData");
                if (guids.Length > 0)
                {
                    levelData = UnityEditor.AssetDatabase.LoadAssetAtPath<Unit8LevelData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        currentLevel = levelData;

        // Auto-find Instruction Panel & Letter Screen
        if (instructionPanel == null)
        {
            Transform t = transform.Find("Instruction Panel");
            if (t == null) t = transform.Find("Instruction_Panel");
            if (t == null) t = transform.Find("InstructionPanel");
            if (t != null) instructionPanel = t.gameObject;
        }

        if (letterScreen == null)
        {
            Transform t = transform.Find("Letter Screen");
            if (t == null) t = transform.Find("LetterScreen");
            if (t != null) letterScreen = t.gameObject;
        }

        // Hide Next Button via Manager on entry
        if (manager != null)
        {
            manager.HideNextButton();
        }

        // Auto-find tilesContainer
        if (tilesContainer == null)
        {
            string[] containerNames = { 
                "Letter Screen/Consonant Section/Consonant View/Viewport/Consonant Content",
                "Consonant Section/Consonant View/Viewport/Consonant Content",
                "Consonant Content",
                "Letter Screen/Consonant Section", "Consonant Section", "ConsonantSection", "Consonant_Section",
                "ConsonantsContainer", "TilesContainer", "Grid", "Content", "Viewport/Content", "Pond_Container" 
            };
            foreach (string cn in containerNames)
            {
                Transform t = transform.Find(cn);
                if (t != null) { tilesContainer = t; break; }
            }

            if (tilesContainer == null && letterScreen != null)
            {
                Transform cc = letterScreen.transform.Find("Consonant Section/Consonant View/Viewport/Consonant Content");
                if (cc == null) cc = letterScreen.transform.Find("Consonant Section");
                if (cc != null) tilesContainer = cc;
            }

            if (tilesContainer == null)
            {
                var layout = GetComponentInChildren<LayoutGroup>(true);
                if (layout != null) tilesContainer = layout.transform;
            }

            if (tilesContainer == null) tilesContainer = transform;
        }

        // Auto-load intro audio clip if null
        if (introAudioClip == null)
        {
            introAudioClip = Resources.Load<AudioClip>("u8_wall");
            if (introAudioClip == null) introAudioClip = Resources.Load<AudioClip>("u8_intro");
#if UNITY_EDITOR
            if (introAudioClip == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("u8_wall t:AudioClip");
                if (guids.Length == 0) guids = UnityEditor.AssetDatabase.FindAssets("u8_intro t:AudioClip");
                if (guids.Length > 0)
                {
                    introAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (levelData != null && levelData.consonantsList != null)
        {
            PopulateSoundWall(levelData.consonantsList);
        }

        StartCoroutine(ShowIntroRoutine());
    }

    private IEnumerator ShowIntroRoutine()
    {
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }

        // Show instruction panel initially
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
            
            // Fail-safe: allow clicking the instruction panel to skip
            Button instructionBtn = instructionPanel.GetComponent<Button>();
            if (instructionBtn == null) instructionBtn = instructionPanel.AddComponent<Button>();
            instructionBtn.onClick.RemoveAllListeners();
            instructionBtn.onClick.AddListener(() => {
                if (instructionPanel != null) instructionPanel.SetActive(false);
            });
        }

        float waitTime = 3.0f;
        if (audioSource != null && introAudioClip != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.PlayOneShot(introAudioClip);
            waitTime = introAudioClip.length + 0.5f;
        }

        yield return new WaitForSeconds(waitTime);

        // After intro audio finishes, hide instruction panel and show letter screen sound wall!
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }

        if (letterScreen != null)
        {
            letterScreen.SetActive(true);
            foreach (Transform child in letterScreen.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && !child.gameObject.activeSelf)
                    child.gameObject.SetActive(true);
            }

            // Hide Vowel Section & More Sounds Section so Consonant Section is featured!
            Transform vowelSec = letterScreen.transform.Find("Vowel Section");
            if (vowelSec != null) vowelSec.gameObject.SetActive(false);

            Transform moreSec = letterScreen.transform.Find("More Sounds Section");
            if (moreSec != null) moreSec.gameObject.SetActive(false);

            // Force ApplyInspectorSizing right when letterScreen turns ON!
            ApplyInspectorSizing();
        }
    }

    private void PopulateSoundWall(List<ConsonantTileData> tiles)
    {
        if (tilesContainer == null || tiles == null) return;

        // Auto-load tile prefab if unassigned
        if (consonantTilePrefab == null)
        {
            consonantTilePrefab = Resources.Load<GameObject>("ConsonantTilePrefab");
            if (consonantTilePrefab == null) consonantTilePrefab = Resources.Load<GameObject>("Sound Tile");
            if (consonantTilePrefab == null) consonantTilePrefab = Resources.Load<GameObject>("WordCard");
#if UNITY_EDITOR
            if (consonantTilePrefab == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Sound Tile t:Prefab");
                if (guids.Length == 0) guids = UnityEditor.AssetDatabase.FindAssets("WordCard t:Prefab");
                if (guids.Length > 0)
                {
                    consonantTilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        // 1. APPLY GRID SCALE BEFORE INSTANTIATING CARDS!
        GridLayoutGroup grid = tilesContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = tilesContainer.gameObject.AddComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.cellSize = new Vector2(BaseCardSize.x * cardScale, BaseCardSize.y * cardScale);
            grid.spacing = gridSpacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnCount;
        }

        // Clean out ALL pre-existing children in tilesContainer
        for (int i = tilesContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(tilesContainer.GetChild(i).gameObject);
        }

        // 2. INSTANTIATE FRESH CARDS
        for (int i = 0; i < tiles.Count; i++)
        {
            ConsonantTileData tileData = tiles[i];
            if (tileData == null) continue;

            GameObject obj = Instantiate(consonantTilePrefab, tilesContainer);
            Transform item = obj.transform;

            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = new Vector3(cardScale, cardScale, 1f);
                rt.anchoredPosition3D = Vector3.zero;
            }

            item.gameObject.SetActive(true);
            item.name = $"Card_{tileData.letter.ToUpper()}{tileData.letter.ToLower()}";

            // Assign Text AS IS
            TextMeshProUGUI[] tmps = item.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length >= 2)
            {
                tmps[0].text = $"{tileData.letter.ToUpper()}{tileData.letter.ToLower()}";
                tmps[1].text = tileData.keywordText;
            }
            else if (tmps.Length == 1)
            {
                tmps[0].text = $"{tileData.letter.ToUpper()}{tileData.letter.ToLower()}\n{tileData.keywordText}";
            }

            Text[] uiTexts = item.GetComponentsInChildren<Text>(true);
            if (uiTexts.Length >= 2)
            {
                uiTexts[0].text = $"{tileData.letter.ToUpper()}{tileData.letter.ToLower()}";
                uiTexts[1].text = tileData.keywordText;
            }
            else if (uiTexts.Length == 1)
            {
                uiTexts[0].text = $"{tileData.letter.ToUpper()}{tileData.letter.ToLower()} {tileData.keywordText}";
            }

            // Disable raycastTarget on inner text labels
            Graphic[] childGraphics = item.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in childGraphics)
            {
                if (g is TextMeshProUGUI || g is Text)
                {
                    g.raycastTarget = false;
                }
            }

            // Assign Picture Sprite AS IS
            Image[] images = item.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.gameObject == item.gameObject) continue;

                if (tileData.keywordSprite != null)
                {
                    img.sprite = tileData.keywordSprite;
                    img.gameObject.SetActive(true);
                }
                img.raycastTarget = false;
            }

            // Bind Button Click Handler
            ConsonantTileData currentTileData = tileData;
            GameObject currentItem = item.gameObject;

            Button[] buttons = item.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnTileClicked(currentTileData, currentItem));
            }

            Button rootBtn = item.GetComponent<Button>();
            if (rootBtn == null) rootBtn = item.gameObject.AddComponent<Button>();
            Image rootImg = item.GetComponent<Image>();
            if (rootImg != null) rootImg.raycastTarget = true;

            rootBtn.onClick.RemoveAllListeners();
            rootBtn.onClick.AddListener(() => OnTileClicked(currentTileData, currentItem));
        }

        ApplyInspectorSizing();
    }

    private void OnTileClicked(ConsonantTileData tileData, GameObject itemObj)
    {
        if (tileData == null) return;

        // 1. Stop any currently playing audio to PREVENT overlapping or double audio!
        if (audioSource != null)
        {
            audioSource.Stop();
            if (tileData.keywordAudio != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.volume = 1f;
                audioSource.mute = false;
                audioSource.PlayOneShot(tileData.keywordAudio);
            }
        }

        // 2. Grey out the clicked card background!
        if (itemObj != null)
        {
            Image bgImg = itemObj.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.65f, 0.65f, 0.65f, 1f); // Grey tint
            }
        }

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayHiAnimation();

        // 3. Track explored tiles and reveal Next Button ONLY AFTER ALL tiles are clicked!
        if (!string.IsNullOrEmpty(tileData.letter))
        {
            exploredTiles.Add(tileData.letter);
            int totalTiles = currentLevel != null && currentLevel.consonantsList != null && currentLevel.consonantsList.Count > 0 
                ? currentLevel.consonantsList.Count : 21;

            if (exploredTiles.Count >= totalTiles)
            {
                if (manager != null) manager.ShowNextButton();
                else if (nextButton != null) nextButton.gameObject.SetActive(true);

                if (mascot != null) mascot.PlayCelebrationAnimation();
            }
        }
    }
}
