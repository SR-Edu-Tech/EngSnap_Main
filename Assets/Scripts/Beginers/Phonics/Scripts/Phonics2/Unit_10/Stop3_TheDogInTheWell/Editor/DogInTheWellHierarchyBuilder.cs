#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit10.Editor
{
    public static class DogInTheWellHierarchyBuilder
    {
        [MenuItem("EngSnap/Phonics2/Unit 10/Build Stop 3 (The Dog in the Well) Hierarchy in Active Scene", false, 104)]
        public static void BuildStop3HierarchyInScene()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            }

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
            }

            Transform parentTransform = canvas.transform;
            Transform existingUnit10Panel = canvas.transform.Find("Unit10_UIPanel");
            if (existingUnit10Panel != null) parentTransform = existingUnit10Panel;

            Transform existingStop3 = parentTransform.Find("Stop3_TheDogInTheWell");
            if (existingStop3 != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Stop3_TheDogInTheWell Already Exists",
                    "A GameObject named 'Stop3_TheDogInTheWell' already exists in the scene. Replace it?",
                    "Replace",
                    "Cancel");

                if (!replace) return;
                Undo.DestroyObjectImmediate(existingStop3.gameObject);
            }

            GameObject rootStop3 = new GameObject("Stop3_TheDogInTheWell", typeof(RectTransform), typeof(CanvasGroup), typeof(DogInTheWellController));
            rootStop3.transform.SetParent(parentTransform, false);
            SetStretchAll(rootStop3.GetComponent<RectTransform>());
            Undo.RegisterCreatedObjectUndo(rootStop3, "Create Stop3_TheDogInTheWell");

            DogInTheWellController controller = rootStop3.GetComponent<DogInTheWellController>();

            // 1. Background
            GameObject bgGO = CreateUIElement("Background", rootStop3.transform, typeof(Image));
            SetStretchAll(bgGO.GetComponent<RectTransform>());
            Image bgImage = bgGO.GetComponent<Image>();
            bgImage.color = new Color(0.92f, 0.96f, 0.92f, 1f); // Soft meadow green tint

            // 2. TopBar
            GameObject topBarGO = CreateUIElement("TopBar", rootStop3.transform, typeof(RectTransform));
            RectTransform topBarRect = topBarGO.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0f, 0f);
            topBarRect.sizeDelta = new Vector2(0f, 100f);

            GameObject progRingGO = CreateUIElement("ProgressRing", topBarGO.transform, typeof(Image));
            RectTransform progRingRect = progRingGO.GetComponent<RectTransform>();
            progRingRect.anchorMin = new Vector2(0f, 0.5f);
            progRingRect.anchorMax = new Vector2(0f, 0.5f);
            progRingRect.pivot = new Vector2(0.5f, 0.5f);
            progRingRect.anchoredPosition = new Vector2(100f, 0f);
            progRingRect.sizeDelta = new Vector2(70f, 70f);
            Image progRingImage = progRingGO.GetComponent<Image>();
            progRingImage.type = Image.Type.Filled;
            progRingImage.fillMethod = Image.FillMethod.Radial360;
            progRingImage.fillAmount = 0.0f;
            progRingImage.color = new Color(0.2f, 0.75f, 0.45f, 1f);

            GameObject progTextGO = CreateUIElement("ProgressText", progRingGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(progTextGO.GetComponent<RectTransform>());
            TMP_Text progText = progTextGO.GetComponent<TextMeshProUGUI>();
            progText.text = "0%";
            progText.fontSize = 22;
            progText.fontStyle = FontStyles.Bold;
            progText.alignment = TextAlignmentOptions.Center;
            progText.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            GameObject starMeterGO = CreateUIElement("StarMeter", topBarGO.transform, typeof(Image));
            RectTransform starMeterRect = starMeterGO.GetComponent<RectTransform>();
            starMeterRect.anchorMin = new Vector2(1f, 0.5f);
            starMeterRect.anchorMax = new Vector2(1f, 0.5f);
            starMeterRect.pivot = new Vector2(1f, 0.5f);
            starMeterRect.anchoredPosition = new Vector2(-100f, 0f);
            starMeterRect.sizeDelta = new Vector2(180f, 50f);
            starMeterGO.GetComponent<Image>().color = new Color(1f, 0.85f, 0.2f, 0.4f);

            // 3. Leo Mascot Container
            GameObject leoContainerGO = CreateUIElement("LeoMascotContainer", rootStop3.transform, typeof(RectTransform));
            RectTransform leoRect = leoContainerGO.GetComponent<RectTransform>();
            leoRect.anchorMin = new Vector2(0f, 0f);
            leoRect.anchorMax = new Vector2(0f, 0f);
            leoRect.pivot = new Vector2(0.5f, 0f);
            leoRect.anchoredPosition = new Vector2(160f, 40f);
            leoRect.sizeDelta = new Vector2(260f, 320f);

            GameObject leoMascotGO = CreateUIElement("LeoMascot", leoContainerGO.transform, typeof(Image));
            SetStretchAll(leoMascotGO.GetComponent<RectTransform>());
            Image leoImg = leoMascotGO.GetComponent<Image>();
            leoImg.color = new Color(1f, 0.75f, 0.3f, 1f);

            // 4. StoryPanel (Phase 1 & 2)
            GameObject storyPanelGO = CreateUIElement("StoryPanel", rootStop3.transform, typeof(Image));
            RectTransform storyPanelRect = storyPanelGO.GetComponent<RectTransform>();
            storyPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            storyPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            storyPanelRect.pivot = new Vector2(0.5f, 0.5f);
            storyPanelRect.anchoredPosition = new Vector2(80f, 30f);
            storyPanelRect.sizeDelta = new Vector2(1200f, 580f);
            storyPanelGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);

            // Well Scene Illustration (Top of StoryPanel)
            GameObject wellSceneGO = CreateUIElement("WellSceneImage", storyPanelGO.transform, typeof(Image));
            RectTransform wellSceneRect = wellSceneGO.GetComponent<RectTransform>();
            wellSceneRect.anchorMin = new Vector2(0.5f, 0.5f);
            wellSceneRect.anchorMax = new Vector2(0.5f, 0.5f);
            wellSceneRect.pivot = new Vector2(0.5f, 0.5f);
            wellSceneRect.anchoredPosition = new Vector2(0f, 110f);
            wellSceneRect.sizeDelta = new Vector2(520f, 260f);
            Image wellSceneImg = wellSceneGO.GetComponent<Image>();
            wellSceneImg.color = new Color(0.85f, 0.92f, 1f, 1f);

            // Line Reading Area (Bottom of StoryPanel)
            GameObject lineAreaGO = CreateUIElement("LineReadingArea", storyPanelGO.transform, typeof(RectTransform));
            RectTransform lineAreaRect = lineAreaGO.GetComponent<RectTransform>();
            lineAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineAreaRect.pivot = new Vector2(0.5f, 0.5f);
            lineAreaRect.anchoredPosition = new Vector2(0f, -130f);
            lineAreaRect.sizeDelta = new Vector2(1100f, 180f);

            GameObject curLineTMPGO = CreateUIElement("CurrentLineTMP", lineAreaGO.transform, typeof(TextMeshProUGUI));
            RectTransform curLineTMPRect = curLineTMPGO.GetComponent<RectTransform>();
            curLineTMPRect.anchorMin = new Vector2(0.5f, 1f);
            curLineTMPRect.anchorMax = new Vector2(0.5f, 1f);
            curLineTMPRect.pivot = new Vector2(0.5f, 1f);
            curLineTMPRect.anchoredPosition = new Vector2(0f, -10f);
            curLineTMPRect.sizeDelta = new Vector2(1000f, 50f);
            TMP_Text curLineTMP = curLineTMPGO.GetComponent<TextMeshProUGUI>();
            curLineTMP.text = "The dog fell in the well.";
            curLineTMP.fontSize = 32;
            curLineTMP.fontStyle = FontStyles.Bold;
            curLineTMP.alignment = TextAlignmentOptions.Center;
            curLineTMP.color = new Color(0.15f, 0.2f, 0.35f, 1f);

            GameObject wordsContainerGO = CreateUIElement("WordsContainer", lineAreaGO.transform, typeof(HorizontalLayoutGroup));
            RectTransform wordsContRect = wordsContainerGO.GetComponent<RectTransform>();
            wordsContRect.anchorMin = new Vector2(0.5f, 0f);
            wordsContRect.anchorMax = new Vector2(0.5f, 0f);
            wordsContRect.pivot = new Vector2(0.5f, 0f);
            wordsContRect.anchoredPosition = new Vector2(0f, 20f);
            wordsContRect.sizeDelta = new Vector2(1050f, 80f);
            HorizontalLayoutGroup hlg = wordsContainerGO.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            Button[] wordButtons = new Button[12];
            TMP_Text[] wordTexts = new TMP_Text[12];
            Image[] wordHighlights = new Image[12];

            for (int i = 0; i < 12; i++)
            {
                GameObject wBtnGO = CreateUIElement($"Word_{i}", wordsContainerGO.transform, typeof(Image), typeof(Button));
                RectTransform wbRect = wBtnGO.GetComponent<RectTransform>();
                wbRect.sizeDelta = new Vector2(105f, 65f);
                wBtnGO.GetComponent<Image>().color = new Color(0.92f, 0.95f, 1f, 1f);
                wordButtons[i] = wBtnGO.GetComponent<Button>();

                GameObject wHighGO = CreateUIElement("Highlight", wBtnGO.transform, typeof(Image));
                SetStretchAll(wHighGO.GetComponent<RectTransform>());
                Image hiImg = wHighGO.GetComponent<Image>();
                hiImg.color = new Color(1f, 0.9f, 0.3f, 0.5f);
                wHighGO.SetActive(false);
                wordHighlights[i] = hiImg;

                GameObject wTMPGO = CreateUIElement("Text", wBtnGO.transform, typeof(TextMeshProUGUI));
                SetStretchAll(wTMPGO.GetComponent<RectTransform>());
                TMP_Text wTMP = wTMPGO.GetComponent<TextMeshProUGUI>();
                wTMP.text = "word";
                wTMP.fontSize = 24;
                wTMP.fontStyle = FontStyles.Bold;
                wTMP.alignment = TextAlignmentOptions.Center;
                wTMP.color = new Color(0.1f, 0.2f, 0.35f, 1f);
                wordTexts[i] = wTMP;
            }

            // Speaker Help Button (Read line aloud)
            GameObject helpBtnGO = CreateUIElement("ReadLineHelpButton", storyPanelGO.transform, typeof(Image), typeof(Button));
            RectTransform helpRect = helpBtnGO.GetComponent<RectTransform>();
            helpRect.anchorMin = new Vector2(0f, 0.5f);
            helpRect.anchorMax = new Vector2(0f, 0.5f);
            helpRect.pivot = new Vector2(0.5f, 0.5f);
            helpRect.anchoredPosition = new Vector2(70f, -130f);
            helpRect.sizeDelta = new Vector2(75f, 75f);
            helpBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.65f, 0.9f, 1f);
            Button helpBtn = helpBtnGO.GetComponent<Button>();
            GameObject helpIcon = CreateUIElement("Icon", helpBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(helpIcon.GetComponent<RectTransform>());
            TMP_Text hTMP = helpIcon.GetComponent<TextMeshProUGUI>();
            hTMP.text = "🔊";
            hTMP.fontSize = 34;
            hTMP.alignment = TextAlignmentOptions.Center;
            hTMP.color = Color.white;

            // Next Line Tick Button
            GameObject tickBtnGO = CreateUIElement("NextLineTickButton", storyPanelGO.transform, typeof(Image), typeof(Button));
            RectTransform tickRect = tickBtnGO.GetComponent<RectTransform>();
            tickRect.anchorMin = new Vector2(1f, 0.5f);
            tickRect.anchorMax = new Vector2(1f, 0.5f);
            tickRect.pivot = new Vector2(0.5f, 0.5f);
            tickRect.anchoredPosition = new Vector2(-70f, -130f);
            tickRect.sizeDelta = new Vector2(85f, 85f);
            tickBtnGO.GetComponent<Image>().color = new Color(0.18f, 0.8f, 0.44f, 1f);
            Button tickBtn = tickBtnGO.GetComponent<Button>();
            GameObject tickIcon = CreateUIElement("Icon", tickBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(tickIcon.GetComponent<RectTransform>());
            TMP_Text tTMP = tickIcon.GetComponent<TextMeshProUGUI>();
            tTMP.text = "✔";
            tTMP.fontSize = 38;
            tTMP.fontStyle = FontStyles.Bold;
            tTMP.alignment = TextAlignmentOptions.Center;
            tTMP.color = Color.white;

            // 5. DialogueUI
            GameObject dialogueUIGO = CreateUIElement("DialogueUI", rootStop3.transform, typeof(Image), typeof(CanvasGroup));
            RectTransform dialRect = dialogueUIGO.GetComponent<RectTransform>();
            dialRect.anchorMin = new Vector2(0.5f, 0f);
            dialRect.anchorMax = new Vector2(0.5f, 0f);
            dialRect.pivot = new Vector2(0.5f, 0f);
            dialRect.anchoredPosition = new Vector2(0f, 30f);
            dialRect.sizeDelta = new Vector2(1050f, 100f);
            dialogueUIGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
            CanvasGroup dialCG = dialogueUIGO.GetComponent<CanvasGroup>();

            GameObject dialTMPGO = CreateUIElement("DialogueTMP", dialogueUIGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(dialTMPGO.GetComponent<RectTransform>());
            TMP_Text dialTMP = dialTMPGO.GetComponent<TextMeshProUGUI>();
            dialTMP.text = "This is the longest story yet. You know every word in it. Take your time!";
            dialTMP.fontSize = 30;
            dialTMP.alignment = TextAlignmentOptions.Center;
            dialTMP.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            // 6. AudioSources
            GameObject audioSourcesGO = CreateUIElement("AudioSources", rootStop3.transform, typeof(RectTransform));
            AudioSource vSrc = audioSourcesGO.AddComponent<AudioSource>();
            vSrc.playOnAwake = false;
            vSrc.spatialBlend = 0f;
            AudioSource sSrc = audioSourcesGO.AddComponent<AudioSource>();
            sSrc.playOnAwake = false;
            sSrc.spatialBlend = 0f;

            // 7. RewardsContainer
            GameObject rewardsGO = CreateUIElement("RewardsContainer", rootStop3.transform, typeof(RectTransform));
            SetStretchAll(rewardsGO.GetComponent<RectTransform>());

            GameObject confettiGO = CreateUIElement("ConfettiParticles", rewardsGO.transform, typeof(RectTransform));
            confettiGO.SetActive(false);

            GameObject rewardPopupGO = CreateUIElement("RewardPopup", rewardsGO.transform, typeof(Image));
            rewardPopupGO.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 350f);
            rewardPopupGO.SetActive(false);

            GameObject stickerPopupGO = CreateUIElement("StickerPopup", rewardsGO.transform, typeof(Image));
            stickerPopupGO.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 300f);
            stickerPopupGO.SetActive(false);

            GameObject contBtnGO = CreateUIElement("ContinueButton", rewardsGO.transform, typeof(Image), typeof(Button));
            RectTransform contRect = contBtnGO.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0.5f);
            contRect.anchorMax = new Vector2(0.5f, 0.5f);
            contRect.pivot = new Vector2(0.5f, 0.5f);
            contRect.anchoredPosition = new Vector2(0f, -220f);
            contRect.sizeDelta = new Vector2(340f, 75f);
            contBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.45f, 1f);
            GameObject contTMPGO = CreateUIElement("Text", contBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(contTMPGO.GetComponent<RectTransform>());
            TMP_Text contTMP = contTMPGO.GetComponent<TextMeshProUGUI>();
            contTMP.text = "Take The Graduation Quiz ➔";
            contTMP.fontSize = 24;
            contTMP.fontStyle = FontStyles.Bold;
            contTMP.alignment = TextAlignmentOptions.Center;
            contTMP.color = Color.white;
            contBtnGO.SetActive(false);

            // 8. Auto-wire Serialized Properties
            SerializedObject sObj = new SerializedObject(controller);
            sObj.FindProperty("unitID").stringValue = "Unit10";
            sObj.FindProperty("topicName").stringValue = "TheDogInTheWell";

            DogInTheWellData dataAsset = Resources.Load<DogInTheWellData>("Phonics2/Unit10/DogInTheWellData_Unit10");
            if (dataAsset != null) sObj.FindProperty("activityData").objectReferenceValue = dataAsset;

            sObj.FindProperty("voiceAudioSource").objectReferenceValue = vSrc;
            sObj.FindProperty("sfxAudioSource").objectReferenceValue = sSrc;
            sObj.FindProperty("dialogueText").objectReferenceValue = dialTMP;
            sObj.FindProperty("dialogueCanvasGroup").objectReferenceValue = dialCG;

            // Phase 1 & 2
            sObj.FindProperty("storyPanel").objectReferenceValue = storyPanelGO;
            sObj.FindProperty("wellSceneImage").objectReferenceValue = wellSceneImg;
            sObj.FindProperty("currentLineTMP").objectReferenceValue = curLineTMP;
            sObj.FindProperty("readLineHelpButton").objectReferenceValue = helpBtn;
            sObj.FindProperty("nextLineTickButton").objectReferenceValue = tickBtn;

            SerializedProperty pWordBtns = sObj.FindProperty("wordButtonsInLine");
            SerializedProperty pWordTxts = sObj.FindProperty("wordTextsInLine");
            SerializedProperty pWordHighs = sObj.FindProperty("wordHighlightsInLine");
            pWordBtns.arraySize = 12;
            pWordTxts.arraySize = 12;
            pWordHighs.arraySize = 12;

            for (int i = 0; i < 12; i++)
            {
                pWordBtns.GetArrayElementAtIndex(i).objectReferenceValue = wordButtons[i];
                pWordTxts.GetArrayElementAtIndex(i).objectReferenceValue = wordTexts[i];
                pWordHighs.GetArrayElementAtIndex(i).objectReferenceValue = wordHighlights[i];
            }

            // Progress & Rewards
            sObj.FindProperty("progressRingFillImage").objectReferenceValue = progRingImage;
            sObj.FindProperty("progressText").objectReferenceValue = progText;
            sObj.FindProperty("starMeterRect").objectReferenceValue = starMeterRect;
            sObj.FindProperty("leoMascotObject").objectReferenceValue = leoContainerGO;
            sObj.FindProperty("confettiParticles").objectReferenceValue = confettiGO;
            sObj.FindProperty("rewardPopup").objectReferenceValue = rewardPopupGO;
            sObj.FindProperty("stickerPopup").objectReferenceValue = stickerPopupGO;
            sObj.FindProperty("continueButton").objectReferenceValue = contBtnGO;
            sObj.FindProperty("currentPanel").objectReferenceValue = rootStop3;

            Transform stop4Transform = parentTransform.Find("Stop4_QuestionsAndGraduation");
            if (stop4Transform != null) sObj.FindProperty("nextPanel").objectReferenceValue = stop4Transform.gameObject;

            sObj.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = rootStop3;
            Debug.Log("[EngSnap] Successfully created 'Stop3_TheDogInTheWell' hierarchy with clean Story Reading UI and auto-wired DogInTheWellController!");
        }

        private static GameObject CreateUIElement(string name, Transform parent, params System.Type[] components)
        {
            GameObject go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
#endif
