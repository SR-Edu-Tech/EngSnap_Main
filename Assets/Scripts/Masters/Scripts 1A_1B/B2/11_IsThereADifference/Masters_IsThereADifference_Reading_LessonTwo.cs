using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Reading Lesson 2 for Unit 11 Is There a Difference?
/// Implements Line Drag Match mechanics for the 8 confusable word definitions.
/// </summary>
public class Masters_IsThereADifference_Reading_LessonTwo : Masters_Lesson, IBeginDragHandler, IEndDragHandler, IDragHandler {

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
    private int pairsPerSet = 4;
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

    private void LoadNextSet() {
        int startIndex = currentSetIndex * pairsPerSet;
        if (startIndex >= totalPairs) return;

        int endIndex = Mathf.Min(startIndex + pairsPerSet, totalPairs);
        currentSetExpectedPairs = endIndex - startIndex;
        correctCountInCurrentSet = 0;

        List<Masters_UniversalLineDragMatch> spawnedNodes = new List<Masters_UniversalLineDragMatch>();

        for (int i = startIndex; i < endIndex; i++) {
            var puzzle = puzzles[i];

            // Instantiate Left Node
            var leftNode = Instantiate(leftMatchNodePrefab, leftContainer);
            leftNode.Initialize(Masters_UniversalLineDragMatch.Column.Left, puzzle.leftPhrase, puzzle.rightPhrase);
            leftNode.transform.SetSiblingIndex(Random.Range(0, leftContainer.childCount));
            spawnedNodes.Add(leftNode);

            // Instantiate Right Node
            var rightNode = Instantiate(rightMatchNodePrefab, rightContainer);
            rightNode.Initialize(Masters_UniversalLineDragMatch.Column.Right, puzzle.rightPhrase, puzzle.rightPhrase);
            rightNode.transform.SetSiblingIndex(Random.Range(0, rightContainer.childCount));
            spawnedNodes.Add(rightNode);
        }

        // Dynamically adjust grid/layout cell size and spacing so cards fill the available area cleanly without fattening
        AdjustContainerLayout(leftContainer, currentSetExpectedPairs);
        AdjustContainerLayout(rightContainer, currentSetExpectedPairs);

        // Enable TMP Auto-Sizing so text shrinks cleanly to fit button without spilling out of the container
        foreach (var node in spawnedNodes) {
            if (node == null) continue;
            var tmp = node.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 13;
                tmp.fontSizeMax = 26;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        if (puzzleContainerRectTransform != null) {
            puzzleContainerRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
    }

    private void AdjustContainerLayout(Transform container, int count) {
        if (container == null || count <= 0) return;
        RectTransform containerRect = container.GetComponent<RectTransform>();
        float totalHeight = containerRect != null && containerRect.rect.height > 100f ? containerRect.rect.height : 420f;

        if (container.TryGetComponent<UnityEngine.UI.GridLayoutGroup>(out var grid)) {
            float availHeight = totalHeight - grid.padding.top - grid.padding.bottom;
            float cellH = Mathf.Clamp((availHeight - (count - 1) * 12f) / count, 65f, 90f);
            float spacingY = (count > 1) ? Mathf.Clamp((availHeight - count * cellH) / (count - 1), 8f, 25f) : 0f;

            grid.cellSize = new Vector2(grid.cellSize.x, cellH);
            grid.spacing = new Vector2(grid.spacing.x, spacingY);
        } else if (container.TryGetComponent<UnityEngine.UI.VerticalLayoutGroup>(out var vLayout)) {
            float availHeight = totalHeight - vLayout.padding.top - vLayout.padding.bottom;
            float cellH = Mathf.Clamp((availHeight - (count - 1) * 12f) / count, 65f, 90f);
            float spacingY = (count > 1) ? Mathf.Clamp((availHeight - count * cellH) / (count - 1), 8f, 25f) : 0f;

            vLayout.spacing = spacingY;
            foreach (Transform child in container) {
                if (child.TryGetComponent<UnityEngine.UI.LayoutElement>(out var le)) {
                    le.preferredHeight = cellH;
                    le.minHeight = cellH;
                } else {
                    le = child.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                    le.preferredHeight = cellH;
                    le.minHeight = cellH;
                }
            }
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

        Masters_UniversalLineDragMatch lineDragToExpressionMatch = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Masters_UniversalLineDragMatch>();

        if (lineDragToExpressionMatch != null && lineDragToExpressionMatch.GetColumnPosition() == Masters_UniversalLineDragMatch.Column.Right) {
            
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
