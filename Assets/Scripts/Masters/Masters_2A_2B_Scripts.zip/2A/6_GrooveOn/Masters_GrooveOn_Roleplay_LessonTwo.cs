using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Controller for Unit 6 (Groove On) Roleplay Lesson Two:
/// RP02 Free Scene — Celebrate Your Festival.
/// Preserves and displays all character boxes, speech clouds, slate container, and word bank chips.
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
        EnsureCharacterAndSlateVisibility();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Roleplay;
        EnsureBackButtonActive();
        AutoWireUIElements();
        UpdateTitleAndUIComponents();
        ConfigureSceneButtonLabels();
        ConfigureWordBankLayout();
        EnsureCharacterAndSlateVisibility();

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
        AutoWireUIElements();
        ConfigureSceneButtonLabels();
        ConfigureWordBankLayout();
        EnsureCharacterAndSlateVisibility();
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
        if (sceneSelectionPanel == null) {
            Transform t = transform.Find("Scenes") ?? transform.Find("SceneSelectionPanel") ?? FindChildFuzzy(transform, "scene");
            if (t != null) sceneSelectionPanel = t.gameObject;
        }

        if (roleplayPanel == null) {
            Transform t = transform.Find("Roleplay panel") ?? transform.Find("RoleplayPanel") ?? FindChildFuzzy(transform, "roleplay");
            if (t != null) roleplayPanel = t.gameObject;
        }

        if (playerWritingContainer == null) {
            Transform t = FindChildFuzzy(transform, "player writing") ?? FindChildFuzzy(transform, "writing container") ?? FindChildFuzzy(transform, "playerwriting");
            if (t != null) playerWritingContainer = t.gameObject;
        }

        if (slateWordsParentTransform == null) {
            Transform t = FindChildFuzzy(transform, "slate") ?? FindChildFuzzy(transform, "slateWords");
            if (t != null) slateWordsParentTransform = t;
        }

        if (buttonsParentTransform == null) {
            Transform t = FindChildFuzzy(transform, "bank") ?? FindChildFuzzy(transform, "buttonsParent");
            if (t != null) buttonsParentTransform = t;
        }

        if (npcCloud == null) {
            Transform t = FindChildFuzzy(transform, "NPCCharacter");
            if (t != null) {
                Transform c = t.Find("Cloud");
                if (c != null) npcCloud = c.gameObject;
            }
        }

        if (playerCloud == null) {
            Transform t = FindChildFuzzy(transform, "StudentCharacter");
            if (t != null) {
                Transform c = t.Find("Cloud");
                if (c != null) playerCloud = c.gameObject;
            }
        }

        if (npcDialogueTMP == null && npcCloud != null) {
            npcDialogueTMP = npcCloud.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (playerDialogueTMP == null && playerCloud != null) {
            playerDialogueTMP = playerCloud.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (wordButtonReference == null) {
#if UNITY_EDITOR
            wordButtonReference = UnityEditor.AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/UI/Wordbutton letschoose.prefab");
            if (wordButtonReference == null) wordButtonReference = UnityEditor.AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/UI/Wordbutton chatting bees Variant.prefab");
#endif
        }
    }

    private void EnsureCharacterAndSlateVisibility() {
        // Ensure character boxes, character images, and slate are visible and properly styled
        Transform npcC = FindChildFuzzy(transform, "NPCCharacter");
        if (npcC != null) {
            npcC.gameObject.SetActive(true);
            Image[] imgs = npcC.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs) {
                if (img != null) img.enabled = true;
            }
        }

        Transform stuC = FindChildFuzzy(transform, "StudentCharacter");
        if (stuC != null) {
            stuC.gameObject.SetActive(true);
            Image[] imgs = stuC.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs) {
                if (img != null) img.enabled = true;
            }
        }

        Transform slate = FindChildFuzzy(transform, "slate");
        if (slate != null) {
            slate.gameObject.SetActive(true);
            Image[] sImgs = slate.GetComponentsInChildren<Image>(true);
            foreach (var img in sImgs) {
                if (img != null) {
                    img.enabled = true;
                }
            }
        }
    }

    protected override void StartScene(SceneData sceneData) {
        base.StartScene(sceneData);

        // Ensure roleplay panel and all its character components are active and visible
        if (roleplayPanel != null) {
            roleplayPanel.SetActive(true);
        }

        EnsureCharacterAndSlateVisibility();
    }

    protected override void StartNextTurn() {
        base.StartNextTurn();

        EnsureCharacterAndSlateVisibility();

        if (activeScene != null && activeScene.turns != null && currentTurnIndex < activeScene.turns.Length) {
            RoleplayTurn turn = activeScene.turns[currentTurnIndex];
            if (turn.turnType == TurnType.PlayerTurn) {
                if (playerWritingContainer != null) {
                    playerWritingContainer.SetActive(true);
                }
                EnsureCharacterAndSlateVisibility();
            }
        }
    }

    private void WireSceneSelectionButtons() {
        if (sceneSelectionPanel != null) {
            sceneSelectionPanel.SetActive(true);
            Button[] selBtns = sceneSelectionPanel.GetComponentsInChildren<Button>(true);

            if (scenes != null) {
                for (int i = 0; i < scenes.Length && i < selBtns.Length; i++) {
                    Button btn = selBtns[i];
                    if (btn == null) continue;
                    btn.gameObject.SetActive(true);

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
        if (buttonsParentTransform != null) {
            GridLayoutGroup glg = buttonsParentTransform.GetComponent<GridLayoutGroup>();
            if (glg == null) glg = buttonsParentTransform.GetComponentInChildren<GridLayoutGroup>(true);
            if (glg != null) {
                glg.cellSize = new Vector2(150f, 62f);
                glg.spacing = new Vector2(12f, 12f);
                glg.padding = new RectOffset(15, 15, 15, 15);
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 4;
            }

            Button[] wordBtns = buttonsParentTransform.GetComponentsInChildren<Button>(true);
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
                    img.enabled = true;
                    img.raycastTarget = true;
                    img.color = new Color(0.12f, 0.25f, 0.48f, 1f); // Solid Royal Blue (#1E40AF)
                }

                TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) {
                    tmp.raycastTarget = false;
                    tmp.color = Color.white;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 14;
                    tmp.fontSizeMax = 26;
                }
            }
        }
    }

    private Transform FindChildFuzzy(Transform parent, string keyword) {
        if (parent == null) return null;
        string kw = keyword.ToLower().Replace(" ", "").Replace("_", "");
        foreach (Transform child in parent) {
            if (child == null) continue;
            string cName = child.name.ToLower().Replace(" ", "").Replace("_", "");
            if (cName.Contains(kw)) return child;
            Transform result = FindChildFuzzy(child, keyword);
            if (result != null) return result;
        }
        return null;
    }

    private void ConfigureSceneButtonLabels() {
        if (sceneSelectionPanel != null) {
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

            Button[] selBtns = sceneSelectionPanel.GetComponentsInChildren<Button>(true);

            for (int i = 0; i < selBtns.Length; i++) {
                if (selBtns[i] == null) continue;

                selBtns[i].gameObject.SetActive(true);
                selBtns[i].transition = Selectable.Transition.None;

                Image img = selBtns[i].GetComponent<Image>();
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
                    tmp.text = (i < cardTitles.Length) ? cardTitles[i] : $"Card {i + 1}";
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
            } else if (lowerName.Contains("heading") || textVal.Contains("THEATRE") || textVal.Contains("BRANCH") || textVal.Contains("GROOVE") || textVal.Contains("ROLEPLAY")) {
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
        rect.anchoredPosition = new Vector2(0f, -165f);
        rect.sizeDelta = new Vector2(800f, 50f);

        TextMeshProUGUI bannerText = bannerObj.GetComponent<TextMeshProUGUI>();
        if (bannerText == null) bannerText = bannerObj.AddComponent<TextMeshProUGUI>();
        bannerText.enabled = true;
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