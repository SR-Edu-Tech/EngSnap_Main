using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseAnimation_SeniorLev1A : MonoBehaviour
{
    public float speed = 2f;          // how fast it pulses
    public float scaleAmount = 0.08f; // how big the pulse is

    private Vector3 baseScale;

    void OnEnable()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1f + Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = baseScale * scale;
    }
}