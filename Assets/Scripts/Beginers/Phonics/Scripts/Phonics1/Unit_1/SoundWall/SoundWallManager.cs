using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundWallManager : MonoBehaviour
{
    [Header("Letters (A-Z)")]
    [SerializeField] private SoundWallLetterData[] letters;

    [Header("Tiles")]
    [SerializeField] private SoundWallTiles[] tiles;

    [Header("Keyword UI")]
    [SerializeField] private Image keywordImage;
    [SerializeField] private TMP_Text keywordText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private int currentLetterIndex = 0;
    private int exploredCount = 0;
    private bool isTransitioning = false;

    public bool IsTransitioning => isTransitioning;

    private void OnEnable()
    {
        RestartSoundWall();
    }

    private void Start()
    {
        // If a SoundWallController is present on the same GameObject, prioritize it!
        SoundWallController controller = GetComponent<SoundWallController>();
        if (controller != null && controller.enabled)
        {
            enabled = false;
            return;
        }

        if (audioSource != null)
        {
            audioSource.loop = false;
        }

        LoadNextFourLetters();
    }

    private void LoadNextFourLetters()
    {
        exploredCount = 0;

        // Clear keyword display for new group
        if (keywordImage != null)
        {
            keywordImage.sprite = null;
            keywordImage.enabled = false;
        }

        if (keywordText != null)
        {
            keywordText.text = "";
        }

        if (tiles == null || letters == null) return;

        int groupSize = tiles.Length;
        for (int i = 0; i < groupSize; i++)
        {
            if (tiles[i] == null) continue;

            int index = currentLetterIndex + i;

            if (index < letters.Length && letters[index] != null)
            {
                tiles[i].gameObject.SetActive(true);
                tiles[i].Setup(letters[index], this, audioSource);
            }
            else
            {
                tiles[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowKeyword(Sprite sprite, string word)
    {
        if (keywordImage != null)
        {
            keywordImage.sprite = sprite;
            keywordImage.enabled = sprite != null;
        }

        if (keywordText != null)
        {
            keywordText.text = word ?? "";
        }
    }

    public void TileExplored(SoundWallTiles tile)
    {
        if (isTransitioning) return;

        exploredCount++;

        int totalLetters = letters != null ? letters.Length : 0;
        int remainingLetters = totalLetters - currentLetterIndex;
        int groupSize = tiles != null ? tiles.Length : 4;
        int lettersInCurrentGroup = Mathf.Min(groupSize, remainingLetters);

        if (exploredCount >= lettersInCurrentGroup)
        {
            StartCoroutine(NextGroupRoutine());
        }
    }

    private IEnumerator NextGroupRoutine()
    {
        isTransitioning = true;
        SetTilesInteractable(false);

        yield return new WaitForSeconds(0.5f);

        // Play rotate animation on tiles
        if (tiles != null)
        {
            foreach (SoundWallTiles tile in tiles)
            {
                if (tile == null || !tile.gameObject.activeSelf) continue;

                Animator anim = tile.GetComponent<Animator>();
                if (anim != null)
                    anim.SetTrigger("Rotate");
            }
        }

        yield return new WaitForSeconds(0.6f);

        int groupSize = tiles != null ? tiles.Length : 4;
        currentLetterIndex += groupSize;

        if (letters == null || currentLetterIndex >= letters.Length)
        {
            isTransitioning = false;
            yield break;
        }

        LoadNextFourLetters();
        SetTilesInteractable(true);
        isTransitioning = false;
    }

    private void SetTilesInteractable(bool state)
    {
        if (tiles == null) return;
        foreach (SoundWallTiles tile in tiles)
        {
            if (tile != null)
            {
                tile.SetInteractable(state);
            }
        }
    }

    public void RestartSoundWall()
    {
        currentLetterIndex = 0;
        isTransitioning = false;
        LoadNextFourLetters();
        SetTilesInteractable(true);
    }
}