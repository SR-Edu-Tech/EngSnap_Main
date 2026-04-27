using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TiltleTextAnimation_SB1 : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    public float delay = 0.05f;
    public float iconDelay = 0.05f;

    [SerializeField] private string fullText;
    [SerializeField] private GameObject[] icon;
    [SerializeField] private GameObject nextButton;

    void Start()
    {
    }
    private void OnEnable()
    {
        if(icon != null)
        {
            foreach (GameObject activate in icon)
            {
                activate.SetActive(false);
            }
            if (nextButton != null)
            {
                nextButton.SetActive(false);
            }
        }
        StartCoroutine(ShowText());
        
    }

    IEnumerator ShowText()
    {
        textComponent.text = "";

        foreach (char letter in fullText)
        {
            textComponent.text += letter;
            yield return new WaitForSeconds(delay);
        }
        if(icon != null)
        {
            foreach (GameObject activate in icon)
            {
                activate.SetActive(true);
                yield return new WaitForSeconds(iconDelay);
            }
            if(nextButton != null)
            {
                yield return new WaitForSeconds(2);
                nextButton.SetActive(true);
            }
        }
    }
}
