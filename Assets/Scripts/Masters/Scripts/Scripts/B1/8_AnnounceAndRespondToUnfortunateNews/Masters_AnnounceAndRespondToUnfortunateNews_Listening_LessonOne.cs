using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_AnnounceAndRespondToUnfortunateNews_Listening_LessonOne : Masters_Lesson {


    [System.Serializable]
    private class PhraseCard {

        public Button button;
        public AudioClip normalAudioClip;
        public AudioClip slowedAudioClip;
        public GameObject speakerGameObject;

    }


    [SerializeField]
    private PhraseCard[] phraseCardArray;
    [SerializeField]
    private float timeBetweenAudioInPlayAll = 2f;
    [SerializeField]
    private Toggle slowToggle;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private Toggle repeatToggle;
    [SerializeField]
    private float timeBetweenEachAnimation, animationSpeed;
    [SerializeField]
    private TextMeshProUGUI expressionCountTMP;


    private HashSet<PhraseCard> phraseCardHashSet = new HashSet<PhraseCard>();
    private bool isSlowed;
    private bool isRepeatOn;
    private Coroutine audioCoroutine;
    private PhraseCard currentPhraseCard;


    protected override void Awake() {
        base.Awake();

        foreach (PhraseCard phraseCard in phraseCardArray) {
            phraseCard.button.onClick.AddListener(() => {
                OnPhraseCardButtonClicked(phraseCard);
            });
        }

        slowToggle.onValueChanged.AddListener(OnSlowToggle);
        repeatToggle.onValueChanged.AddListener(OnRepeatToggle);
    }

    private void OnEnable() {
        StartCoroutine(PhraseCardsPopUpAnimation());
    }

    protected override void Start() {
        base.Start();
    }

    private IEnumerator PhraseCardsPopUpAnimation() {
        foreach (PhraseCard phraseCard in phraseCardArray) {
            phraseCard.button.transform.localScale = Vector3.zero;
        }
        slowToggle.transform.localScale = Vector3.zero;
        repeatToggle.transform.localScale = Vector3.zero;

        for (int i = 0; i < phraseCardArray.Length; i++) {
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            phraseCardArray[i].button.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        slowToggle.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        repeatToggle.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private void OnSlowToggle(bool value) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        slowToggle.DOKill(true);
        slowToggle.transform.localScale = Vector3.one;

        slowToggle.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        isSlowed = value;
    }

    private void OnRepeatToggle(bool value) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        if (currentPhraseCard != null) {
            currentPhraseCard.speakerGameObject.SetActive(false);
        }

        repeatToggle.DOKill(true);
        repeatToggle.transform.localScale = Vector3.one;

        repeatToggle.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        if (value == false) {
            Masters_AudioManager.Instance.StopVoiceOver();

            if (audioCoroutine != null) {
                StopCoroutine(audioCoroutine);
            }
        }

        isRepeatOn = value;
    }

    private void OnPhraseCardButtonClicked(PhraseCard phraseCard) {
        if (currentPhraseCard != null) {
            currentPhraseCard.speakerGameObject.SetActive(false);
        }
        currentPhraseCard = phraseCard;

        phraseCard.speakerGameObject.SetActive(true);

        if (!phraseCardHashSet.Contains(phraseCard)) {
            phraseCardHashSet.Add(phraseCard);
            expressionCountTMP.text = $"{phraseCardHashSet.Count}/8";

            if (phraseCardHashSet.Count == 8) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }

        // Stop any running audioCoroutine
        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
        }

        AudioClip audioClip = isSlowed ? phraseCard.slowedAudioClip : phraseCard.normalAudioClip;

        // Play in repeat
        if (isRepeatOn) {
            audioCoroutine = StartCoroutine(PlayInRepeatCoroutine(audioClip));
            return;
        }

        // Play audio
        Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
            phraseCard.speakerGameObject.SetActive(false);
        }));
    }

    private IEnumerator PlayInRepeatCoroutine(AudioClip audioClip) {
        while (true) {
            Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
            yield return new WaitForSeconds(audioClip.length + timeBetweenAudioInPlayAll);
        }
    }


}
