using System.Collections;
using UnityEngine;
using Junior2A; // Hooks into the GameManager namespace seamlessly

public class PopEffect_Junior2A : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Slowing this down makes the card grow and bounce more gradually.")]
    [SerializeField] private float popDuration = 0.8f;
    [SerializeField] private float overshoot = 2.0f;
    [SerializeField] private bool shouldDisable = false;

    public float PopDuration => popDuration;

    private Vector3 _targetScale = Vector3.one;

    private void Awake()
    {
        _targetScale = transform.localScale;
    }

    private void OnEnable() => StartCoroutine(Pop());

    private IEnumerator Pop()
    {
        // Added a quick null-check to prevent any background crashes
        if (GameManager_Junior2A.Instance != null)
        {
            GameManager_Junior2A.Instance.Pop();
        }

        transform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float c3 = overshoot + 1f;
            float scaleMultiplier = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);

            transform.localScale = _targetScale * scaleMultiplier;

            yield return null;
        }

        transform.localScale = _targetScale;
        if (shouldDisable) this.enabled = false;
    }
}