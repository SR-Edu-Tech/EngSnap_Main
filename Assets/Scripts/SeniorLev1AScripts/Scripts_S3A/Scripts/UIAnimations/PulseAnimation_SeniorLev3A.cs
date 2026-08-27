using UnityEngine;

public class PulseAnimation_SeniorLev3A : MonoBehaviour
{
    public float speed = 2f;
    public float scaleAmount = 0.08f;

    private readonly Vector3 baseScale = Vector3.one;

    void OnEnable()
    {
        transform.localScale = baseScale;
    }

    void OnDisable()
    {
        transform.localScale = baseScale;
    }

    void Update()
    {
        float scale = 1f + ((Mathf.Sin(Time.time * speed) + 1f) * 0.5f) * scaleAmount;
        transform.localScale = baseScale * scale;
    }
}