using Junior2B;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class U7_S00_Junior2B : MonoBehaviour, Interfaces_Junior2B
{
    [SerializeField] GameObject[] _cards;
    [SerializeField] AudioClip _introClip;
    [SerializeField] bool _isViewed = false;

    public bool IsViewed => _isViewed;
    void OnEnable() => StartCoroutine(StartCard());
    IEnumerator StartCard()
    {
        foreach (GameObject card in _cards) card.SetActive(false);
        yield return new WaitForSeconds(_introClip.length / 2);
        foreach (GameObject card in _cards)
        {
            card.SetActive(true);
            yield return new WaitForSeconds(1f);
        }
        _isViewed = true;
        GameManager_Junior2B.Instance.Next(true);
    }
}
