using Junior2A;
using System.Collections;
using UnityEngine;

public class U6_S00_Junior2A : MonoBehaviour, Interfaces_Junior2A
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
        GameManager_Junior2A.Instance.Next(true);
    }
}
