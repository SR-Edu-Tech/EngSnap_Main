using Junior2A;
using System.Collections;
using UnityEngine;

namespace Junior2A
{
    public class WiggleEffect_Junior2A : MonoBehaviour
    {
        [SerializeField] float wiggleDuration = 0.5f;
        [SerializeField] float intensity = 25f;

        void OnEnable() => StartCoroutine(Wiggle());

        void OnDisable()
        {
            StopAllCoroutines();
            transform.localRotation = Quaternion.identity;
            this.enabled = false;
        }

        IEnumerator Wiggle()
        {
            float elapsed = 0f;

            while (elapsed < wiggleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / wiggleDuration;
                float angle = intensity * Mathf.Sin(t * Mathf.PI * 6f) * (1f - t);
                transform.localRotation = Quaternion.identity * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            transform.localRotation = Quaternion.identity;
            this.enabled = false;
        }
    }
}