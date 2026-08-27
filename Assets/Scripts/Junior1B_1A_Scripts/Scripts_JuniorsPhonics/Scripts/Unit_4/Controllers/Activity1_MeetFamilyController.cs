using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Activity1_MeetFamilyController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI familyTitleText;
    public TextMeshProUGUI chunkHeaderText;
    public Transform houseCenterSpotlight; // Blue box center of house
    public Transform wordListContainer;    // Right side list container
    public GameObject wordCardPrefab;
    public AudioSource audioSource;

    private WordFamilyData currentFamily;
    private List<GameObject> spawnedCards = new List<GameObject>();

    public System.Action OnActivityComplete;

    private Coroutine familyRoutine;

    private void OnDisable()
    {
        StopAllCoroutines();
        familyRoutine = null;
        if (audioSource != null) audioSource.Stop();
    }

    public void Setup(Unit4LevelData levelData)
    {
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();

        ConfigureLayout();

        if (wordListContainer != null)
        {
            foreach (Transform child in wordListContainer) Destroy(child.gameObject);
        }
        spawnedCards.Clear();

        if (levelData != null && levelData.families != null && levelData.families.Count > 0)
        {
            familyRoutine = StartCoroutine(RunFamiliesRoutine(levelData.families));
        }
    }

    private void ConfigureLayout()
    {
        if (wordListContainer != null)
        {
            HorizontalLayoutGroup hLayout = wordListContainer.GetComponent<HorizontalLayoutGroup>();
            if (hLayout != null)
            {
                DestroyImmediate(hLayout);
            }

            VerticalLayoutGroup vLayout = wordListContainer.GetComponent<VerticalLayoutGroup>();
            if (vLayout == null)
            {
                vLayout = wordListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            if (vLayout != null)
            {
                vLayout.spacing = 15f;
                vLayout.childAlignment = TextAnchor.MiddleCenter;
                vLayout.childControlWidth = false;
                vLayout.childControlHeight = false;
                vLayout.childForceExpandWidth = false;
                vLayout.childForceExpandHeight = false;
            }
        }
    }

    private IEnumerator RunFamiliesRoutine(List<WordFamilyData> families)
    {
        if (families == null) yield break;

        foreach (var family in families)
        {
            if (family == null) continue;
            currentFamily = family;
            string chunkNameStr = !string.IsNullOrEmpty(family.chunkName) ? family.chunkName : "";
            if (familyTitleText != null) familyTitleText.text = $"Meet the {chunkNameStr} Family";
            if (chunkHeaderText != null) chunkHeaderText.text = chunkNameStr;

            // Clear previous cards
            if (wordListContainer != null)
            {
                foreach (Transform child in wordListContainer)
                {
                    if (child != null) Destroy(child.gameObject);
                }
            }
            spawnedCards.Clear();

            // Mascot speaks chunk sound in parallel with first word fly-in
            MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
            if (mascot != null) mascot.PlayHiAnimation();

            if (audioSource != null && family.chunkAudio != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(family.chunkAudio);
            }

            Vector3 spotlightPos = houseCenterSpotlight != null ? 
                houseCenterSpotlight.position : 
                (chunkHeaderText != null ? chunkHeaderText.transform.position + Vector3.down * 140f : Vector3.zero);

            // Reveal family words one by one
            if (family.familyWords != null)
            {
                foreach (var wordData in family.familyWords)
                {
                    if (wordData == null || string.IsNullOrEmpty(wordData.word)) continue;

                    if (wordCardPrefab != null)
                    {
                        GameObject card = null;
                        try
                        {
                            // 1. Instantiate card directly in the House Blue Box Spotlight
                            card = Instantiate(wordCardPrefab, transform);
                            card.transform.position = spotlightPos;
                            card.transform.localScale = Vector3.zero;

                            // Set fixed card size (220 x 65)
                            RectTransform rect = card.GetComponent<RectTransform>();
                            if (rect != null) rect.sizeDelta = new Vector2(220f, 65f);

                            // Format text with highlighted ending chunk (e.g. c<color=#FFDD00>an</color>)
                            TextMeshProUGUI tmp = card.GetComponentInChildren<TextMeshProUGUI>();
                            if (tmp != null)
                            {
                                string wordStr = wordData.word.ToLower();
                                string cleanChunk = chunkNameStr.Replace("-", "").ToLower();

                                if (!string.IsNullOrEmpty(cleanChunk) && wordStr.EndsWith(cleanChunk) && wordStr.Length > cleanChunk.Length)
                                {
                                    string prefix = wordStr.Substring(0, wordStr.Length - cleanChunk.Length);
                                    tmp.text = $"{prefix}<color=#ff0022>{cleanChunk}</color>";
                                }
                                else
                                {
                                    tmp.text = wordStr;
                                }

                                tmp.fontSize = 44;
                                tmp.fontStyle = FontStyles.Bold;
                            }

                            // Set picture image if available on word card
                            Image[] cardImages = card.GetComponentsInChildren<Image>(true);
                            foreach (var childImg in cardImages)
                            {
                                if (childImg != null && childImg.gameObject != card) // Child picture object
                                {
                                    if (wordData.wordPicture != null)
                                    {
                                        childImg.sprite = wordData.wordPicture;
                                        childImg.color = Color.white;
                                        childImg.gameObject.SetActive(true);
                                    }
                                    break;
                                }
                            }

                            Image img = card.GetComponent<Image>();
                            if (img != null) img.color = Color.white; // Preserve original card prefab sprite artwork without green tint

                            // Add Button component for interactive tap to replay audio
                            Button btn = card.GetComponent<Button>();
                            if (btn == null) btn = card.gameObject.AddComponent<Button>();
                            CVCWordData localWord = wordData;
                            btn.onClick.RemoveAllListeners();
                            btn.onClick.AddListener(() => {
                                if (audioSource != null && localWord != null && localWord.fullWordAudio != null)
                                {
                                    audioSource.Stop();
                                    audioSource.PlayOneShot(localWord.fullWordAudio);
                                }
                            });
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning("[Activity1] Error initializing card: " + ex.Message);
                        }

                        // 2. Scale-Pop Appearance directly inside the House Blue Box Spotlight
                        if (card != null)
                        {
                            float elapsed = 0f;
                            float animDuration = 0.25f;
                            while (elapsed < animDuration)
                            {
                                elapsed += Time.deltaTime;
                                float t = elapsed / animDuration;
                                if (card != null)
                                {
                                    card.transform.position = spotlightPos;
                                    card.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.25f, t);
                                }
                                yield return null;
                            }
                            if (card != null)
                            {
                                card.transform.position = spotlightPos;
                                card.transform.localScale = Vector3.one * 1.25f;
                            }
                        }

                        // 3. Phonics Audio Breakdown inside House Spotlight: "c... an... can!"
                        if (audioSource != null)
                        {
                            // First Letter sound: /c/
                            if (wordData.letter1Sound != null)
                            {
                                audioSource.Stop();
                                audioSource.PlayOneShot(wordData.letter1Sound);
                                float waitDuration = Mathf.Clamp(wordData.letter1Sound.length, 0.15f, 2.5f);
                                yield return new WaitForSeconds(waitDuration + 0.1f);
                            }

                            // Chunk sound: /an/
                            if (family.chunkAudio != null)
                            {
                                audioSource.Stop();
                                audioSource.PlayOneShot(family.chunkAudio);
                                float waitDuration = Mathf.Clamp(family.chunkAudio.length, 0.15f, 2.5f);
                                yield return new WaitForSeconds(waitDuration + 0.1f);
                            }

                            // Whole Word: "can!"
                            if (wordData.fullWordAudio != null)
                            {
                                audioSource.Stop();
                                audioSource.PlayOneShot(wordData.fullWordAudio);
                                float waitDuration = Mathf.Clamp(wordData.fullWordAudio.length, 0.2f, 3.0f);
                                yield return new WaitForSeconds(waitDuration + 0.2f);
                            }
                            else
                            {
                                yield return new WaitForSeconds(0.3f);
                            }
                        }
                        else
                        {
                            yield return new WaitForSeconds(0.4f);
                        }

                        // 4. Move Card from House Blue Box into the Right Side List
                        if (card != null && wordListContainer != null)
                        {
                            card.transform.SetParent(wordListContainer, true);
                            
                            LayoutElement layoutElement = card.GetComponent<LayoutElement>();
                            if (layoutElement == null) layoutElement = card.gameObject.AddComponent<LayoutElement>();
                            layoutElement.preferredWidth = 220f;
                            layoutElement.preferredHeight = 65f;
                            layoutElement.minWidth = 220f;
                            layoutElement.minHeight = 65f;

                            card.transform.localScale = Vector3.one;
                            spawnedCards.Add(card);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(1.0f);
        }

        OnActivityComplete?.Invoke();
    }
}
