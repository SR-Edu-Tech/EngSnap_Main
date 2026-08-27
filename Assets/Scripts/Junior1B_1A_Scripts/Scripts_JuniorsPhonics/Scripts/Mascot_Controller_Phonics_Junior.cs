using System.Collections;
using UnityEngine;

public class MascotController_Phonics_Junior : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public bool IsVisible => gameObject.activeSelf;

    private Vector3 originalLocalPos;
    private Vector3 originalScale;
    private bool isBouncing = false;
    private bool isDemoing = false;

    private Transform cachedBone6;

    private void Awake()
    {
        FindBone6();
        EnsureFrontVisibility();
    }

    private void Start()
    {
        FindBone6();
        EnsureFrontVisibility();
    }

    private void OnEnable()
    {
        var scripts = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var script in scripts)
        {
            if (script != null && script.GetType().Name == "SpriteSkin")
            {
                script.enabled = true;
            }
        }
        FindBone6();
        EnsureFrontVisibility();
    }

    private void OnDisable()
    {
        var scripts = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var script in scripts)
        {
            if (script != null && script.GetType().Name == "SpriteSkin" && script.enabled)
            {
                script.enabled = false;
            }
        }
    }

    private void FindBone6()
    {
        if (cachedBone6 == null)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name == "bone_6")
                {
                    cachedBone6 = t;
                    break;
                }
            }
        }
    }

    public void EnsureFrontVisibility()
    {
        // 1. Ensure local scale is not 0
        if (transform.localScale.sqrMagnitude < 0.01f)
        {
            transform.localScale = Vector3.one;
        }

        // 2. Ensure UI Canvas or Sorting Group is set to render in front of background panels
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder    = 100;
        }

        UnityEngine.Rendering.SortingGroup sg = GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sg != null)
        {
            sg.sortingOrder = 100;
        }
    }

    public void ShowMascot()
    {
        gameObject.SetActive(true);
        Transform p = transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            p = p.parent;
        }

        EnsureFrontVisibility();
        transform.SetAsLastSibling();
    }

    public void HideMascot()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public void PlayHiAnimation()
    {
        ShowMascot();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.ResetTrigger("Hi");
            animator.SetTrigger("Hi");
        }
    }

    // Dedicated Throat Touch Demo Animation: Rotates bone_6 by 135° to touch throat!
    public void PlayThroatTouchDemoAnimation(float duration = 3.5f)
    {
        ShowMascot();
        if (!isDemoing)
        {
            StartCoroutine(ThroatTouchRoutine(duration));
        }
    }

    private IEnumerator ThroatTouchRoutine(float duration)
    {
        isDemoing = true;
        FindBone6();

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Temporarily disable Animator so it doesn't fight bone_6 rotation
        if (animator != null) animator.enabled = false;

        Vector3 mascotBasePos = transform.localPosition;

        if (cachedBone6 != null)
        {
            Vector3 startEuler = cachedBone6.localEulerAngles;
            Vector3 targetEuler = startEuler + new Vector3(0f, 0f, 135f);

            // 1. Smoothly swing arm UP to throat (0.3s)
            float swingTime = 0.3f;
            float elapsed = 0f;
            while (elapsed < swingTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / swingTime;
                cachedBone6.localEulerAngles = Vector3.Lerp(startEuler, targetEuler, t);
                yield return null;
            }
            cachedBone6.localEulerAngles = targetEuler;

            // 2. Hold hand on throat gently during speech audio
            float holdDuration = Mathf.Max(0.5f, duration - 0.6f);
            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.deltaTime;

                // Gentle subtle arm touch wobble
                float armBuzz = Mathf.Sin(elapsed * 25f) * 3f;
                cachedBone6.localEulerAngles = targetEuler + new Vector3(0f, 0f, armBuzz);

                yield return null;
            }

            // 3. Smoothly swing arm BACK DOWN to side (0.3s)
            elapsed = 0f;
            while (elapsed < swingTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / swingTime;
                cachedBone6.localEulerAngles = Vector3.Lerp(targetEuler, startEuler, t);
                yield return null;
            }
            cachedBone6.localEulerAngles = startEuler;
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        // Re-enable Animator
        if (animator != null) animator.enabled = true;
        transform.localPosition = mascotBasePos;

        isDemoing = false;
    }

    // Celebration Jump & Squish Bounce for Rewards & Success
    public void PlayCelebrationAnimation()
    {
        ShowMascot();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.ResetTrigger("Hi");
            animator.SetTrigger("Hi");
        }

        if (!isBouncing)
        {
            StartCoroutine(BounceRoutine());
        }
    }

    private IEnumerator BounceRoutine()
    {
        isBouncing = true;
        originalLocalPos = transform.localPosition;
        originalScale = transform.localScale;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float yOffset = Mathf.Sin(t * Mathf.PI) * 40f;
            transform.localPosition = originalLocalPos + new Vector3(0f, yOffset, 0f);

            float scaleY = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            float scaleX = 1f - Mathf.Sin(t * Mathf.PI) * 0.10f;
            transform.localScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z);

            yield return null;
        }

        transform.localPosition = originalLocalPos;
        transform.localScale = originalScale;
        isBouncing = false;
    }
}