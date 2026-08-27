using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U12_W01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    public enum TargetOption { OptionA, OptionB }

    [System.Serializable]
    public class PronunciationData
    {
        public string wordName;
        public string choiceAText;
        public string choiceBText;
        public TargetOption correctOption;
    }

    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] AudioClip[] _audioClips;

    [Header("Pronunciation Quiz Configuration")]
    [SerializeField] PronunciationData[] _quizData;
    [SerializeField] Button _optionAButton;
    [SerializeField] Button _optionBButton;

    [Header("Fish Flight Setup")]
    [SerializeField] RectTransform _fish, _target;
    [SerializeField] int _currentclipIndex = 0;
    [SerializeField] float speed = 500;
    [SerializeField] float arcHeight = 150f;
    [SerializeField] bool _isViewed = false;

    Coroutine _coroutine, _moveCoroutine, _fishAudioCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _currentclipIndex = 0;
        _audioSource.clip = _introClip;
        _audioSource.Play();

        _fish.anchoredPosition = Vector3.zero;
        _fish.localScale = Vector3.one;
        _fish.localEulerAngles = Vector3.zero;

        if (_fish.TryGetComponent(out Button fishBtn)) fishBtn.interactable = true;
        if (_fish.TryGetComponent(out Popeffect_Junior1B fishPop)) fishPop.enabled = true;

        _fish.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Phrase Fish";
        _fish.gameObject.SetActive(true);
        _fish.GetChild(1).GetComponent<Image>().enabled = true;

        _optionAButton.gameObject.SetActive(false);
        _optionBButton.gameObject.SetActive(false);
        if (fishBtn != null) fishBtn.interactable = false;

        yield return new WaitForSeconds(_audioSource.clip.length / 2);

        _optionAButton.gameObject.SetActive(true);
        _optionBButton.gameObject.SetActive(true);
        _optionAButton.interactable = _optionBButton.interactable = false;

        if (_optionAButton.TryGetComponent(out Popeffect_Junior1B popA)) popA.enabled = true;
        if (_optionBButton.TryGetComponent(out Popeffect_Junior1B popB)) popB.enabled = true;

        yield return new WaitForSeconds(_audioSource.clip.length / 2);

        UpdateQuizUI();

        if (fishBtn != null) fishBtn.interactable = true;
        _optionAButton.interactable = _optionBButton.interactable = true;
        _fish.GetChild(1).GetComponent<Image>().enabled = false;
    }

    // --- LOCKED TO THE NESTED 'CENTER' TRANSFORMS ---
    public void SelectOptionA()
    {
        if (_moveCoroutine != null) return;

        // Find the "center" child object inside Option A Button
        Transform centerChild = _optionAButton.transform.Find("center");
        _target = centerChild != null ? centerChild.GetComponent<RectTransform>() : _optionAButton.GetComponent<RectTransform>();

        _moveCoroutine = StartCoroutine(CheckSelectedAnswer(TargetOption.OptionA));
    }

    public void SelectOptionB()
    {
        if (_moveCoroutine != null) return;

        // Find the "center" child object inside Option B Button
        Transform centerChild = _optionBButton.transform.Find("center");
        _target = centerChild != null ? centerChild.GetComponent<RectTransform>() : _optionBButton.GetComponent<RectTransform>();

        _moveCoroutine = StartCoroutine(CheckSelectedAnswer(TargetOption.OptionB));
    }

    public void PlayAudio()
    {
        _fish.GetChild(1).GetComponent<Image>().enabled = false;
        if (_fishAudioCoroutine != null) StopCoroutine(_fishAudioCoroutine);
        _fishAudioCoroutine = StartCoroutine(StartPlayAudio());
    }

    IEnumerator StartPlayAudio()
    {
        if (_currentclipIndex < _audioClips.Length && _audioClips[_currentclipIndex] != null)
        {
            _audioSource.clip = _audioClips[_currentclipIndex];
            _audioSource.Play();
            _fish.GetChild(1).GetComponent<Image>().enabled = true;
            yield return new WaitForSeconds(_audioSource.clip.length);
            _fish.GetChild(1).GetComponent<Image>().enabled = false;
        }
    }

    IEnumerator CheckSelectedAnswer(TargetOption selectedOption)
    {
        _optionAButton.interactable = _optionBButton.interactable = false;
        if (_fish.TryGetComponent(out Button fishBtn)) fishBtn.interactable = false;

        if (_currentclipIndex < _quizData.Length && selectedOption == _quizData[_currentclipIndex].correctOption)
        {
            yield return StartCoroutine(MoveFish());

            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_correctClip.length);

            _currentclipIndex++;

            _fish.anchoredPosition = Vector3.zero;
            _fish.localEulerAngles = Vector3.zero;
            _fish.localScale = Vector3.one;
            _fish.gameObject.SetActive(true);
            if (_fish.TryGetComponent(out Popeffect_Junior1B pop)) pop.enabled = true;

            if (_currentclipIndex < _audioClips.Length && _currentclipIndex < _quizData.Length)
            {
                UpdateQuizUI();
                _optionAButton.interactable = _optionBButton.interactable = true;
                if (fishBtn != null) fishBtn.interactable = true;
            }
            else
            {
                _optionAButton.gameObject.SetActive(false);
                _optionBButton.gameObject.SetActive(false);
                _fish.GetComponent<Button>().interactable = false;
                _fish.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Completed!";
                _isViewed = true;
                GameManager_Junior1B.Instance.Next(true);
            }
        }
        else
        {
            if (_fish.TryGetComponent(out WiggleEffect_Junior1B wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }

            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_wrongClip.length);
        }

        _optionAButton.interactable = _optionBButton.interactable = true;
        if (fishBtn != null) fishBtn.interactable = true;
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
    }

    Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return (1f - t) * (1f - t) * a + 2f * (1f - t) * t * b + t * t * c;
    }

    void UpdateQuizUI()
    {
        if (_currentclipIndex >= _quizData.Length) return;

        _fish.GetChild(0).GetComponent<TextMeshProUGUI>().text = _quizData[_currentclipIndex].wordName;
        _optionAButton.GetComponentInChildren<TextMeshProUGUI>().text = _quizData[_currentclipIndex].choiceAText;
        _optionBButton.GetComponentInChildren<TextMeshProUGUI>().text = _quizData[_currentclipIndex].choiceBText;
    }
}