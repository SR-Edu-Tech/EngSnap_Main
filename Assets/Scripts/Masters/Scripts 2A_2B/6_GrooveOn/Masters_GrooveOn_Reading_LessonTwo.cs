using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag Relay Helper to ensure Unity EventSystem drag events on card child objects are
/// reliably forwarded to the main line-drawing controller.
/// </summary>
public class Masters_NodeDragRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {
    public Masters_GrooveOn_Reading_LessonTwo controller;
    public Masters_UniversalLineDragMatch node;

    public void OnBeginDrag(PointerEventData eventData) {
        if (controller != null) controller.OnNodeBeginDrag(node, eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        if (controller != null) controller.OnNodeDrag(node, eventData);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (controller != null) controller.OnNodeEndDrag(node, eventData);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (controller != null) controller.OnNodeCardClicked(node);
    }
}

/// <summary>
/// Controller for Unit 6 (Groove On) Reading Branch - Stage R02: Match — Festival <-> Greeting.
/// Features 100% reliable drag-and-drop line drawing and tap matching with bright, highly-visible UI line rendering.
/// </summary>
public class Masters_GrooveOn_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

    [Header("Line Visual Tokens")]
    [SerializeField] private Color activeLineColor = new Color(0.12f, 0.53f, 1.0f, 1.0f); // Vibrant Royal Blue (#1D4ED8 / #2563EB)
    [SerializeField] private Color dragLineColor = new Color(0.0f, 0.82f, 1.0f, 1.0f);   // Bright Cyan / Electric Blue (#00D2FF)
    [SerializeField] private float lineWidth = 14f;

    private GameObject activeDragLineObj;
    private RectTransform activeDragLineRect;
    private Masters_UniversalLineDragMatch currentDragStartNode;
    private Masters_UniversalLineDragMatch currentHoverTargetNode;
    private Masters_UniversalLineDragMatch currentSelectedNode;
    private List<GameObject> createdPermanentLines = new List<GameObject>();
    private RectTransform canvasRect;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        FindCanvasRect();
    }

    protected override void Start() {
        base.Start();
        FindCanvasRect();
        UpdateTitleAndUIComponents();
        StartCoroutine(AttachDragRelaysRoutine());

        // Play VO_R02_ARIA intro voiceover when R02 starts
        if (Masters_AudioManager.Instance != null) {
            AudioClip introClip = null;
#if UNITY_EDITOR
            introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Reading/Every festival has its own greeting pair them up.mp3");
            #endif
            if (introClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            }
        }
    }

    private void FindCanvasRect() {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null) canvasRect = c.GetComponent<RectTransform>();
        if (canvasRect == null) canvasRect = GetComponent<RectTransform>();
    }

    private Camera GetUICamera() {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        if (c != null && c.worldCamera != null) return c.worldCamera;
        return Camera.main;
    }

    private void ClearAllLines() {
        DestroyActiveDragLine();
        if (createdPermanentLines != null) {
            foreach (var line in createdPermanentLines) {
                if (line != null) Destroy(line);
            }
            createdPermanentLines.Clear();
        }
    }

    private IEnumerator AttachDragRelaysRoutine() {
        yield return new WaitForSeconds(0.1f);
        AttachDragRelaysToNodes();
    }

    private void AttachDragRelaysToNodes() {
        FindCanvasRect();
        Masters_UniversalLineDragMatch[] nodes = GetComponentsInChildren<Masters_UniversalLineDragMatch>(true);
        foreach (var node in nodes) {
            if (node == null) continue;
            Masters_NodeDragRelay relay = node.GetComponent<Masters_NodeDragRelay>();
            if (relay == null) relay = node.gameObject.AddComponent<Masters_NodeDragRelay>();
            relay.controller = this;
            relay.node = node;

            // Also attach relay to child Image / Text so drag is never blocked by raycasts
            Image[] childImgs = node.GetComponentsInChildren<Image>(true);
            foreach (var img in childImgs) {
                if (img == null) continue;
                Masters_NodeDragRelay childRelay = img.GetComponent<Masters_NodeDragRelay>();
                if (childRelay == null) childRelay = img.gameObject.AddComponent<Masters_NodeDragRelay>();
                childRelay.controller = this;
                childRelay.node = node;
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Match") || textVal.Contains("Polished") || textVal.Contains("R02")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "R02 Match — Festival ↔ Greeting";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("READING")) {
                tmp.text = "READING BRANCH (Book Stall)";
            }
        }
    }

    // --- OVERRIDE BASE EVENT HANDLERS SO DRAG IS HANDLED BY THIS CLASS ---
    public virtual new void OnBeginDrag(PointerEventData eventData) {
        if (eventData == null) return;
        GameObject pressedObj = eventData.pointerPress ?? eventData.pointerEnter ?? eventData.rawPointerPress;
        Masters_UniversalLineDragMatch node = (pressedObj != null) ? pressedObj.GetComponentInParent<Masters_UniversalLineDragMatch>() : null;
        if (node != null) {
            OnNodeBeginDrag(node, eventData);
        }
    }

    public virtual new void OnDrag(PointerEventData eventData) {
        if (currentDragStartNode != null) {
            OnNodeDrag(currentDragStartNode, eventData);
        }
    }

    public virtual new void OnEndDrag(PointerEventData eventData) {
        if (currentDragStartNode != null) {
            OnNodeEndDrag(currentDragStartNode, eventData);
        }
    }

    public void OnNodeBeginDrag(Masters_UniversalLineDragMatch node, PointerEventData eventData) {
        if (node == null || node.GetIsSolved()) return;
        if (currentDragStartNode != null) return; // Prevent multiple simultaneous drags

        currentDragStartNode = node;
        currentSelectedNode = node;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        Vector3 startWorld = GetNodeDotPosition(node);
        CreateActiveDragLine(startWorld);
    }

    public void OnNodeDrag(Masters_UniversalLineDragMatch node, PointerEventData eventData) {
        if (currentDragStartNode == null || activeDragLineRect == null) return;

        Vector3 startWorld = GetNodeDotPosition(currentDragStartNode);

        // Check if currently hovering over a valid target node of opposite column
        Masters_UniversalLineDragMatch candidateTarget = null;
        if (eventData != null && eventData.pointerCurrentRaycast.gameObject != null) {
            candidateTarget = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Masters_UniversalLineDragMatch>();
        }

        bool isValidTargetHover = (candidateTarget != null &&
                                   !candidateTarget.GetIsSolved() &&
                                   candidateTarget != currentDragStartNode &&
                                   candidateTarget.GetColumnPosition() != currentDragStartNode.GetColumnPosition());

        if (isValidTargetHover) {
            if (currentHoverTargetNode != candidateTarget) {
                ClearHoverHighlight();
                currentHoverTargetNode = candidateTarget;
                HighlightTargetHover(currentHoverTargetNode, true);
            }
            Vector3 targetWorld = GetNodeDotPosition(currentHoverTargetNode);
            UpdateUILinePositionWorld(activeDragLineRect, startWorld, targetWorld);
        } else {
            ClearHoverHighlight();
            Vector2 mouseOrTouchPos = (eventData != null) ? eventData.position : (Vector2)Input.mousePosition;
            UpdateUILinePositionScreen(activeDragLineRect, startWorld, mouseOrTouchPos);
        }
    }

    public void OnNodeEndDrag(Masters_UniversalLineDragMatch node, PointerEventData eventData) {
        ClearHoverHighlight();
        DestroyActiveDragLine();

        if (currentDragStartNode != null && eventData != null && eventData.pointerCurrentRaycast.gameObject != null) {
            Masters_UniversalLineDragMatch targetNode = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Masters_UniversalLineDragMatch>();
            if (targetNode != null && !targetNode.GetIsSolved() && targetNode.GetColumnPosition() != currentDragStartNode.GetColumnPosition()) {
                CheckAndCompleteMatch(currentDragStartNode, targetNode);
            }
        }

        currentDragStartNode = null;
    }

    private void HighlightTargetHover(Masters_UniversalLineDragMatch node, bool isHovered) {
        if (node == null) return;
        node.transform.DOKill();
        if (isHovered) {
            node.transform.DOScale(1.06f, 0.15f).SetEase(Ease.OutQuad);
        } else {
            node.transform.DOScale(1.0f, 0.15f).SetEase(Ease.OutQuad);
        }
    }

    private void ClearHoverHighlight() {
        if (currentHoverTargetNode != null) {
            HighlightTargetHover(currentHoverTargetNode, false);
            currentHoverTargetNode = null;
        }
    }

    public void OnNodeCardClicked(Masters_UniversalLineDragMatch node) {
        if (node == null || node.GetIsSolved()) return;

        if (currentSelectedNode == null) {
            currentSelectedNode = node;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            node.transform.DOKill();
            node.transform.DOPunchScale(Vector3.one * 0.12f, 0.25f);
        } else if (currentSelectedNode == node) {
            currentSelectedNode = null;
        } else {
            if (node.GetColumnPosition() != currentSelectedNode.GetColumnPosition()) {
                CheckAndCompleteMatch(currentSelectedNode, node);
            } else {
                currentSelectedNode = node;
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                }
                node.transform.DOKill();
                node.transform.DOPunchScale(Vector3.one * 0.12f, 0.25f);
            }
        }
    }

    private void CheckAndCompleteMatch(Masters_UniversalLineDragMatch nodeA, Masters_UniversalLineDragMatch nodeB) {
        if (nodeA == null || nodeB == null || nodeA.GetIsSolved() || nodeB.GetIsSolved()) return;

        Masters_UniversalLineDragMatch leftNode = (nodeA.GetColumnPosition() == Masters_UniversalLineDragMatch.Column.Left) ? nodeA : nodeB;
        Masters_UniversalLineDragMatch rightNode = (nodeA.GetColumnPosition() == Masters_UniversalLineDragMatch.Column.Right) ? nodeA : nodeB;

        string leftTarget = leftNode.GetCorrectMatch();
        string rightText = rightNode.GetCorrectMatch();
        if (string.IsNullOrEmpty(rightText)) {
            TMP_Text tmp = rightNode.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) rightText = tmp.text;
        }

        bool isMatch = false;
        if (!string.IsNullOrEmpty(leftTarget) && !string.IsNullOrEmpty(rightText)) {
            string lTrim = leftTarget.Trim();
            string rTrim = rightText.Trim();

            if (lTrim.Equals(rTrim, System.StringComparison.OrdinalIgnoreCase)) isMatch = true;
            else if (lTrim.Contains("Diwali") && rTrim.Contains("Diwali")) isMatch = true;
            else if (lTrim.Contains("Christmas") && rTrim.Contains("Christmas")) isMatch = true;
            else if (lTrim.Contains("Easter") && rTrim.Contains("Easter")) isMatch = true;
            else if (lTrim.Contains("Eid") && rTrim.Contains("Eid")) isMatch = true;
            else if (lTrim.Contains("New Year") && rTrim.Contains("New Year")) isMatch = true;
            else if (lTrim.Contains("Independence") && rTrim.Contains("Independence")) isMatch = true;
            else if (lTrim.Contains("Gandhi") && rTrim.Contains("Gandhi")) isMatch = true;
            else if (lTrim.Contains("Guru") && rTrim.Contains("Gurpurab")) isMatch = true;
        }

        if (isMatch) {
            Vector3 startPos = GetNodeDotPosition(leftNode);
            Vector3 endPos = GetNodeDotPosition(rightNode);
            CreatePermanentUILine(startPos, endPos);

            leftNode.Solved();
            rightNode.Solved();
            currentSelectedNode = null;

            HighlightNodeColor(leftNode, activeLineColor);
            HighlightNodeColor(rightNode, activeLineColor);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            correctCount++;
            correctCountInCurrentSet++;
            if (progressCountTMP != null) progressCountTMP.text = $"{correctCount}/{totalPairs}";

            if (correctCount >= totalPairs) {
                OnLessonSuccess();
            } else if (correctCountInCurrentSet >= currentSetExpectedPairs) {
                if (puzzleContainerRectTransform != null) {
                    puzzleContainerRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                        ClearAllLines();
                        if (leftContainer != null) foreach (Transform child in leftContainer) Destroy(child.gameObject);
                        if (rightContainer != null) foreach (Transform child in rightContainer) Destroy(child.gameObject);

                        currentSetIndex++;
                        LoadNextSet();
                        AttachDragRelaysToNodes();
                    });
                } else {
                    ClearAllLines();
                    if (leftContainer != null) foreach (Transform child in leftContainer) Destroy(child.gameObject);
                    if (rightContainer != null) foreach (Transform child in rightContainer) Destroy(child.gameObject);
                    currentSetIndex++;
                    LoadNextSet();
                    AttachDragRelaysToNodes();
                }
            }
        } else {
            currentSelectedNode = null;
            Transform shakeT = nodeB != null ? nodeB.transform : null;
            if (shakeT != null) {
                shakeT.DOKill();
                shakeT.DOShakePosition(0.35f, new Vector3(12f, 0, 0));
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private void HighlightNodeColor(Masters_UniversalLineDragMatch node, Color targetColor) {
        if (node == null) return;
        Image nodeImg = node.GetComponent<Image>() ?? node.GetComponentInChildren<Image>();
        if (nodeImg != null) {
            nodeImg.DOColor(targetColor, 0.3f);
        }
    }

    private Vector3 GetNodeDotPosition(Masters_UniversalLineDragMatch node) {
        if (node == null) return Vector3.zero;
        Transform dotT = node.transform.Find("LineRendererPoint") 
                         ?? node.transform.Find("Dot") 
                         ?? node.transform.Find("Point");
        if (dotT == null && node.GetLineRendererPointTransform() != null) {
            dotT = node.GetLineRendererPointTransform();
        }
        if (dotT == null) dotT = node.transform;
        return dotT.position;
    }

    private void CreateActiveDragLine(Vector3 startWorldPos) {
        DestroyActiveDragLine();
        FindCanvasRect();

        activeDragLineObj = new GameObject("ActiveDragLine");
        activeDragLineObj.transform.SetParent(canvasRect, false);
        activeDragLineObj.transform.SetAsLastSibling(); // Render ON TOP of panel so line is 100% visible!

        Image img = activeDragLineObj.AddComponent<Image>();
        img.color = dragLineColor; // Bright Cyan / Electric Blue (#00D2FF)
        img.raycastTarget = false;

        activeDragLineRect = activeDragLineObj.GetComponent<RectTransform>();
        activeDragLineRect.anchorMin = new Vector2(0.5f, 0.5f);
        activeDragLineRect.anchorMax = new Vector2(0.5f, 0.5f);
        activeDragLineRect.pivot = new Vector2(0f, 0.5f);

        UpdateUILinePositionWorld(activeDragLineRect, startWorldPos, startWorldPos);
    }

    private void DestroyActiveDragLine() {
        if (activeDragLineObj != null) {
            Destroy(activeDragLineObj);
            activeDragLineObj = null;
            activeDragLineRect = null;
        }
    }

    private void CreatePermanentUILine(Vector3 startWorldPos, Vector3 endWorldPos) {
        FindCanvasRect();

        GameObject lineObj = new GameObject($"MatchedUILine_{createdPermanentLines.Count}");
        lineObj.transform.SetParent(canvasRect, false);
        lineObj.transform.SetAsLastSibling(); // Render ON TOP of panel so line is 100% visible!

        Image img = lineObj.AddComponent<Image>();
        img.color = activeLineColor; // Vibrant Royal Blue (#1D4ED8)
        img.raycastTarget = false;

        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0f, 0.5f);

        UpdateUILinePositionWorld(lineRect, startWorldPos, endWorldPos);

        lineObj.transform.DOKill();
        lineObj.transform.localScale = Vector3.one;
        lineObj.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.25f);

        createdPermanentLines.Add(lineObj);
    }

    private void UpdateUILinePositionWorld(RectTransform lineRect, Vector3 worldStart, Vector3 worldEnd) {
        if (lineRect == null || canvasRect == null) return;
        Camera uiCam = GetUICamera();

        Vector2 localStart, localEnd;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(uiCam, worldStart), uiCam, out localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(uiCam, worldEnd), uiCam, out localEnd);

        ApplyLineRectTransform(lineRect, localStart, localEnd);
    }

    private void UpdateUILinePositionScreen(RectTransform lineRect, Vector3 worldStart, Vector2 screenEnd) {
        if (lineRect == null || canvasRect == null) return;
        Camera uiCam = GetUICamera();

        Vector2 localStart, localEnd;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(uiCam, worldStart), uiCam, out localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenEnd, uiCam, out localEnd);

        ApplyLineRectTransform(lineRect, localStart, localEnd);
    }

    private void ApplyLineRectTransform(RectTransform lineRect, Vector2 localStart, Vector2 localEnd) {
        Vector2 dir = localEnd - localStart;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.anchoredPosition = localStart;
        lineRect.sizeDelta = new Vector2(distance, lineWidth);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Reading;
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Reading);
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

}