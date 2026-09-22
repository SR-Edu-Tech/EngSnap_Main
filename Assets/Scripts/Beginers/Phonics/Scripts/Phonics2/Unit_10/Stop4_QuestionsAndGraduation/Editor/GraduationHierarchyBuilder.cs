#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit10.Editor
{
    public static class GraduationHierarchyBuilder
    {
        [MenuItem("EngSnap/Phonics2/Unit 10/Build Stop 4 (Questions & Graduation) Hierarchy in Active Scene", false, 105)]
        public static void BuildStop4HierarchyInScene()
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

            Transform existingStop4 = parentTransform.Find("Stop4_QuestionsAndGraduation");
            if (existingStop4 != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Stop4_QuestionsAndGraduation Already Exists",
                    "A GameObject named 'Stop4_QuestionsAndGraduation' already exists. Replace it?",
                    "Replace",
                    "Cancel");

                if (!replace) return;
                Undo.DestroyObjectImmediate(existingStop4.gameObject);
            }

            GameObject rootStop4 = new GameObject("Stop4_QuestionsAndGraduation", typeof(RectTransform), typeof(CanvasGroup), typeof(GraduationController));
            rootStop4.transform.SetParent(parentTransform, false);
            SetStretchAll(rootStop4.GetComponent<RectTransform>());
            Undo.RegisterCreatedObjectUndo(rootStop4, "Create Stop4_QuestionsAndGraduation");

            GraduationController controller = rootStop4.GetComponent<GraduationController>();

            // 1. Background
            GameObject bgGO = CreateUIElement("Background", rootStop4.transform, typeof(Image));
            SetStretchAll(bgGO.GetComponent<RectTransform>());
            bgGO.GetComponent<Image>().color = new Color(0.92f, 0.95f, 1f, 1f); // Grand Sound Island Horizon Tint

            // 2. TopBar
            GameObject topBarGO = CreateUIElement("TopBar", rootStop4.transform, typeof(RectTransform));
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
            progRingImage.color = new Color(0.18f, 0.8f, 0.44f, 1f);

            GameObject progTextGO = CreateUIElement("ProgressText", progRingGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(progTextGO.GetComponent<RectTransform>());
            TMP_Text progText = progTextGO.GetComponent<TextMeshProUGUI>();
            progText.text = "0%";
            progText.fontSize = 22;
            progText.fontStyle = FontStyles.Bold;
            progText.alignment = TextAlignmentOptions.Center;
            progText.color = new Color(0.12f, 0.35f, 0.18f, 1f);

            GameObject starMeterGO = CreateUIElement("StarMeter", topBarGO.transform, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform starMeterRect = starMeterGO.GetComponent<RectTransform>();
            starMeterRect.anchorMin = new Vector2(1f, 0.5f);
            starMeterRect.anchorMax = new Vector2(1f, 0.5f);
            starMeterRect.pivot = new Vector2(1f, 0.5f);
            starMeterRect.anchoredPosition = new Vector2(-80f, 0f);
            starMeterRect.sizeDelta = new Vector2(220f, 60f);
            HorizontalLayoutGroup starLayout = starMeterGO.GetComponent<HorizontalLayoutGroup>();
            starLayout.spacing = 10f;
            starLayout.childAlignment = TextAnchor.MiddleRight;
            starLayout.childControlWidth = false;
            starLayout.childControlHeight = false;

            for (int s = 0; s < 3; s++)
            {
                GameObject starIcon = CreateUIElement($"Star_{s}", starMeterGO.transform, typeof(Image));
                starIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(50f, 50f);
                starIcon.GetComponent<Image>().color = new Color(1f, 0.84f, 0.0f, 1f);
            }

            // 3. Phase 1: QuestionsPanel
            GameObject questionsPanelGO = CreateUIElement("QuestionsPanel", rootStop4.transform, typeof(RectTransform));
            RectTransform qPanelRect = questionsPanelGO.GetComponent<RectTransform>();
            qPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            qPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            qPanelRect.pivot = new Vector2(0.5f, 0.5f);
            qPanelRect.anchoredPosition = new Vector2(0f, 30f);
            qPanelRect.sizeDelta = new Vector2(1460f, 650f);

            // Pinned Story Panel (Top Area)
            GameObject pinnedStoryPanelGO = CreateUIElement("PinnedStoryPanel", questionsPanelGO.transform, typeof(Image));
            RectTransform pinnedStoryRect = pinnedStoryPanelGO.GetComponent<RectTransform>();
            pinnedStoryRect.anchorMin = new Vector2(0.5f, 1f);
            pinnedStoryRect.anchorMax = new Vector2(0.5f, 1f);
            pinnedStoryRect.pivot = new Vector2(0.5f, 1f);
            pinnedStoryRect.anchoredPosition = new Vector2(0f, 0f);
            pinnedStoryRect.sizeDelta = new Vector2(1400f, 290f);
            pinnedStoryPanelGO.GetComponent<Image>().color = new Color(0.98f, 0.98f, 0.96f, 0.95f); // Parchment tone

            GameObject storyTitleGO = CreateUIElement("StoryTitleTMP", pinnedStoryPanelGO.transform, typeof(TextMeshProUGUI));
            RectTransform titleRect = storyTitleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -10f);
            titleRect.sizeDelta = new Vector2(600f, 30f);
            TMP_Text sTitleTMP = storyTitleGO.GetComponent<TextMeshProUGUI>();
            sTitleTMP.text = "📖 The Dog in the Well (Reference Story)";
            sTitleTMP.fontSize = 20;
            sTitleTMP.fontStyle = FontStyles.Bold;
            sTitleTMP.alignment = TextAlignmentOptions.Center;
            sTitleTMP.color = new Color(0.4f, 0.35f, 0.25f, 1f);

            // 8 Pinned Story Line Elements
            GameObject linesGridGO = CreateUIElement("LinesContainer", pinnedStoryPanelGO.transform, typeof(RectTransform), typeof(GridLayoutGroup));
            RectTransform linesGridRect = linesGridGO.GetComponent<RectTransform>();
            linesGridRect.anchorMin = new Vector2(0.5f, 0f);
            linesGridRect.anchorMax = new Vector2(0.5f, 0f);
            linesGridRect.pivot = new Vector2(0.5f, 0f);
            linesGridRect.anchoredPosition = new Vector2(0f, 15f);
            linesGridRect.sizeDelta = new Vector2(1360f, 230f);
            GridLayoutGroup linesLayout = linesGridGO.GetComponent<GridLayoutGroup>();
            linesLayout.cellSize = new Vector2(660f, 48f);
            linesLayout.spacing = new Vector2(25f, 8f);
            linesLayout.childAlignment = TextAnchor.UpperLeft;

            TMP_Text[] pinnedLineTexts = new TMP_Text[8];
            Image[] pinnedLineHighlights = new Image[8];
            string[] defaultStoryLines = new string[] {
                "1. The well.",
                "2. The bell.",
                "3. The dog fell in the well.",
                "4. Dell, tell Dad the dog fell in the well.",
                "5. Dell rang the bell to tell Dad the dog fell.",
                "6. Dell had to yell, \"The dog is in the well.\"",
                "7. Dad ran to Dell at the well.",
                "8. Dad got the dog out of the well."
            };

            for (int l = 0; l < 8; l++)
            {
                GameObject lineGO = CreateUIElement($"PinnedLine_{l}", linesGridGO.transform, typeof(Image));
                lineGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.7f);

                GameObject highGO = CreateUIElement("Highlight", lineGO.transform, typeof(Image));
                SetStretchAll(highGO.GetComponent<RectTransform>());
                Image highImg = highGO.GetComponent<Image>();
                highImg.color = new Color(1f, 0.92f, 0.2f, 0.5f); // Yellow clue highlight
                highGO.SetActive(false);

                GameObject txtGO = CreateUIElement("Text", lineGO.transform, typeof(TextMeshProUGUI));
                SetStretchAll(txtGO.GetComponent<RectTransform>());
                txtGO.GetComponent<RectTransform>().offsetMin = new Vector2(12f, 0f);
                TMP_Text lTMP = txtGO.GetComponent<TextMeshProUGUI>();
                lTMP.text = defaultStoryLines[l];
                lTMP.fontSize = 20;
                lTMP.fontStyle = FontStyles.Bold;
                lTMP.alignment = TextAlignmentOptions.MidlineLeft;
                lTMP.color = new Color(0.15f, 0.2f, 0.3f, 1f);

                pinnedLineTexts[l] = lTMP;
                pinnedLineHighlights[l] = highImg;
            }

            // Question Area (Bottom Area)
            GameObject questionAreaGO = CreateUIElement("QuestionArea", questionsPanelGO.transform, typeof(Image));
            RectTransform qAreaRect = questionAreaGO.GetComponent<RectTransform>();
            qAreaRect.anchorMin = new Vector2(0.5f, 0f);
            qAreaRect.anchorMax = new Vector2(0.5f, 0f);
            qAreaRect.pivot = new Vector2(0.5f, 0f);
            qAreaRect.anchoredPosition = new Vector2(0f, 10f);
            qAreaRect.sizeDelta = new Vector2(1400f, 320f);
            questionAreaGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);

            GameObject qPromptGO = CreateUIElement("QuestionPromptTMP", questionAreaGO.transform, typeof(TextMeshProUGUI));
            RectTransform qPromptRect = qPromptGO.GetComponent<RectTransform>();
            qPromptRect.anchorMin = new Vector2(0.5f, 1f);
            qPromptRect.anchorMax = new Vector2(0.5f, 1f);
            qPromptRect.pivot = new Vector2(0.5f, 1f);
            qPromptRect.anchoredPosition = new Vector2(0f, -20f);
            qPromptRect.sizeDelta = new Vector2(1200f, 50f);
            TMP_Text qPromptTMP = qPromptGO.GetComponent<TextMeshProUGUI>();
            qPromptTMP.text = "Who rang the bell?";
            qPromptTMP.fontSize = 36;
            qPromptTMP.fontStyle = FontStyles.Bold;
            qPromptTMP.alignment = TextAlignmentOptions.Center;
            qPromptTMP.color = new Color(0.12f, 0.2f, 0.45f, 1f);

            // Choices Container (3 Choice Cards)
            GameObject choicesGO = CreateUIElement("ChoicesContainer", questionAreaGO.transform, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform choicesRect = choicesGO.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0.5f, 0f);
            choicesRect.anchorMax = new Vector2(0.5f, 0f);
            choicesRect.pivot = new Vector2(0.5f, 0f);
            choicesRect.anchoredPosition = new Vector2(0f, 25f);
            choicesRect.sizeDelta = new Vector2(1200f, 190f);
            HorizontalLayoutGroup choiceLayout = choicesGO.GetComponent<HorizontalLayoutGroup>();
            choiceLayout.spacing = 40f;
            choiceLayout.childAlignment = TextAnchor.MiddleCenter;
            choiceLayout.childControlWidth = false;
            choiceLayout.childControlHeight = false;

            Button[] choiceButtons = new Button[3];
            TMP_Text[] choiceTexts = new TMP_Text[3];
            Image[] choiceImages = new Image[3];
            string[] defaultChoices = new string[] { "Dell", "Dad", "the dog" };

            for (int c = 0; c < 3; c++)
            {
                GameObject cardGO = CreateUIElement($"Choice_{c}", choicesGO.transform, typeof(Image), typeof(Button));
                RectTransform cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(340f, 170f);
                cardGO.GetComponent<Image>().color = new Color(0.96f, 0.97f, 1f, 1f);
                Button cardBtn = cardGO.GetComponent<Button>();

                GameObject imgGO = CreateUIElement("Image", cardGO.transform, typeof(Image));
                RectTransform imgRect = imgGO.GetComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0.5f, 0.5f);
                imgRect.anchorMax = new Vector2(0.5f, 0.5f);
                imgRect.pivot = new Vector2(0.5f, 0.5f);
                imgRect.anchoredPosition = new Vector2(0f, 20f);
                imgRect.sizeDelta = new Vector2(90f, 90f);
                Image cImg = imgGO.GetComponent<Image>();
                cImg.color = new Color(0.85f, 0.9f, 0.98f, 1f);

                GameObject txtGO = CreateUIElement("Text", cardGO.transform, typeof(TextMeshProUGUI));
                RectTransform txtRect = txtGO.GetComponent<RectTransform>();
                txtRect.anchorMin = new Vector2(0f, 0f);
                txtRect.anchorMax = new Vector2(1f, 0f);
                txtRect.pivot = new Vector2(0.5f, 0f);
                txtRect.anchoredPosition = new Vector2(0f, 12f);
                txtRect.sizeDelta = new Vector2(0f, 40f);
                TMP_Text cTxt = txtGO.GetComponent<TextMeshProUGUI>();
                cTxt.text = defaultChoices[c];
                cTxt.fontSize = 28;
                cTxt.fontStyle = FontStyles.Bold;
                cTxt.alignment = TextAlignmentOptions.Center;
                cTxt.color = new Color(0.15f, 0.22f, 0.4f, 1f);

                choiceButtons[c] = cardBtn;
                choiceTexts[c] = cTxt;
                choiceImages[c] = cImg;
            }

            // 4. Phase 2: GraduationPanel (Inactive initially)
            GameObject gradPanelGO = CreateUIElement("GraduationPanel", rootStop4.transform, typeof(Image));
            RectTransform gradRect = gradPanelGO.GetComponent<RectTransform>();
            gradRect.anchorMin = new Vector2(0.5f, 0.5f);
            gradRect.anchorMax = new Vector2(0.5f, 0.5f);
            gradRect.pivot = new Vector2(0.5f, 0.5f);
            gradRect.anchoredPosition = new Vector2(0f, 20f);
            gradRect.sizeDelta = new Vector2(1460f, 660f);
            gradPanelGO.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.22f, 0.95f); // Majestic nighttime celebration sky

            // Sound Island Map Background
            GameObject mapBgGO = CreateUIElement("SoundIslandMapBackground", gradPanelGO.transform, typeof(Image));
            SetStretchAll(mapBgGO.GetComponent<RectTransform>());
            mapBgGO.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f, 0.35f);

            // 10 Island World Glow Nodes
            GameObject worldGlowsGO = CreateUIElement("IslandWorldGlows", gradPanelGO.transform, typeof(RectTransform));
            SetStretchAll(worldGlowsGO.GetComponent<RectTransform>());
            Image[] worldGlowImages = new Image[10];

            Vector2[] glowPositions = new Vector2[] {
                new Vector2(-500f, -150f), new Vector2(-400f, -60f), new Vector2(-280f, 40f),
                new Vector2(-150f, 100f), new Vector2(0f, 140f), new Vector2(150f, 100f),
                new Vector2(280f, 40f), new Vector2(400f, -60f), new Vector2(500f, -150f),
                new Vector2(0f, 0f) // Central Sound Peak (World 10)
            };

            for (int g = 0; g < 10; g++)
            {
                GameObject glowNode = CreateUIElement($"WorldGlow_{g}", worldGlowsGO.transform, typeof(Image));
                RectTransform gRect = glowNode.GetComponent<RectTransform>();
                gRect.anchorMin = new Vector2(0.5f, 0.5f);
                gRect.anchorMax = new Vector2(0.5f, 0.5f);
                gRect.pivot = new Vector2(0.5f, 0.5f);
                gRect.anchoredPosition = glowPositions[g];
                gRect.sizeDelta = new Vector2(90f, 90f);
                Image gImg = glowNode.GetComponent<Image>();
                gImg.color = new Color(1f, 0.88f, 0.2f, 0.9f); // Brilliant Gold Glow
                glowNode.SetActive(false);
                worldGlowImages[g] = gImg;
            }

            // All Four Mascots Container
            GameObject mascotsContGO = CreateUIElement("AllFourMascotsContainer", gradPanelGO.transform, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform mascotsRect = mascotsContGO.GetComponent<RectTransform>();
            mascotsRect.anchorMin = new Vector2(0.5f, 0f);
            mascotsRect.anchorMax = new Vector2(0.5f, 0f);
            mascotsRect.pivot = new Vector2(0.5f, 0f);
            mascotsRect.anchoredPosition = new Vector2(0f, 20f);
            mascotsRect.sizeDelta = new Vector2(1200f, 260f);
            HorizontalLayoutGroup mascotsLayout = mascotsContGO.GetComponent<HorizontalLayoutGroup>();
            mascotsLayout.spacing = 30f;
            mascotsLayout.childAlignment = TextAnchor.MiddleCenter;
            mascotsLayout.childControlWidth = false;
            mascotsLayout.childControlHeight = false;

            GameObject leoMascotGO = CreateUIElement("LeoMascot", mascotsContGO.transform, typeof(Image));
            leoMascotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 240f);
            leoMascotGO.GetComponent<Image>().color = new Color(1f, 0.75f, 0.35f, 1f);

            GameObject gigiMascotGO = CreateUIElement("GigiMascot", mascotsContGO.transform, typeof(Image));
            gigiMascotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 230f);
            gigiMascotGO.GetComponent<Image>().color = new Color(0.95f, 0.45f, 0.65f, 1f);

            GameObject taraMascotGO = CreateUIElement("TaraMascot", mascotsContGO.transform, typeof(Image));
            taraMascotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 230f);
            taraMascotGO.GetComponent<Image>().color = new Color(0.4f, 0.75f, 0.95f, 1f);

            GameObject momoMascotGO = CreateUIElement("MomoMascot", mascotsContGO.transform, typeof(Image));
            momoMascotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 230f);
            momoMascotGO.GetComponent<Image>().color = new Color(0.6f, 0.85f, 0.4f, 1f);

            mascotsContGO.SetActive(false);

            GameObject fireworksGO = CreateUIElement("FireworksParticleSystem", gradPanelGO.transform, typeof(RectTransform));
            SetStretchAll(fireworksGO.GetComponent<RectTransform>());
            fireworksGO.SetActive(false);

            gradPanelGO.SetActive(false);

            // 5. Phase 3: Modals (Champion Badge & Certificate)
            GameObject badgePopupGO = CreateUIElement("PhonicsChampionBadgePopup", rootStop4.transform, typeof(Image));
            RectTransform badgeRect = badgePopupGO.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(600f, 440f);
            badgePopupGO.GetComponent<Image>().color = new Color(1f, 0.92f, 0.65f, 0.98f);
            GameObject badgeTMPGO = CreateUIElement("TitleTMP", badgePopupGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(badgeTMPGO.GetComponent<RectTransform>());
            badgeTMPGO.GetComponent<TextMeshProUGUI>().text = "🏆 PHONICS CHAMPION! 🏆\nYou Mastered Sound Island!";
            badgeTMPGO.GetComponent<TextMeshProUGUI>().fontSize = 32;
            badgeTMPGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            badgeTMPGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            badgeTMPGO.GetComponent<TextMeshProUGUI>().color = new Color(0.4f, 0.25f, 0.05f, 1f);
            badgePopupGO.SetActive(false);

            // Printable Certificate Modal
            GameObject certModalGO = CreateUIElement("CertificateModalPopup", rootStop4.transform, typeof(Image));
            RectTransform certRect = certModalGO.GetComponent<RectTransform>();
            certRect.anchorMin = new Vector2(0.5f, 0.5f);
            certRect.anchorMax = new Vector2(0.5f, 0.5f);
            certRect.pivot = new Vector2(0.5f, 0.5f);
            certRect.sizeDelta = new Vector2(740f, 520f);
            certModalGO.GetComponent<Image>().color = new Color(1f, 0.98f, 0.92f, 0.98f);

            GameObject certTitleGO = CreateUIElement("CertificateTitleTMP", certModalGO.transform, typeof(TextMeshProUGUI));
            RectTransform certTitleRect = certTitleGO.GetComponent<RectTransform>();
            certTitleRect.anchorMin = new Vector2(0.5f, 1f);
            certTitleRect.anchorMax = new Vector2(0.5f, 1f);
            certTitleRect.pivot = new Vector2(0.5f, 1f);
            certTitleRect.anchoredPosition = new Vector2(0f, -40f);
            certTitleRect.sizeDelta = new Vector2(650f, 50f);
            TMP_Text cTitle = certTitleGO.GetComponent<TextMeshProUGUI>();
            cTitle.text = "⭐ SOUND ISLAND GRADUATE ⭐";
            cTitle.fontSize = 32;
            cTitle.fontStyle = FontStyles.Bold;
            cTitle.alignment = TextAlignmentOptions.Center;
            cTitle.color = new Color(0.45f, 0.3f, 0.1f, 1f);

            GameObject childNameGO = CreateUIElement("CertificateChildNameTMP", certModalGO.transform, typeof(TextMeshProUGUI));
            RectTransform childNameRect = childNameGO.GetComponent<RectTransform>();
            childNameRect.anchorMin = new Vector2(0.5f, 0.5f);
            childNameRect.anchorMax = new Vector2(0.5f, 0.5f);
            childNameRect.pivot = new Vector2(0.5f, 0.5f);
            childNameRect.anchoredPosition = new Vector2(0f, 40f);
            childNameRect.sizeDelta = new Vector2(600f, 70f);
            TMP_Text childNameTMP = childNameGO.GetComponent<TextMeshProUGUI>();
            childNameTMP.text = "Star Reader";
            childNameTMP.fontSize = 52;
            childNameTMP.fontStyle = FontStyles.Bold;
            childNameTMP.alignment = TextAlignmentOptions.Center;
            childNameTMP.color = new Color(0.1f, 0.35f, 0.7f, 1f);

            GameObject wordCountGO = CreateUIElement("CertificateWordCountTMP", certModalGO.transform, typeof(TextMeshProUGUI));
            RectTransform wcRect = wordCountGO.GetComponent<RectTransform>();
            wcRect.anchorMin = new Vector2(0.5f, 0.5f);
            wcRect.anchorMax = new Vector2(0.5f, 0.5f);
            wcRect.pivot = new Vector2(0.5f, 0.5f);
            wcRect.anchoredPosition = new Vector2(0f, -30f);
            wcRect.sizeDelta = new Vector2(500f, 40f);
            TMP_Text wcTMP = wordCountGO.GetComponent<TextMeshProUGUI>();
            wcTMP.text = "100+ Words Mastered";
            wcTMP.fontSize = 26;
            wcTMP.fontStyle = FontStyles.Bold;
            wcTMP.alignment = TextAlignmentOptions.Center;
            wcTMP.color = new Color(0.2f, 0.5f, 0.3f, 1f);

            GameObject finishGameBtnGO = CreateUIElement("FinishGameButton", certModalGO.transform, typeof(Image), typeof(Button));
            RectTransform finRect = finishGameBtnGO.GetComponent<RectTransform>();
            finRect.anchorMin = new Vector2(0.5f, 0f);
            finRect.anchorMax = new Vector2(0.5f, 0f);
            finRect.pivot = new Vector2(0.5f, 0f);
            finRect.anchoredPosition = new Vector2(0f, 30f);
            finRect.sizeDelta = new Vector2(360f, 75f);
            finishGameBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.45f, 1f);
            Button finishGameBtn = finishGameBtnGO.GetComponent<Button>();
            GameObject finTMPGO = CreateUIElement("Text", finishGameBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(finTMPGO.GetComponent<RectTransform>());
            TMP_Text finTMP = finTMPGO.GetComponent<TextMeshProUGUI>();
            finTMP.text = "Explore Island / Finish ➔";
            finTMP.fontSize = 24;
            finTMP.fontStyle = FontStyles.Bold;
            finTMP.alignment = TextAlignmentOptions.Center;
            finTMP.color = Color.white;

            certModalGO.SetActive(false);

            // 6. DialogueUI
            GameObject dialogueUIGO = CreateUIElement("DialogueUI", rootStop4.transform, typeof(Image), typeof(CanvasGroup));
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
            dialTMP.text = "Now — did you understand it? The story is right there!";
            dialTMP.fontSize = 30;
            dialTMP.alignment = TextAlignmentOptions.Center;
            dialTMP.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            // 7. AudioSources
            GameObject audioSourcesGO = CreateUIElement("AudioSources", rootStop4.transform, typeof(RectTransform));
            AudioSource vSrc = audioSourcesGO.AddComponent<AudioSource>();
            vSrc.playOnAwake = false;
            vSrc.spatialBlend = 0f;
            AudioSource sSrc = audioSourcesGO.AddComponent<AudioSource>();
            sSrc.playOnAwake = false;
            sSrc.spatialBlend = 0f;

            // 8. RewardsContainer
            GameObject rewardsGO = CreateUIElement("RewardsContainer", rootStop4.transform, typeof(RectTransform));
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
            contRect.sizeDelta = new Vector2(320f, 75f);
            contBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.45f, 1f);
            GameObject contTMPGO = CreateUIElement("Text", contBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(contTMPGO.GetComponent<RectTransform>());
            TMP_Text contTMP = contTMPGO.GetComponent<TextMeshProUGUI>();
            contTMP.text = "Finish Course ➔";
            contTMP.fontSize = 26;
            contTMP.fontStyle = FontStyles.Bold;
            contTMP.alignment = TextAlignmentOptions.Center;
            contTMP.color = Color.white;
            contBtnGO.SetActive(false);

            // 9. Auto-wire Serialized Properties
            SerializedObject sObj = new SerializedObject(controller);
            sObj.FindProperty("unitID").stringValue = "Unit10";
            sObj.FindProperty("topicName").stringValue = "Graduation";

            GraduationData dataAsset = Resources.Load<GraduationData>("Phonics2/Unit10/GraduationData_Unit10");
            if (dataAsset != null) sObj.FindProperty("activityData").objectReferenceValue = dataAsset;

            sObj.FindProperty("voiceAudioSource").objectReferenceValue = vSrc;
            sObj.FindProperty("sfxAudioSource").objectReferenceValue = sSrc;
            sObj.FindProperty("dialogueText").objectReferenceValue = dialTMP;
            sObj.FindProperty("dialogueCanvasGroup").objectReferenceValue = dialCG;

            // Phase 1: Questions & Pinned Story
            sObj.FindProperty("questionsPanel").objectReferenceValue = questionsPanelGO;
            sObj.FindProperty("pinnedStoryPanel").objectReferenceValue = pinnedStoryPanelGO;
            sObj.FindProperty("questionPromptTMP").objectReferenceValue = qPromptTMP;

            SerializedProperty pLineTxts = sObj.FindProperty("pinnedStoryLineTexts");
            SerializedProperty pLineHighs = sObj.FindProperty("pinnedStoryLineHighlights");
            pLineTxts.arraySize = 8;
            pLineHighs.arraySize = 8;
            for (int l = 0; l < 8; l++)
            {
                pLineTxts.GetArrayElementAtIndex(l).objectReferenceValue = pinnedLineTexts[l];
                pLineHighs.GetArrayElementAtIndex(l).objectReferenceValue = pinnedLineHighlights[l];
            }

            SerializedProperty pChoiceBtns = sObj.FindProperty("questionChoiceButtons");
            SerializedProperty pChoiceTxts = sObj.FindProperty("questionChoiceTexts");
            SerializedProperty pChoiceImgs = sObj.FindProperty("questionChoiceImages");
            pChoiceBtns.arraySize = 3;
            pChoiceTxts.arraySize = 3;
            pChoiceImgs.arraySize = 3;
            for (int c = 0; c < 3; c++)
            {
                pChoiceBtns.GetArrayElementAtIndex(c).objectReferenceValue = choiceButtons[c];
                pChoiceTxts.GetArrayElementAtIndex(c).objectReferenceValue = choiceTexts[c];
                pChoiceImgs.GetArrayElementAtIndex(c).objectReferenceValue = choiceImages[c];
            }

            // Phase 2: Graduation & Mascots
            sObj.FindProperty("graduationPanel").objectReferenceValue = gradPanelGO;
            sObj.FindProperty("allFourMascotsContainer").objectReferenceValue = mascotsContGO;
            sObj.FindProperty("leoMascot").objectReferenceValue = leoMascotGO;
            sObj.FindProperty("gigiMascot").objectReferenceValue = gigiMascotGO;
            sObj.FindProperty("taraMascot").objectReferenceValue = taraMascotGO;
            sObj.FindProperty("momoMascot").objectReferenceValue = momoMascotGO;
            sObj.FindProperty("fireworksParticleSystem").objectReferenceValue = fireworksGO;

            SerializedProperty pGlows = sObj.FindProperty("islandWorldGlowImages");
            pGlows.arraySize = 10;
            for (int g = 0; g < 10; g++)
            {
                pGlows.GetArrayElementAtIndex(g).objectReferenceValue = worldGlowImages[g];
            }

            // Phase 3: Badge & Certificate
            sObj.FindProperty("phonicsChampionBadgePopup").objectReferenceValue = badgePopupGO;
            sObj.FindProperty("certificateModalPopup").objectReferenceValue = certModalGO;
            sObj.FindProperty("certificateChildNameTMP").objectReferenceValue = childNameTMP;
            sObj.FindProperty("certificateWordCountTMP").objectReferenceValue = wcTMP;
            sObj.FindProperty("finishGameButton").objectReferenceValue = finishGameBtn;

            // Progress & Rewards
            sObj.FindProperty("progressRingFillImage").objectReferenceValue = progRingImage;
            sObj.FindProperty("progressText").objectReferenceValue = progText;
            sObj.FindProperty("starMeterRect").objectReferenceValue = starMeterRect;
            sObj.FindProperty("confettiParticles").objectReferenceValue = confettiGO;
            sObj.FindProperty("rewardPopup").objectReferenceValue = rewardPopupGO;
            sObj.FindProperty("stickerPopup").objectReferenceValue = stickerPopupGO;
            sObj.FindProperty("continueButton").objectReferenceValue = contBtnGO;
            sObj.FindProperty("currentPanel").objectReferenceValue = rootStop4;

            sObj.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = rootStop4;
            Debug.Log("[EngSnap] Successfully created 'Stop4_QuestionsAndGraduation' hierarchy with complete UI and auto-wired GraduationController!");
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
