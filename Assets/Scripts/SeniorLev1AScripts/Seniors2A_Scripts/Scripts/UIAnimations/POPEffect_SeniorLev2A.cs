using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POPEffect_SeniorLev2A : MonoBehaviour
{
    [SerializeField] float popDuration = 0.4f, overshoot = 2.0f;
    [SerializeField] bool shouldDisable = false;

    void OnEnable() => StartCoroutine(Pop());

    IEnumerator Pop()
    {
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
        if (shouldDisable) this.enabled = false;
    }
}