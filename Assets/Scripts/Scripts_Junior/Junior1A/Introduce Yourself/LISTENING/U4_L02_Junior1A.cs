using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U4_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _question, _response;
    [SerializeField] bool _isViewed = false, _didSlided = false, _didTab1 = false, _didTab2 = false;
    [SerializeField] CanvasGroup _canvasParent;
    [SerializeField] RectTransform _tab1 , _tab2;
    Coroutine _coroutine;

    public bool IsViewed => _isViewed;
    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _didSlided = _didTab2 = _didTab1 = false;
        _tab1.anchorMin = new Vector2(.5f, .5f);
        _tab1.anchorMax = new Vector2(.5f, .5f);
        _tab2.anchorMin = new Vector2(.5f, .5f);
        _tab2.anchorMax = new Vector2(.5f, .5f);

        _tab1.anchoredPosition = Vector3.up * 50;
        _tab2.anchoredPosition = Vector3.down * 250;

        _tab1.gameObject.SetActive(false);
        _tab2.gameObject.SetActive(false);
        _tab1.GetComponent<PopEffect_Junior1A>().enabled = true;
        _tab2.GetComponent<PopEffect_Junior1A>().enabled = true;
        _canvasParent.alpha = 0f;
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        _tab1.gameObject.SetActive(true);
        _tab2.gameObject.SetActive(true);
    }
    public void MoveTabs(int index)
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
       _coroutine = StartCoroutine(MoveTab(index));
    }
    IEnumerator MoveTab(int index)
    {
        if(index == 0)
        {
            _didTab1 = true;
            _canvasParent.transform.GetChild(0).GetComponent<Image>().sprite = _tab1.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite;
            _canvasParent.transform.GetChild(1).GetComponent<Image>().sprite = _tab1.GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite;
        }
        else
        {
            _didTab2 = true;
            _canvasParent.transform.GetChild(0).GetComponent<Image>().sprite = _tab2.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite;
            _canvasParent.transform.GetChild(1).GetComponent<Image>().sprite = _tab2.GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite;
        }
        if(_didTab1 && _didTab2)
        {
            GameManager_Junior1A.Instance.Next(true);
            _isViewed = true;
        }
        _canvasParent.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
        _canvasParent.transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
        _canvasParent.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _question[index].name + "?";
        _canvasParent.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _response[index].name;

        if (!_didSlided)
        {
            Vector3 worldPos1 = _tab1.position;
            Vector3 worldPos2 = _tab2.position;

            _tab1.anchorMin = new Vector2(1f, .5f);
            _tab1.anchorMax = new Vector2(1f, .5f);
            _tab2.anchorMin = new Vector2(1f, .5f);
            _tab2.anchorMax = new Vector2(1f, .5f);

            _tab1.position = worldPos1;
            _tab2.position = worldPos2;

            Vector2 startPos1 = _tab1.anchoredPosition;
            Vector2 startPos2 = _tab2.anchoredPosition;
            Vector2 targetPos1 = new Vector2(-250f, startPos1.y);
            Vector2 targetPos2 = new Vector2(-250f, startPos2.y);

            float slideSpeed = 2.5f;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
                _tab1.anchoredPosition = Vector2.LerpUnclamped(startPos1, targetPos1, easedT);
                _tab2.anchoredPosition = Vector2.LerpUnclamped(startPos2, targetPos2, easedT);
                _canvasParent.alpha = easedT;
                yield return null;
            }

            _tab1.anchoredPosition = targetPos1;
            _tab2.anchoredPosition = targetPos2;
            _canvasParent.alpha = 1f;
            _didSlided = true;
        }
        _audioSource.clip = _question[index];
        _audioSource.Play();
        _canvasParent.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);

        yield return new WaitForSeconds(_audioSource.clip.length);

        _audioSource.clip = _response[index];
        _audioSource.Play();
        _canvasParent.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);

        yield return new WaitForSeconds(_audioSource.clip.length);
    }
}
