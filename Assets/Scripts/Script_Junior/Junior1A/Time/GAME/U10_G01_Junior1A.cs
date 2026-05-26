using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U10_G01_Junior1A_QuestionData
{
    public Sprite ClueScene;
    public string QuestionText;
    public string[] OptionText;
    public int CorrectAnsIndex;
}
public class U10_G01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] U10_G01_Junior1A_QuestionData[] _questionData;
    [SerializeField] GameObject _sceneImage, _buttonParent, _starsPanel;
    [SerializeField] Transform _selectedButton;
    [SerializeField] int _currentQuestionIndex = 0, _correctAnsCount;
    [SerializeField] bool _isViewed = false;
    [SerializeField] TextMeshProUGUI _currentQuestionIndexText;
    Coroutine _coroutineAudioPlayer, _coroutineNextFish;

    public bool IsViewed => _isViewed;
    void OnEnable() => StartCoroutine(Starter());
    IEnumerator Starter()
    {
        _audioSource.pitch = 1;
        foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);
        transform.GetChild(0).gameObject.SetActive(true);
        _sceneImage.SetActive(false);
        _starsPanel.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        _currentQuestionIndex = _correctAnsCount = 0;
        _selectedButton = null;
        _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";

        yield return new WaitForSeconds(_introClip.length);

        _coroutineNextFish = StartCoroutine(PopScene());
    }
    IEnumerator PopScene()
    {
        foreach (U10_G01_Junior1A_QuestionData item in _questionData)
        {
            int _optionIndex = 0;

            yield return new WaitForSeconds(.5f);

            foreach (Transform button in _buttonParent.transform)
            {
                button.GetComponent<Image>().color = Color.white;
                button.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.OptionText[_optionIndex++];
                button.GetComponent<PopEffect_Junior1A>().enabled = true;
                button.gameObject.SetActive(true);
                button.GetChild(0).GetComponent<TextPopEffect_Junior1A>().enabled = false;
                button.GetChild(0).GetComponent<TextPopEffect_Junior1A>().enabled = true;
            }
            _sceneImage.GetComponent<Image>().sprite = item.ClueScene;
            _sceneImage.GetComponent<PopEffect_Junior1A>().enabled = true;
            _sceneImage.SetActive(true);
            _sceneImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.QuestionText;
            _sceneImage.transform.GetChild(0).gameObject.SetActive(true);
            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = true;

            yield return new WaitUntil(() => _selectedButton != null);

            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = false;

            if (_selectedButton == _buttonParent.transform.GetChild(item.CorrectAnsIndex))
            {
                _correctAnsCount++;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _correctColor;
                _selectedButton.GetComponent<PopEffect_Junior1A>().enabled = true;
            }
            else
            {
                _audioSource.clip = _wrongClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _wrongColor;
                _selectedButton.GetComponent<WiggleEffect_Junior1A1>().enabled = true;
            }

            yield return new WaitForSeconds(_audioSource.clip.length);

            _sceneImage.SetActive(false);
            foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);

            _sceneImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            _sceneImage.transform.GetChild(0).gameObject.SetActive(false);
            _selectedButton = null;
            _sceneImage.SetActive(false);
            _currentQuestionIndex++;
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }
        transform.GetChild(0).gameObject.SetActive(false);
        _starsPanel.SetActive(true);
        for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
        {
            _starsPanel.transform.GetChild(i).GetComponent<PopEffect_Junior1A>().enabled = true;
            _starsPanel.transform.GetChild(i).gameObject.SetActive(true);
            if (i < _correctAnsCount)
            {
                _starsPanel.transform.GetChild(i).GetComponent<Image>().color = _wonStar;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _audioSource.pitch += .1f;
                yield return new WaitForSeconds(_correctClip.length);
            }
            else _starsPanel.transform.GetChild(i).GetComponent<Image>().color = _loseStar;
        }
        GameManager_Junior1A.Instance.Next(true);
        _isViewed = true;
    }
    public void SelectedObject(Transform button) => _selectedButton = button;
}

