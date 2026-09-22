#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit10.Editor
{
    public static class ICanSeeHierarchyBuilder
    {
        [MenuItem("EngSnap/Phonics2/Unit 10/Build Stop 1 (I Can See) Hierarchy in Active Scene", false, 102)]
        public static void BuildStop1HierarchyInScene()
        {
            // 1. Ensure or find Canvas
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

            // Ensure EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
            }

            // Find or create Unit10_UIPanel container if preferred, or build directly under Canvas
            Transform parentTransform = canvas.transform;
            Transform existingUnit10Panel = canvas.transform.Find("Unit10_UIPanel");
            if (existingUnit10Panel != null)
            {
                parentTransform = existingUnit10Panel;
            }

            // Check if Stop1 already exists
            Transform existingStop1 = parentTransform.Find("Stop1_ICanSee");
            if (existingStop1 != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Stop1_ICanSee Already Exists",
                    "A GameObject named 'Stop1_ICanSee' already exists in the scene. Do you want to replace it?",
                    "Replace",
                    "Cancel");

                if (!replace) return;
                Undo.DestroyObjectImmediate(existingStop1.gameObject);
            }

            // 2. Root Stop 1 GameObject
            GameObject rootStop1 = new GameObject("Stop1_ICanSee", typeof(RectTransform), typeof(CanvasGroup), typeof(ICanSeeController));
            rootStop1.transform.SetParent(parentTransform, false);
            RectTransform rootRect = rootStop1.GetComponent<RectTransform>();
            SetStretchAll(rootRect);
            Undo.RegisterCreatedObjectUndo(rootStop1, "Create Stop1_ICanSee");

            ICanSeeController controller = rootStop1.GetComponent<ICanSeeController>();

            // 3. Background
            GameObject bgGO = CreateUIElement("Background", rootStop1.transform, typeof(Image));
            SetStretchAll(bgGO.GetComponent<RectTransform>());
            Image bgImage = bgGO.GetComponent<Image>();
            bgImage.color = new Color(0.92f, 0.97f, 1.0f, 1f); // Soft airy sky tint

            // 4. TopBar
            GameObject topBarGO = CreateUIElement("TopBar", rootStop1.transform, typeof(RectTransform));
            RectTransform topBarRect = topBarGO.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = new Vector2(0f, 0f);
            topBarRect.sizeDelta = new Vector2(0f, 100f);

            // ProgressRing
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
            progRingImage.color = new Color(0.18f, 0.8f, 0.44f, 1f); // Emerald Green

            // ProgressText
            GameObject progTextGO = CreateUIElement("ProgressText", progRingGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(progTextGO.GetComponent<RectTransform>());
            TMP_Text progText = progTextGO.GetComponent<TextMeshProUGUI>();
            progText.text = "0%";
            progText.fontSize = 22;
            progText.fontStyle = FontStyles.Bold;
            progText.alignment = TextAlignmentOptions.Center;
            progText.color = new Color(0.12f, 0.35f, 0.18f, 1f);

            // StarMeter
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
                RectTransform sRect = starIcon.GetComponent<RectTransform>();
                sRect.sizeDelta = new Vector2(50f, 50f);
                Image sImg = starIcon.GetComponent<Image>();
                sImg.color = new Color(1f, 0.84f, 0.0f, 1f); // Gold
            }

            // 5. Leo Mascot Container
            GameObject leoContainerGO = CreateUIElement("LeoMascotContainer", rootStop1.transform, typeof(RectTransform));
            RectTransform leoContRect = leoContainerGO.GetComponent<RectTransform>();
            leoContRect.anchorMin = new Vector2(0f, 0f);
            leoContRect.anchorMax = new Vector2(0f, 0f);
            leoContRect.pivot = new Vector2(0f, 0f);
            leoContRect.anchoredPosition = new Vector2(80f, 60f);
            leoContRect.sizeDelta = new Vector2(300f, 380f);

            GameObject leoMascotGO = CreateUIElement("LeoMascot", leoContainerGO.transform, typeof(Image));
            SetStretchAll(leoMascotGO.GetComponent<RectTransform>());
            Image leoImg = leoMascotGO.GetComponent<Image>();
            leoImg.color = new Color(1f, 0.75f, 0.35f, 0.9f); // Mascot placeholder tint

            // 6. Phase 1: SentenceSetsPanel
            GameObject sentenceSetsPanelGO = CreateUIElement("SentenceSetsPanel", rootStop1.transform, typeof(Image));
            RectTransform setsPanelRect = sentenceSetsPanelGO.GetComponent<RectTransform>();
            setsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            setsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            setsPanelRect.pivot = new Vector2(0.5f, 0.5f);
            setsPanelRect.anchoredPosition = new Vector2(80f, 30f);
            setsPanelRect.sizeDelta = new Vector2(1300f, 620f);
            Image setsBg = sentenceSetsPanelGO.GetComponent<Image>();
            setsBg.color = new Color(1f, 1f, 1f, 0.85f); // Crisp semi-transparent white card

            // SetTitleTMP
            GameObject setTitleGO = CreateUIElement("SetTitleTMP", sentenceSetsPanelGO.transform, typeof(TextMeshProUGUI));
            RectTransform setTitleRect = setTitleGO.GetComponent<RectTransform>();
            setTitleRect.anchorMin = new Vector2(0.5f, 1f);
            setTitleRect.anchorMax = new Vector2(0.5f, 1f);
            setTitleRect.pivot = new Vector2(0.5f, 1f);
            setTitleRect.anchoredPosition = new Vector2(0f, -30f);
            setTitleRect.sizeDelta = new Vector2(800f, 50f);
            TMP_Text setTitleTMP = setTitleGO.GetComponent<TextMeshProUGUI>();
            setTitleTMP.text = "Ben with a pen";
            setTitleTMP.fontSize = 36;
            setTitleTMP.fontStyle = FontStyles.Bold;
            setTitleTMP.alignment = TextAlignmentOptions.Center;
            setTitleTMP.color = new Color(0.15f, 0.2f, 0.35f, 1f);

            // Smooth Meter Container & Fill
            GameObject smoothMeterGO = CreateUIElement("SmoothMeterContainer", sentenceSetsPanelGO.transform, typeof(Image));
            RectTransform smoothMeterRect = smoothMeterGO.GetComponent<RectTransform>();
            smoothMeterRect.anchorMin = new Vector2(1f, 1f);
            smoothMeterRect.anchorMax = new Vector2(1f, 1f);
            smoothMeterRect.pivot = new Vector2(1f, 1f);
            smoothMeterRect.anchoredPosition = new Vector2(-40f, -30f);
            smoothMeterRect.sizeDelta = new Vector2(240f, 36f);
            Image smoothBg = smoothMeterGO.GetComponent<Image>();
            smoothBg.color = new Color(0.85f, 0.88f, 0.92f, 1f);

            GameObject smoothFillGO = CreateUIElement("SmoothMeterFill", smoothMeterGO.transform, typeof(Image));
            SetStretchAll(smoothFillGO.GetComponent<RectTransform>());
            Image smoothMeterFill = smoothFillGO.GetComponent<Image>();
            smoothMeterFill.type = Image.Type.Filled;
            smoothMeterFill.fillMethod = Image.FillMethod.Horizontal;
            smoothMeterFill.fillAmount = 0.15f;
            smoothMeterFill.color = new Color(0.2f, 0.75f, 0.65f, 1f); // Smooth Cyan

            // 3 Line Containers
            GameObject[] lineContainers = new GameObject[3];
            TMP_Text[] lineTexts = new TMP_Text[3];
            Image[] lineIllustrations = new Image[3];
            Button[] lineSpeakerButtons = new Button[3];

            string[] defaultLines = new string[] {
                "I can see Ben.",
                "I can see a pen.",
                "I can see Ben with a pen."
            };
            float[] lineYPositions = new float[] { 90f, -35f, -160f };

            for (int i = 0; i < 3; i++)
            {
                GameObject lineGO = CreateUIElement($"LineContainer_{i}", sentenceSetsPanelGO.transform, typeof(Image));
                RectTransform lineRect = lineGO.GetComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.anchoredPosition = new Vector2(0f, lineYPositions[i]);
                lineRect.sizeDelta = new Vector2(1160f, 110f);
                Image lineCardBg = lineGO.GetComponent<Image>();
                lineCardBg.color = new Color(0.96f, 0.97f, 1.0f, 0.95f);

                // Illustration
                GameObject illGO = CreateUIElement($"LineIllustration_{i}", lineGO.transform, typeof(Image));
                RectTransform illRect = illGO.GetComponent<RectTransform>();
                illRect.anchorMin = new Vector2(0f, 0.5f);
                illRect.anchorMax = new Vector2(0f, 0.5f);
                illRect.pivot = new Vector2(0f, 0.5f);
                illRect.anchoredPosition = new Vector2(25f, 0f);
                illRect.sizeDelta = new Vector2(90f, 90f);
                Image illImg = illGO.GetComponent<Image>();
                illImg.color = new Color(0.85f, 0.9f, 0.98f, 1f);

                // Text
                GameObject textGO = CreateUIElement($"LineText_{i}", lineGO.transform, typeof(TextMeshProUGUI));
                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot = new Vector2(0f, 0.5f);
                textRect.offsetMin = new Vector2(140f, 10f);
                textRect.offsetMax = new Vector2(-120f, -10f);
                TMP_Text lineTMP = textGO.GetComponent<TextMeshProUGUI>();
                lineTMP.text = defaultLines[i];
                lineTMP.fontSize = 38;
                lineTMP.fontStyle = FontStyles.Bold;
                lineTMP.alignment = TextAlignmentOptions.MidlineLeft;
                lineTMP.color = new Color(0.12f, 0.15f, 0.25f, 1f);

                // Speaker Button
                GameObject spkGO = CreateUIElement($"LineSpeakerButton_{i}", lineGO.transform, typeof(Image), typeof(Button));
                RectTransform spkRect = spkGO.GetComponent<RectTransform>();
                spkRect.anchorMin = new Vector2(1f, 0.5f);
                spkRect.anchorMax = new Vector2(1f, 0.5f);
                spkRect.pivot = new Vector2(1f, 0.5f);
                spkRect.anchoredPosition = new Vector2(-25f, 0f);
                spkRect.sizeDelta = new Vector2(70f, 70f);
                Image spkImg = spkGO.GetComponent<Image>();
                spkImg.color = new Color(0.3f, 0.6f, 0.95f, 1f);
                Button spkBtn = spkGO.GetComponent<Button>();

                // Speaker icon label
                GameObject spkIcon = CreateUIElement("IconTMP", spkGO.transform, typeof(TextMeshProUGUI));
                SetStretchAll(spkIcon.GetComponent<RectTransform>());
                TMP_Text spkIconTMP = spkIcon.GetComponent<TextMeshProUGUI>();
                spkIconTMP.text = "🔊";
                spkIconTMP.fontSize = 30;
                spkIconTMP.alignment = TextAlignmentOptions.Center;

                lineContainers[i] = lineGO;
                lineTexts[i] = lineTMP;
                lineIllustrations[i] = illImg;
                lineSpeakerButtons[i] = spkBtn;
            }

            // Reread Set Button
            GameObject rereadGO = CreateUIElement("RereadSetButton", sentenceSetsPanelGO.transform, typeof(Image), typeof(Button));
            RectTransform rereadRect = rereadGO.GetComponent<RectTransform>();
            rereadRect.anchorMin = new Vector2(0.5f, 0f);
            rereadRect.anchorMax = new Vector2(0.5f, 0f);
            rereadRect.pivot = new Vector2(0.5f, 0f);
            rereadRect.anchoredPosition = new Vector2(-150f, 25f);
            rereadRect.sizeDelta = new Vector2(230f, 65f);
            rereadGO.GetComponent<Image>().color = new Color(0.9f, 0.93f, 0.98f, 1f);
            Button rereadBtn = rereadGO.GetComponent<Button>();
            GameObject rereadTMPGO = CreateUIElement("Text", rereadGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(rereadTMPGO.GetComponent<RectTransform>());
            TMP_Text rereadTMP = rereadTMPGO.GetComponent<TextMeshProUGUI>();
            rereadTMP.text = "Read Again 🔄";
            rereadTMP.fontSize = 24;
            rereadTMP.fontStyle = FontStyles.Bold;
            rereadTMP.alignment = TextAlignmentOptions.Center;
            rereadTMP.color = new Color(0.2f, 0.3f, 0.5f, 1f);
            rereadGO.SetActive(false);

            // Next Set Button
            GameObject nextSetGO = CreateUIElement("NextSetButton", sentenceSetsPanelGO.transform, typeof(Image), typeof(Button));
            RectTransform nextSetRect = nextSetGO.GetComponent<RectTransform>();
            nextSetRect.anchorMin = new Vector2(0.5f, 0f);
            nextSetRect.anchorMax = new Vector2(0.5f, 0f);
            nextSetRect.pivot = new Vector2(0.5f, 0f);
            nextSetRect.anchoredPosition = new Vector2(150f, 25f);
            nextSetRect.sizeDelta = new Vector2(230f, 65f);
            nextSetGO.GetComponent<Image>().color = new Color(0.25f, 0.75f, 0.45f, 1f);
            Button nextSetBtn = nextSetGO.GetComponent<Button>();
            GameObject nextSetTMPGO = CreateUIElement("Text", nextSetGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(nextSetTMPGO.GetComponent<RectTransform>());
            TMP_Text nextSetTMP = nextSetTMPGO.GetComponent<TextMeshProUGUI>();
            nextSetTMP.text = "Next Set ➔";
            nextSetTMP.fontSize = 24;
            nextSetTMP.fontStyle = FontStyles.Bold;
            nextSetTMP.alignment = TextAlignmentOptions.Center;
            nextSetTMP.color = Color.white;
            nextSetGO.SetActive(false);

            // 7. Phase 2: MakeYourOwnPanel (Inactive initially)
            GameObject makeYourOwnGO = CreateUIElement("MakeYourOwnPanel", rootStop1.transform, typeof(Image));
            RectTransform myoRect = makeYourOwnGO.GetComponent<RectTransform>();
            myoRect.anchorMin = new Vector2(0.5f, 0.5f);
            myoRect.anchorMax = new Vector2(0.5f, 0.5f);
            myoRect.pivot = new Vector2(0.5f, 0.5f);
            myoRect.anchoredPosition = new Vector2(80f, 30f);
            myoRect.sizeDelta = new Vector2(1300f, 620f);
            makeYourOwnGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

            // Prompt TMP
            GameObject myoPromptGO = CreateUIElement("MakeYourOwnPromptTMP", makeYourOwnGO.transform, typeof(TextMeshProUGUI));
            RectTransform myoPromptRect = myoPromptGO.GetComponent<RectTransform>();
            myoPromptRect.anchorMin = new Vector2(0.5f, 1f);
            myoPromptRect.anchorMax = new Vector2(0.5f, 1f);
            myoPromptRect.pivot = new Vector2(0.5f, 1f);
            myoPromptRect.anchoredPosition = new Vector2(0f, -30f);
            myoPromptRect.sizeDelta = new Vector2(1000f, 50f);
            TMP_Text myoPromptTMP = myoPromptGO.GetComponent<TextMeshProUGUI>();
            myoPromptTMP.text = "Now make your own. Pick a picture and read your sentence out loud!";
            myoPromptTMP.fontSize = 32;
            myoPromptTMP.fontStyle = FontStyles.Bold;
            myoPromptTMP.alignment = TextAlignmentOptions.Center;
            myoPromptTMP.color = new Color(0.15f, 0.2f, 0.35f, 1f);

            // Sentence Drop Area
            GameObject dropAreaGO = CreateUIElement("SentenceDropArea", makeYourOwnGO.transform, typeof(Image));
            RectTransform dropRect = dropAreaGO.GetComponent<RectTransform>();
            dropRect.anchorMin = new Vector2(0.5f, 0.5f);
            dropRect.anchorMax = new Vector2(0.5f, 0.5f);
            dropRect.pivot = new Vector2(0.5f, 0.5f);
            dropRect.anchoredPosition = new Vector2(0f, 75f);
            dropRect.sizeDelta = new Vector2(1000f, 130f);
            dropAreaGO.GetComponent<Image>().color = new Color(0.94f, 0.97f, 1f, 1f);

            GameObject myoSentenceTMPGO = CreateUIElement("MakeYourOwnSentenceTMP", dropAreaGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(myoSentenceTMPGO.GetComponent<RectTransform>());
            TMP_Text myoSentenceTMP = myoSentenceTMPGO.GetComponent<TextMeshProUGUI>();
            myoSentenceTMP.text = "I can see a ____";
            myoSentenceTMP.fontSize = 44;
            myoSentenceTMP.fontStyle = FontStyles.Bold;
            myoSentenceTMP.alignment = TextAlignmentOptions.Center;
            myoSentenceTMP.color = new Color(0.15f, 0.25f, 0.45f, 1f);

            GameObject myoDropSlotGO = CreateUIElement("MakeYourOwnDropSlotImage", dropAreaGO.transform, typeof(Image));
            RectTransform dropSlotRect = myoDropSlotGO.GetComponent<RectTransform>();
            dropSlotRect.anchorMin = new Vector2(1f, 0.5f);
            dropSlotRect.anchorMax = new Vector2(1f, 0.5f);
            dropSlotRect.pivot = new Vector2(1f, 0.5f);
            dropSlotRect.anchoredPosition = new Vector2(-40f, 0f);
            dropSlotRect.sizeDelta = new Vector2(100f, 100f);
            Image myoDropSlotImage = myoDropSlotGO.GetComponent<Image>();
            myoDropSlotImage.color = Color.white;
            myoDropSlotGO.SetActive(false);

            // Choices Grid Container
            GameObject choicesGridGO = CreateUIElement("ChoicesGridContainer", makeYourOwnGO.transform, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            RectTransform choicesRect = choicesGridGO.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0.5f, 0.5f);
            choicesRect.anchorMax = new Vector2(0.5f, 0.5f);
            choicesRect.pivot = new Vector2(0.5f, 0.5f);
            choicesRect.anchoredPosition = new Vector2(0f, -90f);
            choicesRect.sizeDelta = new Vector2(960f, 150f);
            HorizontalLayoutGroup choiceLayout = choicesGridGO.GetComponent<HorizontalLayoutGroup>();
            choiceLayout.spacing = 30f;
            choiceLayout.childAlignment = TextAnchor.MiddleCenter;
            choiceLayout.childControlWidth = false;
            choiceLayout.childControlHeight = false;

            Button[] choicePictureButtons = new Button[4];
            Image[] choicePictureImages = new Image[4];
            TMP_Text[] choicePictureTexts = new TMP_Text[4];
            string[] choiceWords = new string[] { "cat", "dog", "pig", "bug" };

            for (int c = 0; c < 4; c++)
            {
                GameObject cardGO = CreateUIElement($"ChoicePicture_{c}", choicesGridGO.transform, typeof(Image), typeof(Button));
                RectTransform cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(210f, 140f);
                cardGO.GetComponent<Image>().color = new Color(0.97f, 0.98f, 1f, 1f);
                Button cardBtn = cardGO.GetComponent<Button>();

                // Picture
                GameObject picGO = CreateUIElement("Image", cardGO.transform, typeof(Image));
                RectTransform picRect = picGO.GetComponent<RectTransform>();
                picRect.anchorMin = new Vector2(0.5f, 0.5f);
                picRect.anchorMax = new Vector2(0.5f, 0.5f);
                picRect.pivot = new Vector2(0.5f, 0.5f);
                picRect.anchoredPosition = new Vector2(0f, 18f);
                picRect.sizeDelta = new Vector2(75f, 75f);
                Image picImg = picGO.GetComponent<Image>();
                picImg.color = new Color(0.8f, 0.85f, 0.95f, 1f);

                // Word Label
                GameObject wordGO = CreateUIElement("Text", cardGO.transform, typeof(TextMeshProUGUI));
                RectTransform wordRect = wordGO.GetComponent<RectTransform>();
                wordRect.anchorMin = new Vector2(0f, 0f);
                wordRect.anchorMax = new Vector2(1f, 0f);
                wordRect.pivot = new Vector2(0.5f, 0f);
                wordRect.anchoredPosition = new Vector2(0f, 10f);
                wordRect.sizeDelta = new Vector2(0f, 35f);
                TMP_Text wordTMP = wordGO.GetComponent<TextMeshProUGUI>();
                wordTMP.text = choiceWords[c];
                wordTMP.fontSize = 26;
                wordTMP.fontStyle = FontStyles.Bold;
                wordTMP.alignment = TextAlignmentOptions.Center;
                wordTMP.color = new Color(0.2f, 0.25f, 0.4f, 1f);

                choicePictureButtons[c] = cardBtn;
                choicePictureImages[c] = picImg;
                choicePictureTexts[c] = wordTMP;
            }

            // Read My Sentence Button
            GameObject readMySentenceGO = CreateUIElement("ReadMySentenceButton", makeYourOwnGO.transform, typeof(Image), typeof(Button));
            RectTransform readMySentenceRect = readMySentenceGO.GetComponent<RectTransform>();
            readMySentenceRect.anchorMin = new Vector2(0.5f, 0f);
            readMySentenceRect.anchorMax = new Vector2(0.5f, 0f);
            readMySentenceRect.pivot = new Vector2(0.5f, 0f);
            readMySentenceRect.anchoredPosition = new Vector2(0f, 25f);
            readMySentenceRect.sizeDelta = new Vector2(300f, 65f);
            readMySentenceGO.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.4f, 1f);
            Button readMySentenceBtn = readMySentenceGO.GetComponent<Button>();
            GameObject readMySentenceTMPGO = CreateUIElement("Text", readMySentenceGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(readMySentenceTMPGO.GetComponent<RectTransform>());
            TMP_Text readMySentenceTMP = readMySentenceTMPGO.GetComponent<TextMeshProUGUI>();
            readMySentenceTMP.text = "Read Aloud 🔊";
            readMySentenceTMP.fontSize = 26;
            readMySentenceTMP.fontStyle = FontStyles.Bold;
            readMySentenceTMP.alignment = TextAlignmentOptions.Center;
            readMySentenceTMP.color = Color.white;
            readMySentenceGO.SetActive(false);

            makeYourOwnGO.SetActive(false); // Inactive initially

            // 8. DialogueUI
            GameObject dialogueUIGO = CreateUIElement("DialogueUI", rootStop1.transform, typeof(Image), typeof(CanvasGroup));
            RectTransform dialRect = dialogueUIGO.GetComponent<RectTransform>();
            dialRect.anchorMin = new Vector2(0.5f, 0f);
            dialRect.anchorMax = new Vector2(0.5f, 0f);
            dialRect.pivot = new Vector2(0.5f, 0f);
            dialRect.anchoredPosition = new Vector2(0f, 30f);
            dialRect.sizeDelta = new Vector2(1050f, 100f);
            dialogueUIGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
            CanvasGroup dialogueCanvasGroup = dialogueUIGO.GetComponent<CanvasGroup>();

            GameObject dialTMPGO = CreateUIElement("DialogueTMP", dialogueUIGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(dialTMPGO.GetComponent<RectTransform>());
            dialTMPGO.GetComponent<RectTransform>().offsetMin = new Vector2(25f, 10f);
            dialTMPGO.GetComponent<RectTransform>().offsetMax = new Vector2(-25f, -10f);
            TMP_Text dialogueTMP = dialTMPGO.GetComponent<TextMeshProUGUI>();
            dialogueTMP.text = "Today we read lots of lines — and we read them SMOOTHLY.";
            dialogueTMP.fontSize = 30;
            dialogueTMP.alignment = TextAlignmentOptions.Center;
            dialogueTMP.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            // 9. AudioSources
            GameObject audioSourcesGO = CreateUIElement("AudioSources", rootStop1.transform, typeof(RectTransform));
            AudioSource voiceSource = audioSourcesGO.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;
            AudioSource sfxSource = audioSourcesGO.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            // 10. RewardsContainer
            GameObject rewardsGO = CreateUIElement("RewardsContainer", rootStop1.transform, typeof(RectTransform));
            SetStretchAll(rewardsGO.GetComponent<RectTransform>());

            GameObject confettiGO = CreateUIElement("ConfettiParticles", rewardsGO.transform, typeof(RectTransform));
            confettiGO.SetActive(false);

            GameObject rewardPopupGO = CreateUIElement("RewardPopup", rewardsGO.transform, typeof(Image));
            RectTransform rPopRect = rewardPopupGO.GetComponent<RectTransform>();
            rPopRect.anchorMin = new Vector2(0.5f, 0.5f);
            rPopRect.anchorMax = new Vector2(0.5f, 0.5f);
            rPopRect.pivot = new Vector2(0.5f, 0.5f);
            rPopRect.sizeDelta = new Vector2(500f, 350f);
            rewardPopupGO.GetComponent<Image>().color = new Color(1f, 0.95f, 0.8f, 0.98f);
            rewardPopupGO.SetActive(false);

            GameObject stickerPopupGO = CreateUIElement("StickerPopup", rewardsGO.transform, typeof(Image));
            RectTransform sPopRect = stickerPopupGO.GetComponent<RectTransform>();
            sPopRect.anchorMin = new Vector2(0.5f, 0.5f);
            sPopRect.anchorMax = new Vector2(0.5f, 0.5f);
            sPopRect.pivot = new Vector2(0.5f, 0.5f);
            sPopRect.sizeDelta = new Vector2(400f, 300f);
            stickerPopupGO.GetComponent<Image>().color = new Color(0.9f, 1f, 0.9f, 0.98f);
            stickerPopupGO.SetActive(false);

            GameObject continueBtnGO = CreateUIElement("ContinueButton", rewardsGO.transform, typeof(Image), typeof(Button));
            RectTransform contRect = continueBtnGO.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0.5f);
            contRect.anchorMax = new Vector2(0.5f, 0.5f);
            contRect.pivot = new Vector2(0.5f, 0.5f);
            contRect.anchoredPosition = new Vector2(0f, -220f);
            contRect.sizeDelta = new Vector2(320f, 75f);
            continueBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.45f, 1f);
            GameObject contTMPGO = CreateUIElement("Text", continueBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(contTMPGO.GetComponent<RectTransform>());
            TMP_Text contTMP = contTMPGO.GetComponent<TextMeshProUGUI>();
            contTMP.text = "Continue to Stop 2 ➔";
            contTMP.fontSize = 26;
            contTMP.fontStyle = FontStyles.Bold;
            contTMP.alignment = TextAlignmentOptions.Center;
            contTMP.color = Color.white;
            continueBtnGO.SetActive(false);

            // 11. Wire serialized fields on ICanSeeController via SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);

            serializedController.FindProperty("unitID").stringValue = "Unit10";
            serializedController.FindProperty("topicName").stringValue = "ICanSee";

            // Load activity data if present
            ICanSeeData dataAsset = Resources.Load<ICanSeeData>("Phonics2/Unit10/ICanSeeData_Unit10");
            if (dataAsset != null)
            {
                serializedController.FindProperty("activityData").objectReferenceValue = dataAsset;
            }

            serializedController.FindProperty("voiceAudioSource").objectReferenceValue = voiceSource;
            serializedController.FindProperty("sfxAudioSource").objectReferenceValue = sfxSource;
            serializedController.FindProperty("dialogueText").objectReferenceValue = dialogueTMP;
            serializedController.FindProperty("dialogueCanvasGroup").objectReferenceValue = dialogueCanvasGroup;

            // Phase 1
            serializedController.FindProperty("sentenceSetsPanel").objectReferenceValue = sentenceSetsPanelGO;
            serializedController.FindProperty("setTitleTMP").objectReferenceValue = setTitleTMP;
            serializedController.FindProperty("smoothMeterFillImage").objectReferenceValue = smoothMeterFill;
            serializedController.FindProperty("nextSetButton").objectReferenceValue = nextSetBtn;
            serializedController.FindProperty("rereadSetButton").objectReferenceValue = rereadBtn;

            SerializedProperty propLineContainers = serializedController.FindProperty("lineContainers");
            SerializedProperty propLineTexts = serializedController.FindProperty("lineTexts");
            SerializedProperty propLineIllustrations = serializedController.FindProperty("lineIllustrations");
            SerializedProperty propLineSpeakerButtons = serializedController.FindProperty("lineSpeakerButtons");

            propLineContainers.arraySize = 3;
            propLineTexts.arraySize = 3;
            propLineIllustrations.arraySize = 3;
            propLineSpeakerButtons.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                propLineContainers.GetArrayElementAtIndex(i).objectReferenceValue = lineContainers[i];
                propLineTexts.GetArrayElementAtIndex(i).objectReferenceValue = lineTexts[i];
                propLineIllustrations.GetArrayElementAtIndex(i).objectReferenceValue = lineIllustrations[i];
                propLineSpeakerButtons.GetArrayElementAtIndex(i).objectReferenceValue = lineSpeakerButtons[i];
            }

            // Phase 2
            serializedController.FindProperty("makeYourOwnPanel").objectReferenceValue = makeYourOwnGO;
            serializedController.FindProperty("makeYourOwnPromptTMP").objectReferenceValue = myoPromptTMP;
            serializedController.FindProperty("makeYourOwnDropSlotImage").objectReferenceValue = myoDropSlotImage;
            serializedController.FindProperty("makeYourOwnSentenceTMP").objectReferenceValue = myoSentenceTMP;
            serializedController.FindProperty("readMySentenceButton").objectReferenceValue = readMySentenceBtn;

            SerializedProperty propChoiceBtns = serializedController.FindProperty("choicePictureButtons");
            SerializedProperty propChoiceImgs = serializedController.FindProperty("choicePictureImages");
            SerializedProperty propChoiceTxts = serializedController.FindProperty("choicePictureTexts");

            propChoiceBtns.arraySize = 4;
            propChoiceImgs.arraySize = 4;
            propChoiceTxts.arraySize = 4;

            for (int c = 0; c < 4; c++)
            {
                propChoiceBtns.GetArrayElementAtIndex(c).objectReferenceValue = choicePictureButtons[c];
                propChoiceImgs.GetArrayElementAtIndex(c).objectReferenceValue = choicePictureImages[c];
                propChoiceTxts.GetArrayElementAtIndex(c).objectReferenceValue = choicePictureTexts[c];
            }

            // Progress & Mascots
            serializedController.FindProperty("progressRingFillImage").objectReferenceValue = progRingImage;
            serializedController.FindProperty("progressText").objectReferenceValue = progText;
            serializedController.FindProperty("starMeterRect").objectReferenceValue = starMeterRect;
            serializedController.FindProperty("leoMascotObject").objectReferenceValue = leoContainerGO;

            // Rewards & Panels
            serializedController.FindProperty("confettiParticles").objectReferenceValue = confettiGO;
            serializedController.FindProperty("rewardPopup").objectReferenceValue = rewardPopupGO;
            serializedController.FindProperty("stickerPopup").objectReferenceValue = stickerPopupGO;
            serializedController.FindProperty("continueButton").objectReferenceValue = continueBtnGO;
            serializedController.FindProperty("currentPanel").objectReferenceValue = rootStop1;

            // Link Next Panel (Stop 2) if it exists
            Transform stop2Transform = parentTransform.Find("Stop2_TheEllFamily");
            if (stop2Transform != null)
            {
                serializedController.FindProperty("nextPanel").objectReferenceValue = stop2Transform.gameObject;
            }

            serializedController.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = rootStop1;

            Debug.Log("[EngSnap] Successfully created 'Stop1_ICanSee' hierarchy with complete UI and auto-wired ICanSeeController in active scene!");
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
