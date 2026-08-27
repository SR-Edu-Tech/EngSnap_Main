using System.Collections;
using UnityEngine;

public class UIButtonAnimation_Phonics_Junior : MonoBehaviour
{
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float tapDuration = 0.1f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        StartCoroutine(PopAnimation());
    }

    private IEnumerator PopAnimation()
    {
        transform.localScale = Vector3.zero;

        float timer = 0f;

        while (timer < popDuration)
        {
            timer += Time.deltaTime;

            float t = timer / popDuration;

            transform.localScale = Vector3.Lerp(
                Vector3.zero,
                originalScale * 1.2f,
                t
            );

            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void PlayTapAnimation()
    {
        // Prevent starting coroutine if the button game object is inactive
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(TapAnimation());
    }

    private IEnumerator TapAnimation()
    {
        float timer = 0f;

        Vector3 smallScale = originalScale * 0.9f;

        while (timer < tapDuration)
        {
            timer += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                originalScale,
                smallScale,
                timer / tapDuration
            );

            yield return null;
        }

        timer = 0f;

        while (timer < tapDuration)
        {
            timer += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                smallScale,
                originalScale,
                timer / tapDuration
            );

            yield return null;
        }

        transform.localScale = originalScale;
    }
}