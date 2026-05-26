using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleToTalkButton_BB1 : MonoBehaviour
{
    [Header("Icon Sprites")]
    public Sprite idleIcon;
    public Sprite listeningIcon;

    [Header("References")]
    public Image    buttonImage;
    public Animator listeningAnim;
    public string   listeningAnimName = "ListeningAnim";
    public TextMeshProUGUI statusLabel;

    [Header("Labels (optional)")]
    public string idleLabel      = "Tap to speak";
    public string listeningLabel = "Listening...";

    private bool _isListening = false;

    void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
    }

    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnButtonClicked);
        }
        ApplyState(false);
    }

    void OnEnable()
    {
        // Reset toggle state on every re-enable so first tap always starts listening
        _isListening = false;
        ApplyState(false);
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    void OnButtonClicked()
    {
        _isListening = !_isListening;

        if (_isListening)
            CrossPlatformSpeechManager.Instance?.StartListening();
        else
            CrossPlatformSpeechManager.Instance?.StopListening();

        ApplyState(_isListening);
    }

    public void ForceIdle()
    {
        if (!_isListening) return;
        _isListening = false;
        CrossPlatformSpeechManager.Instance?.StopListening();
        ApplyState(false);
    }

    void ApplyState(bool listening)
    {
        if (buttonImage != null)
            buttonImage.sprite = listening ? listeningIcon : idleIcon;

        if (listeningAnim != null)
        {
            if (listening)
            {
                listeningAnim.speed = 1f;
                listeningAnim.Play(listeningAnimName, 0, 0f);
            }
            else
            {
                listeningAnim.speed = 0f;
                listeningAnim.Play(listeningAnimName, 0, 0f);
                listeningAnim.Update(0f);
            }
        }

        if (statusLabel != null)
            statusLabel.text = listening ? listeningLabel : idleLabel;
    }

    void OnSpeechResult(string _)
    {
        _isListening = false;
        ApplyState(false);
    }
}