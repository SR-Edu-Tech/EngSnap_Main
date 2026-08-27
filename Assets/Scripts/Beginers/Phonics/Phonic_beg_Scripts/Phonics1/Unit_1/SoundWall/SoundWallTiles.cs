using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class SoundWallTiles : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Button button;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private SoundWallLetterData currentLetter;
    private SoundWallManager manager;
    private AudioSource audioSource;

    private bool explored;

    public void Setup(SoundWallLetterData data, SoundWallManager wallManager, AudioSource source)
    {
        currentLetter = data;
        manager = wallManager;
        audioSource = source;

        explored = false;

        if (letterText != null && data != null && !string.IsNullOrEmpty(data.letter))
        {
            letterText.text = data.letter;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnTileClicked);
            button.interactable = true;
        }
    }

    private void OnTileClicked()
    {
        if (currentLetter == null) return;
        if (manager != null && manager.IsTransitioning) return;

        // Play button animation
        if (animator != null)
            animator.SetTrigger("Tap");

        // Play letter sound (explicitly non-looping)
        if (audioSource != null && currentLetter.soundClip != null)
        {
            audioSource.Stop();
            audioSource.loop = false; // Prevent repeating audio loop
            audioSource.PlayOneShot(currentLetter.soundClip);
        }

        // Show keyword image & text
        if (manager != null)
        {
            manager.ShowKeyword(currentLetter.keywordImage, currentLetter.keywordWord);

            // Count only the first tap per cycle
            if (!explored)
            {
                explored = true;
                manager.TileExplored(this);
            }
        }
    }

    public void SetInteractable(bool state)
    {
        if (button != null)
        {
            button.interactable = state;
        }
    }

    public void ResetTile()
    {
        explored = false;
    }

    public SoundWallLetterData GetData()
    {
        return currentLetter;
    }
}
