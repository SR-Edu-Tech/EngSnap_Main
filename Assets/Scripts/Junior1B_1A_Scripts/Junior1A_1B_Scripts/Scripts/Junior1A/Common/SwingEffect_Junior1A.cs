using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwingEffect_Junior1A : MonoBehaviour
{
    [SerializeField] RectTransform _targerObj;
    [SerializeField] float _startAngleX = 90f;
    [SerializeField] float _swingFrequency = 0.5f;
    [SerializeField] float _swingDuration = 5f;

    void OnEnable() => StartCoroutine(Swing());
    IEnumerator Swing()
    {
        _targerObj = GetComponent<RectTransform>();
        Vector3 initialEuler = _targerObj.localEulerAngles;
        float targetY = initialEuler.y;
        float targetZ = initialEuler.z;

        float elapsed = 0f;
        while (elapsed < _swingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / _swingDuration);
            float dampen = 1f - progress;
            float currentAngleX = Mathf.Cos(elapsed * _swingFrequency * 2f * Mathf.PI) * _startAngleX * dampen;

            _targerObj.localRotation = Quaternion.Euler(currentAngleX, targetY, targetZ);
            yield return null;
        }
        _targerObj.localRotation = Quaternion.Euler(0f, targetY, targetZ);
    }
}
