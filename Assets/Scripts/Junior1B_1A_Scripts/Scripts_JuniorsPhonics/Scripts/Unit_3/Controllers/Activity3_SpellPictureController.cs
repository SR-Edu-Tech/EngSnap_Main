using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Activity3_SpellPictureController : MonoBehaviour
{
    [Header("UI References")]
    public Image targetPicture;
    public Transform boxContainer; // 3 Drop Boxes container
    public Transform trayContainer; // Tray tiles container
    public GameObject dropBoxPrefab;
    public GameObject letterTilePrefab;
    public AudioSource audioSource;
    public AudioClip chimeSFX;
    public AudioClip tryAgainSFX;

    private List<CVCWordData> wordList;
    private int currentWordIndex = 0;
    private CVCWordData currentWord;
    private List<TextMeshProUGUI> boxTexts = new List<TextMeshProUGUI>();
    private List<SpellPictureDragTile> spawnedTiles = new List<SpellPictureDragTile>();
    private int wrongAttempts = 0;

    public System.Action OnActivityComplete;

    public void Setup(Unit3LevelData levelData)
    {
        wordList = levelData.spellPictureWords;
        currentWordIndex = 0;
        LoadWord();
    }

    private void LoadWord()
    {
        wrongAttempts = 0;
        currentWord = wordList[currentWordIndex];
        if (targetPicture != null) targetPicture.sprite = currentWord.wordPicture;

        // Ensure boxContainer has HorizontalLayoutGroup with proper spacing
        if (boxContainer != null)
        {
            boxContainer.SetAsLastSibling();
            HorizontalLayoutGroup boxLayout = boxContainer.GetComponent<HorizontalLayoutGroup>();
            if (boxLayout == null) boxLayout = boxContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

            boxLayout.spacing = 30f;
            boxLayout.childAlignment = TextAnchor.MiddleCenter;
            boxLayout.childControlWidth = true;
            boxLayout.childControlHeight = true;
            boxLayout.childForceExpandWidth = false;
            boxLayout.childForceExpandHeight = false;

            foreach (Transform child in boxContainer) Destroy(child.gameObject);
        }

        // Ensure trayContainer has HorizontalLayoutGroup with proper spacing
        if (trayContainer != null)
        {
            trayContainer.SetAsLastSibling();
            HorizontalLayoutGroup trayLayout = trayContainer.GetComponent<HorizontalLayoutGroup>();
            if (trayLayout == null) trayLayout = trayContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

            trayLayout.spacing = 25f;
            trayLayout.childAlignment = TextAnchor.MiddleCenter;
            trayLayout.childControlWidth = true;
            trayLayout.childControlHeight = true;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = false;

            foreach (Transform child in trayContainer) Destroy(child.gameObject);
        }

        boxTexts.Clear();
        spawnedTiles.Clear();

        // Create 3 Empty Drop Boxes side-by-side with guaranteed layout & raycasts
        if (boxContainer != null && dropBoxPrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject box = Instantiate(dropBoxPrefab, boxContainer);
                box.transform.localScale = Vector3.one;
                box.transform.SetAsLastSibling();

                RectTransform boxRect = box.GetComponent<RectTransform>();
                if (boxRect != null) boxRect.sizeDelta = new Vector2(110f, 110f);

                LayoutElement le = box.GetComponent<LayoutElement>();
                if (le == null) le = box.AddComponent<LayoutElement>();
                le.preferredWidth = 110f;
                le.preferredHeight = 110f;

                // Dynamic Guard: Auto-attach SpellPictureDropBox if missing
                SpellPictureDropBox dropBoxComp = box.GetComponent<SpellPictureDropBox>();
                if (dropBoxComp == null) dropBoxComp = box.AddComponent<SpellPictureDropBox>();

                // Dynamic Guard: Ensure Image receives raycasts
                Image boxImg = box.GetComponent<Image>();
                if (boxImg == null) boxImg = box.AddComponent<Image>();
                boxImg.raycastTarget = true;

                // Ensure child text does not block raycasts
                TextMeshProUGUI tmp = box.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "";
                    tmp.fontSize = 76; // Match card text size
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.raycastTarget = false;
                    boxTexts.Add(tmp);
                }
            }
        }

        // Generate scrambled tray tiles (Target letters ONLY)
        if (trayContainer != null && letterTilePrefab != null && currentWord != null)
        {
            List<char> trayLetters = new List<char> { currentWord.Letter1, currentWord.Letter2, currentWord.Letter3 };
            for (int i = 0; i < trayLetters.Count; i++)
            {
                char temp = trayLetters[i];
                int rand = Random.Range(i, trayLetters.Count);
                trayLetters[i] = trayLetters[rand];
                trayLetters[rand] = temp;
            }

            foreach (char c in trayLetters)
            {
                GameObject tileObj = Instantiate(letterTilePrefab, trayContainer);
                tileObj.transform.localScale = Vector3.one;
                tileObj.transform.SetAsLastSibling();

                RectTransform tileRect = tileObj.GetComponent<RectTransform>();
                if (tileRect != null) tileRect.sizeDelta = new Vector2(110f, 110f);

                LayoutElement le = tileObj.GetComponent<LayoutElement>();
                if (le == null) le = tileObj.AddComponent<LayoutElement>();
                le.preferredWidth = 110f;
                le.preferredHeight = 110f;

                Image tileImg = tileObj.GetComponent<Image>();
                if (tileImg != null) tileImg.raycastTarget = true;

                SpellPictureDragTile dragTile = tileObj.GetComponent<SpellPictureDragTile>();
                if (dragTile == null) dragTile = tileObj.AddComponent<SpellPictureDragTile>();

                if (dragTile != null)
                {
                    if (dragTile.letterText != null)
                    {
                        dragTile.letterText.text = c.ToString().ToLower();
                        dragTile.letterText.fontSize = 76;
                        dragTile.letterText.fontStyle = FontStyles.Bold;
                        dragTile.letterText.alignment = TextAlignmentOptions.Center;
                        dragTile.letterText.raycastTarget = false;
                    }
                    spawnedTiles.Add(dragTile);
                }
            }
        }
    }

    public void CheckSpelling()
    {
        // Check if all 3 drop boxes are filled
        int filledCount = 0;
        string result = "";
        foreach (var tmp in boxTexts)
        {
            if (!string.IsNullOrEmpty(tmp.text))
            {
                filledCount++;
                result += tmp.text;
            }
        }

        if (filledCount < 3) return; // Wait until all 3 boxes are filled

        if (result.ToLower() == currentWord.word.ToLower())
        {
            // Start sequential audio playback & celebration!
            StartCoroutine(SuccessSequenceRoutine());
        }
        else
        {
            // Incorrect Spelling -> Revert tiles to tray
            if (audioSource != null && tryAgainSFX != null)
            {
                audioSource.PlayOneShot(tryAgainSFX);
            }
            wrongAttempts++;
            StartCoroutine(RevertTilesRoutine());
        }
    }

    private IEnumerator SuccessSequenceRoutine()
    {
        // Turn all letter cards & drop boxes to vibrant Emerald Green
        Color emeraldGreen = new Color(0.2f, 0.85f, 0.3f, 1f);

        foreach (var tile in spawnedTiles)
        {
            if (tile != null)
            {
                Image img = tile.GetComponent<Image>();
                if (img == null) img = tile.GetComponentInChildren<Image>(true);
                if (img != null) img.color = emeraldGreen;
                tile.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            }
        }

        if (boxContainer != null)
        {
            foreach (Transform child in boxContainer)
            {
                if (child != null)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null) img.color = emeraldGreen;
                }
            }
        }

        // 1. Play Great Job / Chime audio FIRST
        float delay = 0.8f;
        if (audioSource != null && chimeSFX != null)
        {
            audioSource.PlayOneShot(chimeSFX);
            delay = chimeSFX.length; // Wait for exact duration of chime clip
        }

        yield return new WaitForSeconds(delay);

        // 2. Play Picture Name audio ("tap!") SECOND
        if (audioSource != null && currentWord != null && currentWord.fullWordAudio != null)
        {
            audioSource.PlayOneShot(currentWord.fullWordAudio);
        }

        yield return new WaitForSeconds(1.5f);

        // 3. Move to next word or complete activity
        currentWordIndex++;
        if (currentWordIndex < wordList.Count)
        {
            LoadWord();
        }
        else
        {
            OnActivityComplete?.Invoke();
        }
    }

    private IEnumerator RevertTilesRoutine()
    {
        yield return new WaitForSeconds(0.4f);

        // Reset box texts
        foreach (var tmp in boxTexts)
        {
            tmp.text = "";
        }

        // Return all letter tiles back to tray
        foreach (var tile in spawnedTiles)
        {
            if (tile != null) tile.ReturnToTray();
        }
    }

    public void OnMascotTapped()
    {
        if (currentWord != null && audioSource != null && currentWord.fullWordAudio != null)
        {
            audioSource.PlayOneShot(currentWord.fullWordAudio);
        }
    }
}