using System.Collections;
using UnityEngine;

public class U2_S00_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] GameObject[] _cards;
    [SerializeField] int _currentcardIndex = -1;
    [SerializeField] bool _slide, _isViewed = false;

    public bool IsViewed => _isViewed;
    void OnEnable() => StartCoroutine(StartCard());
    void OnDisable()
    {
        if (_currentcardIndex > -1) _cards[_currentcardIndex].SetActive(false);
        _currentcardIndex = -1;
        _slide = false;
    }
    IEnumerator StartCard()
    {
        yield return new WaitForSeconds(3f);
        _slide = true;
    }
    public void ShowCard()
    {
        if (!_slide || _currentcardIndex >= _cards.Length - 1) return;
        if (_currentcardIndex > -1) _cards[_currentcardIndex].SetActive(false);
        if (_currentcardIndex == _cards.Length - 2)
        {
            _isViewed = true;
            GameManager_Junior1A.Instance.Next(true);
        }
        _currentcardIndex++;
        _cards[_currentcardIndex].SetActive(true);
    }
}
