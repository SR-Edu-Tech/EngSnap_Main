using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class AndRowData
{
    [Header("Row Audio Elements")]
    public AudioClip leftClip;
    public AudioClip centreClip;
    public AudioClip rightClip;

    [HideInInspector] public bool leftClicked = false;
    [HideInInspector] public bool centreClicked = false;
    [HideInInspector] public bool rightClicked = false;
    [HideInInspector] public bool isRowComplete = false;
}

public class U11_R01_Junior1A : MonoBehaviour
{
    [Header("Audio Components")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;

    [Header("Data Matrix (12 Elements)")]
    [SerializeField] private List<AndRowData> _rows = new List<AndRowData>();

    [Header("Hierarchy Targets")]
    [SerializeField] private Transform _buttonsParent; 
    [SerializeField] private GameObject nextWaveButton;

    [Header("Score Configuration")]
    [SerializeField] private TextMeshProUGUI scoreText; 

    [Header("Layout Tuning")]
    [Tooltip("Target horizontal scale factor to assign across sub-button elements dynamically.")]
    [SerializeField] private float _buttonScaleX = 0.29f;

    private int _currentCardIndex = 0;
    private bool _isViewed = false;
    private bool _isSlowed = false;
    private int _completedRowsCount = 0;

    private Coroutine _coroutine;

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        if (nextWaveButton != null) nextWaveButton.SetActive(false);
        _completedRowsCount = 0;
        _isViewed = false;

        UpdateScoreUI();

        // 1. Reset backend runtime tracking data
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].leftClicked = false;
            _rows[i].centreClicked = false;
            _rows[i].rightClicked = false;
            _rows[i].isRowComplete = false;
        }

        // 2. Uniform layout adjustment and setting colors on reset
        if (_buttonsParent != null)
        {
            Color centerDefaultColor = Color.white;
            ColorUtility.TryParseHtmlString("#FFE670", out centerDefaultColor);

            foreach (Transform row in _buttonsParent)
            {
                // Fixed: Explicit loop based strictly on direct child transforms hierarchy index
                // This ensures index 0, 1, and 2 align exactly to Left, Middle, and Right layout columns.
                int childCount = row.childCount;
                for (int childIndex = 0; childIndex < childCount; childIndex++)
                {
                    Transform childButton = row.GetChild(childIndex);
                    
                    // Assign scale cleanly to direct child elements
                    childButton.localScale = new Vector3(_buttonScaleX, 1f, 1f);

                    if (childButton.TryGetComponent(out Button btn))
                    {
                        btn.interactable = true;
                    }

                    if (childButton.TryGetComponent(out Image img))
                    {
                        // Index 1 remains your specific gold/yellow middle accent
                        img.color = (childIndex == 1) ? centerDefaultColor : Color.white;
                    }
                }
            }
        }

        // 3. Start playing the Intro Vocals immediately
        float introDuration = 0f;
        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            introDuration = _introClip.length;
        }

        // 4. Spawning Animation
        float totalSpawnTime = 0f;
        if (_buttonsParent != null)
        {
            foreach (Transform row in _buttonsParent)
            {
                row.gameObject.SetActive(true);
                if (row.TryGetComponent(out PopEffect_Junior1A pop)) pop.enabled = true;
                yield return new WaitForSeconds(0.12f);
                totalSpawnTime += 0.12f;
            }
        }

        // 5. Fixed safety clock check
        float remainingIntroTime = introDuration - totalSpawnTime;
        if (remainingIntroTime > 0f)
        {
            yield return new WaitForSeconds(remainingIntroTime);
        }

        _isViewed = true;
    }

    public void PlayLeftSegment(int rowIndex)
    {
        if (!_isViewed || rowIndex >= _rows.Count || _rows[rowIndex].isRowComplete) return;

        SetSubButtonColor(rowIndex, 0, Color.gray);
        SetSubButtonInteractable(rowIndex, 0, false);

        if (_coroutine != null) StopCoroutine(_coroutine);
        _currentCardIndex = rowIndex;

        _rows[rowIndex].leftClicked = true;
        _coroutine = StartCoroutine(StartButtonAudio(_rows[rowIndex].leftClip));
    }

    public void PlayCentreSegment(int rowIndex)
    {
        if (!_isViewed || rowIndex >= _rows.Count || _rows[rowIndex].isRowComplete) return;

        SetSubButtonColor(rowIndex, 1, Color.gray);
        SetSubButtonInteractable(rowIndex, 1, false);

        if (_coroutine != null) StopCoroutine(_coroutine);
        _currentCardIndex = rowIndex;

        _rows[rowIndex].centreClicked = true;
        _coroutine = StartCoroutine(StartButtonAudio(_rows[rowIndex].centreClip));
    }

    public void PlayRightSegment(int rowIndex)
    {
        if (!_isViewed || rowIndex >= _rows.Count || _rows[rowIndex].isRowComplete) return;

        SetSubButtonColor(rowIndex, 2, Color.gray);
        SetSubButtonInteractable(rowIndex, 2, false);

        if (_coroutine != null) StopCoroutine(_coroutine);
        _currentCardIndex = rowIndex;

        _rows[rowIndex].rightClicked = true;
        _coroutine = StartCoroutine(StartButtonAudio(_rows[rowIndex].rightClip));
    }

    private void SetSubButtonInteractable(int rowIndex, int childIndex, bool state)
    {
        if (_buttonsParent != null && rowIndex < _buttonsParent.childCount)
        {
            Transform rowTrans = _buttonsParent.GetChild(rowIndex);
            if (childIndex < rowTrans.childCount)
            {
                if (rowTrans.GetChild(childIndex).TryGetComponent(out Button btn))
                {
                    btn.interactable = state;
                }
            }
        }
    }

    private void SetSubButtonColor(int rowIndex, int childIndex, Color colorTarget)
    {
        if (_buttonsParent != null && rowIndex < _buttonsParent.childCount)
        {
            Transform rowTrans = _buttonsParent.GetChild(rowIndex);
            if (childIndex < rowTrans.childCount)
            {
                if (rowTrans.GetChild(childIndex).TryGetComponent(out Image img))
                {
                    img.color = colorTarget;
                }
            }
        }
    }

    private IEnumerator StartButtonAudio(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.clip = clip;
            _audioSource.Play();

            float pitchVal = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float duration = clip.length / pitchVal;
            yield return new WaitForSeconds(duration);
        }

        CheckAndValidateRowCompletion(_currentCardIndex);
    }

    private void CheckAndValidateRowCompletion(int rowIndex)
    {
        if (rowIndex >= _rows.Count) return;

        AndRowData row = _rows[rowIndex];

        if (row.leftClicked && row.centreClicked && row.rightClicked && !row.isRowComplete)
        {
            row.isRowComplete = true;
            _completedRowsCount++;

            UpdateScoreUI();

            if (_buttonsParent != null && rowIndex < _buttonsParent.childCount)
            {
                Transform completedRow = _buttonsParent.GetChild(rowIndex);
                foreach (Transform childButton in completedRow)
                {
                    if (childButton.TryGetComponent(out Button btn))
                    {
                        btn.interactable = false;
                    }
                }
            }

            if (_completedRowsCount >= _rows.Count)
            {
                if (nextWaveButton != null) nextWaveButton.SetActive(true);
                if (GameManager_Junior1A.Instance != null) GameManager_Junior1A.Instance.Next(true);
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + _completedRowsCount + " / " + _rows.Count;
        }
    }

    public void Slow(TextMeshProUGUI text)
    {
        if (text != null) text.text = _isSlowed ? "    SLOW" : "    FAST";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }

    public void LoadNextWaveScene()
    {
        SceneManager.LoadScene("S05");
    }
}