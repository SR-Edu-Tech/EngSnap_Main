using System.Collections;
using UnityEngine;

public class SlideEffect_Junior1A : MonoBehaviour
{
    [SerializeField] RectTransform _targetObj;
    [SerializeField] Vector3 _offset;
    [SerializeField] float _slideSpeed = 2.5f;
    [SerializeField] float _overshoot = 1.5f;
    [SerializeField] bool _slideToOriginZero = false;
    public Vector3 _targetPosition;

    Coroutine _slideCoroutine;

    void Awake()
    {
        _targetObj = GetComponent<RectTransform>();
        _targetPosition = _slideToOriginZero ? Vector3.zero : _targetObj.anchoredPosition3D;
    }

    void OnEnable()
    {
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlideRoutine());
    }

    IEnumerator SlideRoutine()
    {
        Vector3 startPosition = _targetPosition + _offset;
        _targetObj.anchoredPosition3D = startPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * _slideSpeed;
            float easedT = BackEaseOut(Mathf.Clamp01(t), _overshoot);
            _targetObj.anchoredPosition3D = Vector3.LerpUnclamped(startPosition, _targetPosition, easedT);
            yield return null;
        }
        _targetObj.anchoredPosition3D = _targetPosition;
        _slideCoroutine = null;
    }

    float BackEaseOut(float t, float overshoot)
    {
        t -= 1f;
        return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
    }
}
