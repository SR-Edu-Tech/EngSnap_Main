using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_JustAMinuteSession_Reading_LessonOne : Masters_Lesson {


    [System.Serializable]
    private class PhraseCard {

        public Button button;
        public AudioClip normalAudioClip;
        public GameObject speakerGameObject;

    }


    [SerializeField]
    private PhraseCard[] phraseCardArray;
    [SerializeField]
    private float timeBetweenAudioInPlayAll = 2f;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private float timeBetweenEachAnimation, animationSpeed;
    [SerializeField]
    private TextMeshProUGUI expressionCountTMP;


    private HashSet<PhraseCard> phraseCardHashSet = new HashSet<PhraseCard>();
    private Coroutine audioCoroutine;
    private PhraseCard currentPhraseCard;


    protected override void Awake() {
        base.Awake();
    }

    private void OnEnable() {
        StartCoroutine(PhraseCardsPopUpAnimation());
    }

    protected override void Start() {
        base.Start();

        foreach (PhraseCard phraseCard in phraseCardArray) {
            phraseCard.button.onClick.AddListener(() => {
                OnPhraseCardButtonClicked(phraseCard);
            });
        }
    }

    private IEnumerator PhraseCardsPopUpAnimation() {
        foreach (PhraseCard phraseCard in phraseCardArray) {
            phraseCard.button.transform.localScale = Vector3.zero;
        }

        for (int i = 0; i < phraseCardArray.Length; i++) {
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            phraseCardArray[i].button.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private void OnPhraseCardButtonClicked(PhraseCard phraseCard) {
        if (currentPhraseCard != null) {
            currentPhraseCard.speakerGameObject.SetActive(false);
        }
        currentPhraseCard = phraseCard;

        phraseCard.speakerGameObject.SetActive(true);

        if (!phraseCardHashSet.Contains(phraseCard)) {
            phraseCardHashSet.Add(phraseCard);
            expressionCountTMP.text = $"{phraseCardHashSet.Count}/12";

            if (phraseCardHashSet.Count == 12) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }

        // Stop any running audioCoroutine
        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
        }

        AudioClip audioClip = phraseCard.normalAudioClip;

        // Play audio
        Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
            phraseCard.speakerGameObject.SetActive(false);
        }));
    }


}
