using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlowerBehaviour : MonoBehaviour
{
    private Image flowerImage;

    private void Awake()
    {
        flowerImage = GetComponent<Image>();
        ResetFill();
    }

    public void ResetFill()
    {
        if (flowerImage == null)
        {
            flowerImage = GetComponent<Image>();
        }

        if (flowerImage != null)
        {
            flowerImage.fillAmount = 0f;
        }
    }

    public void IncrementFill(int starIndex, float duration = 0.5f)
    {
        IncrementFill(starIndex, 4, duration);
    }

    public void IncrementFill(int starIndex, int totalSteps, float duration = 0.5f)
    {
        if (flowerImage == null)
        {
            flowerImage = GetComponent<Image>();
        }

        int steps = totalSteps > 0 ? totalSteps : 4;
        float targetFill = (1.0f / steps) * (starIndex + 1);
        StopAllCoroutines();
        StartCoroutine(AnimateFlowerFill(targetFill, duration));
    }

    private IEnumerator AnimateFlowerFill(float targetFill, float duration)
    {
        if (flowerImage == null) yield break;
        float startFill = flowerImage.fillAmount;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            flowerImage.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / duration);
            yield return null;
        }
        flowerImage.fillAmount = targetFill;
    }
}
