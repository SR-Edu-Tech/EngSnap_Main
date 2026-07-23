using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Masters_WordSwitch_Reading_LessonOne : Masters_Lesson, IBeginDragHandler, IEndDragHandler, IDragHandler {

    [System.Serializable]
    public class MatchPuzzle {
        public string leftPhrase;
        public string rightPhrase;
    }

    [SerializeField]
    private MatchPuzzle[] puzzles;
    [SerializeField]
    private Masters_UniversalLineDragMatch leftMatchNodePrefab;
    [SerializeField]
    private Masters_UniversalLineDragMatch rightMatchNodePrefab;
    [SerializeField]
    private Transform leftContainer;
    [SerializeField]
    private Transform rightContainer;

    [SerializeField]
    private int[] pairsPerSetArray;
    [SerializeField]
    private RectTransform puzzleContainerRectTransform;
    [SerializeField]
    private float animationSpeed = 0.5f;

    [SerializeField]
    private TextMeshProUGUI progressCountTMP;
    private int totalPairs;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    private string currentCorrectMatch;
    private LineRenderer currentLineRenderer;
    private bool canDrawLine;
    private Masters_UniversalLineDragMatch startLineDragToExpressionMatch;
    private int correctCount;
    
    private int currentSetIndex = 0;
    private int correctCountInCurrentSet = 0;
    private int currentSetExpectedPairs = 0;

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }

        totalPairs = puzzles != null ? puzzles.Length : 0;
        if (progressCountTMP != null) progressCountTMP.text = $"0/{totalPairs}";

        if (puzzles == null || leftMatchNodePrefab == null || rightMatchNodePrefab == null) return;

        LoadNextSet();
    }

    private int GetStartIndexForSet(int setIndex) {
        int start = 0;
        for (int i = 0; i < setIndex; i++) {
            if (i < pairsPerSetArray.Length) {
                start += pairsPerSetArray[i];
            }
        }
        return start;
    }

    private void LoadNextSet() {
        if (pairsPerSetArray == null || currentSetIndex >= pairsPerSetArray.Length) return;

        int startIndex = GetStartIndexForSet(currentSetIndex);
        if (startIndex >= totalPairs) return;

        int pairsToLoad = pairsPerSetArray[currentSetIndex];
        int endIndex = Mathf.Min(startIndex + pairsToLoad, totalPairs);
        currentSetExpectedPairs = endIndex - startIndex;
        correctCountInCurrentSet = 0;

        for (int i = startIndex; i < endIndex; i++) {
            var puzzle = puzzles[i];

            var leftNode = Instantiate(leftMatchNodePrefab, leftContainer);
            leftNode.Initialize(Masters_UniversalLineDragMatch.Column.Left, puzzle.leftPhrase, puzzle.rightPhrase);
            leftNode.transform.SetSiblingIndex(Random.Range(0, leftContainer.childCount));

            var rightNode = Instantiate(rightMatchNodePrefab, rightContainer);
            rightNode.Initialize(Masters_UniversalLineDragMatch.Column.Right, puzzle.rightPhrase, puzzle.rightPhrase);
            rightNode.transform.SetSiblingIndex(Random.Range(0, rightContainer.childCount));
        }

        if (puzzleContainerRectTransform != null) {
            puzzleContainerRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        Masters_UniversalLineDragMatch lineDragToExpressionMatch = eventData.pointerPress.GetComponentInParent<Masters_UniversalLineDragMatch>();
        
        if (lineDragToExpressionMatch != null &&
            lineDragToExpressionMatch.GetColumnPosition() == Masters_UniversalLineDragMatch.Column.Left && !lineDragToExpressionMatch.GetIsSolved()) {
            
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
        if (!canDrawLine) return;

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

        if (startLineDragToExpressionMatch.GetIsSolved()) return;

        Masters_UniversalLineDragMatch lineDragToExpressionMatch = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Masters_UniversalLineDragMatch>();

        if (lineDragToExpressionMatch != null && lineDragToExpressionMatch.GetColumnPosition() == Masters_UniversalLineDragMatch.Column.Right) {
            
            if (lineDragToExpressionMatch.TryGetComponent(out Masters_ButtonPunchAnimator buttonPunchAnimator)) {
                buttonPunchAnimator.Punch();
            }

            if (currentCorrectMatch == lineDragToExpressionMatch.GetCorrectMatch()) {
                Vector3 lineRendererEndPosition = lineDragToExpressionMatch.GetLineRendererPointTransform().position;
                Vector3 worldPosition = new Vector3(
                    lineRendererEndPosition.x,
                    lineRendererEndPosition.y,
                    0f
                );
                currentLineRenderer.SetPosition(1, worldPosition);
                startLineDragToExpressionMatch.Solved();
                lineDragToExpressionMatch.Solved();

                correctCount++;
                correctCountInCurrentSet++;
                if (progressCountTMP != null) progressCountTMP.text = $"{correctCount}/{totalPairs}";
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

                currentLineRenderer = null;
                canDrawLine = false;

                if (correctCount == totalPairs) {
                    if (nextButton != null) {
                        nextButton.gameObject.SetActive(true);
                        nextButton.interactable = true;
                    }
                    NextButtonAnimation();
                } else if (correctCountInCurrentSet == currentSetExpectedPairs) {
                    if (puzzleContainerRectTransform != null) {
                        puzzleContainerRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                            foreach (Transform child in leftContainer) Destroy(child.gameObject);
                            foreach (Transform child in rightContainer) Destroy(child.gameObject);
                            
                            currentSetIndex++;
                            LoadNextSet();
                        });
                    } else {
                        foreach (Transform child in leftContainer) Destroy(child.gameObject);
                        foreach (Transform child in rightContainer) Destroy(child.gameObject);
                        currentSetIndex++;
                        LoadNextSet();
                    }
                }
            } else {
                currentLineRenderer.positionCount = 2;
                currentLineRenderer.SetPosition(0, Vector3.zero);
                currentLineRenderer.SetPosition(1, Vector3.zero);

                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);

                currentLineRenderer = null;
                canDrawLine = false;
            }
        } else {
            if (canDrawLine) {
                currentLineRenderer.positionCount = 2;
                currentLineRenderer.SetPosition(0, Vector3.zero);
                currentLineRenderer.SetPosition(1, Vector3.zero);
                currentLineRenderer = null;
                canDrawLine = false;
            }
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
