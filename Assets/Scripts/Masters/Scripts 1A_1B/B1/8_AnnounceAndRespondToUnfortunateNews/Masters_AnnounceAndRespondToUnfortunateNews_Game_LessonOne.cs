using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Masters_AnnounceAndRespondToUnfortunateNews_Game_LessonOne : Masters_Lesson, IBeginDragHandler, IEndDragHandler, IDragHandler {


    public enum LeftOrRightPhrase {
        Left,
        Right
    }


    [SerializeField]
    private TextMeshProUGUI progressCountTMP;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;


    private string currentCorrectMatch;
    private LineRenderer currentLineRenderer;
    private bool canDrawLine;
    private Masters_LineDragToExpressionMatch_AnnounceAndRespondToUnfortunateNews startLineDragToExpressionMatch;
    private int correctCount;


    public void OnBeginDrag(PointerEventData eventData) {
        if (eventData.pointerPress.TryGetComponent(out Masters_LineDragToExpressionMatch_AnnounceAndRespondToUnfortunateNews lineDragToExpressionMatch) &&
            lineDragToExpressionMatch.GetLeftOrRightPhrase() == LeftOrRightPhrase.Left && !lineDragToExpressionMatch.GetIsSolved()) {
            // Pressed on a valid gameobject

            if (lineDragToExpressionMatch.TryGetComponent(out Masters_ButtonPunchAnimator buttonPunchAnimator)) {
                buttonPunchAnimator.Punch();
            }

            canDrawLine = true;

            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

            currentCorrectMatch = lineDragToExpressionMatch.GetCorrectMatch();
            currentLineRenderer = lineDragToExpressionMatch.GetLineRenderer();
            startLineDragToExpressionMatch = lineDragToExpressionMatch;
            currentLineRenderer.widthMultiplier = 20f;

            Vector3 lineRendererStartPosition = lineDragToExpressionMatch.GetLineRendererPointTransform().position;
            Vector3 worldPosition = new Vector3(
                lineRendererStartPosition.x,
                lineRendererStartPosition.y,
                0f
            );
            currentLineRenderer.SetPosition(0, worldPosition);

        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (!canDrawLine) {
            return;
        }

        Vector3 screenPosition = new Vector3(
            eventData.position.x,
            eventData.position.y,
            0f
        );
        Vector3 worldPosition = eventData.pressEventCamera.ScreenToWorldPoint(screenPosition);
        currentLineRenderer.SetPosition(1, worldPosition);


    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerCurrentRaycast.gameObject == null) {
            if (canDrawLine) {
                currentLineRenderer.positionCount = 2;
                currentLineRenderer.SetPosition(0, Vector3.zero);
                currentLineRenderer.SetPosition(1, Vector3.zero);

                currentLineRenderer = null;
                canDrawLine = false;
            }
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            return;
        }

        if (startLineDragToExpressionMatch.GetIsSolved()) {
            return;
        }

        if (eventData.pointerCurrentRaycast.gameObject.TryGetComponent(out Masters_LineDragToExpressionMatch_AnnounceAndRespondToUnfortunateNews
            lineDragToExpressionMatch) && lineDragToExpressionMatch.GetLeftOrRightPhrase() == LeftOrRightPhrase.Right) {
            // Ended on a valid gameobject

            if (lineDragToExpressionMatch.TryGetComponent(out Masters_ButtonPunchAnimator buttonPunchAnimator)) {
                buttonPunchAnimator.Punch();
            }

            if (currentCorrectMatch == lineDragToExpressionMatch.GetCorrectMatch()) {
                // Correct match
                Vector3 lineRendererEndPosition = lineDragToExpressionMatch.GetLineRendererPointTransform().position;
                Vector3 worldPosition = new Vector3(
                    lineRendererEndPosition.x,
                    lineRendererEndPosition.y,
                    0f
                );
                currentLineRenderer.SetPosition(1, worldPosition);
                startLineDragToExpressionMatch.Solved();
                lineDragToExpressionMatch.Solved();

                progressCountTMP.text = $"{++correctCount}/6";
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

                if (correctCount == 6) {
                    nextButton.interactable = true;
                    NextButtonAnimation();
                }

                currentLineRenderer = null;
                canDrawLine = false;
            } else {
                // Wrong match
                currentLineRenderer.positionCount = 2;
                currentLineRenderer.SetPosition(0, Vector3.zero);
                currentLineRenderer.SetPosition(1, Vector3.zero);

                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);

                currentLineRenderer = null;
                canDrawLine = false;
            }
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
