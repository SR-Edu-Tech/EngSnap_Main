using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Activity1_BlendReadController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI tile1Text;
    public TextMeshProUGUI tile2Text;
    public TextMeshProUGUI tile3Text;
    public RectTransform tile1Transform, tile2Transform, tile3Transform;
    public Image resultPicture;
    public Button blendButton;
    public AudioSource audioSource;

    private List<Vector2> originalPositions = new List<Vector2>();
    private CVCWordData currentWord;
    private int currentWordIndex = 0;
    private Unit3LevelData levelData;

    public System.Action OnActivityComplete;

    private void Awake()
    {
        InitializeOriginalPositions();
    }

    private void InitializeOriginalPositions()
    {
        if (originalPositions.Count == 0)
        {
            if (tile1Transform != null) originalPositions.Add(tile1Transform.anchoredPosition);
            if (tile2Transform != null) originalPositions.Add(tile2Transform.anchoredPosition);
            if (tile3Transform != null) originalPositions.Add(tile3Transform.anchoredPosition);
        }
    }

    public void Setup(Unit3LevelData data)
    {
        levelData = data;
        currentWordIndex = 0;
        InitializeOriginalPositions();

        if (levelData != null && levelData.blendReadWords != null && levelData.blendReadWords.Count > 0)
        {
            LoadWord(levelData.blendReadWords[currentWordIndex]);
        }
        else
        {
            Debug.LogWarning("Activity1_BlendReadController: Level data or blendReadWords list is empty!");
        }
    }

    private void LoadWord(CVCWordData word)
    {
        if (word == null) return;
        currentWord = word;

        if (resultPicture != null) resultPicture.gameObject.SetActive(false);
        if (blendButton != null) blendButton.interactable = true;

        // Reset Tile Positions & Active States
        if (tile1Transform != null) tile1Transform.gameObject.SetActive(true);
        if (tile2Transform != null) tile2Transform.gameObject.SetActive(true);
        if (tile3Transform != null) tile3Transform.gameObject.SetActive(true);

        if (originalPositions.Count >= 3)
        {
            if (tile1Transform != null) tile1Transform.anchoredPosition = originalPositions[0];
            if (tile2Transform != null) tile2Transform.anchoredPosition = originalPositions[1];
            if (tile3Transform != null) tile3Transform.anchoredPosition = originalPositions[2];
        }

        if (tile1Text != null) tile1Text.text = word.Letter1.ToString().ToLower();
        if (tile2Text != null) tile2Text.text = word.Letter2.ToString().ToLower();
        if (tile3Text != null) tile3Text.text = word.Letter3.ToString().ToLower();
    }

    public void OnTileTapped(int tileIndex)
    {
        if (currentWord == null || audioSource == null) return;

        AudioClip clipToPlay = tileIndex switch
        {
            1 => currentWord.letter1Sound,
            2 => currentWord.letter2Sound,
            3 => currentWord.letter3Sound,
            _ => null
        };

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
        else
        {
            Debug.LogWarning($"Activity1_BlendReadController: Letter sound {tileIndex} is null on word asset '{currentWord.name}'!");
        }
    }

    public void OnBlendButtonPressed()
    {
        if (currentWord != null) StartCoroutine(BlendRoutine());
    }

    private IEnumerator BlendRoutine()
    {
        if (blendButton != null) blendButton.interactable = false;

        // Play individual sound tiles in sequence
        if (audioSource != null)
        {
            if (currentWord.letter1Sound != null) audioSource.PlayOneShot(currentWord.letter1Sound);
            yield return new WaitForSeconds(0.4f);
            if (currentWord.letter2Sound != null) audioSource.PlayOneShot(currentWord.letter2Sound);
            yield return new WaitForSeconds(0.4f);
            if (currentWord.letter3Sound != null) audioSource.PlayOneShot(currentWord.letter3Sound);
            yield return new WaitForSeconds(0.5f);
        }

        // Slide tiles together toward middle tile position
        if (originalPositions.Count >= 3 && tile1Transform != null && tile3Transform != null)
        {
            Vector2 targetPos = originalPositions[1];
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tile1Transform.anchoredPosition = Vector2.Lerp(originalPositions[0], targetPos, t);
                tile3Transform.anchoredPosition = Vector2.Lerp(originalPositions[2], targetPos, t);
                yield return null;
            }
        }

        // Merge text into single tile: hide side tiles and show full word on middle tile
        if (tile1Transform != null) tile1Transform.gameObject.SetActive(false);
        if (tile3Transform != null) tile3Transform.gameObject.SetActive(false);
        if (tile2Text != null) tile2Text.text = currentWord.word.ToLower(); // Shows full word e.g. "cat"

        // Play full word audio and reveal image
        if (audioSource != null && currentWord.fullWordAudio != null)
            audioSource.PlayOneShot(currentWord.fullWordAudio);

        if (resultPicture != null && currentWord.wordPicture != null)
        {
            resultPicture.sprite = currentWord.wordPicture;
            resultPicture.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(1.8f);

        // Next word or complete activity
        currentWordIndex++;
        if (levelData != null && currentWordIndex < levelData.blendReadWords.Count)
        {
            LoadWord(levelData.blendReadWords[currentWordIndex]);
        }
        else
        {
            OnActivityComplete?.Invoke();
        }
    }
}