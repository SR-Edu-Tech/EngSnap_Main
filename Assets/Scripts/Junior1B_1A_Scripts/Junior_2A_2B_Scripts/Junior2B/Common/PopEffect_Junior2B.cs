using Junior2B;
using System.Collections;
using UnityEngine;

public class Popeffect_Junior2B : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Slowing this down makes the card grow and bounce more gradually.")]
    [SerializeField] private float popDuration = 0.8f; 
    [SerializeField] private float overshoot = 2.0f;
    [SerializeField] private bool shouldDisable = false;

    // Public Property accessor so our main script knows exactly how long to wait
    public float PopDuration => popDuration;

    private Vector3 _targetScale = Vector3.one;

    private void Awake()
    {
        // 🔧 Save whatever scale (e.g., 1.6) you configured in the Inspector before animating
        _targetScale = transform.localScale; 
    }

    private void OnEnable() => StartCoroutine(Pop());

    private IEnumerator Pop()
    {
        if (GameManager_Junior2B.Instance != null) GameManager_Junior2B.Instance.Pop();
        
        transform.localScale = Vector3.zero;
        float elapsed = 0f;
        
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float c3 = overshoot + 1f;
            float scaleMultiplier = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);

            // 🔧 Multiply the calculation by your design scale instead of hardcoded Vector3.one
            transform.localScale = _targetScale * scaleMultiplier;

            yield return null;
        }
        
        // 🔧 Safe termination back to your intended 1.6 value
        transform.localScale = _targetScale;
        if (shouldDisable) this.enabled = false;
    }
}