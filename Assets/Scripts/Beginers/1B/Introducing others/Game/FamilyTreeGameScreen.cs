using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  FamilyTreeGameScreen
///  Controls Screen 1: The Family Tree drag-and-drop game.
///  Coordinates frame evaluation, card snapback, sparkles, audio feedback,
///  and transition to Screen 2.
/// ════════════════════════════════════════════════════════════════════
/// </summary>
public class FamilyTreeGameScreen : MonoBehaviour
{
    [Header("Frames & Portraits")]
    [Tooltip("All empty slots on the family tree")]
    public FamilyTreeFrame[] treeFrames;
    [Tooltip("All draggable family portraits in the tray")]
    public FamilyPortraitCard[] portraitCards;

    [Header("Visual Areas")]
    [Tooltip("The main container of the family tree itself, used for celebration scaling")]
    public RectTransform familyTreeContainer;
    [Tooltip("The canvas background or screen area used as parent during dragging")]
    public RectTransform dragParentArea;
    [Tooltip("The parent object for spawned sparkles")]
    public RectTransform sparkleRoot;
    [Tooltip("Optional sparkle/star prefab")]
    public GameObject sparklePrefab;

    [Header("UI Controls")]
    [Tooltip("Robin mascot speech bubble text")]
    public TMP_Text robinSpeechText;
    [Tooltip("Button to transition to Screen 2")]
    public Button nextButton;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("Audio Clips")]
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip completeFanfare;

    private IntroducingOthersGameController _controller;
    private Dictionary<FamilyPortraitCard, Vector2> _cardInitialPositions = new Dictionary<FamilyPortraitCard, Vector2>();
    private bool _isCelebrating = false;

    void Awake()
    {
        // Cache the initial positions of the portrait cards in the tray
        foreach (var card in portraitCards)
        {
            if (card != null)
            {
                _cardInitialPositions[card] = card.RectTransform.anchoredPosition;
            }
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    /// <summary>
    /// Initialises the screen and resets gameplay state.
    /// </summary>
    public void Initialise(IntroducingOthersGameController controller)
    {
        _controller = controller;
        ResetScreen();
    }

    private void ResetScreen()
    {
        _isCelebrating = false;
        StopAllCoroutines();

        // Reset frames
        foreach (var frame in treeFrames)
        {
            if (frame != null) frame.ResetFrame();
        }

        // Reset and animate portrait cards pop-in
        foreach (var card in portraitCards)
        {
            if (card != null)
            {
                Vector2 initialPos = _cardInitialPositions.ContainsKey(card) ? _cardInitialPositions[card] : card.RectTransform.anchoredPosition;
                card.Initialise(this, dragParentArea, initialPos);
                
                // Pop-in animation
                card.transform.localScale = Vector3.zero;
                card.transform.DOScale(Vector3.one, 0.5f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(Random.Range(0f, 0.3f));
            }
        }

        // Hide NEXT button initially
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = false;
        }

        SetRobinSpeech("Drag each family portrait to the correct frame!");
    }

    /// <summary>
    /// Evaluates a dropped portrait card. Called from FamilyPortraitCard.OnEndDrag.
    /// </summary>
    public void OnPortraitDropped(FamilyPortraitCard card)
    {
        if (_isCelebrating) return;

        FamilyTreeFrame targetFrame = FindOverlappingFrame(card);

        if (targetFrame != null)
        {
            // Frame is hit. Is it the correct one?
            if (targetFrame.relativeId == card.relativeId && !targetFrame.IsFilled)
            {
                // ✅ CORRECT SNAPS IN
                targetFrame.IsFilled = true;
                card.SnapToFrame(targetFrame);
                targetFrame.PlayCorrectAnimation();

                // Sparkles at the frame location
                SpawnSparkles(targetFrame.RectTransform.position);

                // Play correct audio
                PlaySFX(correctClip);

                // Play character voice line and update speech bubble
                if (card.voiceLine != null && voiceSource != null)
                {
                    voiceSource.clip = card.voiceLine;
                    voiceSource.Play();
                }

                string nameText = card.labelText != null ? card.labelText.text : card.relativeId;
                string heShe = (card.relativeId == "father" || card.relativeId == "brother" || card.relativeId == "grandpa") ? "He" : "She";
                SetRobinSpeech($"Correct!");
                StartCoroutine(PunchSpeechBubble());

                // Check for completion
                if (CheckAllFramesFilled())
                {
                    StartCoroutine(CelebrateGameComplete());
                }
            }
            else
            {
                // ❌ WRONG FRAME (OR ALREADY FILLED)
                PlaySFX(wrongClip);
                
                // Shake card slightly
                card.RectTransform.DOShakePosition(0.3f, 15f, 10, 90f, false, true);

                // Show correct frame hint glow
                FamilyTreeFrame correctFrame = FindFrameById(card.relativeId);
                if (correctFrame != null)
                {
                    correctFrame.PlayHintGlow();
                }

                // Send card back to tray
                card.ReturnToTray();
                SetRobinSpeech("Oops! Let's find the correct spot! 😊");
            }
        }
        else
        {
            // Dropped in empty space — send back to tray
            card.ReturnToTray();
        }
    }

    private FamilyTreeFrame FindOverlappingFrame(FamilyPortraitCard card)
    {
        foreach (var frame in treeFrames)
        {
            if (frame == null || frame.IsFilled) continue;
            
            // Check if card RectTransform overlaps frame dropZone
            if (RectOverlaps(card.RectTransform, frame.dropZone))
            {
                return frame;
            }
        }
        return null;
    }

    private FamilyTreeFrame FindFrameById(string relativeId)
    {
        foreach (var frame in treeFrames)
        {
            if (frame != null && frame.relativeId == relativeId)
                return frame;
        }
        return null;
    }

    private bool CheckAllFramesFilled()
    {
        foreach (var frame in treeFrames)
        {
            if (frame != null && !frame.IsFilled)
                return false;
        }
        return true;
    }

    private IEnumerator CelebrateGameComplete()
    {
        _isCelebrating = true;
        yield return new WaitForSeconds(0.4f);

        // Play fanfare sound
        PlaySFX(completeFanfare);
        SetRobinSpeech("Fantastic! The family tree is complete! 🌟🎉");

        // Scale the entire tree in a heartbeat celebration pulse
        if (familyTreeContainer != null)
        {
            familyTreeContainer.DOPunchScale(new Vector3(0.04f, 0.04f, 0f), 1.2f, 4, 0.5f);
        }

        // Spawn a series of celebration sparkles from different frames
        float timer = 0f;
        while (timer < 1.5f)
        {
            int randIndex = Random.Range(0, treeFrames.Length);
            if (treeFrames[randIndex] != null)
            {
                SpawnSparkles(treeFrames[randIndex].RectTransform.position);
            }
            yield return new WaitForSeconds(0.2f);
            timer += 0.2f;
        }

        // Activate and pop-in NEXT button
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.transform.localScale = Vector3.zero;
            nextButton.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
    }

    // ── UI Sparkles Generator ────────────────────────────────────────

    private void SpawnSparkles(Vector3 worldPos)
    {
        if (sparklePrefab == null || sparkleRoot == null) return;
        
        // Spawn 8 sparkles expanding outward
        for (int i = 0; i < 8; i++)
        {
            StartCoroutine(SingleSparkleCoroutine(worldPos, i));
        }
    }

    private IEnumerator SingleSparkleCoroutine(Vector3 worldPos, int index)
    {
        // Stagger spawn times slightly
        yield return new WaitForSeconds(index * 0.03f);

        GameObject go = Instantiate(sparklePrefab, sparkleRoot);
        RectTransform rt = go.GetComponent<RectTransform>();
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // Convert world position to local coordinates inside sparkle root
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sparkleRoot,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null, out localPos);
        rt.anchoredPosition = localPos;

        // Random velocity direction and distance
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(50f, 110f);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        float duration = Random.Range(0.45f, 0.65f);
        float time = 0f;
        Vector2 startPos = localPos;
        
        while (time < duration)
        {
            
            if (go == null) yield break;
            time += Time.deltaTime;
            float p = time / duration;
            
            rt.anchoredPosition = startPos + dir * dist * EaseOutQuad(p);
            cg.alpha = 1f - p;
            
            float scale = Mathf.Lerp(1.3f, 0.1f, p);
            rt.localScale = Vector3.one * scale;
            
            // Spin sparkle
            rt.Rotate(new Vector3(0f, 0f, 180f * Time.deltaTime));
            
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ── Helper math ──────────────────────────────────────────────────

    private bool RectOverlaps(RectTransform a, RectTransform b)
    {
        Vector3[] cornersA = new Vector3[4];
        Vector3[] cornersB = new Vector3[4];
        a.GetWorldCorners(cornersA);
        b.GetWorldCorners(cornersB);

        Rect ra = new Rect(cornersA[0].x, cornersA[0].y,
                           cornersA[2].x - cornersA[0].x,
                           cornersA[2].y - cornersA[0].y);
        Rect rb = new Rect(cornersB[0].x, cornersB[0].y,
                           cornersB[2].x - cornersB[0].x,
                           cornersB[2].y - cornersB[0].y);
        return ra.Overlaps(rb);
    }

    private float EaseOutQuad(float t)
    {
        return t * (2f - t);
    }

    // ── Audio and Dialog Helpers ─────────────────────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void SetRobinSpeech(string text)
    {
        if (robinSpeechText != null)
        {
            robinSpeechText.text = text;
        }
    }

    private IEnumerator PunchSpeechBubble()
    {
        if (robinSpeechText == null) yield break;
        Vector3 orig = robinSpeechText.transform.localScale;
        robinSpeechText.transform.localScale = orig * 1.2f;
        yield return new WaitForSeconds(0.12f);
        robinSpeechText.transform.localScale = orig;
    }

    private void OnNextClicked()
    {
        // For game 1 completion, transition to Screen 2
        _controller?.ShowScreen2();
    }
}
