using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U10_G01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    enum TargetType { Greetings, Response, Both }
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] TargetType _currentTargetType;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] TargetType[] _targetType;

    [Header("UI Element Explicit References")]
    [SerializeField] RectTransform _fish;
    [SerializeField] TextMeshProUGUI _fishText;       // Drag the Fish Text component here
    [SerializeField] Image _fishAudioPlayingIcon;     // Drag the Fish active audio icon component here (if it exists)
    [SerializeField] Button _greetingButton;          // Drag the Greeting button component here
    [SerializeField] Button _responseButton;          // Drag the Response button component here

    [SerializeField] RectTransform _target;
    [SerializeField] int _currentclipIndex = 0;
    [SerializeField] float speed = 500;
    [SerializeField] float arcHeight = 150f;
    [SerializeField] bool _isViewed = false;

    [Header("Hint Image Configuration")]
    [SerializeField] Image _hintImageComponent;
    [SerializeField] Sprite[] _hintSprites;

    Coroutine _coroutine, _moveCoroutine, _fishAudioCoroutine;
    void OnEnable() => _coroutine = StartCoroutine(Starter());
    public bool IsViewed => _isViewed;

    IEnumerator Starter()
    {
        _currentclipIndex = 0;
        _audioSource.clip = _introClip;
        _audioSource.Play();

        if (_fish != null)
        {
            _fish.anchoredPosition = Vector3.zero;
            if (_fish.TryGetComponent(out Button fishBtn)) fishBtn.interactable = true;
            if (_fish.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
            _fish.gameObject.SetActive(true);
        }

        if (_fishText != null) _fishText.text = "Phrase Fish";
        if (_fishAudioPlayingIcon != null) _fishAudioPlayingIcon.enabled = true;

        // Safely turn off choice buttons during intro narration
        if (_greetingButton != null) _greetingButton.gameObject.SetActive(false);
        if (_responseButton != null) _responseButton.gameObject.SetActive(false);

        if (_fish.TryGetComponent(out Button b)) b.interactable = false;

        UpdateHintImage();

        yield return new WaitForSeconds(_audioSource.clip.length / 2);

        // Turn buttons back on safely
        if (_greetingButton != null)
        {
            _greetingButton.gameObject.SetActive(true);
            _greetingButton.interactable = false;
            if (_greetingButton.TryGetComponent(out PopEffect_Junior2A p)) p.enabled = true;
        }
        if (_responseButton != null)
        {
            _responseButton.gameObject.SetActive(true);
            _responseButton.interactable = false;
            if (_responseButton.TryGetComponent(out PopEffect_Junior2A p)) p.enabled = true;
        }

        yield return new WaitForSeconds(_audioSource.clip.length / 2);

        if (_currentclipIndex < _audioClips.Length && _audioClips[_currentclipIndex] != null)
        {
            if (_fishText != null) _fishText.text = _audioClips[_currentclipIndex].name;
        }

        if (_fish.TryGetComponent(out Button b2)) b2.interactable = true;
        if (_greetingButton != null) _greetingButton.interactable = true;
        if (_responseButton != null) _responseButton.interactable = true;
        if (_fishAudioPlayingIcon != null) _fishAudioPlayingIcon.enabled = false;
    }

    public void Response(RectTransform targetRef)
    {
        if (_moveCoroutine != null) return;
        _target = targetRef;
        _currentTargetType = TargetType.Response;
        _moveCoroutine = StartCoroutine(CheckFishType());
    }

    public void Greeting(RectTransform targetRef)
    {
        if (_moveCoroutine != null) return;
        _target = targetRef;
        _currentTargetType = TargetType.Greetings;
        _moveCoroutine = StartCoroutine(CheckFishType());
    }

    public void PlayAudio()
    {
        if (_fishAudioPlayingIcon != null) _fishAudioPlayingIcon.enabled = false;
        if (_fishAudioCoroutine != null) StopCoroutine(_fishAudioCoroutine);
        _fishAudioCoroutine = StartCoroutine(StartPlayAudio());
    }

    IEnumerator StartPlayAudio()
    {
        if (_currentclipIndex < _audioClips.Length && _audioClips[_currentclipIndex] != null)
        {
            _audioSource.clip = _audioClips[_currentclipIndex];
            _audioSource.Play();
            if (_fishAudioPlayingIcon != null) _fishAudioPlayingIcon.enabled = true;
            yield return new WaitForSeconds(_audioSource.clip.length);
        }
        if (_fishAudioPlayingIcon != null) _fishAudioPlayingIcon.enabled = false;
    }

    private void UpdateHintImage()
    {
        if (_hintImageComponent != null && _hintSprites != null && _currentclipIndex < _hintSprites.Length)
        {
            if (_hintSprites[_currentclipIndex] != null)
            {
                _hintImageComponent.gameObject.SetActive(true);
                _hintImageComponent.sprite = _hintSprites[_currentclipIndex];
            }
            else
            {
                _hintImageComponent.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator CheckFishType()
    {
        if (_greetingButton != null) _greetingButton.interactable = false;
        if (_responseButton != null) _responseButton.interactable = false;

        if (_targetType[_currentclipIndex] == TargetType.Both || _currentTargetType == _targetType[_currentclipIndex])
        {
            yield return StartCoroutine(MoveFish());
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_correctClip.length);

            _currentclipIndex++;

            if (_fish != null)
            {
                _fish.anchoredPosition = Vector3.zero;
                _fish.gameObject.SetActive(true);
                if (_fish.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
            }

            if (_currentclipIndex < _audioClips.Length)
            {
                if (_fishText != null) _fishText.text = _audioClips[_currentclipIndex].name;
                UpdateHintImage();
            }
            else
            {
                if (_hintImageComponent != null) _hintImageComponent.gameObject.SetActive(false);
                if (_greetingButton != null) _greetingButton.gameObject.SetActive(false);
                if (_responseButton != null) _responseButton.gameObject.SetActive(false);
                if (_fish.TryGetComponent(out Button fishBtn)) fishBtn.interactable = false;
                if (_fishText != null) _fishText.text = "Completed!";

                _isViewed = true;
                if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            }
        }
        else
        {
            if (_fish != null && _fish.TryGetComponent(out WiggleEffect_Junior2A wiggle))
            {
                wiggle.enabled = true;
            }
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_wrongClip.length);
        }

        if (_greetingButton != null) _greetingButton.interactable = true;
        if (_responseButton != null) _responseButton.interactable = true;
        _moveCoroutine = null;
    }

    IEnumerator MoveFish()
    {
        if (_fish == null || _target == null) yield break;

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
    }

    Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return (1f - t) * (1f - t) * a + 2f * (1f - t) * t * b + t * t * c;
    }
}