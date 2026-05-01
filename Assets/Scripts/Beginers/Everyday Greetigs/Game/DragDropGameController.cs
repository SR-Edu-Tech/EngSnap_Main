using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel 2 - starts INACTIVE in the hierarchy.
/// Activated by ImageOptionGameController.OnNextPressed().
/// OnEnable resets the game every time it becomes active (first play + replays).
/// OnFinish calls UnitFinished() which disables the parent unitGameObject,
/// taking both panels inactive so the next open always starts from Panel 1.
///
/// FEEDBACK CHANGE: correct/wrong feedback is shown by coloring each draggable
/// word card's background Image component instead of the label text color.
/// </summary>
public class DragDropGameController : MonoBehaviour
{
    [Header("Data")]
    public DragWordData[] words;

    [Header("UI")]
    public Transform spawnArea;
    public GameObject wordPrefab;

    public DropContainer greetingBox;
    public DropContainer responseBox;

    public Button submitButton;
    public TMP_Text resultText;

    [Header("Finish")]
    public GameObject finishPanel;
    public Button finishButton;

    [Header("Unit System")]
    public UnitPanelController_BB1 unitPanel;
    public UnitButton_BB1 unitButton;

    [Header("Spawn Layout")]
    public float spawnSpacingX = 220f;
    public int batchSize = 2;

    [Header("Feedback Colors")]
    public Color correctColor = Color.green;
    public Color wrongColor   = Color.red;

    private List<DraggableWord> spawnedWords = new List<DraggableWord>();
    private int spawnIndex = 0;
    private bool waitingForBatchToClear = false;
    private bool listenersWired = false;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Wire listeners in Awake so they are ready before the first OnEnable
        if (!listenersWired)
        {
            submitButton.onClick.AddListener(CheckAnswers);
            finishButton.onClick.AddListener(OnFinish);
            listenersWired = true;
        }
    }

    void OnEnable()
    {
        // Resets and starts fresh every time this panel is shown
        ResetGame();
    }

    // ─────────────────────────────────────────────────────────────────────

    public void ResetGame()
    {
        StopAllCoroutines();

        foreach (var w in spawnedWords)
            if (w != null) Destroy(w.gameObject);

        spawnedWords.Clear();
        spawnIndex = 0;
        waitingForBatchToClear = false;

        ClearContainer(greetingBox != null ? greetingBox.transform : null);
        ClearContainer(responseBox  != null ? responseBox.transform  : null);

        if (resultText != null)   resultText.text = "";
        if (submitButton != null) submitButton.gameObject.SetActive(false);
        if (finishPanel != null)  finishPanel.SetActive(false);

        SpawnNextBatch();
    }

    void ClearContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!waitingForBatchToClear) return;

        int remaining = 0;
        foreach (var word in spawnedWords)
            if (word != null && word.transform.parent == spawnArea)
                remaining++;

        if (remaining == 0)
        {
            waitingForBatchToClear = false;
            SpawnNextBatch();
        }
    }

    void SpawnNextBatch()
    {
        int spawned = 0;

        for (int i = 0; i < batchSize; i++)
        {
            if (spawnIndex >= words.Length) break;

            GameObject obj = Instantiate(wordPrefab, spawnArea);
            var drag = obj.GetComponent<DraggableWord>();
            drag.Init(words[spawnIndex]);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.localScale    = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin     = new Vector2(0.5f, 0.5f);
            rt.anchorMax     = new Vector2(0.5f, 0.5f);
            rt.pivot         = new Vector2(0.5f, 0.5f);

            float totalWidth = (batchSize - 1) * spawnSpacingX;
            rt.anchoredPosition = new Vector2(-totalWidth / 2f + i * spawnSpacingX, 0f);

            obj.transform.localScale = Vector3.zero;
            StartCoroutine(PopIn(obj.transform));

            spawnedWords.Add(drag);
            spawnIndex++;
            spawned++;
        }

        if (spawned > 0)
            waitingForBatchToClear = true;
    }

    IEnumerator PopIn(Transform target)
    {
        float time = 0f, duration = 0.25f;
        while (time < duration)
        {
            target.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        time = 0f;
        float settle = 0.1f;
        while (time < settle)
        {
            target.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, time / settle);
            time += Time.deltaTime;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────────────

    public void OnWordDropped()
    {
        CheckSubmitVisibility();
    }

    void CheckSubmitVisibility()
    {
        foreach (var word in spawnedWords)
        {
            if (word == null) continue;
            if (!word.isDropped)
            {
                submitButton.gameObject.SetActive(false);
                return;
            }
        }

        if (spawnIndex >= words.Length)
            submitButton.gameObject.SetActive(true);
    }

    void CheckAnswers()
    {
        submitButton.gameObject.SetActive(false);

        int correct = 0, wrong = 0;

        foreach (var word in spawnedWords)
        {
            if (word == null) continue;

            bool isCorrect = false;
            Transform parent = word.transform.parent;

            if (parent == greetingBox.transform && word.data.isGreeting) isCorrect = true;
            if (parent == responseBox.transform  && word.data.isResponse) isCorrect = true;

            // Color the card's background Image instead of the text label
            if (word.background != null)
                word.background.color = isCorrect ? correctColor : wrongColor;

            if (isCorrect) correct++;
            else           wrong++;
        }

        resultText.text = $"✓ Correct: {correct}    ✗ Wrong: {wrong}";
        StartCoroutine(ShowFinishAfterDelay(2f));
    }

    IEnumerator ShowFinishAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        finishPanel.SetActive(true);
    }

    void OnFinish()
    {
        finishPanel.SetActive(false);

        // UnitPanelController.UnitFinished() disables unitGameObject entirely.
        // Both child panels (Image + DragDrop) go inactive together.
        // Next time the player opens this unit, unitGameObject is re-enabled,
        // ImageOptionPanel (active by default) fires OnEnable and resets to Q0.
        if (unitPanel != null && unitButton != null)
            unitPanel.UnitFinished(unitButton);
    }
}