using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Final Game — Pick the Digraph (Page 41).
/// Picture + incomplete word (e.g. 🧀 ___eese, ☎️ ___one, 🩹 ___ee, ⚙️ ___eel).
/// Child drags or taps the correct digraph tile from tray (ch, ph, th, wh, sh, kn).
/// Tray tiles feature smooth, soft rounded corner card containers!
/// Includes Mascot intros for ph ("p and h say /f/!") and kn ("the k is silent!").
/// Completing all items unlocks the Digraph Detective Trophy via Manager!
/// </summary>
public class U9_A3_PickDigraphController : MonoBehaviour
{
    [Header("UI Containers")]
    public Image            pictureDisplayImage;   // Picture item (e.g. cheese, phone, knee)
    public TextMeshProUGUI incompleteWordText;    // e.g. "___eese"
    public Transform        digraphTrayContainer;  // Tray holding digraph tiles (ch, ph, th, wh, sh, kn)
    public GameObject       digraphTilePrefab;     // Prefab tile

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctChime;
    public AudioClip   wrongShake;
    public AudioClip   completionClip;
    public AudioClip   phIntroClip;               // u9_ph_intro: "p and h together say /f/!"
    public AudioClip   knIntroClip;               // u9_kn_intro: "The k is silent — k and n say /n/!"

    [Header("References")]
    public U9_Manager manager;

    private List<DigraphWordData> pickWordList = new List<DigraphWordData>();
    private int  currentWordIndex = 0;
    private bool isProcessing     = false;
    private List<GameObject> trayTileObjs = new List<GameObject>();

    private static readonly string[] DigraphTrayKeys = { "ch", "ph", "th", "wh", "sh", "kn" };
    private static Sprite roundedTraySprite = null;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SetupActivity(List<DigraphWordData> wordList)
    {
        pickWordList     = wordList != null ? wordList : new List<DigraphWordData>();
        currentWordIndex = 0;
        isProcessing     = false;

        AutoFindUIElements();

        if (manager != null) manager.HideNextButton();

        if (pickWordList.Count > 0)
        {
            DisplayCurrentPickWord();
        }

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }
    }

    private void DisplayCurrentPickWord()
    {
        isProcessing = false;
        if (currentWordIndex >= pickWordList.Count) return;

        DigraphWordData data = pickWordList[currentWordIndex];

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
                : $"___{data.wordText}";
            incompleteWordText.color = new Color(0.08f, 0.15f, 0.35f, 1f);
            incompleteWordText.fontSize = 58;
        }

        // Mascot bonus intro for ph & kn
        if (data.targetDigraph.ToLower() == "ph") PlayClip(phIntroClip);
        else if (data.targetDigraph.ToLower() == "kn") PlayClip(knIntroClip);
        else PlayClip(data.wordAudio);

        // Populate tray tiles
        PopulateDigraphTray();
    }

    private void PopulateDigraphTray()
    {
        foreach (GameObject g in trayTileObjs)
            if (g != null) Destroy(g);
        trayTileObjs.Clear();

        Transform container = digraphTrayContainer != null ? digraphTrayContainer : transform;

        foreach (string dg in DigraphTrayKeys)
        {
            string digraphKey = dg;
            GameObject tileObj = InstantiateTrayTile(digraphKey, container);
            trayTileObjs.Add(tileObj);
        }
    }

    private GameObject InstantiateTrayTile(string digraphKey, Transform parent)
    {
        GameObject obj;
        if (digraphTilePrefab != null)
        {
            obj = Instantiate(digraphTilePrefab, parent);
        }
        else
        {
            // Soft rounded corner card container for digraph tray tiles
            obj = new GameObject($"TrayTile_{digraphKey}");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<CanvasRenderer>();

            Image bg = obj.AddComponent<Image>();
            bg.sprite = GetOrCreateRoundedSprite();
            bg.type   = Image.Type.Sliced;
            bg.color  = new Color(0.95f, 0.97f, 1.0f, 1.0f); // Clean light blue-white rounded card

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110f, 110f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = digraphKey;
            tmp.fontSize = 52;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.08f, 0.22f, 0.42f, 1f); // Deep navy blue bold text

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            obj.AddComponent<Button>();
        }

        Button btn = obj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnTrayTileTapped(digraphKey, obj));
        }

        return obj;
    }

    private void OnTrayTileTapped(string selectedDigraph, GameObject tileObj)
    {
        if (isProcessing || currentWordIndex >= pickWordList.Count) return;

        DigraphWordData data = pickWordList[currentWordIndex];
        bool isCorrect = selectedDigraph.ToLower() == data.targetDigraph.ToLower();

        if (isCorrect)
        {
            StartCoroutine(HandleCorrectPick(data));
        }
        else
        {
            StartCoroutine(HandleWrongPick(tileObj));
        }
    }

    private IEnumerator HandleCorrectPick(DigraphWordData data)
    {
        isProcessing = true;

        if (incompleteWordText != null)
        {
            incompleteWordText.text = $"<color=#D62828><b>{data.targetDigraph}</b></color><b>{data.wordText.Substring(data.targetDigraph.Length)}</b>";
        }

        PlayClip(correctChime);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();

        yield return new WaitForSeconds(1.0f);

        PlayClip(data.wordAudio);

        yield return new WaitForSeconds(1.2f);

        currentWordIndex++;
        if (currentWordIndex < pickWordList.Count)
        {
            DisplayCurrentPickWord();
        }
        else
        {
            // Pick-the-Digraph Complete! Open Reward Panel!
            PlayClip(completionClip);
            if (manager != null) manager.ShowReward();
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

        int width = 64;
        int height = 64;
        int radius = 18;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cornerX = Mathf.Min(x, width - 1 - x);
                int cornerY = Mathf.Min(y, height - 1 - y);

                if (cornerX < radius && cornerY < radius)
                {
                    float dx = radius - cornerX;
                    float dy = radius - cornerY;
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
        if (digraphTrayContainer == null)
        {
            Transform t = transform.Find("DigraphTrayContainer");
            if (t == null) t = transform.Find("TrayContainer");
            if (t == null) t = transform.Find("Tray");
            if (t != null) digraphTrayContainer = t;
            else digraphTrayContainer = transform;
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

        if (correctChime   == null) correctChime   = Resources.Load<AudioClip>("u8_pop");
        if (wrongShake     == null) wrongShake     = Resources.Load<AudioClip>("u8_wrong");
        if (completionClip == null) completionClip = Resources.Load<AudioClip>("u8_complete");
        if (phIntroClip    == null) phIntroClip    = Resources.Load<AudioClip>("u9_ph_intro");
        if (knIntroClip    == null) knIntroClip    = Resources.Load<AudioClip>("u9_kn_intro");
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
