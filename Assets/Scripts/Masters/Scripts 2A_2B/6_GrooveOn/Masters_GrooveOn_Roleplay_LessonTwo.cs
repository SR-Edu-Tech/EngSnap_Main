using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Controller for Unit 6 (Groove On) Roleplay Lesson Two:
/// RP02 Free Scene — Celebrate Your Festival.
/// Features 3 GDD celebration scene cards:
/// Card A — Guests arrive for a festival
/// Card B — Shopping before a festival
/// Card C — Any festival your family celebrates
/// Evaluates Turn 1 festival greeting and Turn 2 family preparations.
/// Includes back button protection, robust UI layouting, and clean scene flow.
/// </summary>
public class Masters_GrooveOn_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
        EnsureBackButtonActive();
        CleanOrphanedSubMeshes();
        InitGDDScenes();
        AutoWireUIElements();
        WireSceneSelectionButtons();
        UpdateTitleAndUIComponents();
        ConfigureSceneButtonLabels();
        ConfigureWordBankLayout();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Roleplay;
        EnsureBackButtonActive();
        UpdateTitleAndUIComponents();
        ConfigureSceneButtonLabels();
        ConfigureWordBankLayout();

        // Auto-wire narratorSpeech intro clip if null
        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Roleplay/Roleplay a festival greeting conversation with your neighbor.mp3");
#endif
        }
    }

    protected virtual void OnEnable() {
        if (Application.isPlaying) {
            CleanOrphanedSubMeshes();
        }
        EnsureBackButtonActive();
        ConfigureSceneButtonLabels();
        ConfigureWordBankLayout();
    }

    private void EnsureBackButtonActive() {
        GameObject backBtnObj = GameObject.Find("BackButton");
        if (backBtnObj == null) {
            Transform t = transform.Find("BackButton") ?? transform.Find("Header/BackButton") ?? transform.Find("Canvas/BackButton") ?? transform.Find("TopBar/BackButton");
            if (t != null) backBtnObj = t.gameObject;
        }

        if (backBtnObj != null) {
            backBtnObj.SetActive(true);
            Image backImg = backBtnObj.GetComponent<Image>();
            if (backImg != null) {
                backImg.enabled = true;
                backImg.raycastTarget = true;
                backImg.color = Color.white;
            }

            Button b = backBtnObj.GetComponent<Button>();
            if (b == null) b = backBtnObj.AddComponent<Button>();
            b.interactable = true;

            Masters_BackButton mbb = backBtnObj.GetComponent<Masters_BackButton>();
            if (mbb == null) mbb = backBtnObj.AddComponent<Masters_BackButton>();

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => {
                Debug.Log("[RP02] Back button clicked -> Loading Hub");
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                    Masters_AudioManager.Instance.StopVoiceOver();
                }
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnBackButtonClicked();
                }
            });
        }
    }

    private void InitGDDScenes() {
        scenes = new SceneData[3];
        string friendBDir = "Assets/Audio/2A/6_GrooveOn/Roleplay/L02_FriendB/";

        // Card A: Guests arrive for a festival
        scenes[0] = CreateGDDCard(
            "Guests arrive for a festival:",
            new string[] { "Wish", "you", "a", "Happy", "Diwali!" },
            new string[] { "Wish you a Happy Diwali!", "Wish you a Happy Diwali" },
            friendBDir + "Wish you a Happy Diwali.mp3",
            "What does your family do to prepare?",
            new string[] { "We", "cleaned", "the", "house", "and", "decorated", "the", "house.", "Please", "have", "some", "sweets." },
            new string[] { "We cleaned the house and decorated the house. Please have some sweets.", "We cleaned the house and decorated the house", "We cleaned the house and decorated it", "Please have some sweets" },
            friendBDir + "We cleaned the house and decorated it.mp3"
        );

        // Card B: Shopping before a festival
        scenes[1] = CreateGDDCard(
            "Shopping before a festival:",
            new string[] { "Happy", "New", "Year!" },
            new string[] { "Happy New Year!", "Happy New Year" },
            friendBDir + "Happy New Year.mp3",
            "What preparations do you do before the festival?",
            new string[] { "We", "do", "shopping", "for", "new", "clothes", "and", "make", "delicious", "food." },
            new string[] { "We do shopping for new clothes and make delicious food.", "We do shopping for new clothes", "make delicious food" },
            friendBDir + "We do shopping for new clothes and make delicious food.mp3"
        );

        // Card C: Any festival your family celebrates
        scenes[2] = CreateGDDCard(
            "Any festival your family celebrates:",
            new string[] { "Eid", "Mubarak!", "Wish", "you", "a", "Happy", "Diwali!", "Merry", "Christmas!" },
            new string[] { "Eid Mubarak!", "Wish you a Happy Diwali!", "Merry Christmas to you!", "Happy New Year!", "Happy Easter to you!", "Happy Eid!" },
            friendBDir + "Happy Eid Have a wonderful celebration with your family.mp3",
            "Name two preparations your family does:",
            new string[] { "We", "clean", "the", "house", "and", "do", "shopping", "for", "new", "clothes." },
            new string[] { "We clean the house and do shopping for new clothes.", "We clean the house and decorate the house", "Clean the house", "Do shopping for new clothes" },
            friendBDir + "We clean the house and decorate the house.mp3"
        );
    }

    private SceneData CreateGDDCard(string t1Prompt, string[] t1Wb, string[] t1Vs, string t1Audio, string t2Prompt, string[] t2Wb, string[] t2Vs, string t2Audio) {
        SceneData sData = new SceneData();
        sData.turns = new RoleplayTurn[4];

        // Turn 0: NPCTurn 1 (LEO / Friend prompt)
        sData.turns[0] = new RoleplayTurn {
            turnType = TurnType.NPCTurn,
            npcDialogueText = t1Prompt
        };

        // Turn 1: PlayerTurn 1 (Festival Greeting)
        sData.turns[1] = new RoleplayTurn {
            turnType = TurnType.PlayerTurn,
            wordBank = t1Wb,
            validSentences = t1Vs,
#if UNITY_EDITOR
            playerCorrectAudioClips = new AudioClip[] { UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(t1Audio) }
#endif
        };

        // Turn 2: NPCTurn 2 (Preparation Prompt)
        sData.turns[2] = new RoleplayTurn {
            turnType = TurnType.NPCTurn,
            npcDialogueText = t2Prompt
        };

        // Turn 3: PlayerTurn 2 (Preparations)
        sData.turns[3] = new RoleplayTurn {
            turnType = TurnType.PlayerTurn,
            wordBank = t2Wb,
            validSentences = t2Vs,
#if UNITY_EDITOR
            playerCorrectAudioClips = new AudioClip[] { UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(t2Audio) }
#endif
        };

        return sData;
    }

    private void AutoWireUIElements() {
        if (roleplayPanel == null) {
            Transform t = transform.Find("RoleplayPanel") ?? transform.Find("Roleplay") ?? transform.Find("Panel") ?? FindChildRecursiveGrooveOn(transform, "RoleplayPanel");
            if (t != null) roleplayPanel = t.gameObject;
        }
        if (sceneSelectionPanel == null) {
            Transform t = transform.Find("Scenes") ?? transform.Find("SceneSelectionPanel") ?? FindChildRecursiveGrooveOn(transform, "Scenes") ?? FindChildRecursiveGrooveOn(transform, "SceneSelectionPanel");
            if (t != null) sceneSelectionPanel = t.gameObject;
        }
        if (playerWritingContainer == null) {
            Transform t = transform.Find("PlayerWritingContainer") ?? FindChildRecursiveGrooveOn(transform, "PlayerWritingContainer");
            if (t != null) playerWritingContainer = t.gameObject;
        }
        if (buttonsParentTransform == null) {
            Transform t = transform.Find("buttonsParentTransform") ?? FindChildRecursiveGrooveOn(transform, "buttonsParentTransform");
            if (t != null) buttonsParentTransform = t;
        }
        if (slateWordsParentTransform == null) {
            Transform t = transform.Find("slateWordsParentTransform") ?? FindChildRecursiveGrooveOn(transform, "slateWordsParentTransform");
            if (t != null) slateWordsParentTransform = t;
        }
        if (wordButtonReference == null) {
#if UNITY_EDITOR
            wordButtonReference = UnityEditor.AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/UI/Wordbutton letschoose.prefab");
            if (wordButtonReference == null) wordButtonReference = UnityEditor.AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/UI/Wordbutton chatting bees Variant.prefab");
#endif
        }
        if (checkButton == null) {
            Transform t = transform.Find("checkButton") ?? FindChildRecursiveGrooveOn(transform, "checkButton") ?? FindChildRecursiveGrooveOn(transform, "CheckButton");
            if (t != null) checkButton = t.GetComponent<Button>();
        }
        if (undoButton == null) {
            Transform t = transform.Find("undoButton") ?? FindChildRecursiveGrooveOn(transform, "undoButton") ?? FindChildRecursiveGrooveOn(transform, "UndoButton");
            if (t != null) undoButton = t.GetComponent<Button>();
        }
        if (retryButton == null) {
            Transform t = transform.Find("retryButton") ?? FindChildRecursiveGrooveOn(transform, "retryButton") ?? FindChildRecursiveGrooveOn(transform, "RetryButton");
            if (t != null) retryButton = t.GetComponent<Button>();
        }
    }

    private void WireSceneSelectionButtons() {
        if (sceneSelectionPanel == null) {
            Transform t = transform.Find("Scenes") ?? transform.Find("SceneSelectionPanel") ?? FindChildRecursiveGrooveOn(transform, "Scenes") ?? FindChildRecursiveGrooveOn(transform, "SceneSelectionPanel");
            if (t != null) sceneSelectionPanel = t.gameObject;
        }

        if (sceneSelectionPanel != null) {
            sceneSelectionPanel.SetActive(true);
            Button[] selBtns = sceneSelectionPanel.GetComponentsInChildren<Button>(true);

            string[] cardLabels = new string[] {
                "Card A: Guests Arrive",
                "Card B: Shopping Time",
                "Card C: Festival Celebration"
            };

            Color[] cardColors = new Color[] {
                new Color(0.12f, 0.25f, 0.48f, 1f), // Royal Blue
                new Color(0.08f, 0.45f, 0.35f, 1f), // Emerald Green
                new Color(0.48f, 0.25f, 0.12f, 1f)  // Warm Amber
            };

            if (scenes != null) {
                for (int i = 0; i < scenes.Length && i < selBtns.Length; i++) {
                    Button btn = selBtns[i];
                    if (btn == null) continue;
                    btn.gameObject.SetActive(true);

                    Image img = btn.GetComponent<Image>();
                    if (img == null) img = btn.gameObject.AddComponent<Image>();
                    img.enabled = true;
                    img.raycastTarget = true;
                    img.color = cardColors[i % cardColors.Length];

                    TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null) {
                        tmp.gameObject.SetActive(true);
                        tmp.enabled = true;
                        tmp.text = cardLabels[i % cardLabels.Length];
                        tmp.color = Color.white;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.alignment = TextAlignmentOptions.Center;
                    }

                    scenes[i].sceneButton = btn;
                    SceneData sData = scenes[i];
                    int cardIdx = i;
                    RectTransform btnRect = btn.GetComponent<RectTransform>();

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        Debug.Log($"[RP02] Card {cardIdx + 1} button clicked!");
                        OnSceneButtonClicked(btnRect, sData);
                    });
                }
            }
        }
    }

    private void CleanOrphanedSubMeshes() {
        TMP_SubMeshUI[] subMeshes = GetComponentsInChildren<TMP_SubMeshUI>(true);
        foreach (var subMesh in subMeshes) {
            if (subMesh != null && (subMesh.sharedMaterial == null || subMesh.canvasRenderer == null)) {
                try {
                    GameObject go = subMesh.gameObject;
                    Destroy(subMesh);
                    if (go != null && go != gameObject && go.transform.childCount == 0) {
                        Destroy(go);
                    }
                } catch { }
            }
        }
    }

    private void ConfigureWordBankLayout() {
        GameObject buttonsParentObj = GameObject.Find("buttonsParentTransform");
        if (buttonsParentObj == null && buttonsParentTransform != null) {
            buttonsParentObj = buttonsParentTransform.gameObject;
        }
        if (buttonsParentObj == null) {
            Transform t = transform.Find("buttonsParentTransform") ?? transform.Find("PlayerWritingContainer");
            if (t != null) buttonsParentObj = t.gameObject;
        }

        if (buttonsParentObj != null) {
            GridLayoutGroup glg = buttonsParentObj.GetComponent<GridLayoutGroup>();
            if (glg == null) glg = buttonsParentObj.GetComponentInChildren<GridLayoutGroup>(true);
            if (glg != null) {
                glg.cellSize = new Vector2(150f, 62f);
                glg.spacing = new Vector2(12f, 12f);
                glg.padding = new RectOffset(15, 15, 15, 15);
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 4;
            }

            Button[] wordBtns = buttonsParentObj.GetComponentsInChildren<Button>(true);
            foreach (var btn in wordBtns) {
                if (btn == null) continue;
                btn.transition = Selectable.Transition.None;

                RectTransform r = btn.GetComponent<RectTransform>();
                if (r != null) {
                    r.sizeDelta = new Vector2(150f, 62f);
                }

                Image img = btn.GetComponent<Image>();
                if (img == null) img = btn.gameObject.AddComponent<Image>();
                if (img != null) {
                    img.raycastTarget = true;
                    img.color = new Color(0.12f, 0.25f, 0.48f, 1f); // Solid Royal Blue (#1E40AF)
                }

                Image[] childImgs = btn.GetComponentsInChildren<Image>(true);
                foreach (var cImg in childImgs) {
                    if (cImg != null) {
                        cImg.raycastTarget = true;
                        cImg.color = new Color(0.12f, 0.25f, 0.48f, 1f);
                    }
                }

                TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) {
                    tmp.raycastTarget = false;
                    tmp.color = Color.white; // Pure White Text
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 14;
                    tmp.fontSizeMax = 26;
                }
            }
        }
    }

    private Transform FindChildRecursiveGrooveOn(Transform parent, string childName) {
        if (parent == null) return null;
        foreach (Transform child in parent) {
            if (child == null) continue;
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform result = FindChildRecursiveGrooveOn(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    private void ConfigureSceneButtonLabels() {
        GameObject selPanelObj = GameObject.Find("Scenes") ?? GameObject.Find("SceneSelectionPanel");
        if (selPanelObj == null) {
            Transform t = transform.Find("Scenes") ?? transform.Find("SceneSelectionPanel") ?? FindChildRecursiveGrooveOn(transform, "Scenes") ?? FindChildRecursiveGrooveOn(transform, "SceneSelectionPanel");
            if (t != null) selPanelObj = t.gameObject;
        }

        if (selPanelObj != null) {
            Image panelImg = selPanelObj.GetComponent<Image>();
            if (panelImg != null) {
                panelImg.enabled = false;
            }

            Image[] panelChildImgs = selPanelObj.GetComponentsInChildren<Image>(true);
            foreach (var cImg in panelChildImgs) {
                if (cImg != null && cImg.GetComponent<Button>() == null && (cImg.transform.parent == null || cImg.transform.parent.GetComponent<Button>() == null)) {
                    cImg.enabled = false;
                }
            }

            RectTransform panelRect = selPanelObj.GetComponent<RectTransform>();
            if (panelRect != null) {
                panelRect.sizeDelta = new Vector2(1000f, 140f);
            }

            HorizontalLayoutGroup hlg = selPanelObj.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) {
                hlg.spacing = 60f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }
            GridLayoutGroup glg = selPanelObj.GetComponent<GridLayoutGroup>();
            if (glg != null) {
                glg.cellSize = new Vector2(360f, 95f);
                glg.spacing = new Vector2(60f, 0f);
            }

            Color[] optionColors = new Color[] {
                new Color(0.12f, 0.40f, 0.85f, 1f),
                new Color(0.55f, 0.23f, 0.85f, 1f),
                new Color(0.08f, 0.55f, 0.55f, 1f)
            };

            string[] cardTitles = new string[] {
                "Card A: Guests Arrive",
                "Card B: Shopping Time",
                "Card C: Celebrate Festival"
            };

            int activeOptionCount = (scenes != null && scenes.Length > 0) ? scenes.Length : 3;
            Button[] selBtns = selPanelObj.GetComponentsInChildren<Button>(true);

            for (int i = 0; i < selBtns.Length; i++) {
                if (selBtns[i] == null) continue;

                if (i >= activeOptionCount) {
                    selBtns[i].gameObject.SetActive(false);
                    continue;
                }

                selBtns[i].gameObject.SetActive(true);
                selBtns[i].transition = Selectable.Transition.None;

                LayoutElement le = selBtns[i].GetComponent<LayoutElement>();
                if (le == null) le = selBtns[i].gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 360f;
                le.preferredHeight = 95f;
                le.minWidth = 300f;
                le.minHeight = 85f;

                RectTransform bRect = selBtns[i].GetComponent<RectTransform>();
                if (bRect != null) {
                    bRect.sizeDelta = new Vector2(360f, 95f);
                }

                Image img = selBtns[i].GetComponent<Image>();
                if (img == null) img = selBtns[i].gameObject.AddComponent<Image>();
                if (img != null) {
                    img.enabled = true;
                    img.raycastTarget = true;
                    img.color = optionColors[i % optionColors.Length];
                }

                TMP_Text[] tmps = selBtns[i].GetComponentsInChildren<TMP_Text>(true);
                foreach (var tmp in tmps) {
                    if (tmp == null) continue;
                    tmp.raycastTarget = false;
                    tmp.color = Color.white;
                    tmp.enableWordWrapping = true;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 18;
                    tmp.fontSizeMax = 28;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.text = (i < cardTitles.Length) ? cardTitles[i] : $"Card {i+1}";
                }

                Text[] uiTexts = selBtns[i].GetComponentsInChildren<Text>(true);
                foreach (var uiText in uiTexts) {
                    if (uiText == null) continue;
                    uiText.raycastTarget = false;
                    uiText.color = Color.white;
                    uiText.fontSize = 24;
                    uiText.alignment = TextAnchor.MiddleCenter;
                    uiText.text = (i < cardTitles.Length) ? cardTitles[i] : $"Card {i+1}";
                }
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;

            if (tmp.GetComponentInParent<Button>() != null) continue;
            if (tmp.transform.parent != null && tmp.transform.parent.name.Contains("SceneSelectionPanel")) continue;

            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("title") || textVal.Contains("FREE SCENE") || textVal.Contains("NEWS") || textVal.Contains("News") || textVal.Contains("TELL THE SAME") || textVal.Contains("Polished") || textVal.Contains("RP02") || textVal.Contains("Festival")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "RP02 Free Scene — Celebrate Your Festival";
            }
            else if (lowerName.Contains("heading") || textVal.Contains("THEATRE") || textVal.Contains("BRANCH") || textVal.Contains("GROOVE") || textVal.Contains("ROLEPLAY")) {
                tmp.text = "ROLE PLAY BRANCH (Theatre Tent)";
            }
        }
    }

    protected override void CompleteScene() {
        base.CompleteScene();

        if (scenes != null && completedScenes != null && completedScenes.Count >= scenes.Length) {
            ShowAllCompletedBanner();

            if (nextButton == null) {
                Transform nbTrans = transform.Find("NextButton") ?? transform.Find("Next Button") ?? transform.Find("Next");
                if (nbTrans != null) nextButton = nbTrans.GetComponent<Button>();
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
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
                if (promptTrans == null && (lowerName.Contains("title") || txt.Contains("RP02") || txt.Contains("Festival") || txt.Contains("Free Scene"))) {
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
        bannerText.color = new Color(1f, 0.92f, 0.23f, 1f);
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.enableWordWrapping = false;

        bannerObj.transform.localScale = Vector3.zero;
        bannerObj.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
    }
}