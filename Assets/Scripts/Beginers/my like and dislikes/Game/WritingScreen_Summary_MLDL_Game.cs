using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
//  WritingScreen_Summary_MLDL_Game
//
//  Shared summary used after Panel A and Panel B.
//  Caller passes an onNext Action — summary fires it when Next is tapped.
//
//  IMPORTANT: caller must SetActive(true) on this GameObject BEFORE calling
//  Initialise(), so that StartCoroutine works immediately.
// ─────────────────────────────────────────────────────────────────────────────
public class WritingScreen_Summary_MLDL_Game : MonoBehaviour
{
    [Header("─── Text ────────────────────────────────────────────")]
    public TextMeshProUGUI youLikeText;
    public TextMeshProUGUI youDontLikeText;

   // [Header("─── Panels (entrance animation) ────────────────────")]
    //public RectTransform   likePanel;
    //public RectTransform   dontLikePanel;

    [Header("─── Highlight Colours ─────────────────────────────")]
    public Color likeColor     = new Color(0.2f, 0.85f, 0.4f, 1f);
    public Color dontLikeColor = new Color(0.95f, 0.3f, 0.3f, 1f);

    [Header("─── Audio ──────────────────────────────────────────")]
    public AudioSource sfxAudio;
    public AudioClip   sfxSummaryAppear;
    public AudioClip   sfxNextButton;

    [Header("─── Buttons ────────────────────────────────────────")]
    public Button        nextButton;
    public RectTransform nextButtonRect;

    // ─────────────────────────────────────────────────────────────────────
    private System.Action _onNext;

    private AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0f,    0f,   0f,  6f),
        new Keyframe(0.65f, 1.1f, 0f,  0f),
        new Keyframe(1f,    1f,   0f,  0f));

    // ─────────────────────────────────────────────────────────────────────
    //  Call this AFTER SetActive(true) on this GameObject.
    // ─────────────────────────────────────────────────────────────────────
    public void Initialise(List<string> yesFoods, List<string> noFoods,
                           System.Action onNext)
    {
        _onNext = onNext;

        string yesStr = yesFoods != null && yesFoods.Count > 0
                        ? string.Join(", ", yesFoods) : "nothing";
        string noStr  = noFoods  != null && noFoods.Count  > 0
                        ? string.Join(", ", noFoods)  : "nothing";

        if (youLikeText    != null)
            youLikeText.text    = $"You like: <color=#{ColorUtility.ToHtmlStringRGB(likeColor)}>{yesStr}</color>.";
        if (youDontLikeText != null)
            youDontLikeText.text = $"You don't like: <color=#{ColorUtility.ToHtmlStringRGB(dontLikeColor)}>{noStr}</color>.";

        // Hide next button — it reappears at end of EntranceSequence
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        StopAllCoroutines();
        StartCoroutine(EntranceSequence());
    }

    // ─────────────────────────────────────────────────────────────────────
    IEnumerator EntranceSequence()
    {
        // Reset panel positions and scales
       // SetAnchoredX(likePanel,     -700f);
       // SetAnchoredX(dontLikePanel,  700f);
       // SetScale(likePanel,     0f);
      //  SetScale(dontLikePanel, 0f);

        yield return new WaitForSeconds(0.3f);

        PlaySFX(sfxSummaryAppear);

       // StartCoroutine(SlideAndPop(likePanel,    -700f, 0f, 0.45f));
        yield return new WaitForSeconds(0.2f);

       // StartCoroutine(SlideAndPop(dontLikePanel, 700f, 0f, 0.45f));
        yield return new WaitForSeconds(0.55f);   // wait for both panels to finish

        // Pop in Next button
        if (nextButtonRect != null) SetScale(nextButtonRect, 0f);
        if (nextButton     != null) nextButton.gameObject.SetActive(true);
        if (nextButtonRect != null) yield return StartCoroutine(ScalePop(nextButtonRect, 0.4f));
    }

    IEnumerator SlideAndPop(RectTransform rt, float fromX, float toX, float dur)
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            rt.localScale = new Vector3(bounceCurve.Evaluate(t), bounceCurve.Evaluate(t), 1f);
            SetAnchoredX(rt, Mathf.Lerp(fromX, toX, bounceCurve.Evaluate(t)));
            yield return null;
        }
        rt.localScale = Vector3.one;
        SetAnchoredX(rt, toX);
    }

    IEnumerator ScalePop(RectTransform rt, float dur)
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = bounceCurve.Evaluate(Mathf.Clamp01(elapsed / dur));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnNextClicked()
    {
        PlaySFX(sfxNextButton);
        gameObject.SetActive(false);
        _onNext?.Invoke();
    }

    void PlaySFX(AudioClip clip)             { if (sfxAudio != null && clip != null) sfxAudio.PlayOneShot(clip); }
    void SetScale(RectTransform rt, float s) { if (rt != null) rt.localScale = new Vector3(s, s, 1f); }
    void SetAnchoredX(RectTransform rt, float x) { if (rt != null) rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y); }
}