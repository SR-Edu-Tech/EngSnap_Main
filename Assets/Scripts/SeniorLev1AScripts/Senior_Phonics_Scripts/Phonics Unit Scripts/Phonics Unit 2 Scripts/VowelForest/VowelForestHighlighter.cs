using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class VowelForestHighlighter : MonoBehaviour
{
    public VowelForestManager_Senior manager;
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
        Button flipBtn = GetFlipButton();
        if (flipBtn != null)
        {
            GameObject choicePanel = GetChoicePanel();
            RectTransform mascot = GetMascotCharacter();

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
        var field = typeof(VowelForestManager_Senior).GetField("currentStoneIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(manager);
        }
        return -1;
    }

    private Button GetFlipButton()
    {
        var field = typeof(VowelForestManager_Senior).GetField("flipButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (Button)field.GetValue(manager);
        }
        return null;
    }

    private GameObject GetChoicePanel()
    {
        var field = typeof(VowelForestManager_Senior).GetField("choicePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (GameObject)field.GetValue(manager);
        }
        return null;
    }

    private RectTransform GetMascotCharacter()
    {
        var field = typeof(VowelForestManager_Senior).GetField("mascotCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (RectTransform)field.GetValue(manager);
        }
        return null;
    }

    private bool IsGameCompleted()
    {
        var field = typeof(VowelForestManager_Senior).GetField("isGameCompleted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (bool)field.GetValue(manager);
        }
        return false;
    }

    private RectTransform GetTargetTransform(int index)
    {
        var field = typeof(VowelForestManager_Senior).GetField("pathStones", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            RectTransform[] stones = (RectTransform[])field.GetValue(manager);
            if (stones != null)
            {
                if (index >= 0 && index < stones.Length && stones[index] != null)
                {
                    return stones[index];
                }
                else if (index == -1) // Start platform
                {
                    var startField = typeof(VowelForestManager_Senior).GetField("startPlatform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (startField != null)
                    {
                        return (RectTransform)startField.GetValue(manager);
                    }
                }
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
            Vector2 finalOffset = offset == Vector2.zero ? new Vector2(0f, 55f) : offset;
            highlighterRect.anchoredPosition = targetTransform.anchoredPosition + finalOffset;
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

            Vector2 finalOffset = offset == Vector2.zero ? new Vector2(0f, 55f) : offset;
            LeanTween.value(highlighterRect.gameObject, highlighterRect.anchoredPosition, targetTransform.anchoredPosition + finalOffset, 0.4f)
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
