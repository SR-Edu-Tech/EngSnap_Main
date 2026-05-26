using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U1_R02_Junior1A_QuestionData
{
    public AudioClip AudioClipData;
    public string QuestionText;
    public string[] OptionText = new string[3];
    public string CorrectAnswer;
}
public class U1_R02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClipTab1, _introClipTab2, _incorrectClip, _correctClip;
    [SerializeField] AudioClip[] _clips;
    [SerializeField] int _currentAudioIndex = 0, _currentQuestionIndex = 0;
    [SerializeField] U1_R02_Junior1A_QuestionData[] _questionData = new U1_R02_Junior1A_QuestionData[3];
    [SerializeField] GameObject[] _option = new GameObject[3];
    [SerializeField] Transform _cardParent;
    [SerializeField] List<int> _clickCheckIndex = new List<int>();
    [SerializeField] bool _isViewed = false;
    [SerializeField] GameObject _tab2Next, _tab1, _tab2, _questionObj;
    [SerializeField] Color _wrongColor, _correctColor;
    [SerializeField] Image _previousButton;
    [SerializeField] TextMeshProUGUI _clickedIndexText;
    Coroutine _coroutine, _buttonCoroutine, _questionCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(StarterTab1());

    IEnumerator StarterTab1()
    {
        foreach (Transform button in _cardParent) button.GetComponent<PopEffect_Junior1A>().enabled = true;
        _clickCheckIndex.Clear();
        _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/8";
        _tab1.SetActive(true);
        _tab2.SetActive(false);
        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = Color.white;
            button.GetComponent<Button>().interactable = false;
            button.gameObject.SetActive(false);
        }
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _audioSource.clip = _introClipTab1;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClipTab1.length / 2);
        foreach (Transform button in _cardParent) button.gameObject.SetActive(true);
        yield return new WaitForSeconds(_introClipTab1.length / 2);
        foreach (Transform button in _cardParent) button.GetComponent<Button>().interactable = true;
    }
    IEnumerator StarterTab2()
    {
        _tab1.SetActive(false);
        _tab2.SetActive(true);
        foreach (Transform button in _cardParent) button.gameObject.SetActive(false);
        _audioSource.clip = _introClipTab2;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClipTab2.length + 1f);
        _questionObj.GetComponentInChildren<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].QuestionText;
        for (int i = 0; i < _option.Length; i++)
        {
            ColorBlock colors = _option[i].GetComponent<Button>().colors;
            _option[i].GetComponentInChildren<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].OptionText[i];
            if (_questionData[_currentQuestionIndex].OptionText[i] != _questionData[_currentQuestionIndex].CorrectAnswer) colors.selectedColor = _wrongColor;
            else colors.selectedColor = _correctColor;
            _option[i].GetComponent<Button>().colors = colors;
        }
        _questionObj.SetActive(true);
        foreach (GameObject obj in _option) obj.SetActive(true);
        _audioSource.clip = _questionData[_currentQuestionIndex].AudioClipData;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        foreach (GameObject obj in _option) obj.GetComponent<Button>().interactable = true;
    }
    public void PlayAudio(int index)
    {
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
        if (!_clickCheckIndex.Contains(index))
        {
            _clickedIndexText.text = (_clickCheckIndex.Count + 1).ToString() + "/8";
            _clickCheckIndex.Add(index);
        }
        if (_clickCheckIndex.Count == _clips.Length) _tab2Next.SetActive(true);
    }
    IEnumerator StartButtonAudio()
    {
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
        _audioSource.clip = _clips[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_audioSource.clip.length);

        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }
    public void OnEnableTab2() => _coroutine = StartCoroutine(StarterTab2());
    public void OnOptionSelect(GameObject button)
    {
        if (_questionCoroutine != null) StopCoroutine(_questionCoroutine);
        if (_previousButton) _previousButton.color = Color.white;
        _previousButton = button.GetComponent<Image>();
        _questionCoroutine = StartCoroutine(QuestionChecker(button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text, button));
    }
    IEnumerator QuestionChecker(string answer, GameObject button)
    {
        if (_questionData[_currentQuestionIndex].CorrectAnswer == answer)
        {
            _currentQuestionIndex++;
            foreach (GameObject option in _option) option.GetComponent<Button>().interactable = false;
            button.GetComponent<Image>().color = _correctColor;
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            button.GetComponent<PopEffect_Junior1A>().enabled = false;
            button.GetComponent<PopEffect_Junior1A>().enabled = true;
            yield return new WaitForSeconds(_correctClip.length);
            if (_currentQuestionIndex >= _questionData.Length)
            {
                _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
                yield break;
            }
            else
            {
                foreach (GameObject option in _option) option.GetComponent<Button>().interactable = false;
                _questionObj.SetActive(false);
                foreach (GameObject option in _option) option.SetActive(false);
                _questionObj.GetComponentInChildren<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].QuestionText;
                for (int i = 0; i < _option.Length; i++) _option[i].GetComponentInChildren<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].OptionText[i];
                _questionObj.SetActive(true);
                button.GetComponent<Image>().color = Color.white;
                foreach (GameObject option in _option)
                {
                    option.SetActive(true);
                    option.GetComponent<Button>().interactable = true;
                }
                _audioSource.clip = _questionData[_currentQuestionIndex].AudioClipData;
                _audioSource.Play();
            }
        }
        else
        {
            button.GetComponent<Image>().color = _wrongColor;
            button.GetComponent<WiggleEffect_Junior1A1>().enabled = true;
            _audioSource.clip = _incorrectClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length / 2);
            button.GetComponent<Image>().color = Color.white;
        }
    }
}
