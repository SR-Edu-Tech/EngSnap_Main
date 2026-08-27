using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Section D — Consonant Safari (Letter Safari / Game 2, Page 32).
/// Goal: pick out only the consonants from a scattered field of letters.
/// 8 cards per round (6 consonants + 2 vowels), spacious 4x2 grid with gentle 6px in-place bobbing.
/// Zero card overlaps. Inspector text objects and positions remain 100% preserved.
/// </summary>
public class U8_A4_ConsonantSafariController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────────────────

    [Header("Spawning & Container")]
    public RectTransform spawnArea;          // The RectTransform within which letters float
    public GameObject    floatingLetterPrefab; // Prefab: "Sound Tile" or clean UI tile
    public Vector2       cardSize = new Vector2(125f, 125f);

    [Header("UI Text & Vowel Hand (Assign in Inspector if desired)")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI vowelHandReminderText; // Optional: "Vowels: A  E  I  O  U"

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   catchClip;            // Sparkle sound when consonant is caught
    public AudioClip   wrongClip;            // Gentle 'no' sound for vowel tap
    public AudioClip   completionClip;       // Safari complete fanfare
    public AudioClip   introSpeechClip;      // Mascot instruction speech

    [Header("Visual")]
    public ParticleSystem sparkleEffect;     // Optional particle burst on correct tap

    [Header("References")]
    public U8_Manager manager;

    // ──────────────────────────────────────────────────────────
    //  Private State
    // ──────────────────────────────────────────────────────────

    private static readonly HashSet<char> Vowels = new HashSet<char> { 'a', 'e', 'i', 'o', 'u' };

    // Select 6 consonants + 2 vowel distractors per round for ultra-spacious layout
    private static readonly string[] ConsonantPool = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "r", "s", "t", "v", "w", "z" };
    private static readonly string[] VowelPool     = { "a", "e", "i", "o", "u" };

    private List<GameObject> spawnedLetters  = new List<GameObject>();
    private List<Coroutine>  floatCoroutines = new List<Coroutine>();
    private int  totalConsonants   = 0;
    private int  caughtConsonants  = 0;
    private bool isComplete        = false;

    public System.Action OnActivityComplete;

    // ──────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    // ──────────────────────────────────────────────────────────
    //  Setup
    // ──────────────────────────────────────────────────────────

    public void SetupActivity(Unit8LevelData levelData)
    {
        isComplete       = false;
        caughtConsonants = 0;

        AutoFindUIElements();

        // Hide Next Button via Manager initially
        if (manager != null)
        {
            manager.HideNextButton();
        }

        // Only update text if explicitly assigned in Inspector
        if (instructionText != null)
        {
            instructionText.text = "Tap all the consonants!";
        }

        if (vowelHandReminderText != null)
        {
            vowelHandReminderText.text = "Vowels: A  E  I  O  U";
        }

        // Clear previous session
        foreach (GameObject g in spawnedLetters)
            if (g != null) Destroy(g);
        spawnedLetters.Clear();
        floatCoroutines.Clear();

        // Build a balanced set of 6 consonants + 2 vowels (8 cards total for ultra-spacious layout)
        List<string> letters = new List<string>();

        // Pick 6 random consonants
        List<string> cPool = new List<string>(ConsonantPool);
        for (int i = 0; i < 6 && cPool.Count > 0; i++)
        {
            int idx = Random.Range(0, cPool.Count);
            letters.Add(cPool[idx]);
            cPool.RemoveAt(idx);
        }

        // Pick 2 vowels
        List<string> vPool = new List<string>(VowelPool);
        for (int i = 0; i < 2 && vPool.Count > 0; i++)
        {
            int idx = Random.Range(0, vPool.Count);
            letters.Add(vPool[idx]);
            vPool.RemoveAt(idx);
        }

        // Shuffle letter order
        for (int i = letters.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string tmp = letters[i]; letters[i] = letters[j]; letters[j] = tmp;
        }

        // Count consonants
        totalConsonants = 0;
        foreach (string l in letters)
            if (l.Length > 0 && !Vowels.Contains(char.ToLower(l[0]))) totalConsonants++;

        // Calculate spacious 4x2 grid positions (no overlaps)
        List<Vector2> gridPositions = GenerateUltraSpaciousPositions(letters.Count);

        // Spawn cards into field
        for (int i = 0; i < letters.Count; i++)
        {
            Vector2 pos = i < gridPositions.Count ? gridPositions[i] : Vector2.zero;
            SpawnLetter(letters[i], pos);
        }

        // Mascot Intro greeting & speech
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }

        PlayClip(introSpeechClip);
    }

    private List<Vector2> GenerateUltraSpaciousPositions(int count)
    {
        List<Vector2> positions = new List<Vector2>();
        int cols = 4;
        int rows = 2;

        float cellW = 260f;
        float cellH = 170f;

        float startX = -((cols - 1) * cellW * 0.5f);
        float startY = ((rows - 1) * cellH * 0.5f) - 60f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (positions.Count >= count) break;
                positions.Add(new Vector2(startX + c * cellW, startY - r * cellH));
            }
        }

        return positions;
    }

    private void AutoFindUIElements()
    {
        if (spawnArea == null)
        {
            Transform t = transform.Find("SpawnArea");
            if (t == null) t = transform.Find("Spawn_Area");
            if (t == null) t = transform.Find("SafariContainer");
            if (t == null) t = transform.Find("Container");
            if (t == null) t = transform.Find("Field");
            if (t != null) spawnArea = t.GetComponent<RectTransform>();
            else spawnArea = GetComponent<RectTransform>();
        }

        // Auto-find existing letter tile prefabs if unassigned
        if (floatingLetterPrefab == null)
        {
            floatingLetterPrefab = Resources.Load<GameObject>("Sound Tile");
            if (floatingLetterPrefab == null) floatingLetterPrefab = Resources.Load<GameObject>("WordCard");
            if (floatingLetterPrefab == null) floatingLetterPrefab = Resources.Load<GameObject>("ConsonantTilePrefab");
#if UNITY_EDITOR
            if (floatingLetterPrefab == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Sound Tile t:Prefab");
                if (guids.Length == 0) guids = UnityEditor.AssetDatabase.FindAssets("WordCard t:Prefab");
                if (guids.Length > 0)
                {
                    floatingLetterPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        if (catchClip == null) catchClip = Resources.Load<AudioClip>("u8_pop");
        if (wrongClip == null) wrongClip = Resources.Load<AudioClip>("u8_wrong");
        if (completionClip == null) completionClip = Resources.Load<AudioClip>("u8_complete");
        if (introSpeechClip == null) introSpeechClip = Resources.Load<AudioClip>("u8_safari_intro");
    }

    // ──────────────────────────────────────────────────────────
    //  Spawning
    // ──────────────────────────────────────────────────────────

    private void SpawnLetter(string letter, Vector2 startPos)
    {
        if (spawnArea == null) return;

        GameObject obj;
        if (floatingLetterPrefab != null)
        {
            obj = Instantiate(floatingLetterPrefab, spawnArea);
        }
        else
        {
            // Fallback: create a clean UI tile at runtime
            obj = new GameObject($"SafariLetter_{letter}");
            obj.transform.SetParent(spawnArea, false);
            obj.AddComponent<CanvasRenderer>();

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.95f, 0.95f, 1.0f, 0.95f);
            bg.raycastTarget = true;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = cardSize;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text      = letter.ToUpper();
            tmp.fontSize  = 48;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = new Color(0.12f, 0.12f, 0.25f, 1.0f);
            tmp.raycastTarget = false;

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            obj.AddComponent<Button>();
        }

        // Set card size to spacious 125x125
        RectTransform objRt = obj.GetComponent<RectTransform>();
        if (objRt != null)
        {
            objRt.anchoredPosition = startPos;
            objRt.sizeDelta = cardSize;
            objRt.localScale = Vector3.one;
        }

        // 1. Hide/disable any pre-existing picture images (like the apple sprite)
        Image[] images = obj.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img.gameObject != obj)
            {
                img.gameObject.SetActive(false); // Hide apple sprite
            }
        }

        // 2. Set the main letter text label strictly inside the card container!
        TextMeshProUGUI[] tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmps.Length > 0)
        {
            tmps[0].text = letter.ToUpper();
            tmps[0].fontSize = 48;
            tmps[0].fontStyle = FontStyles.Bold;
            tmps[0].alignment = TextAlignmentOptions.Center;
            tmps[0].gameObject.SetActive(true);

            RectTransform trt = tmps[0].rectTransform;
            if (trt != null)
            {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }

            // Hide extra sub-labels (e.g. "Apple" keyword text)
            for (int k = 1; k < tmps.Length; k++)
            {
                tmps[k].gameObject.SetActive(false);
            }
        }

        Text[] legacyTexts = obj.GetComponentsInChildren<Text>(true);
        if (legacyTexts.Length > 0)
        {
            legacyTexts[0].text = letter.ToUpper();
            legacyTexts[0].alignment = TextAnchor.MiddleCenter;
            legacyTexts[0].fontSize = 44;
            legacyTexts[0].gameObject.SetActive(true);

            RectTransform trt = legacyTexts[0].rectTransform;
            if (trt != null)
            {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }

            for (int k = 1; k < legacyTexts.Length; k++)
            {
                legacyTexts[k].gameObject.SetActive(false);
            }
        }

        // Wire button
        Button btn = obj.GetComponent<Button>();
        if (btn != null)
        {
            bool isVowel = letter.Length > 0 && Vowels.Contains(char.ToLower(letter[0]));
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnLetterTapped(letter, obj, isVowel));
        }

        // Start gentle in-place bobbing motion (zero overlaps)
        Coroutine c = StartCoroutine(FloatRoutine(obj, startPos));
        floatCoroutines.Add(c);
        spawnedLetters.Add(obj);
    }

    // ──────────────────────────────────────────────────────────
    //  Gentle In-Place Bobbing Motion (Zero Overlaps)
    // ──────────────────────────────────────────────────────────

    private IEnumerator FloatRoutine(GameObject obj, Vector2 basePos)
    {
        if (obj == null) yield break;
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float speed = Random.Range(1.5f, 2.5f);
        float radiusY = Random.Range(6f, 12f); // Gentle 6-12px vertical bobbing only
        float timeOffset = Random.Range(0f, 10f);

        while (obj != null && obj.activeSelf)
        {
            float t = (Time.time + timeOffset) * speed;
            Vector2 offset = new Vector2(0f, Mathf.Sin(t) * radiusY);
            rt.anchoredPosition = basePos + offset;
            yield return null;
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Tap Handling
    // ──────────────────────────────────────────────────────────

    private void OnLetterTapped(string letter, GameObject obj, bool isVowel)
    {
        if (isComplete || obj == null) return;

        if (!isVowel)
        {
            // ✅ Correct — consonant caught
            StartCoroutine(CatchConsonant(letter, obj));
        }
        else
        {
            // ❌ Vowel — gentle shake and alert
            StartCoroutine(RejectVowel(letter, obj));
        }
    }

    private IEnumerator CatchConsonant(string letter, GameObject obj)
    {
        // Sparkle feedback
        if (sparkleEffect != null)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null) sparkleEffect.transform.position = rt.position;
            sparkleEffect.Play();
        }

        // Pop scale animation
        RectTransform objRt = obj.GetComponent<RectTransform>();
        if (objRt != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + 0.35f * Mathf.Sin(elapsed / 0.2f * Mathf.PI);
                objRt.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        PlayClip(catchClip);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayHiAnimation();

        // Remove caught consonant from field
        if (obj != null) obj.SetActive(false);
        caughtConsonants++;

        if (caughtConsonants >= totalConsonants)
            StartCoroutine(SafariComplete());
    }

    private IEnumerator RejectVowel(string letter, GameObject obj)
    {
        if (instructionText != null)
        {
            instructionText.text = $"Oops! {letter.ToUpper()} is a vowel! We only want consonants!";
        }

        PlayClip(wrongClip);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 origin = rt.anchoredPosition;
        float elapsed  = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            rt.anchoredPosition = origin + new Vector2(Mathf.Sin(elapsed * 50f) * 6f, 0f);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    private IEnumerator SafariComplete()
    {
        isComplete = true;

        if (instructionText != null)
        {
            instructionText.text = "Safari Complete! You caught all the consonants!";
        }

        PlayClip(completionClip);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();

        yield return new WaitForSeconds(1.2f);

        // Reveal Next Button via Manager!
        if (manager != null) manager.ShowNextButton();
        if (OnActivityComplete != null) OnActivityComplete.Invoke();
    }

    // ──────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        // STOP ANY CURRENTLY PLAYING AUDIO IMMEDIATELY TO PREVENT OVERLAPPING AUDIOS!
        audioSource.Stop();
        audioSource.spatialBlend = 0f;
        audioSource.volume       = 1f;
        audioSource.mute         = false;
        audioSource.PlayOneShot(clip);
    }
}
