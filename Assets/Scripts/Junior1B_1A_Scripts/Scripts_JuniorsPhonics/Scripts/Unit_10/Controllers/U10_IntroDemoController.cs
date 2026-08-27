using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Intro Concept Demo for Unit 10 — Consonant Blends vs Digraphs.
/// Teaches the key visual & phonetic contrast:
/// - Digraph (sh): Tiles merge into 1 single tile [sh] (1 new sound).
/// - Blend (sl): Tiles slide close together but STAY as 2 separate tiles [s] [l] (2 sounds you still hear!).
/// Mascot speaks: "In sh you hear one sound. In sl you hear s AND l — quick together!"
/// </summary>
public class U10_IntroDemoController : MonoBehaviour
{
    [Header("UI Demo Containers")]
    public Transform digraphDemoContainer;   // Container showing Digraph demo (s + h -> sh)
    public Transform blendDemoContainer;     // Container showing Blend demo (s + l -> s l)
    public TextMeshProUGUI explanationLabel; // Displays concept text
    public Button replayButton;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   introConceptClip;     // u10_intro: "A blend is two letters said quickly together..."
    public AudioClip   vsClip;               // u10_vs: "In sh you hear one sound. In sl you hear s AND l..."
    public AudioClip   chimeClip;

    [Header("References")]
    public U10_Manager manager;

    private bool isDemoPlaying = false;
    private List<GameObject> spawnedDemoObjs = new List<GameObject>();
    private static Sprite roundedTileSprite = null;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayDemo);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void SetupActivity()
    {
        isDemoPlaying = false;

        AutoFindUIElements();

        if (explanationLabel != null)
        {
            explanationLabel.text = "In a Digraph (sh) you hear ONE sound.\nIn a Blend (sl) you hear BOTH sounds quickly together!";
            explanationLabel.color = new Color(0.08f, 0.18f, 0.4f, 1f);
        }

        StartCoroutine(PlayDemoAnimationRoutine());
    }

    public void ReplayDemo()
    {
        if (!isDemoPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(PlayDemoAnimationRoutine());
        }
    }

    public void OnNextButtonClicked()
    {
        if (manager != null)
        {
            manager.OnNextButtonClicked();
        }
    }

    private IEnumerator PlayDemoAnimationRoutine()
    {
        isDemoPlaying = true;
        if (manager != null) manager.HideNextButton();

        // Clear existing demo objects
        foreach (GameObject g in spawnedDemoObjs)
            if (g != null) Destroy(g);
        spawnedDemoObjs.Clear();

        Transform digParent = digraphDemoContainer != null ? digraphDemoContainer : transform;
        Transform bleParent = blendDemoContainer != null ? blendDemoContainer : transform;

        // 1. Setup Digraph Demo Visuals: [s] and [h]
        GameObject digS = CreateDemoTile("s", new Color(1f, 0.95f, 0.85f, 1f), digParent);
        GameObject digH = CreateDemoTile("h", new Color(1f, 0.95f, 0.85f, 1f), digParent);
        spawnedDemoObjs.Add(digS); spawnedDemoObjs.Add(digH);

        // 2. Setup Blend Demo Visuals: [s] and [l]
        GameObject bleS = CreateDemoTile("s", new Color(0.85f, 0.93f, 1f, 1f), bleParent);
        GameObject bleL = CreateDemoTile("l", new Color(0.85f, 0.93f, 1f, 1f), bleParent);
        spawnedDemoObjs.Add(bleS); spawnedDemoObjs.Add(bleL);

        // Position tiles initially apart
        RectTransform rtDigS = digS.GetComponent<RectTransform>();
        RectTransform rtDigH = digH.GetComponent<RectTransform>();
        RectTransform rtBleS = bleS.GetComponent<RectTransform>();
        RectTransform rtBleL = bleL.GetComponent<RectTransform>();

        rtDigS.anchoredPosition = new Vector2(-70f, 0f);
        rtDigH.anchoredPosition = new Vector2(70f, 0f);
        rtBleS.anchoredPosition = new Vector2(-70f, 0f);
        rtBleL.anchoredPosition = new Vector2(70f, 0f);

        // Mascot Voiceover: Intro concept
        PlayClip(introConceptClip != null ? introConceptClip : Resources.Load<AudioClip>("A blend is two letters said quickly together"));

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }

        yield return new WaitForSeconds(3.0f);

        // 3. Animate Digraph: [s] and [h] merge into ONE single tile [sh]
        PlayClip(chimeClip);
        float elapsed = 0f;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.8f;
            rtDigS.anchoredPosition = Vector2.Lerp(new Vector2(-70f, 0f), Vector2.zero, t);
            rtDigH.anchoredPosition = Vector2.Lerp(new Vector2(70f, 0f), Vector2.zero, t);
            yield return null;
        }

        // Replace with single merged Digraph tile [sh]
        digS.SetActive(false);
        digH.SetActive(false);
        GameObject digMerged = CreateDemoTile("sh", new Color(1f, 0.88f, 0.65f, 1f), digParent);
        digMerged.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        spawnedDemoObjs.Add(digMerged);

        yield return new WaitForSeconds(0.5f);

        // 4. Animate Blend: [s] and [l] slide close TOGETHER, but STAY as 2 separate tiles!
        PlayClip(vsClip != null ? vsClip : Resources.Load<AudioClip>("In sh you hear one sound In sl you hear s and l"));

        elapsed = 0f;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.8f;
            rtBleS.anchoredPosition = Vector2.Lerp(new Vector2(-70f, 0f), new Vector2(-35f, 0f), t);
            rtBleL.anchoredPosition = Vector2.Lerp(new Vector2(70f, 0f), new Vector2(35f, 0f), t);
            yield return null;
        }

        if (mascot != null) mascot.PlayCelebrationAnimation();

        yield return new WaitForSeconds(3.5f);

        // Demo completed — reveal Next button!
        if (manager != null) manager.ShowNextButton();
        isDemoPlaying = false;
    }

    private GameObject CreateDemoTile(string label, Color bgColor, Transform parent)
    {
        GameObject obj = new GameObject($"DemoTile_{label}");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<CanvasRenderer>();

        Image bg = obj.AddComponent<Image>();
        bg.sprite = GetOrCreateRoundedSprite();
        bg.type   = Image.Type.Sliced;
        bg.color  = bgColor;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 120f);

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 64;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.08f, 0.18f, 0.4f, 1f);

        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return obj;
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
        if (digraphDemoContainer == null)
        {
            Transform t = transform.Find("DigraphDemoContainer");
            if (t == null) t = transform.Find("DigraphContainer");
            if (t != null) digraphDemoContainer = t;
        }

        if (blendDemoContainer == null)
        {
            Transform t = transform.Find("BlendDemoContainer");
            if (t == null) t = transform.Find("BlendContainer");
            if (t != null) blendDemoContainer = t;
        }

        if (explanationLabel == null)
        {
            Transform t = transform.Find("ExplanationLabel");
            if (t == null) t = transform.Find("Label");
            if (t != null) explanationLabel = t.GetComponent<TextMeshProUGUI>();
        }

        if (replayButton == null)
        {
            Transform t = transform.Find("ReplayButton");
            if (t != null) replayButton = t.GetComponent<Button>();
        }

        if (nextButton == null)
        {
            Transform t = transform.Find("Next_Button");
            if (t == null) t = transform.Find("NextButton");
            if (t == null) t = transform.Find("Next Button");
            if (t != null) nextButton = t.GetComponent<Button>();
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

#if UNITY_EDITOR
        if (introConceptClip == null)
            introConceptClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit 10/A blend is two letters said quickly together — but you still hear both.mp3");
        if (vsClip == null)
            vsClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit 10/In sh you hear one sound In sl you hear s and l — quick together.mp3");
#endif
        if (introConceptClip == null) introConceptClip = Resources.Load<AudioClip>("u10_intro");
        if (vsClip           == null) vsClip           = Resources.Load<AudioClip>("u10_vs");
        if (chimeClip        == null) chimeClip        = Resources.Load<AudioClip>("u8_pop");
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
