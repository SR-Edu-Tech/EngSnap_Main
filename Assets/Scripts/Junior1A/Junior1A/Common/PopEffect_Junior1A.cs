using System.Collections;
using UnityEngine;

public class PopEffect_Junior1A : MonoBehaviour
{
    [SerializeField] float popDuration = 0.4f, overshoot = 2.0f;

    void OnEnable() => StartCoroutine(Pop());

    IEnumerator Pop()
    {
        if (GameManager_Junior1A.Instance) GameManager_Junior1A.Instance.Pop();
        transform.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float c3 = overshoot + 1f;
            float scaleMultiplier = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);

            transform.localScale = Vector3.one * scaleMultiplier;

            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
