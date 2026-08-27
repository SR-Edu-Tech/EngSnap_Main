using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Subclass for Unit 6 (Groove On) Roleplay Lesson One.
/// </summary>
public class Masters_GrooveOn_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;

        // Auto-wire narratorSpeech intro clip if null
        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Roleplay/Roleplay a birthday party conversation with your friend.mp3");
#endif
        }

        // Force populate roleplayTurns with the 4 GDD steps and matching audio files
        if (roleplayTurns == null || roleplayTurns.Length != 4 || roleplayTurns[0] == null || roleplayTurns[0].npcDialogueText != "My birthday is on the 16th of September.") {
            roleplayTurns = new RoleplayTurn[4];

            string friendADir = "Assets/Audio/2A/6_GrooveOn/Roleplay/L01_FriendA/";
            string friendBDir = "Assets/Audio/2A/6_GrooveOn/Roleplay/L01_FriendB/";

            // Step 1:
            roleplayTurns[0] = new RoleplayTurn {
                speakerTitle = "NPC Friend",
                npcDialogueText = "My birthday is on the 16th of September.",
#if UNITY_EDITOR
                npcAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendADir + "My birthday is on the 16th of September.mp3"),
                correctOptionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendBDir + "Oh Its tomorrow Advance birthday wishes.mp3"),
#endif
                studentOptions = new string[] {
                    "Oh! It's tomorrow. Advance birthday wishes!",
                    "Happy Diwali to you!",
                    "Belated birthday wishes!",
                    "We clean the house before a festival."
                },
                correctOptionIndex = 0
            };

            // Step 2:
            roleplayTurns[1] = new RoleplayTurn {
                speakerTitle = "NPC Friend",
                npcDialogueText = "Thank you. I am planning a party tomorrow.",
#if UNITY_EDITOR
                npcAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendADir + "Thank you I am planning a party tomorrow.mp3"),
                correctOptionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendBDir + "Wheres the party.mp3"),
#endif
                studentOptions = new string[] {
                    "Where's the party?",
                    "Happy Eid to your family!",
                    "What about the theme?",
                    "I clean the house everyday."
                },
                correctOptionIndex = 0
            };

            // Step 3:
            roleplayTurns[2] = new RoleplayTurn {
                speakerTitle = "NPC Friend",
                npcDialogueText = "It'll be in my farmhouse.",
#if UNITY_EDITOR
                npcAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendADir + "Itll be in my farmhouse.mp3"),
                correctOptionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendBDir + "What about the theme.mp3"),
#endif
                studentOptions = new string[] {
                    "Greetings of the season!",
                    "What about the theme?",
                    "We decorate the house.",
                    "Best wishes to your family!"
                },
                correctOptionIndex = 1
            };

            // Step 4:
            roleplayTurns[3] = new RoleplayTurn {
                speakerTitle = "NPC Friend",
                npcDialogueText = "Since it's in the farmhouse, I thought Jungle would be fun.",
#if UNITY_EDITOR
                npcAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendADir + "Since its in the farmhouse I thought Jungle would be fun.mp3"),
                correctOptionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(friendBDir + "Have fun See you tomorrow then.mp3"),
#endif
                studentOptions = new string[] {
                    "We prepare delicious food.",
                    "Have fun! See you tomorrow, then!",
                    "Happy New Year!",
                    "Belated wishes!"
                },
                correctOptionIndex = 1
            };
        }
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Roleplay;
        UpdateTitleAndUIComponents();
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Polished") || textVal.Contains("RP01") || textVal.Contains("Birthday") || textVal.Contains("Party")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "RP01 Birthday Party Roleplay";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("ROLEPLAY")) {
                tmp.text = "ROLEPLAY BRANCH (Real Conversation)";
            }
        }
    }

    protected override void LoadNextRoleplay() {
        if (roleplayTurns == null || dialogueIndex >= roleplayTurns.Length) {
            if (npcAndStudentGameObject != null) {
                npcAndStudentGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            }
            if (skipButton != null) skipButton.interactable = false;
            
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (optionsPrompt != null) optionsPrompt.SetActive(false);

            ShowAllCompletedBanner();

            if (nextButton == null) {
                Transform nbTrans = transform.Find("NextButton") ?? transform.Find("Next Button") ?? transform.Find("Next");
                if (nbTrans != null) nextButton = nbTrans.GetComponent<UnityEngine.UI.Button>();
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        base.LoadNextRoleplay();
    }

    private void ShowAllCompletedBanner() {
        TMP_FontAsset fontAsset = null;
        Transform promptTrans = null;
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t != null) {
                if (fontAsset == null && t.font != null) fontAsset = t.font;
                string lowerName = t.name.ToLower();
                string txt = t.text ?? "";
                if (promptTrans == null && (lowerName.Contains("title") || txt.Contains("RP01") || txt.Contains("Birthday") || txt.Contains("Party") || txt.Contains("LESSON"))) {
                    promptTrans = t.transform;
                }
            }
        }

        Transform bannerTrans = transform.Find("AllCompletedBanner");
        GameObject bannerObj;
        if (bannerTrans == null) {
            bannerObj = new GameObject("AllCompletedBanner");
            bannerObj.transform.SetParent(transform, false);
        } else {
            bannerObj = bannerTrans.gameObject;
        }

        bannerObj.SetActive(true);
        bannerObj.transform.SetAsLastSibling();

        RectTransform rect = bannerObj.GetComponent<RectTransform>();
        if (rect == null) rect = bannerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        float yPos = -165f;
        if (promptTrans != null) {
            RectTransform pRect = promptTrans.GetComponent<RectTransform>();
            if (pRect != null) {
                float pBottom = pRect.anchoredPosition.y - (pRect.rect.height * (1f - pRect.pivot.y)) - 35f;
                if (pBottom < -80f && pBottom > -300f) {
                    yPos = pBottom;
                }
            }
        }

        rect.anchoredPosition = new Vector2(0f, yPos);
        rect.sizeDelta = new Vector2(800f, 50f);

        TextMeshProUGUI bannerText = bannerObj.GetComponent<TextMeshProUGUI>();
        if (bannerText == null) bannerText = bannerObj.AddComponent<TextMeshProUGUI>();
        bannerText.enabled = true;
        if (fontAsset != null) bannerText.font = fontAsset;
        bannerText.text = "ALL COMPLETED!";
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.fontSize = 38;
        bannerText.color = new Color(1f, 0.92f, 0.23f, 1f); // Vibrant Gold
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.enableWordWrapping = false;

        bannerObj.transform.localScale = Vector3.zero;
        bannerObj.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
    }
}