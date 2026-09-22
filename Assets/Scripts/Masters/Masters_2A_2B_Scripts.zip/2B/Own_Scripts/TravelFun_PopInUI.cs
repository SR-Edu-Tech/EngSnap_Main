using UnityEngine;
using DG.Tweening;

public class TravelFun_PopInUI : MonoBehaviour
{
    [Header("Pop Settings")]
    [SerializeField] private float delay = 0f;
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float startScale = 0f;
    [SerializeField] private float overshoot = 1.2f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        PlayPop();
    }

    public void PlayPop()
    {
        transform.DOKill();

        transform.localScale = Vector3.one * startScale;

        transform
            .DOScale(originalScale, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutBack, overshoot);
    }
}