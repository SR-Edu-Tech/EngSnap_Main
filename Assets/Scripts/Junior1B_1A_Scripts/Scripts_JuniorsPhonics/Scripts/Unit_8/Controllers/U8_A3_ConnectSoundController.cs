using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Section C — Connect the Sound (Seek and Circle / Match pairs).
/// Goal: match a letter to the picture that starts with it.
/// Supports both Drag & Drop Line Drawing AND Two-Tap Selection.
/// Button colors and designs remain 100% preserved AS IS without green color overriding.
/// </summary>
public class U8_A3_ConnectSoundController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────────────────

    [Header("Left Column — Letter Buttons")]
    public Button[] letterButtons;          // 5 buttons: p, d, b, t, m
    public TextMeshProUGUI[] letterLabels;  // Label on each button

    [Header("Right Column — Picture Buttons")]
    public Button[] pictureButtons;         // 5 buttons: bicycle, telescope, window, pumpkin, matchbox
    public Image[]  pictureImages;          // Sprite on each picture button

    [Header("Layout Controls")]
    public bool autoArrangeLayout = false;  // Toggle true for auto-spacing, or keep false to position manually in Unity Editor!
    public float columnXOffset = 280f;
    public float columnYOffset = -20f;
    public float buttonSpacing = 20f;
    public Vector2 buttonCellSize = new Vector2(160f, 70f);

    [Header("Visual Feedback")]
    public Image    connectionLinePrefab;   // Optional: thin line Image drawn between matched pairs
    public Transform linesContainer;        // Parent for line objects

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctChime;
    public AudioClip   wrongShake;
    public AudioClip   completionClip;

    [Header("References")]
    public U8_Manager manager;

    // ──────────────────────────────────────────────────────────
    //  Data
    // ──────────────────────────────────────────────────────────

    private string[] defaultLetters   = { "p", "d", "b", "t", "m" };
    private string[] defaultPictures  = { "bicycle", "telescope", "window", "pumpkin", "matchbox" };
    private int[]    correctPairIndex = { 3, 2, 0, 1, 4 };

    private int   selectedLetterIndex = -1;
    private bool[] letterConnected;
    private bool[] pictureConnected;
    private int    connectedCount = 0;
    private bool   isProcessing   = false;

    private Unit8LevelData currentLevel;
    private List<Image>    drawnLines = new List<Image>();

    // Live Drag-Line
    private Image activeDragLine;
    private int activeDragLetterIndex = -1;

    public System.Action OnActivityComplete;

    // ──────────────────────────────────────────────────────────
    //  Lifecycle & Layout
    // ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        UpdateActiveDragLine();
    }

    private Camera GetUICamera()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return c.worldCamera != null ? c.worldCamera : Camera.main;
        }
        return null;
    }

    private void OnValidate()
    {
        if (autoArrangeLayout)
        {
            ApplyColumnLayouts();
        }
    }

    public void ApplyColumnLayouts()
    {
        AutoFindUIElements();

        if (!autoArrangeLayout) return;

        // 1. Configure Left Column (Letters)
        Transform leftCol = transform.Find("LettersColumn");
        if (leftCol == null) leftCol = transform.Find("Left Column");
        if (leftCol == null) leftCol = transform.Find("Letters");
        if (leftCol != null)
        {
            RectTransform lRt = leftCol.GetComponent<RectTransform>();
            if (lRt != null)
            {
                lRt.anchoredPosition = new Vector2(-columnXOffset, columnYOffset);
                lRt.sizeDelta = new Vector2(buttonCellSize.x + 20f, (buttonCellSize.y + buttonSpacing) * 5f);
            }

            VerticalLayoutGroup vlg = leftCol.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = leftCol.gameObject.AddComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = buttonSpacing;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = false;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;
            }

            foreach (Transform child in leftCol)
            {
                RectTransform crt = child.GetComponent<RectTransform>();
                if (crt != null) crt.sizeDelta = buttonCellSize;
            }
        }

        // 2. Configure Right Column (Pictures)
        Transform rightCol = transform.Find("PicturesColumn");
        if (rightCol == null) rightCol = transform.Find("Right Column");
        if (rightCol == null) rightCol = transform.Find("Pictures");
        if (rightCol != null)
        {
            RectTransform rRt = rightCol.GetComponent<RectTransform>();
            if (rRt != null)
            {
                rRt.anchoredPosition = new Vector2(columnXOffset, columnYOffset);
                rRt.sizeDelta = new Vector2(buttonCellSize.x + 20f, (buttonCellSize.y + buttonSpacing) * 5f);
            }

            VerticalLayoutGroup vlg = rightCol.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = rightCol.gameObject.AddComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = buttonSpacing;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = false;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.childForceExpandHeight = false;
            }

            foreach (Transform child in rightCol)
            {
                RectTransform crt = child.GetComponent<RectTransform>();
                if (crt != null) crt.sizeDelta = buttonCellSize;
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    public void SetupActivity(Unit8LevelData levelData)
    {
        currentLevel = levelData;

        // Reset state
        selectedLetterIndex = -1;
        activeDragLetterIndex = -1;
        connectedCount      = 0;
        isProcessing        = false;

        AutoFindUIElements();
        if (autoArrangeLayout) ApplyColumnLayouts();

        int count = letterButtons != null ? letterButtons.Length : 0;
        letterConnected  = new bool[count];
        pictureConnected = new bool[count];

        ClearLines();

        // Hide Next Button via Manager initially
        if (manager != null)
        {
            manager.HideNextButton();
        }

        // Populate letters from ScriptableObject data if available, else use defaults
        string[] letters = defaultLetters;
        if (levelData != null && levelData.connectPairs != null && levelData.connectPairs.Count >= count && count > 0)
        {
            letters = new string[count];
            for (int i = 0; i < count; i++)
                letters[i] = levelData.connectPairs[i].letter;
        }

        // Set up letter labels & Drag Triggers
        for (int i = 0; i < count; i++)
        {
            if (letterLabels != null && i < letterLabels.Length && letterLabels[i] != null && i < letters.Length)
                letterLabels[i].text = letters[i].ToUpper();

            if (letterButtons != null && i < letterButtons.Length && letterButtons[i] != null)
            {
                int captured = i;
                letterButtons[i].onClick.RemoveAllListeners();
                letterButtons[i].onClick.AddListener(() => OnLetterClicked(captured));
                SetButtonState(letterButtons[i], true);

                // Attach EventTrigger for Drag & Drop line drawing!
                AttachDragTriggers(letterButtons[i].gameObject, captured);
            }
        }

        // Set up pictures from ScriptableObject
        if (levelData != null && levelData.connectPairs != null)
        {
            for (int i = 0; i < count && i < levelData.connectPairs.Count; i++)
            {
                if (pictureImages != null && i < pictureImages.Length && pictureImages[i] != null
                    && levelData.connectPairs[i].keywordSprite != null)
                {
                    pictureImages[i].sprite = levelData.connectPairs[i].keywordSprite;
                }

                if (pictureButtons != null && i < pictureButtons.Length && pictureButtons[i] != null)
                {
                    int captured = i;
                    pictureButtons[i].onClick.RemoveAllListeners();
                    pictureButtons[i].onClick.AddListener(() => OnPictureClicked(captured));
                    SetButtonState(pictureButtons[i], true);

                    // Attach Drop Triggers to picture buttons
                    AttachPictureDropTriggers(pictureButtons[i].gameObject, captured);
                }
            }
        }

        // Mascot Intro greeting
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }
    }

    private void AutoFindUIElements()
    {
        if (linesContainer == null)
        {
            Transform t = transform.Find("LinesContainer");
            if (t == null) t = transform.Find("Lines_Container");
            if (t == null) t = transform.Find("Lines");
            if (t != null) linesContainer = t;
            else
            {
                GameObject lObj = new GameObject("LinesContainer", typeof(RectTransform));
                lObj.transform.SetParent(transform, false);
                lObj.transform.SetAsFirstSibling();
                linesContainer = lObj.transform;
            }
        }

        if (letterButtons == null || letterButtons.Length == 0)
        {
            Transform leftCol = transform.Find("LettersColumn");
            if (leftCol == null) leftCol = transform.Find("Left Column");
            if (leftCol == null) leftCol = transform.Find("Letters");
            if (leftCol != null)
            {
                Button[] btns = leftCol.GetComponentsInChildren<Button>(true);
                if (btns.Length > 0) letterButtons = btns;
            }
        }

        if (pictureButtons == null || pictureButtons.Length == 0)
        {
            Transform rightCol = transform.Find("PicturesColumn");
            if (rightCol == null) rightCol = transform.Find("Right Column");
            if (rightCol == null) rightCol = transform.Find("Pictures");
            if (rightCol != null)
            {
                Button[] btns = rightCol.GetComponentsInChildren<Button>(true);
                if (btns.Length > 0) pictureButtons = btns;
            }
        }

        if (letterLabels == null || letterLabels.Length == 0)
        {
            if (letterButtons != null)
            {
                List<TextMeshProUGUI> tmps = new List<TextMeshProUGUI>();
                foreach (var b in letterButtons)
                {
                    if (b != null)
                    {
                        var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (tmp != null) tmps.Add(tmp);
                    }
                }
                letterLabels = tmps.ToArray();
            }
        }

        if (pictureImages == null || pictureImages.Length == 0)
        {
            if (pictureButtons != null)
            {
                List<Image> imgs = new List<Image>();
                foreach (var b in pictureButtons)
                {
                    if (b != null)
                    {
                        Image[] childImgs = b.GetComponentsInChildren<Image>(true);
                        foreach (var img in childImgs)
                        {
                            if (img.gameObject != b.gameObject) { imgs.Add(img); break; }
                        }
                    }
                }
                pictureImages = imgs.ToArray();
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Drag & Drop Event Triggers
    // ──────────────────────────────────────────────────────────

    private void AttachDragTriggers(GameObject obj, int letterIdx)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        // Pointer Down / Begin Drag
        EventTrigger.Entry beginEntry = new EventTrigger.Entry();
        beginEntry.eventID = EventTriggerType.PointerDown;
        beginEntry.callback.AddListener((data) => { OnStartDragLetter(letterIdx); });
        trigger.triggers.Add(beginEntry);

        // Pointer Up / End Drag
        EventTrigger.Entry endEntry = new EventTrigger.Entry();
        endEntry.eventID = EventTriggerType.PointerUp;
        endEntry.callback.AddListener((data) => { OnEndDragLetter((PointerEventData)data); });
        trigger.triggers.Add(endEntry);
    }

    private void AttachPictureDropTriggers(GameObject obj, int pictureIdx)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = obj.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            if (activeDragLetterIndex >= 0)
            {
                OnPictureClicked(pictureIdx);
            }
        });
        trigger.triggers.Add(pointerEnter);
    }

    private void OnStartDragLetter(int lIdx)
    {
        if (isProcessing || (letterConnected != null && lIdx < letterConnected.Length && letterConnected[lIdx])) return;

        OnLetterClicked(lIdx);
        activeDragLetterIndex = lIdx;

        // Create live drag line
        if (activeDragLine == null)
        {
            Transform container = linesContainer != null ? linesContainer : transform;
            activeDragLine = connectionLinePrefab != null ? Instantiate(connectionLinePrefab, container) : null;
            if (activeDragLine == null)
            {
                GameObject lObj = new GameObject("ActiveDragLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lObj.transform.SetParent(container, false);
                activeDragLine = lObj.GetComponent<Image>();
                activeDragLine.color = new Color(1f, 0.85f, 0f, 0.9f); // Gold line while dragging
            }
            activeDragLine.gameObject.SetActive(true);
            activeDragLine.transform.SetAsLastSibling();
        }
    }

    private void UpdateActiveDragLine()
    {
        if (activeDragLine == null || activeDragLetterIndex < 0 || letterButtons == null || activeDragLetterIndex >= letterButtons.Length) return;

        RectTransform from = letterButtons[activeDragLetterIndex].GetComponent<RectTransform>();
        if (from == null) return;

        Transform container = linesContainer != null ? linesContainer : transform;
        RectTransform containerRt = container.GetComponent<RectTransform>();
        if (containerRt == null) containerRt = GetComponent<RectTransform>();

        Camera uiCam = GetUICamera();

        Vector2 screenFrom = RectTransformUtility.WorldToScreenPoint(uiCam, from.position);
        Vector2 screenTo = Input.mousePosition;

        Vector2 localFrom, localTo;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRt, screenFrom, uiCam, out localFrom);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRt, screenTo, uiCam, out localTo);

        Vector2 dir = localTo - localFrom;
        float length = dir.magnitude;

        RectTransform rt = activeDragLine.rectTransform;
        rt.anchoredPosition = localFrom;
        rt.sizeDelta = new Vector2(length, 12f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void OnEndDragLetter(PointerEventData eventData)
    {
        if (activeDragLine != null)
        {
            Destroy(activeDragLine.gameObject);
            activeDragLine = null;
        }

        // Check if pointer ended over a picture button
        if (eventData != null && eventData.pointerCurrentRaycast.gameObject != null)
        {
            GameObject hoveredObj = eventData.pointerCurrentRaycast.gameObject;
            if (pictureButtons != null)
            {
                for (int i = 0; i < pictureButtons.Length; i++)
                {
                    if (pictureButtons[i] != null && (hoveredObj == pictureButtons[i].gameObject || hoveredObj.transform.IsChildOf(pictureButtons[i].transform)))
                    {
                        OnPictureClicked(i);
                        break;
                    }
                }
            }
        }

        activeDragLetterIndex = -1;
    }

    // ──────────────────────────────────────────────────────────
    //  Interaction
    // ──────────────────────────────────────────────────────────

    private void OnLetterClicked(int letterIdx)
    {
        if (isProcessing || (letterConnected != null && letterIdx < letterConnected.Length && letterConnected[letterIdx])) return;

        // Deselect previous selection without overriding button colors
        selectedLetterIndex = letterIdx;

        // Play the letter sound if available
        if (currentLevel != null && currentLevel.connectPairs != null && letterIdx < currentLevel.connectPairs.Count)
        {
            AudioClip clip = currentLevel.connectPairs[letterIdx].keywordAudio;
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
        }
    }

    private void OnPictureClicked(int pictureIdx)
    {
        if (isProcessing || selectedLetterIndex < 0) return;
        if (pictureConnected != null && pictureIdx < pictureConnected.Length && pictureConnected[pictureIdx]) return;

        bool isCorrect = (selectedLetterIndex < correctPairIndex.Length && correctPairIndex[selectedLetterIndex] == pictureIdx);
        StartCoroutine(HandleConnection(selectedLetterIndex, pictureIdx, isCorrect));
    }

    private IEnumerator HandleConnection(int lIdx, int pIdx, bool correct)
    {
        isProcessing = true;

        if (correct)
        {
            // Lock the pair
            if (letterConnected != null && lIdx < letterConnected.Length) letterConnected[lIdx]  = true;
            if (pictureConnected != null && pIdx < pictureConnected.Length) pictureConnected[pIdx] = true;
            connectedCount++;

            // Draw connecting line between letter and picture
            DrawLine(lIdx, pIdx);

            // Disable matched buttons without mutating button colors
            if (letterButtons != null && lIdx < letterButtons.Length) SetButtonState(letterButtons[lIdx],   false);
            if (pictureButtons != null && pIdx < pictureButtons.Length) SetButtonState(pictureButtons[pIdx],  false);

            // Audio: 1. Play chime audio
            PlayClip(correctChime);
            yield return new WaitForSeconds(0.4f);

            // Audio: 2. Mascot says the letter sound and the picture word!
            if (currentLevel != null && currentLevel.connectPairs != null && lIdx < currentLevel.connectPairs.Count)
            {
                AudioClip clip = currentLevel.connectPairs[lIdx].keywordAudio;
                if (clip != null && audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(clip);
                }
            }

            // Mascot cheers!
            MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
            if (mascot != null) mascot.PlayCelebrationAnimation();

            DeselectAll();

            if (connectedCount >= (letterButtons != null ? letterButtons.Length : 5))
            {
                yield return new WaitForSeconds(0.8f);
                PlayClip(completionClip);
                yield return new WaitForSeconds(0.5f);

                // Reveal Next Button via Manager when all pairs are connected!
                if (manager != null) manager.ShowNextButton();
                if (OnActivityComplete != null) OnActivityComplete.Invoke();
            }
        }
        else
        {
            // Wrong — shake the picture button and replay the letter sound as a hint
            PlayClip(wrongShake);
            yield return ShakeRoutine(pictureButtons != null && pIdx < pictureButtons.Length
                ? pictureButtons[pIdx].transform : null);

            if (currentLevel != null && currentLevel.connectPairs != null && lIdx < currentLevel.connectPairs.Count)
            {
                AudioClip clip = currentLevel.connectPairs[lIdx].keywordAudio;
                if (clip != null && audioSource != null)
                {
                    yield return new WaitForSeconds(0.3f);
                    audioSource.PlayOneShot(clip);
                }
            }
        }

        isProcessing = false;
    }

    // ──────────────────────────────────────────────────────────
    //  Line Drawing (Camera-Aware Canvas Space Math)
    // ──────────────────────────────────────────────────────────

    private void DrawLine(int lIdx, int pIdx)
    {
        if (letterButtons == null || pictureButtons == null) return;
        if (lIdx >= letterButtons.Length || pIdx >= pictureButtons.Length) return;

        RectTransform from = letterButtons[lIdx].GetComponent<RectTransform>();
        RectTransform to   = pictureButtons[pIdx].GetComponent<RectTransform>();
        if (from == null || to == null) return;

        Transform container = linesContainer != null ? linesContainer : transform;

        // Auto-create line prefab if unassigned
        Image line = connectionLinePrefab != null ? Instantiate(connectionLinePrefab, container) : null;
        if (line == null)
        {
            GameObject lineObj = new GameObject("ConnectionLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObj.transform.SetParent(container, false);
            line = lineObj.GetComponent<Image>();
            line.color = new Color(0.2f, 0.85f, 0.3f, 1f); // Vibrant green line
        }

        drawnLines.Add(line);

        // Convert world positions using UI Camera projection
        RectTransform containerRt = container.GetComponent<RectTransform>();
        if (containerRt == null) containerRt = GetComponent<RectTransform>();

        Camera uiCam = GetUICamera();

        Vector2 screenFrom = RectTransformUtility.WorldToScreenPoint(uiCam, from.position);
        Vector2 screenTo = RectTransformUtility.WorldToScreenPoint(uiCam, to.position);

        Vector2 localFrom, localTo;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRt, screenFrom, uiCam, out localFrom);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRt, screenTo, uiCam, out localTo);

        Vector2 dir    = localTo - localFrom;
        float   length = dir.magnitude;

        RectTransform rt = line.rectTransform;
        rt.anchoredPosition = localFrom;
        rt.sizeDelta        = new Vector2(length, 14f); // Thick 14px green line!
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        line.transform.SetAsLastSibling();
    }

    private void ClearLines()
    {
        if (activeDragLine != null)
        {
            Destroy(activeDragLine.gameObject);
            activeDragLine = null;
        }

        foreach (Image l in drawnLines)
            if (l != null) Destroy(l.gameObject);
        drawnLines.Clear();
    }

    // ──────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────

    private void DeselectAll()
    {
        selectedLetterIndex = -1;
        activeDragLetterIndex = -1;
    }

    private void SetButtonState(Button btn, bool interactable)
    {
        if (btn == null) return;
        btn.interactable = interactable;
    }

    private IEnumerator ShakeRoutine(Transform t)
    {
        if (t == null) yield break;
        Vector3 origin = t.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            t.localPosition = origin + new Vector3(Mathf.Sin(elapsed * 60f) * 6f, 0f, 0f);
            yield return null;
        }
        t.localPosition = origin;
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.PlayOneShot(clip);
        }
    }
}
