using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class BirthdayBashBoardHighlighter : MonoBehaviour
{
    public BirthdayBashBoard_P_Senior manager;
    public RectTransform highlighterRect;
    public Vector2 offset = new Vector2(0f, 55f);

    private int lastStoneIndex = -2;

    private void Start()
    {
        if (highlighterRect != null && Application.isPlaying)
        {
            highlighterRect.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (manager == null || highlighterRect == null) return;

        int currentIdx = GetCurrentStoneIndex();

        // Real-time WYSIWYG editor preview of offset location in edit mode
        if (!Application.isPlaying)
        {
            UpdateHighlighterPositionImmediate(currentIdx);
            return;
        }

        if (currentIdx != lastStoneIndex)
        {
            lastStoneIndex = currentIdx;
            UpdateHighlighterPosition(currentIdx);
        }

        // Manage flip button visibility based on game state
        Button flipBtn = manager.flipButton;
        if (flipBtn != null)
        {
            GameObject choicePanel = manager.choicePanel;
            RectTransform mascot = manager.mascotCharacter;

            bool isMascotHopping = mascot != null && LeanTween.isTweening(mascot.gameObject);
            bool isChoiceActive = choicePanel != null && choicePanel.activeSelf;
            bool isCompleted = IsGameCompleted();

            bool shouldHide = isCompleted || isChoiceActive || isMascotHopping;
            bool shouldBeVisible = !shouldHide;

            if (flipBtn.gameObject.activeSelf != shouldBeVisible)
            {
                flipBtn.gameObject.SetActive(shouldBeVisible);
            }
        }
    }

    private int GetCurrentStoneIndex()
    {
        var field = typeof(BirthdayBashBoard_P_Senior).GetField("_currentStoneIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(manager);
        }
        return -1;
    }

    private bool IsGameCompleted()
    {
        var field = typeof(BirthdayBashBoard_P_Senior).GetField("_isGameCompleted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (bool)field.GetValue(manager);
        }
        return false;
    }

    private RectTransform GetTargetTransform(int index)
    {
        if (manager.pathStones != null)
        {
            if (index >= 0 && index < manager.pathStones.Length && manager.pathStones[index] != null)
            {
                return manager.pathStones[index];
            }
            else if (index == -1) // Start platform
            {
                return manager.startPlatform;
            }
        }
        return null;
    }

    private void UpdateHighlighterPositionImmediate(int index)
    {
        RectTransform targetTransform = GetTargetTransform(index);
        if (targetTransform != null)
        {
            highlighterRect.gameObject.SetActive(true);
            highlighterRect.anchoredPosition = targetTransform.anchoredPosition + offset;
        }
        else
        {
            highlighterRect.gameObject.SetActive(false);
        }
    }

    private void UpdateHighlighterPosition(int index)
    {
        RectTransform targetTransform = GetTargetTransform(index);
        if (targetTransform != null)
        {
            highlighterRect.gameObject.SetActive(true);
            LeanTween.cancel(highlighterRect.gameObject);

            LeanTween.value(highlighterRect.gameObject, highlighterRect.anchoredPosition, targetTransform.anchoredPosition + offset, 0.4f)
                .setOnUpdate((Vector2 val) => {
                    highlighterRect.anchoredPosition = val;
                })
                .setEase(LeanTweenType.easeOutQuad);
        }
        else
        {
            highlighterRect.gameObject.SetActive(false);
        }
    }
}
