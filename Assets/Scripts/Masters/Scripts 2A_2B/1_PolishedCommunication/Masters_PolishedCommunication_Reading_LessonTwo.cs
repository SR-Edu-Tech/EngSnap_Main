using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Core Reading 2 controller for Unit 1: Polished Communication (Book 2A Reference Base).
/// R02 Match — Expression <-> Register: 8 expressions on left, 2 registers on right.
/// Student drags lines from expressions to their exact register column.
/// Supports pagination (pairsPerSet) exactly as in older project units when total pairs > 6.
/// </summary>
public class Masters_PolishedCommunication_Reading_LessonTwo : Masters_Lesson, IBeginDragHandler, IEndDragHandler, IDragHandler {

    [System.Serializable]
    public class MatchPuzzle {
        public string leftPhrase;
        public string rightPhrase;
    }

    [Header("Match Setup")]
    [SerializeField] private MatchPuzzle[] puzzles;
    [SerializeField] private Masters_UniversalLineDragMatch leftMatchNodePrefab;
    [SerializeField] private Masters_UniversalLineDragMatch rightMatchNodePrefab;
    [SerializeField] private Transform leftContainer;
    [SerializeField] private Transform rightContainer;
    [SerializeField] private bool useUniqueRightCards = true; // True = 2 cards on right (FORMAL & INFORMAL)
    [SerializeField] private int pairsPerSet = 4; // Splits 8 puzzles into 2 pages of 4 items each

    [SerializeField] private RectTransform puzzleContainerRectTransform;
    [SerializeField] private float animationSpeed = 0.5f;
    [SerializeField] private TextMeshProUGUI progressCountTMP;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int totalPairs;
    private int currentSetIndex = 0;
    private int correctCountInCurrentSet = 0;
    private int currentSetExpectedPairs = 0;
    private int correctCount = 0;

    private string currentCorrectMatch;
    private LineRenderer currentLineRenderer;
    private bool canDrawLine;
    private Masters_UniversalLineDragMatch startLineDragToExpressionMatch;

#if UNITY_EDITOR
    private void Reset() {
        InitializePuzzlesIfEmpty();
    }

    private void OnValidate() {
        if (puzzles == null || puzzles.Length == 0) {
            InitializePuzzlesIfEmpty();
        }
    }
#endif

    protected override void Awake() {
        base.Awake();
        InitializePuzzlesIfEmpty();
    }

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }

        totalPairs = puzzles != null ? puzzles.Length : 0;
        correctCount = 0;
        currentSetIndex = 0;
        if (progressCountTMP != null) progressCountTMP.text = $"0/{totalPairs}";

        // Immediately clear base prefab sample nodes and scale down on frame 0 to prevent initial flash
        if (leftContainer != null) {
            foreach (Transform child in leftContainer) Destroy(child.gameObject);
        }
        if (rightContainer != null) {
            foreach (Transform child in rightContainer) Destroy(child.gameObject);
        }
        if (puzzleContainerRectTransform != null) {
            puzzleContainerRectTransform.DOKill();
            puzzleContainerRectTransform.localScale = Vector3.zero;
        }

        StartCoroutine(InitializeLessonRoutine());
    }

    public void InitializePuzzlesIfEmpty() {
        if (puzzles != null && puzzles.Length > 0) return;

        string formalReg = "FORMAL — for teachers, elders, strangers";
        string informalReg = "INFORMAL — for friends and family";

        puzzles = new MatchPuzzle[] {
            new MatchPuzzle { leftPhrase = "Hello!", rightPhrase = formalReg },
            new MatchPuzzle { leftPhrase = "How is it going with you?", rightPhrase = formalReg },
            new MatchPuzzle { leftPhrase = "I would like to introduce myself.", rightPhrase = formalReg },
            new MatchPuzzle { leftPhrase = "Nice to meet you!", rightPhrase = formalReg },
            new MatchPuzzle { leftPhrase = "Hey!", rightPhrase = informalReg },
            new MatchPuzzle { leftPhrase = "Howdy?", rightPhrase = informalReg },
            new MatchPuzzle { leftPhrase = "Chill", rightPhrase = informalReg },
            new MatchPuzzle { leftPhrase = "Shades", rightPhrase = informalReg }
        };
    }

    private IEnumerator InitializeLessonRoutine() {
        yield return new WaitForSeconds(1.0f);
        LoadNextSet();
    }

    private void LoadNextSet() {
        if (puzzles == null || leftMatchNodePrefab == null || rightMatchNodePrefab == null) return;

        int startIndex = currentSetIndex * pairsPerSet;
        if (startIndex >= totalPairs) {
            OnLessonSuccess();
            return;
        }

        int endIndex = Mathf.Min(startIndex + pairsPerSet, totalPairs);
        currentSetExpectedPairs = endIndex - startIndex;
        correctCountInCurrentSet = 0;

        if (leftContainer != null) {
            foreach (Transform child in leftContainer) Destroy(child.gameObject);
        }
        if (rightContainer != null) {
            foreach (Transform child in rightContainer) Destroy(child.gameObject);
        }

        List<Masters_UniversalLineDragMatch> spawnedNodes = new List<Masters_UniversalLineDragMatch>();

        for (int i = startIndex; i < endIndex; i++) {
            var puzzle = puzzles[i];
            var leftNode = Instantiate(leftMatchNodePrefab, leftContainer);
            leftNode.Initialize(Masters_UniversalLineDragMatch.Column.Left, puzzle.leftPhrase, puzzle.rightPhrase);
            leftNode.transform.SetSiblingIndex(Random.Range(0, leftContainer.childCount));
            spawnedNodes.Add(leftNode);

            if (!useUniqueRightCards) {
                var rightNode = Instantiate(rightMatchNodePrefab, rightContainer);
                rightNode.Initialize(Masters_UniversalLineDragMatch.Column.Right, puzzle.rightPhrase, puzzle.rightPhrase);
                rightNode.transform.SetSiblingIndex(Random.Range(0, rightContainer.childCount));
                spawnedNodes.Add(rightNode);
            }
        }

        if (useUniqueRightCards) {
            string formalReg = "FORMAL — for teachers, elders, strangers";
            string informalReg = "INFORMAL — for friends and family";

            var formalNode = Instantiate(rightMatchNodePrefab, rightContainer);
            formalNode.Initialize(Masters_UniversalLineDragMatch.Column.Right, formalReg, formalReg);
            spawnedNodes.Add(formalNode);

            var informalNode = Instantiate(rightMatchNodePrefab, rightContainer);
            informalNode.Initialize(Masters_UniversalLineDragMatch.Column.Right, informalReg, informalReg);
            spawnedNodes.Add(informalNode);
        }

        foreach (var node in spawnedNodes) {
            if (node == null) continue;
            var tmp = node.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 14;
                tmp.fontSizeMax = 28;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        if (puzzleContainerRectTransform != null) {
            puzzleContainerRectTransform.DOKill();
            puzzleContainerRectTransform.localScale = Vector3.zero;
            puzzleContainerRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        Masters_UniversalLineDragMatch lineDragNode = eventData.pointerPress != null ? eventData.pointerPress.GetComponentInParent<Masters_UniversalLineDragMatch>() : null;

        if (lineDragNode != null && lineDragNode.GetColumnPosition() == Masters_UniversalLineDragMatch.Column.Left && !lineDragNode.GetIsSolved()) {
            if (lineDragNode.TryGetComponent(out Masters_ButtonPunchAnimator punch)) {
                punch.Punch();
            }

            canDrawLine = true;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }

            currentCorrectMatch = lineDragNode.GetCorrectMatch();
            currentLineRenderer = lineDragNode.GetLineRenderer();
            startLineDragToExpressionMatch = lineDragNode;
            if (currentLineRenderer != null) {
                currentLineRenderer.widthMultiplier = 20f;
                Vector3 startPos = lineDragNode.GetLineRendererPointTransform().position;
                currentLineRenderer.SetPosition(0, new Vector3(startPos.x, startPos.y, 0f));
            }
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (!canDrawLine || currentLineRenderer == null) return;

        Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, 0f);
        Vector3 worldPos = eventData.pressEventCamera != null ? eventData.pressEventCamera.ScreenToWorldPoint(screenPos) : Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;
        currentLineRenderer.SetPosition(1, worldPos);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!canDrawLine || currentLineRenderer == null || startLineDragToExpressionMatch == null) {
            ResetLine();
            return;
        }

        if (eventData.pointerCurrentRaycast.gameObject == null) {
            ResetLine();
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            return;
        }

        if (startLineDragToExpressionMatch.GetIsSolved()) return;

        Masters_UniversalLineDragMatch targetNode = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Masters_UniversalLineDragMatch>();

        if (targetNode != null && targetNode.GetColumnPosition() == Masters_UniversalLineDragMatch.Column.Right) {
            if (targetNode.TryGetComponent(out Masters_ButtonPunchAnimator punch)) {
                punch.Punch();
            }

            string targetMatchText = targetNode.GetCorrectMatch();
            if (string.IsNullOrEmpty(targetMatchText) && useUniqueRightCards) {
                TMP_Text tmp = targetNode.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) targetMatchText = tmp.text;
            }

            if (currentCorrectMatch == targetMatchText) {
                // Correct Match
                Vector3 endPos = targetNode.GetLineRendererPointTransform().position;
                currentLineRenderer.SetPosition(1, new Vector3(endPos.x, endPos.y, 0f));

                startLineDragToExpressionMatch.Solved();
                if (!useUniqueRightCards) {
                    targetNode.Solved();
                }

                correctCount++;
                correctCountInCurrentSet++;
                if (progressCountTMP != null) progressCountTMP.text = $"{correctCount}/{totalPairs}";
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                currentLineRenderer = null;
                canDrawLine = false;

                if (correctCount >= totalPairs) {
                    OnLessonSuccess();
                } else if (correctCountInCurrentSet >= currentSetExpectedPairs) {
                    // Page / Set completed! Transition to next page exactly as in IsThereADifference / older units
                    if (puzzleContainerRectTransform != null) {
                        puzzleContainerRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                            if (leftContainer != null) foreach (Transform child in leftContainer) Destroy(child.gameObject);
                            if (rightContainer != null) foreach (Transform child in rightContainer) Destroy(child.gameObject);

                            currentSetIndex++;
                            LoadNextSet();
                        });
                    } else {
                        if (leftContainer != null) foreach (Transform child in leftContainer) Destroy(child.gameObject);
                        if (rightContainer != null) foreach (Transform child in rightContainer) Destroy(child.gameObject);
                        currentSetIndex++;
                        LoadNextSet();
                    }
                }
            } else {
                ResetLine();
                if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        } else {
            ResetLine();
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void ResetLine() {
        if (currentLineRenderer != null) {
            currentLineRenderer.positionCount = 2;
            currentLineRenderer.SetPosition(0, Vector3.zero);
            currentLineRenderer.SetPosition(1, Vector3.zero);
        }
        currentLineRenderer = null;
        canDrawLine = false;
    }

    private void OnLessonSuccess() {
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
        NextButtonAnimation();
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
