using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_QuestionAndAnswerButton : MonoBehaviour {


    [SerializeField]
    private Button questionButton, answerButton;
    [SerializeField]
    private AudioClip questionAudioClip, answerAudioClip;
    [SerializeField]
    private GameObject questionSpeakerIconGameObject, answerSpeakerIconGameObject;
    [SerializeField]
    private Image questionFillImage, questionBorderImage, answerFillImage, answerBorderImage;
    [SerializeField]
    private Color completedFillColor, completedBorderColor;


    private bool doOnce;


    public void PlayQuestionAndAnswerAudioClip() {
        questionButton.transform.DOKill(true);
        answerButton.transform.DOKill(true);
        questionButton.transform.localScale = Vector3.one;
        answerButton.transform.localScale = Vector3.one;
        questionButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        answerButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        // For the first time playing change the color to completed
        if (!doOnce) {
            doOnce = true;

            questionFillImage.color = completedFillColor;
            questionBorderImage.color = completedBorderColor;
            answerFillImage.color = completedFillColor;
            answerBorderImage.color = completedBorderColor;

            questionSpeakerIconGameObject.SetActive(true);
            Masters_AudioManager.Instance.PlayVoiceOver(questionAudioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                questionSpeakerIconGameObject.SetActive(false);

                answerSpeakerIconGameObject.SetActive(true);
                Masters_AudioManager.Instance.PlayVoiceOver(answerAudioClip);

                StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                    answerSpeakerIconGameObject.SetActive(false);
                }));
            }));
            return;
        }

        questionSpeakerIconGameObject.SetActive(true);
        Masters_AudioManager.Instance.PlayVoiceOver(questionAudioClip);
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
            questionSpeakerIconGameObject.SetActive(false);
            answerSpeakerIconGameObject.SetActive(true);
            Masters_AudioManager.Instance.PlayVoiceOver(answerAudioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                answerSpeakerIconGameObject.SetActive(false);
            }));
        }));
    }

    public Button[] GetQuestionAndAnswerButtonArray() {
        Button[] buttonArray = { questionButton, answerButton };
        return buttonArray;
    }

    public void StopCoroutine() {
        StopAllCoroutines();
        questionSpeakerIconGameObject.SetActive(false);
        answerSpeakerIconGameObject.SetActive(false);
    }

    
}
