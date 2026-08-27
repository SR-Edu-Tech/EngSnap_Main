using System;
using System.Collections;
using System.Collections.Generic;
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
    /// </summary>
    public class LetterTracingComponent : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image outlineImage;        // Dotted outline image (PNG)
        [SerializeField] private Image filledLetterImage;   // Filled letter image (hidden at first)
        [SerializeField] private GameObject startDot;       // Start dot sprite GameObject (at Checkpoint 0)
        [SerializeField] private LineRenderer lineRenderer;  // LineRenderer to draw child's finger path

        [Header("Checkpoint Tracing Setup")]
        [Tooltip("Place 5 Checkpoint GameObjects/Transforms along the stroke path in exact sequential order (0 -> 1 -> 2 -> 3 -> 4).")]
        [SerializeField] private Transform[] checkpoints = new Transform[5];

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

        private RectTransform _rectTransform;
        private List<Vector2> _drawnPoints = new List<Vector2>();
        private int _currentCheckpointIndex = 0;
        private bool _isTracing = false;
        private bool _isCompleted = false;
        private Coroutine _checkpointPopUpCoroutine;

        public int CurrentCheckpointIndex => _currentCheckpointIndex;
        public int TotalCheckpoints => checkpoints != null ? checkpoints.Length : 0;
        public float CheckpointProgress => TotalCheckpoints > 0 ? (float)_currentCheckpointIndex / TotalCheckpoints : 0f;

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

            if (outlineImage != null && !outlineImage.raycastTarget)
            {
                outlineImage.raycastTarget = true;
            }

            Image selfImg = GetComponent<Image>();
            if (selfImg != null && !selfImg.raycastTarget)
            {
                selfImg.raycastTarget = true;
            }
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

        public void SetupTracing(Sprite outlineSprite, AudioClip soundClip, char missingLetter = 'a', Sprite filledSprite = null, Vector2[] itemCheckpoints = null)
        {
            _isCompleted = false;
            _isTracing = false;
            _currentCheckpointIndex = 0;
            tracingSoundClip = soundClip;

            _drawnPoints.Clear();

            EnsureLineRenderer();
            if (lineRenderer != null) lineRenderer.positionCount = 0;

            // Apply custom 5 checkpoint positions from ScriptableObject item data if non-zero positions exist
            if (itemCheckpoints != null && itemCheckpoints.Length > 0 && checkpoints != null)
            {
                bool hasNonZeroPositions = false;
                for (int i = 0; i < itemCheckpoints.Length; i++)
                {
                    if (itemCheckpoints[i] != Vector2.zero)
                    {
                        hasNonZeroPositions = true;
                        break;
                    }
                }

                if (hasNonZeroPositions)
                {
                    int count = Mathf.Min(itemCheckpoints.Length, checkpoints.Length);
                    for (int i = 0; i < count; i++)
                    {
                        if (checkpoints[i] != null)
                        {
                            RectTransform rt = checkpoints[i] as RectTransform;
                            if (rt != null) rt.anchoredPosition = itemCheckpoints[i];
                            else checkpoints[i].localPosition = itemCheckpoints[i];
                        }
                    }
                }
            }

            // Dotted outline visible
            if (outlineImage != null)
            {
                outlineImage.gameObject.SetActive(true);
                if (outlineSprite != null) outlineImage.sprite = outlineSprite;
            }

            // Filled letter image hidden at first
            if (filledLetterImage != null)
            {
                if (filledSprite != null) filledLetterImage.sprite = filledSprite;
                else if (outlineSprite != null) filledLetterImage.sprite = outlineSprite;
                filledLetterImage.gameObject.SetActive(false);
            }

            // Hide all checkpoints initially, then pop up Checkpoint 0
            HideAllCheckpoints();
            ShowCheckpointPopUp(0);
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
            if (checkpoints != null && index >= 0 && index < checkpoints.Length && checkpoints[index] != null)
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

            // Reset back to Checkpoint 0 with Pop-Up Animation!
            HideAllCheckpoints();
            ShowCheckpointPopUp(0);
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
            if (checkpoints == null || checkpoints.Length == 0 || _currentCheckpointIndex >= checkpoints.Length) return;

            Transform targetCheckpoint = checkpoints[_currentCheckpointIndex];
            if (targetCheckpoint == null)
            {
                _currentCheckpointIndex++;
                CheckCompletion();
                return;
            }

            Vector2 checkpointLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, targetCheckpoint.position, null, out checkpointLocalPos);

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

                if (_currentCheckpointIndex < checkpoints.Length)
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
            if (checkpoints != null && _currentCheckpointIndex >= checkpoints.Length)
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

            // Show full filled letter image
            if (filledLetterImage != null)
            {
                filledLetterImage.gameObject.SetActive(true);
            }

            Debug.Log("[LetterTracingComponent] Sequential Checkpoint Tracing Completed!");
            OnTracingCompleted?.Invoke();
        }

        public void PlayGhostFingerGuide()
        {
            // Optional stub for Momo hint audio/dialogue trigger
        }
    }
}
