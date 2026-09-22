using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit2
{
    /// <summary>
    /// Sequential Checkpoint Tracing Component with Animated Pop-Up Guidance.
    /// Checkpoints appear one after another sequentially (0 -> 1 -> 2 -> 3 -> 4).
    /// Enforces continuous single-stroke tracing: if finger is lifted before completing all checkpoints,
    /// the drawn line resets and Checkpoint 0 pops up again for a fresh redraw.
    /// Supports both Sprite-based and Text-based tracing (single letters and multi-letter teams).
    /// </summary>
    public class LetterTracingComponent : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image outlineImage;        // Dotted outline image (PNG)
        [SerializeField] private Image filledLetterImage;   // Filled letter image (hidden at first)
        [SerializeField] private TMP_Text outlineText;      // Optional dotted/guide text for spelling (e.g. "ee", "ea")
        [SerializeField] private TMP_Text filledLetterText; // Optional filled completed text
        [SerializeField] private GameObject startDot;       // Start dot sprite GameObject (at Checkpoint 0)
        [SerializeField] private LineRenderer lineRenderer;  // LineRenderer to draw child's finger path

        [Header("Checkpoint Tracing Setup")]
        [Tooltip("Place Checkpoint GameObjects/Transforms (e.g. 3, 5, or 8) along the stroke path in exact sequential order.")]
        [SerializeField] private Transform[] checkpoints = new Transform[8];

        [Tooltip("Distance threshold (in UI pixels) around each checkpoint to register touch.")]
        [SerializeField] private float checkpointRadius = 40f;

        [Tooltip("Minimum finger movement distance before appending a new line renderer point.")]
        [SerializeField] private float minDistanceBetweenPoints = 5f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tracingSoundClip;

        // Events
        public event Action OnTracingCompleted;
        public event Action OnTracingFailedAttempt;
        private Action _onCompleteCallback;

        private RectTransform _rectTransform;
        private List<Vector2> _drawnPoints = new List<Vector2>();
        private int _currentCheckpointIndex = 0;
        private int _activeCheckpointCount = 0;
        private bool _isTracing = false;
        private bool _isCompleted = false;
        private Coroutine _checkpointPopUpCoroutine;

        public int CurrentCheckpointIndex => _currentCheckpointIndex;
        public int TotalCheckpoints => _activeCheckpointCount;
        public float CheckpointProgress => _activeCheckpointCount > 0 ? (float)_currentCheckpointIndex / _activeCheckpointCount : 0f;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            EnsureLineRenderer();
        }

        private void Start()
        {
            ValidateSceneSetup();
        }

        private void ValidateSceneSetup()
        {
            if (EventSystem.current == null)
            {
                Debug.LogWarning("[LetterTracingComponent] No EventSystem found in scene! Auto-creating EventSystem...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogWarning("[LetterTracingComponent] Parent Canvas missing GraphicRaycaster! Auto-adding GraphicRaycaster...");
                parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // Ensure this component's GameObject captures raycasts
            Image selfImg = GetComponent<Image>();
            if (selfImg == null)
            {
                selfImg = gameObject.AddComponent<Image>();
                selfImg.color = new Color(0f, 0f, 0f, 0f); // Transparent raycast catcher
            }
            selfImg.raycastTarget = true;

            // Child decoration images shouldn't intercept parent pointer events
            if (outlineImage != null) outlineImage.raycastTarget = false;
            if (filledLetterImage != null) filledLetterImage.raycastTarget = false;
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        private void ConfigureLineRenderer()
        {
            if (lineRenderer != null)
            {
                if (lineRenderer.sharedMaterial == null || lineRenderer.material == null)
                {
                    Shader defaultShader = Shader.Find("Sprites/Default");
                    if (defaultShader == null) defaultShader = Shader.Find("UI/Default");
                    if (defaultShader != null)
                    {
                        lineRenderer.material = new Material(defaultShader);
                    }
                }

                lineRenderer.useWorldSpace = false; // Local UI Space ensures rendering in Overlay Canvas
                lineRenderer.alignment = LineAlignment.TransformZ;
                lineRenderer.startWidth = 24f;
                lineRenderer.endWidth = 24f;
                lineRenderer.numCapVertices = 5;    // Round line start/end caps
                lineRenderer.numCornerVertices = 5; // Round smooth line corners
                lineRenderer.startColor = new Color(0f, 0.75f, 0.95f, 1f); // Vibrant Cyan Blue
                lineRenderer.endColor = new Color(0f, 0.75f, 0.95f, 1f);
                lineRenderer.sortingOrder = 100; // Render on top of UI Canvas Images
                lineRenderer.positionCount = 0;
            }
        }

        public void SetupTracing(Sprite outlineSprite, AudioClip soundClip, string letterSpelling, Sprite filledSprite = null, Vector2[] itemCheckpoints = null, Action onComplete = null)
        {
            _onCompleteCallback = onComplete;
            _isCompleted = false;
            _isTracing = false;
            _currentCheckpointIndex = 0;
            tracingSoundClip = soundClip;

            _drawnPoints.Clear();

            EnsureLineRenderer();
            if (lineRenderer != null) lineRenderer.positionCount = 0;

            // 1. Determine active checkpoint count (e.g. 3, 5, or 8) based on item data or pool size
            if (itemCheckpoints != null && itemCheckpoints.Length > 0)
            {
                EnsureCheckpointPool(itemCheckpoints.Length);
                _activeCheckpointCount = itemCheckpoints.Length;

                // Assign positions for active checkpoints
                for (int i = 0; i < _activeCheckpointCount; i++)
                {
                    if (i < checkpoints.Length && checkpoints[i] != null)
                    {
                        RectTransform rt = checkpoints[i] as RectTransform;
                        if (rt != null) rt.anchoredPosition = itemCheckpoints[i];
                        else checkpoints[i].localPosition = itemCheckpoints[i];
                    }
                }
            }
            else
            {
                _activeCheckpointCount = checkpoints != null ? checkpoints.Length : 0;
            }

            // 2. Hide all checkpoints initially and deactivate any unused checkpoints beyond _activeCheckpointCount
            HideAllCheckpoints();

            // Dotted outline image visible
            if (outlineImage != null)
            {
                if (outlineSprite != null)
                {
                    outlineImage.sprite = outlineSprite;
                    outlineImage.gameObject.SetActive(true);
                }
                else if (outlineImage.sprite != null)
                {
                    outlineImage.gameObject.SetActive(true);
                }
                else
                {
                    outlineImage.gameObject.SetActive(false);
                }
            }

            // Outline Text guide
            if (outlineText != null)
            {
                if (!string.IsNullOrEmpty(letterSpelling))
                {
                    outlineText.text = letterSpelling;
                    outlineText.gameObject.SetActive(true);
                }
                else
                {
                    outlineText.gameObject.SetActive(false);
                }
            }

            // Filled letter image hidden at first
            if (filledLetterImage != null)
            {
                if (filledSprite != null) filledLetterImage.sprite = filledSprite;
                else if (outlineSprite != null) filledLetterImage.sprite = outlineSprite;
                filledLetterImage.gameObject.SetActive(false);
            }

            // Filled letter text hidden at first
            if (filledLetterText != null)
            {
                if (!string.IsNullOrEmpty(letterSpelling))
                {
                    filledLetterText.text = letterSpelling;
                }
                filledLetterText.gameObject.SetActive(false);
            }

            // Pop up Checkpoint 0 if active
            if (_activeCheckpointCount > 0)
            {
                ShowCheckpointPopUp(0);
            }
        }

        public void SetupTracing(Sprite outlineSprite, AudioClip soundClip, char missingLetter = 'a', Sprite filledSprite = null, Vector2[] itemCheckpoints = null, Action onComplete = null)
        {
            SetupTracing(outlineSprite, soundClip, missingLetter.ToString(), filledSprite, itemCheckpoints, onComplete);
        }

        public void SetupTracing(char missingLetter, Sprite outlineSprite, Sprite filledSprite, Action onComplete = null, AudioClip soundClip = null, Vector2[] itemCheckpoints = null)
        {
            SetupTracing(outlineSprite, soundClip, missingLetter.ToString(), filledSprite, itemCheckpoints, onComplete);
        }

        public void SetupTracing(string letterSpelling, Sprite outlineSprite, Sprite filledSprite, Action onComplete = null, AudioClip soundClip = null, Vector2[] itemCheckpoints = null)
        {
            SetupTracing(outlineSprite, soundClip, letterSpelling, filledSprite, itemCheckpoints, onComplete);
        }

        private void EnsureCheckpointPool(int requiredCount)
        {
            if (checkpoints == null)
            {
                checkpoints = new Transform[0];
            }

            if (checkpoints.Length >= requiredCount) return;

            // Find a template checkpoint in the pool to clone if needed
            Transform template = null;
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] != null)
                {
                    template = checkpoints[i];
                    break;
                }
            }

            Transform parent = template != null ? template.parent : transform;
            List<Transform> list = new List<Transform>(checkpoints);

            while (list.Count < requiredCount)
            {
                int newIndex = list.Count;
                GameObject newObj;
                if (template != null)
                {
                    newObj = Instantiate(template.gameObject, parent);
                    newObj.name = $"Checkpoint_{newIndex}";
                }
                else
                {
                    newObj = new GameObject($"Checkpoint_{newIndex}", typeof(RectTransform));
                    newObj.transform.SetParent(parent, false);
                }
                newObj.SetActive(false);
                list.Add(newObj.transform);
            }

            checkpoints = list.ToArray();
        }

        private void HideAllCheckpoints()
        {
            StopCheckpointPopUpAnimation();
            if (checkpoints != null)
            {
                for (int i = 0; i < checkpoints.Length; i++)
                {
                    if (checkpoints[i] != null)
                    {
                        checkpoints[i].gameObject.SetActive(false);
                    }
                }
            }
            if (startDot != null) startDot.SetActive(false);
        }

        private void ShowCheckpointPopUp(int index)
        {
            StopCheckpointPopUpAnimation();
            if (checkpoints != null && index >= 0 && index < _activeCheckpointCount && checkpoints[index] != null)
            {
                checkpoints[index].gameObject.SetActive(true);
                _checkpointPopUpCoroutine = StartCoroutine(CheckpointPopUpSequence(checkpoints[index]));

                // Position StartDot over Checkpoint 0 if configured
                if (index == 0 && startDot != null)
                {
                    startDot.transform.position = checkpoints[0].position;
                    startDot.SetActive(true);
                }
            }
        }

        private void StopCheckpointPopUpAnimation()
        {
            if (_checkpointPopUpCoroutine != null)
            {
                StopCoroutine(_checkpointPopUpCoroutine);
                _checkpointPopUpCoroutine = null;
            }
        }

        private IEnumerator CheckpointPopUpSequence(Transform targetCheckpoint)
        {
            if (targetCheckpoint == null) yield break;

            Vector3 baseScale = Vector3.one;
            Vector3 popScale = baseScale * 1.3f;
            Vector3 pulseScale = baseScale * 1.2f;

            // 1. Pop-Up Animation (0.0 -> 1.3 -> 1.0)
            targetCheckpoint.localScale = Vector3.zero;
            float elapsed = 0f;
            float popTime = 0.15f;

            while (elapsed < popTime)
            {
                elapsed += Time.deltaTime;
                targetCheckpoint.localScale = Vector3.Lerp(Vector3.zero, popScale, elapsed / popTime);
                yield return null;
            }

            elapsed = 0f;
            float settleTime = 0.1f;
            while (elapsed < settleTime)
            {
                elapsed += Time.deltaTime;
                targetCheckpoint.localScale = Vector3.Lerp(popScale, baseScale, elapsed / settleTime);
                yield return null;
            }

            // 2. Continuous Scale Pulse Loop while waiting for touch
            while (!_isCompleted)
            {
                elapsed = 0f;
                float duration = 0.5f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    targetCheckpoint.localScale = Vector3.Lerp(baseScale, pulseScale, elapsed / duration);
                    yield return null;
                }

                elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    targetCheckpoint.localScale = Vector3.Lerp(pulseScale, baseScale, elapsed / duration);
                    yield return null;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isCompleted) return;

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out localPos);

            _isTracing = true;

            // If user lifted hand previously, start fresh from Checkpoint 0
            if (_currentCheckpointIndex == 0)
            {
                _drawnPoints.Clear();
                if (lineRenderer != null) lineRenderer.positionCount = 0;
            }

            AddDrawnPoint(localPos, true);
            EvaluateSequentialCheckpoints(localPos);

            if (audioSource != null && tracingSoundClip != null)
            {
                audioSource.clip = tracingSoundClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isTracing || _isCompleted) return;

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out localPos);

            AddDrawnPoint(localPos, false);
            EvaluateSequentialCheckpoints(localPos);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isTracing) return;
            _isTracing = false;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // If finger is lifted before reaching all checkpoints, reset line and return to Checkpoint 0!
            if (!_isCompleted)
            {
                Debug.Log("[LetterTracingComponent] Finger lifted before stroke completion. Resetting back to Checkpoint 0.");
                ResetTracingProgress();
                OnTracingFailedAttempt?.Invoke();
            }
        }

        private void ResetTracingProgress()
        {
            _drawnPoints.Clear();
            _currentCheckpointIndex = 0;

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }

            if (filledLetterImage != null) filledLetterImage.gameObject.SetActive(false);
            if (filledLetterText != null) filledLetterText.gameObject.SetActive(false);

            // Reset back to Checkpoint 0 with Pop-Up Animation!
            HideAllCheckpoints();
            if (_activeCheckpointCount > 0)
            {
                ShowCheckpointPopUp(0);
            }
        }

        private void AddDrawnPoint(Vector2 localPos, bool forceFirstPoint)
        {
            if (!forceFirstPoint && _drawnPoints.Count > 0)
            {
                float dist = Vector2.Distance(localPos, _drawnPoints[_drawnPoints.Count - 1]);
                if (dist < minDistanceBetweenPoints) return;
            }

            _drawnPoints.Add(localPos);

            if (lineRenderer != null)
            {
                int count = lineRenderer.positionCount + 1;
                lineRenderer.positionCount = count;
                Vector3 renderPos = new Vector3(localPos.x, localPos.y, -5f);
                lineRenderer.SetPosition(count - 1, renderPos);
            }
        }

        private void EvaluateSequentialCheckpoints(Vector2 currentLocalPos)
        {
            if (checkpoints == null || _activeCheckpointCount == 0 || _currentCheckpointIndex >= _activeCheckpointCount) return;

            Transform targetCheckpoint = checkpoints[_currentCheckpointIndex];
            if (targetCheckpoint == null)
            {
                _currentCheckpointIndex++;
                CheckCompletion();
                return;
            }

            Vector2 checkpointLocalPos;
            if (targetCheckpoint.parent == _rectTransform)
            {
                RectTransform cpRt = targetCheckpoint as RectTransform;
                checkpointLocalPos = cpRt != null ? cpRt.anchoredPosition : (Vector2)targetCheckpoint.localPosition;
            }
            else
            {
                checkpointLocalPos = _rectTransform.InverseTransformPoint(targetCheckpoint.position);
            }

            float distance = Vector2.Distance(currentLocalPos, checkpointLocalPos);
            if (distance <= checkpointRadius)
            {
                Debug.Log($"[LetterTracingComponent] Checkpoint {_currentCheckpointIndex} reached!");

                // Hide reached checkpoint and startDot
                targetCheckpoint.gameObject.SetActive(false);
                if (_currentCheckpointIndex == 0 && startDot != null)
                {
                    startDot.SetActive(false);
                }

                _currentCheckpointIndex++;

                if (_currentCheckpointIndex < _activeCheckpointCount)
                {
                    // Show next active checkpoint with Pop-Up Animation!
                    ShowCheckpointPopUp(_currentCheckpointIndex);
                }
                else
                {
                    CheckCompletion();
                }
            }
        }

        private void CheckCompletion()
        {
            if (_currentCheckpointIndex >= _activeCheckpointCount && _activeCheckpointCount > 0)
            {
                CompleteTracing();
            }
        }

        private void CompleteTracing()
        {
            _isCompleted = true;
            _isTracing = false;

            HideAllCheckpoints();

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Show full filled letter image or text
            if (filledLetterImage != null && filledLetterImage.sprite != null)
            {
                filledLetterImage.gameObject.SetActive(true);
            }
            if (filledLetterText != null)
            {
                filledLetterText.gameObject.SetActive(true);
            }

            Debug.Log("[LetterTracingComponent] Sequential Checkpoint Tracing Completed!");
            _onCompleteCallback?.Invoke();
            OnTracingCompleted?.Invoke();
        }

        public void PlayGhostFingerGuide()
        {
            // Optional stub for Momo hint audio/dialogue trigger
        }
    }
}
