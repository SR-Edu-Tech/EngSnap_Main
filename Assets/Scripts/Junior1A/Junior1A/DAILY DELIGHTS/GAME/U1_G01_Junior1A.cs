using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_G01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    enum TargetType { Greetings, Response, Both }
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] TargetType _currentTargetType;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] TargetType[] _targetType;
    [SerializeField] RectTransform _fish, _target;
    [SerializeField] int _currentclipIndex = 0;
    [SerializeField] float speed = 500;
    [SerializeField] float arcHeight = 150f;
    [SerializeField] bool _isViewed = false;
    Coroutine _coroutine, _moveCoroutine, _fishAudioCoroutine;
    void OnEnable() => _coroutine = StartCoroutine(Starter());
    public bool IsViewed => _isViewed;
    IEnumerator Starter()
    {
        _currentclipIndex = 0;
        _audioSource.clip = _introClip;
        _audioSource.Play();
        _fish.anchoredPosition = Vector3.zero;
        _fish.GetComponent<Button>().interactable = true;
        _fish.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Phrase Fish";
        _fish.gameObject.SetActive(true);
        _fish.GetChild(1).GetComponent<Image>().enabled = true;
        transform.GetChild(1).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(false);
        _fish.GetComponent<Button>().interactable = false;
        yield return new WaitForSeconds(_audioSource.clip.length / 2);
        transform.GetChild(1).gameObject.SetActive(true);
        transform.GetChild(2).gameObject.SetActive(true);
        transform.GetChild(2).GetComponent<Button>().interactable = transform.GetChild(1).GetComponent<Button>().interactable = false;
        yield return new WaitForSeconds(_audioSource.clip.length / 2);
        _fish.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentclipIndex].name;
        _fish.GetComponent<Button>().interactable = transform.GetChild(2).GetComponent<Button>().interactable = transform.GetChild(1).GetComponent<Button>().interactable = true;
        _fish.GetChild(1).GetComponent<Image>().enabled = false;
    }
    public void Response(RectTransform _target)
    {
        if (_moveCoroutine != null) return;
        this._target = _target;
        _currentTargetType = TargetType.Response;
        _moveCoroutine = StartCoroutine(CheckFishType());
    }
    public void Greeting(RectTransform _target)
    {
        if (_moveCoroutine != null) return;
        this._target = _target;
        _currentTargetType = TargetType.Greetings;
        _moveCoroutine = StartCoroutine(CheckFishType());
    }
    public void PlayAudio()
    {
        if (_fishAudioCoroutine != null) return;
        _fishAudioCoroutine = StartCoroutine(StartPlayAudio());
    }
    IEnumerator StartPlayAudio()
    {
        _audioSource.clip = _audioClips[_currentclipIndex];
        _audioSource.Play();
        _fish.GetChild(1).GetComponent<Image>().enabled = true;
        yield return new WaitForSeconds(_audioSource.clip.length);
        _fish.GetChild(1).GetComponent<Image>().enabled = false;
        _fishAudioCoroutine = null;
    }
    IEnumerator CheckFishType()
    {
        transform.GetChild(2).GetComponent<Button>().interactable = transform.GetChild(1).GetComponent<Button>().interactable = false;
        if (_targetType[_currentclipIndex] == TargetType.Both || _currentTargetType == _targetType[_currentclipIndex])
        {
            StartCoroutine(MoveFish());
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_correctClip.length);
            _currentclipIndex++;
            _fish.anchoredPosition = Vector3.zero;
            _fish.gameObject.SetActive(true);
            if (_currentclipIndex < _audioClips.Length) _fish.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentclipIndex].name;
            else
            {
                transform.GetChild(2).gameObject.SetActive(false);
                transform.GetChild(1).gameObject.SetActive(false);
                _fish.GetComponent<Button>().interactable = false;
                _fish.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Completed!";
                _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
            }
        }
        else
        {
            _fish.GetComponent<WiggleEffect_Junior1A1>().enabled = true;
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_wrongClip.length);
        }
        transform.GetChild(2).GetComponent<Button>().interactable = transform.GetChild(1).GetComponent<Button>().interactable = true;
        _moveCoroutine = null;
    }
    IEnumerator MoveFish()
    {
        Vector3 startPos = _fish.position;
        Vector3 endPos = _target.position;

        Vector3 midPoint = (startPos + endPos) * 0.5f;
        Vector3 controlPoint = midPoint + Vector3.up * arcHeight;

        float totalDist = Vector3.Distance(startPos, endPos);
        float t = 0f;

        while (t < 1f)
        {
            t += (totalDist > 0 ? speed / totalDist : 1f) * Time.deltaTime;
            t = Mathf.Clamp01(t);
            _fish.position = QuadraticBezier(startPos, controlPoint, endPos, t);

            if (t >= 0.5f)
            {
                float halfProgress = (t - 0.5f) / 0.5f;
                float zAngle = Mathf.Lerp(-90f, 0f, halfProgress);
                _fish.localEulerAngles = new Vector3(_fish.localEulerAngles.x, _fish.localEulerAngles.y, zAngle);
                float s = Mathf.Lerp(1f, 0.25f, halfProgress);
                _fish.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
        _fish.position = endPos;
        _fish.localEulerAngles = new Vector3(_fish.localEulerAngles.x, _fish.localEulerAngles.y, 0f);
        _fish.localScale = new Vector3(0.25f, 0.25f, 1f);
        _fish.gameObject.SetActive(false);
        _moveCoroutine = null;
    }
    Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return (1f - t) * (1f - t) * a + 2f * (1f - t) * t * b + t * t * c;
    }
}
