using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_InvitationsAcceptingRefusingButton : MonoBehaviour {


    [SerializeField]
    private Button invitationsButton, acceptingButton, refusingButton;
    [SerializeField]
    private AudioClip invitationsAudioClip, acceptingAudioClip, refusingAudioClip;
    [SerializeField]
    private GameObject invitationsSpeakerIconGameObject, acceptingSpeakerIconGameObject, refusingSpeakerIconGameObject;
    [SerializeField]
    private Image invitationsFillImage, invitationsBorderImage, acceptingFillImage, acceptingBorderImage, refusingFillImage,
        refusingBorderImage;
    [SerializeField]
    private Color completedFillColor, completedBorderColor;


    private bool doOnce;


    public void PlayInvitationsAcceptingRefusingAudioClip() {
        invitationsButton.transform.DOKill(true);
        acceptingButton.transform.DOKill(true);
        refusingButton.transform.DOKill(true);

        invitationsButton.transform.localScale = Vector3.one;
        acceptingButton.transform.localScale = Vector3.one;
        refusingButton.transform.localScale = Vector3.one;

        invitationsButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8, 0.8f);
        acceptingButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8, 0.8f);
        refusingButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 8, 0.8f);

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        // For the first time playing change the color to completed
        if (!doOnce) {
            doOnce = true;

            invitationsFillImage.color = completedFillColor;
            invitationsBorderImage.color = completedBorderColor;

            acceptingFillImage.color = completedFillColor;
            acceptingBorderImage.color = completedBorderColor;

            refusingFillImage.color = completedFillColor;
            refusingBorderImage.color = completedBorderColor;

            invitationsSpeakerIconGameObject.SetActive(true);
            Masters_AudioManager.Instance.PlayVoiceOver(invitationsAudioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                invitationsSpeakerIconGameObject.SetActive(false);

                acceptingSpeakerIconGameObject.SetActive(true);
                Masters_AudioManager.Instance.PlayVoiceOver(acceptingAudioClip);

                StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                    acceptingSpeakerIconGameObject.SetActive(false);

                    refusingSpeakerIconGameObject.SetActive(true);
                    Masters_AudioManager.Instance.PlayVoiceOver(refusingAudioClip);

                    StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                        refusingSpeakerIconGameObject.SetActive(false);
                    }));
                }));
            }));
            return;
        }

        invitationsSpeakerIconGameObject.SetActive(true);
        Masters_AudioManager.Instance.PlayVoiceOver(invitationsAudioClip);
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
            invitationsSpeakerIconGameObject.SetActive(false);

            acceptingSpeakerIconGameObject.SetActive(true);
            Masters_AudioManager.Instance.PlayVoiceOver(acceptingAudioClip);

            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                acceptingSpeakerIconGameObject.SetActive(false);

                refusingSpeakerIconGameObject.SetActive(true);
                Masters_AudioManager.Instance.PlayVoiceOver(refusingAudioClip);

                StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                    refusingSpeakerIconGameObject.SetActive(false);
                }));
            }));
        }));
    }

    public Button[] GetInvitationsAcceptingRefusingButtonArray() {
        Button[] buttonArray = { invitationsButton, acceptingButton, refusingButton };
        return buttonArray;
    }

    public void StopCoroutine() {
        StopAllCoroutines();
        invitationsSpeakerIconGameObject.SetActive(false);
        acceptingSpeakerIconGameObject.SetActive(false);
        refusingSpeakerIconGameObject.SetActive(false);
    }


}
