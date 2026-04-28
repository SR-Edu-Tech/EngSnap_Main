using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ButtonClickSound.cs
/// Attach directly to any Button GameObject.
/// All buttons share a single static AudioSource (created once, reused everywhere).
/// Plays the assigned AudioClip whenever the button is clicked.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    [Header("Sound")]
    public AudioClip clickSound;

    [Range(0f, 1f)]
    public float volume = 1f;

    private Button _button;

    // One AudioSource shared across every ButtonClickSound in the scene
    private static AudioSource _sharedSource;

    void Awake()
    {
        _button = GetComponent<Button>();

        if (_sharedSource == null)
        {
            var go = new GameObject("ButtonClickSound_SharedAudioSource");
            DontDestroyOnLoad(go);
            _sharedSource = go.AddComponent<AudioSource>();
            _sharedSource.playOnAwake = false;
        }
    }

    void OnEnable()  => _button.onClick.AddListener(PlaySound);
    void OnDisable() => _button.onClick.RemoveListener(PlaySound);

    void PlaySound()
    {
        if (clickSound != null)
            _sharedSource.PlayOneShot(clickSound, volume);
    }
}