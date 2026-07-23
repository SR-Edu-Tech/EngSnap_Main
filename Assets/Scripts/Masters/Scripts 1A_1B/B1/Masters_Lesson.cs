using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Masters_Lesson : MonoBehaviour {


    [SerializeField]
    protected Masters_Topic topic;
    [SerializeField]
    protected Button nextButton;
    [SerializeField]
    protected AudioClip narratorSpeech;


    protected virtual void Awake() {
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    protected virtual void Start() {
        Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
    }

    protected virtual void NextButtonAnimation() {
        if (nextButton != null) {
            nextButton.interactable = true;
            nextButton.transform.DOScale(Vector2.one * 0.75f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }
    }

    protected abstract void OnNextButtonClicked();


}
