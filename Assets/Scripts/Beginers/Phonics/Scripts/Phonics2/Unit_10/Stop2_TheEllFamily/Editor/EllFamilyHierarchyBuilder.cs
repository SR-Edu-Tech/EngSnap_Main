#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit10.Editor
{
    public static class EllFamilyHierarchyBuilder
    {
        [MenuItem("EngSnap/Phonics2/Unit 10/Build Stop 2 (The Ell Family) Hierarchy in Active Scene", false, 103)]
        public static void BuildStop2HierarchyInScene()
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

            Transform existingStop2 = parentTransform.Find("Stop2_TheEllFamily");
            if (existingStop2 != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Stop2_TheEllFamily Already Exists",
                    "A GameObject named 'Stop2_TheEllFamily' already exists in the scene. Replace it?",
                    "Replace",
                    "Cancel");

                if (!replace) return;
                Undo.DestroyObjectImmediate(existingStop2.gameObject);
            }

            GameObject rootStop2 = new GameObject("Stop2_TheEllFamily", typeof(RectTransform), typeof(CanvasGroup), typeof(EllFamilyController));
            rootStop2.transform.SetParent(parentTransform, false);
            SetStretchAll(rootStop2.GetComponent<RectTransform>());
            Undo.RegisterCreatedObjectUndo(rootStop2, "Create Stop2_TheEllFamily");

            EllFamilyController controller = rootStop2.GetComponent<EllFamilyController>();

            // 1. Background
            GameObject bgGO = CreateUIElement("Background", rootStop2.transform, typeof(Image));
            SetStretchAll(bgGO.GetComponent<RectTransform>());
            bgGO.GetComponent<Image>().color = new Color(0.96f, 0.94f, 0.98f, 1f); // Soft lavender/workshop tint

            // 2. TopBar
            GameObject topBarGO = CreateUIElement("TopBar", rootStop2.transform, typeof(RectTransform));
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

            // 3. Leo Mascot Container
            GameObject leoContainerGO = CreateUIElement("LeoMascotContainer", rootStop2.transform, typeof(RectTransform));
            RectTransform leoContRect = leoContainerGO.GetComponent<RectTransform>();
            leoContRect.anchorMin = new Vector2(0f, 0f);
            leoContRect.anchorMax = new Vector2(0f, 0f);
            leoContRect.pivot = new Vector2(0f, 0f);
            leoContRect.anchoredPosition = new Vector2(80f, 60f);
            leoContRect.sizeDelta = new Vector2(300f, 380f);

            GameObject leoMascotGO = CreateUIElement("LeoMascot", leoContainerGO.transform, typeof(Image));
            SetStretchAll(leoMascotGO.GetComponent<RectTransform>());
            leoMascotGO.GetComponent<Image>().color = new Color(1f, 0.75f, 0.35f, 0.9f);

            // 4. Phase 1: Swap Machine Panel
            GameObject swapMachinePanelGO = CreateUIElement("SwapMachinePanel", rootStop2.transform, typeof(Image));
            RectTransform swapRect = swapMachinePanelGO.GetComponent<RectTransform>();
            swapRect.anchorMin = new Vector2(0.5f, 0.5f);
            swapRect.anchorMax = new Vector2(0.5f, 0.5f);
            swapRect.pivot = new Vector2(0.5f, 0.5f);
            swapRect.anchoredPosition = new Vector2(80f, 30f);
            swapRect.sizeDelta = new Vector2(1200f, 580f);
            swapMachinePanelGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

            // Machine Title
            GameObject machTitleGO = CreateUIElement("TitleTMP", swapMachinePanelGO.transform, typeof(TextMeshProUGUI));
            RectTransform machTitleRect = machTitleGO.GetComponent<RectTransform>();
            machTitleRect.anchorMin = new Vector2(0.5f, 1f);
            machTitleRect.anchorMax = new Vector2(0.5f, 1f);
            machTitleRect.pivot = new Vector2(0.5f, 1f);
            machTitleRect.anchoredPosition = new Vector2(0f, -25f);
            machTitleRect.sizeDelta = new Vector2(800f, 45f);
            TMP_Text machTitleTMP = machTitleGO.GetComponent<TextMeshProUGUI>();
            machTitleTMP.text = "The -ell Word Family Machine";
            machTitleTMP.fontSize = 32;
            machTitleTMP.fontStyle = FontStyles.Bold;
            machTitleTMP.alignment = TextAlignmentOptions.Center;
            machTitleTMP.color = new Color(0.2f, 0.2f, 0.4f, 1f);

            // Onset Slot
            GameObject onsetSlotGO = CreateUIElement("OnsetSlot", swapMachinePanelGO.transform, typeof(Image));
            RectTransform onsetRect = onsetSlotGO.GetComponent<RectTransform>();
            onsetRect.anchorMin = new Vector2(0.5f, 0.5f);
            onsetRect.anchorMax = new Vector2(0.5f, 0.5f);
            onsetRect.pivot = new Vector2(0.5f, 0.5f);
            onsetRect.anchoredPosition = new Vector2(-240f, 80f);
            onsetRect.sizeDelta = new Vector2(160f, 160f);
            onsetSlotGO.GetComponent<Image>().color = new Color(0.9f, 0.95f, 1f, 1f);

            GameObject onsetTMPGO = CreateUIElement("OnsetLetterTMP", onsetSlotGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(onsetTMPGO.GetComponent<RectTransform>());
            TMP_Text onsetTMP = onsetTMPGO.GetComponent<TextMeshProUGUI>();
            onsetTMP.text = "w";
            onsetTMP.fontSize = 72;
            onsetTMP.fontStyle = FontStyles.Bold;
            onsetTMP.alignment = TextAlignmentOptions.Center;
            onsetTMP.color = new Color(0.1f, 0.35f, 0.7f, 1f);

            // Plus Sign
            GameObject plusSignGO = CreateUIElement("PlusTMP", swapMachinePanelGO.transform, typeof(TextMeshProUGUI));
            RectTransform plusRect = plusSignGO.GetComponent<RectTransform>();
            plusRect.anchorMin = new Vector2(0.5f, 0.5f);
            plusRect.anchorMax = new Vector2(0.5f, 0.5f);
            plusRect.pivot = new Vector2(0.5f, 0.5f);
            plusRect.anchoredPosition = new Vector2(-120f, 80f);
            plusRect.sizeDelta = new Vector2(60f, 60f);
            TMP_Text plusTMP = plusSignGO.GetComponent<TextMeshProUGUI>();
            plusTMP.text = "+";
            plusTMP.fontSize = 54;
            plusTMP.alignment = TextAlignmentOptions.Center;
            plusTMP.color = new Color(0.5f, 0.5f, 0.6f, 1f);

            // Double L Slot
            GameObject doubleLSlotGO = CreateUIElement("DoubleLSlot", swapMachinePanelGO.transform, typeof(Image));
            RectTransform doubleLRect = doubleLSlotGO.GetComponent<RectTransform>();
            doubleLRect.anchorMin = new Vector2(0.5f, 0.5f);
            doubleLRect.anchorMax = new Vector2(0.5f, 0.5f);
            doubleLRect.pivot = new Vector2(0.5f, 0.5f);
            doubleLRect.anchoredPosition = new Vector2(20f, 80f);
            doubleLRect.sizeDelta = new Vector2(180f, 160f);
            doubleLSlotGO.GetComponent<Image>().color = new Color(0.92f, 1f, 0.94f, 1f);

            GameObject doubleLTMPGO = CreateUIElement("DoubleLLetterTMP", doubleLSlotGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(doubleLTMPGO.GetComponent<RectTransform>());
            TMP_Text doubleLTMP = doubleLTMPGO.GetComponent<TextMeshProUGUI>();
            doubleLTMP.text = "ell";
            doubleLTMP.fontSize = 72;
            doubleLTMP.fontStyle = FontStyles.Bold;
            doubleLTMP.alignment = TextAlignmentOptions.Center;
            doubleLTMP.color = new Color(0.1f, 0.6f, 0.3f, 1f);

            // Word Illustration
            GameObject wordIllGO = CreateUIElement("WordIllustrationImage", swapMachinePanelGO.transform, typeof(Image));
            RectTransform wordIllRect = wordIllGO.GetComponent<RectTransform>();
            wordIllRect.anchorMin = new Vector2(0.5f, 0.5f);
            wordIllRect.anchorMax = new Vector2(0.5f, 0.5f);
            wordIllRect.pivot = new Vector2(0.5f, 0.5f);
            wordIllRect.anchoredPosition = new Vector2(280f, 80f);
            wordIllRect.sizeDelta = new Vector2(180f, 180f);
            Image wordIllImg = wordIllGO.GetComponent<Image>();
            wordIllImg.color = new Color(0.85f, 0.9f, 0.98f, 1f);

            // Blended Word Result Slot
            GameObject blendedWordSlotGO = CreateUIElement("BlendedWordSlot", swapMachinePanelGO.transform, typeof(Image));
            RectTransform blendedRect = blendedWordSlotGO.GetComponent<RectTransform>();
            blendedRect.anchorMin = new Vector2(0.5f, 0.5f);
            blendedRect.anchorMax = new Vector2(0.5f, 0.5f);
            blendedRect.pivot = new Vector2(0.5f, 0.5f);
            blendedRect.anchoredPosition = new Vector2(-100f, -80f);
            blendedRect.sizeDelta = new Vector2(400f, 110f);
            blendedWordSlotGO.GetComponent<Image>().color = new Color(0.95f, 0.97f, 1f, 1f);

            GameObject blendedWordTMPGO = CreateUIElement("BlendedWordTMP", blendedWordSlotGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(blendedWordTMPGO.GetComponent<RectTransform>());
            TMP_Text blendedWordTMP = blendedWordTMPGO.GetComponent<TextMeshProUGUI>();
            blendedWordTMP.text = "well";
            blendedWordTMP.fontSize = 52;
            blendedWordTMP.fontStyle = FontStyles.Bold;
            blendedWordTMP.alignment = TextAlignmentOptions.Center;
            blendedWordTMP.color = new Color(0.1f, 0.2f, 0.45f, 1f);

            // Blend Lever Button
            GameObject leverBtnGO = CreateUIElement("BlendLeverButton", swapMachinePanelGO.transform, typeof(Image), typeof(Button));
            RectTransform leverRect = leverBtnGO.GetComponent<RectTransform>();
            leverRect.anchorMin = new Vector2(0.5f, 0.5f);
            leverRect.anchorMax = new Vector2(0.5f, 0.5f);
            leverRect.pivot = new Vector2(0.5f, 0.5f);
            leverRect.anchoredPosition = new Vector2(200f, -80f);
            leverRect.sizeDelta = new Vector2(180f, 90f);
            leverBtnGO.GetComponent<Image>().color = new Color(0.95f, 0.6f, 0.2f, 1f);
            Button leverBtn = leverBtnGO.GetComponent<Button>();
            GameObject leverTMPGO = CreateUIElement("Text", leverBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(leverTMPGO.GetComponent<RectTransform>());
            TMP_Text leverTMP = leverTMPGO.GetComponent<TextMeshProUGUI>();
            leverTMP.text = "Pull Lever 🕹️";
            leverTMP.fontSize = 24;
            leverTMP.fontStyle = FontStyles.Bold;
            leverTMP.alignment = TextAlignmentOptions.Center;
            leverTMP.color = Color.white;

            // Next Word Button
            GameObject nextWordGO = CreateUIElement("NextWordButton", swapMachinePanelGO.transform, typeof(Image), typeof(Button));
            RectTransform nextWordRect = nextWordGO.GetComponent<RectTransform>();
            nextWordRect.anchorMin = new Vector2(0.5f, 0f);
            nextWordRect.anchorMax = new Vector2(0.5f, 0f);
            nextWordRect.pivot = new Vector2(0.5f, 0f);
            nextWordRect.anchoredPosition = new Vector2(0f, 25f);
            nextWordRect.sizeDelta = new Vector2(240f, 65f);
            nextWordGO.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.45f, 1f);
            Button nextWordBtn = nextWordGO.GetComponent<Button>();
            GameObject nextWordTMPGO = CreateUIElement("Text", nextWordGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(nextWordTMPGO.GetComponent<RectTransform>());
            TMP_Text nextWordTMP = nextWordTMPGO.GetComponent<TextMeshProUGUI>();
            nextWordTMP.text = "Next Word ➔";
            nextWordTMP.fontSize = 26;
            nextWordTMP.fontStyle = FontStyles.Bold;
            nextWordTMP.alignment = TextAlignmentOptions.Center;
            nextWordTMP.color = Color.white;
            nextWordGO.SetActive(false);

            // 5. Phase 2: MeetDellPanel (Inactive initially)
            GameObject meetDellPanelGO = CreateUIElement("MeetDellPanel", rootStop2.transform, typeof(Image));
            RectTransform dellRect = meetDellPanelGO.GetComponent<RectTransform>();
            dellRect.anchorMin = new Vector2(0.5f, 0.5f);
            dellRect.anchorMax = new Vector2(0.5f, 0.5f);
            dellRect.pivot = new Vector2(0.5f, 0.5f);
            dellRect.anchoredPosition = new Vector2(80f, 30f);
            dellRect.sizeDelta = new Vector2(1100f, 540f);
            meetDellPanelGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);

            GameObject dellImgGO = CreateUIElement("DellCharacterImage", meetDellPanelGO.transform, typeof(Image));
            RectTransform dellImgRect = dellImgGO.GetComponent<RectTransform>();
            dellImgRect.anchorMin = new Vector2(0.5f, 0.5f);
            dellImgRect.anchorMax = new Vector2(0.5f, 0.5f);
            dellImgRect.pivot = new Vector2(0.5f, 0.5f);
            dellImgRect.anchoredPosition = new Vector2(-220f, 40f);
            dellImgRect.sizeDelta = new Vector2(260f, 300f);
            Image dellCharImg = dellImgGO.GetComponent<Image>();
            dellCharImg.color = new Color(0.9f, 0.85f, 1f, 1f);

            GameObject dellTMPGO = CreateUIElement("DellCharacterTMP", meetDellPanelGO.transform, typeof(TextMeshProUGUI));
            RectTransform dellTMPRect = dellTMPGO.GetComponent<RectTransform>();
            dellTMPRect.anchorMin = new Vector2(0.5f, 0.5f);
            dellTMPRect.anchorMax = new Vector2(0.5f, 0.5f);
            dellTMPRect.pivot = new Vector2(0.5f, 0.5f);
            dellTMPRect.anchoredPosition = new Vector2(180f, 90f);
            dellTMPRect.sizeDelta = new Vector2(400f, 80f);
            TMP_Text dellTMP = dellTMPGO.GetComponent<TextMeshProUGUI>();
            dellTMP.text = "Dell";
            dellTMP.fontSize = 68;
            dellTMP.fontStyle = FontStyles.Bold;
            dellTMP.alignment = TextAlignmentOptions.Center;
            dellTMP.color = new Color(0.2f, 0.15f, 0.45f, 1f);

            GameObject dellPronounceBtnGO = CreateUIElement("DellPronounceButton", meetDellPanelGO.transform, typeof(Image), typeof(Button));
            RectTransform dellPronRect = dellPronounceBtnGO.GetComponent<RectTransform>();
            dellPronRect.anchorMin = new Vector2(0.5f, 0.5f);
            dellPronRect.anchorMax = new Vector2(0.5f, 0.5f);
            dellPronRect.pivot = new Vector2(0.5f, 0.5f);
            dellPronRect.anchoredPosition = new Vector2(180f, -10f);
            dellPronRect.sizeDelta = new Vector2(220f, 70f);
            dellPronounceBtnGO.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.95f, 1f);
            Button dellPronBtn = dellPronounceBtnGO.GetComponent<Button>();
            GameObject dellPronTMPGO = CreateUIElement("Text", dellPronounceBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(dellPronTMPGO.GetComponent<RectTransform>());
            TMP_Text dellPronTMP = dellPronTMPGO.GetComponent<TextMeshProUGUI>();
            dellPronTMP.text = "Say Dell 🔊";
            dellPronTMP.fontSize = 24;
            dellPronTMP.fontStyle = FontStyles.Bold;
            dellPronTMP.alignment = TextAlignmentOptions.Center;
            dellPronTMP.color = Color.white;

            GameObject nextToWallBtnGO = CreateUIElement("NextToWallButton", meetDellPanelGO.transform, typeof(Image), typeof(Button));
            RectTransform nextToWallRect = nextToWallBtnGO.GetComponent<RectTransform>();
            nextToWallRect.anchorMin = new Vector2(0.5f, 0f);
            nextToWallRect.anchorMax = new Vector2(0.5f, 0f);
            nextToWallRect.pivot = new Vector2(0.5f, 0f);
            nextToWallRect.anchoredPosition = new Vector2(0f, 30f);
            nextToWallRect.sizeDelta = new Vector2(300f, 70f);
            nextToWallBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.45f, 1f);
            Button nextToWallBtn = nextToWallBtnGO.GetComponent<Button>();
            GameObject nextToWallTMPGO = CreateUIElement("Text", nextToWallBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(nextToWallTMPGO.GetComponent<RectTransform>());
            TMP_Text nextToWallTMP = nextToWallTMPGO.GetComponent<TextMeshProUGUI>();
            nextToWallTMP.text = "Read Family Wall ➔";
            nextToWallTMP.fontSize = 24;
            nextToWallTMP.fontStyle = FontStyles.Bold;
            nextToWallTMP.alignment = TextAlignmentOptions.Center;
            nextToWallTMP.color = Color.white;

            meetDellPanelGO.SetActive(false);

            // 6. Phase 3: FamilyWallPanel (Inactive initially)
            GameObject familyWallPanelGO = CreateUIElement("FamilyWallPanel", rootStop2.transform, typeof(Image));
            RectTransform wallRect = familyWallPanelGO.GetComponent<RectTransform>();
            wallRect.anchorMin = new Vector2(0.5f, 0.5f);
            wallRect.anchorMax = new Vector2(0.5f, 0.5f);
            wallRect.pivot = new Vector2(0.5f, 0.5f);
            wallRect.anchoredPosition = new Vector2(80f, 30f);
            wallRect.sizeDelta = new Vector2(1200f, 560f);
            familyWallPanelGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

            GameObject wallTitleGO = CreateUIElement("WallTitleTMP", familyWallPanelGO.transform, typeof(TextMeshProUGUI));
            RectTransform wallTitleRect = wallTitleGO.GetComponent<RectTransform>();
            wallTitleRect.anchorMin = new Vector2(0.5f, 1f);
            wallTitleRect.anchorMax = new Vector2(0.5f, 1f);
            wallTitleRect.pivot = new Vector2(0.5f, 1f);
            wallTitleRect.anchoredPosition = new Vector2(0f, -25f);
            wallTitleRect.sizeDelta = new Vector2(800f, 45f);
            TMP_Text wallTitleTMP = wallTitleGO.GetComponent<TextMeshProUGUI>();
            wallTitleTMP.text = "Tap & Read All the Words!";
            wallTitleTMP.fontSize = 32;
            wallTitleTMP.fontStyle = FontStyles.Bold;
            wallTitleTMP.alignment = TextAlignmentOptions.Center;
            wallTitleTMP.color = new Color(0.15f, 0.2f, 0.4f, 1f);

            GameObject wordsContainerGO = CreateUIElement("WordButtonsContainer", familyWallPanelGO.transform, typeof(RectTransform), typeof(GridLayoutGroup));
            RectTransform wordsContRect = wordsContainerGO.GetComponent<RectTransform>();
            wordsContRect.anchorMin = new Vector2(0.5f, 0.5f);
            wordsContRect.anchorMax = new Vector2(0.5f, 0.5f);
            wordsContRect.pivot = new Vector2(0.5f, 0.5f);
            wordsContRect.anchoredPosition = new Vector2(0f, 20f);
            wordsContRect.sizeDelta = new Vector2(1050f, 300f);
            GridLayoutGroup wallLayout = wordsContainerGO.GetComponent<GridLayoutGroup>();
            wallLayout.cellSize = new Vector2(310f, 120f);
            wallLayout.spacing = new Vector2(35f, 30f);
            wallLayout.childAlignment = TextAnchor.MiddleCenter;

            Button[] wallWordButtons = new Button[6];
            TMP_Text[] wallWordTexts = new TMP_Text[6];
            Image[] wallWordHighlights = new Image[6];
            string[] ellWordList = new string[] { "well", "bell", "fell", "tell", "yell", "sell" };

            for (int w = 0; w < 6; w++)
            {
                GameObject wBtnGO = CreateUIElement($"WallWord_{w}", wordsContainerGO.transform, typeof(Image), typeof(Button));
                wBtnGO.GetComponent<Image>().color = new Color(0.96f, 0.97f, 1f, 1f);
                Button wBtn = wBtnGO.GetComponent<Button>();

                GameObject wHighGO = CreateUIElement("Highlight", wBtnGO.transform, typeof(Image));
                SetStretchAll(wHighGO.GetComponent<RectTransform>());
                Image wHigh = wHighGO.GetComponent<Image>();
                wHigh.color = new Color(0.2f, 0.8f, 0.4f, 0.4f);
                wHighGO.SetActive(false);

                GameObject wTxtGO = CreateUIElement("Text", wBtnGO.transform, typeof(TextMeshProUGUI));
                SetStretchAll(wTxtGO.GetComponent<RectTransform>());
                TMP_Text wTxt = wTxtGO.GetComponent<TextMeshProUGUI>();
                wTxt.text = ellWordList[w];
                wTxt.fontSize = 44;
                wTxt.fontStyle = FontStyles.Bold;
                wTxt.alignment = TextAlignmentOptions.Center;
                wTxt.color = new Color(0.12f, 0.18f, 0.35f, 1f);

                wallWordButtons[w] = wBtn;
                wallWordTexts[w] = wTxt;
                wallWordHighlights[w] = wHigh;
            }

            GameObject finishWallBtnGO = CreateUIElement("FinishFamilyWallButton", familyWallPanelGO.transform, typeof(Image), typeof(Button));
            RectTransform finishWallRect = finishWallBtnGO.GetComponent<RectTransform>();
            finishWallRect.anchorMin = new Vector2(0.5f, 0f);
            finishWallRect.anchorMax = new Vector2(0.5f, 0f);
            finishWallRect.pivot = new Vector2(0.5f, 0f);
            finishWallRect.anchoredPosition = new Vector2(0f, 25f);
            finishWallRect.sizeDelta = new Vector2(320f, 70f);
            finishWallBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.45f, 1f);
            Button finishWallBtn = finishWallBtnGO.GetComponent<Button>();
            GameObject finishWallTMPGO = CreateUIElement("Text", finishWallBtnGO.transform, typeof(TextMeshProUGUI));
            SetStretchAll(finishWallTMPGO.GetComponent<RectTransform>());
            TMP_Text finishWallTMP = finishWallTMPGO.GetComponent<TextMeshProUGUI>();
            finishWallTMP.text = "Ready for Story! ➔";
            finishWallTMP.fontSize = 24;
            finishWallTMP.fontStyle = FontStyles.Bold;
            finishWallTMP.alignment = TextAlignmentOptions.Center;
            finishWallTMP.color = Color.white;
            finishWallBtnGO.SetActive(false);

            familyWallPanelGO.SetActive(false);

            // 7. DialogueUI
            GameObject dialogueUIGO = CreateUIElement("DialogueUI", rootStop2.transform, typeof(Image), typeof(CanvasGroup));
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
            dialTMP.text = "Remember s-c-oo-p, when two letters held hands? Look — two l's!";
            dialTMP.fontSize = 30;
            dialTMP.alignment = TextAlignmentOptions.Center;
            dialTMP.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            // 8. AudioSources
            GameObject audioSourcesGO = CreateUIElement("AudioSources", rootStop2.transform, typeof(RectTransform));
            AudioSource vSrc = audioSourcesGO.AddComponent<AudioSource>();
            vSrc.playOnAwake = false;
            vSrc.spatialBlend = 0f;
            AudioSource sSrc = audioSourcesGO.AddComponent<AudioSource>();
            sSrc.playOnAwake = false;
            sSrc.spatialBlend = 0f;

            // 9. RewardsContainer
            GameObject rewardsGO = CreateUIElement("RewardsContainer", rootStop2.transform, typeof(RectTransform));
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
            contTMP.text = "Read The Story ➔";
            contTMP.fontSize = 26;
            contTMP.fontStyle = FontStyles.Bold;
            contTMP.alignment = TextAlignmentOptions.Center;
            contTMP.color = Color.white;
            contBtnGO.SetActive(false);

            // 10. Auto-wire Serialized Properties
            SerializedObject sObj = new SerializedObject(controller);
            sObj.FindProperty("unitID").stringValue = "Unit10";
            sObj.FindProperty("topicName").stringValue = "TheEllFamily";

            EllFamilyData dataAsset = Resources.Load<EllFamilyData>("Phonics2/Unit10/EllFamilyData_Unit10");
            if (dataAsset != null) sObj.FindProperty("activityData").objectReferenceValue = dataAsset;

            sObj.FindProperty("voiceAudioSource").objectReferenceValue = vSrc;
            sObj.FindProperty("sfxAudioSource").objectReferenceValue = sSrc;
            sObj.FindProperty("dialogueText").objectReferenceValue = dialTMP;
            sObj.FindProperty("dialogueCanvasGroup").objectReferenceValue = dialCG;

            // Phase 1
            sObj.FindProperty("swapMachinePanel").objectReferenceValue = swapMachinePanelGO;
            sObj.FindProperty("onsetLetterTMP").objectReferenceValue = onsetTMP;
            sObj.FindProperty("doubleLLetterTMP").objectReferenceValue = doubleLTMP;
            sObj.FindProperty("blendedWordTMP").objectReferenceValue = blendedWordTMP;
            sObj.FindProperty("wordIllustrationImage").objectReferenceValue = wordIllImg;
            sObj.FindProperty("blendLeverButton").objectReferenceValue = leverBtn;
            sObj.FindProperty("nextWordButton").objectReferenceValue = nextWordBtn;

            // Phase 2
            sObj.FindProperty("meetDellPanel").objectReferenceValue = meetDellPanelGO;
            sObj.FindProperty("dellCharacterTMP").objectReferenceValue = dellTMP;
            sObj.FindProperty("dellCharacterImage").objectReferenceValue = dellCharImg;
            sObj.FindProperty("dellPronounceButton").objectReferenceValue = dellPronBtn;
            sObj.FindProperty("nextToWallButton").objectReferenceValue = nextToWallBtn;

            // Phase 3
            sObj.FindProperty("familyWallPanel").objectReferenceValue = familyWallPanelGO;
            sObj.FindProperty("finishFamilyWallButton").objectReferenceValue = finishWallBtn;

            SerializedProperty pWallBtns = sObj.FindProperty("wallWordButtons");
            SerializedProperty pWallTxts = sObj.FindProperty("wallWordTexts");
            SerializedProperty pWallHighs = sObj.FindProperty("wallWordHighlights");
            pWallBtns.arraySize = 6;
            pWallTxts.arraySize = 6;
            pWallHighs.arraySize = 6;

            for (int i = 0; i < 6; i++)
            {
                pWallBtns.GetArrayElementAtIndex(i).objectReferenceValue = wallWordButtons[i];
                pWallTxts.GetArrayElementAtIndex(i).objectReferenceValue = wallWordTexts[i];
                pWallHighs.GetArrayElementAtIndex(i).objectReferenceValue = wallWordHighlights[i];
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
            sObj.FindProperty("currentPanel").objectReferenceValue = rootStop2;

            Transform stop3Transform = parentTransform.Find("Stop3_TheDogInTheWell");
            if (stop3Transform != null) sObj.FindProperty("nextPanel").objectReferenceValue = stop3Transform.gameObject;

            sObj.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = rootStop2;
            Debug.Log("[EngSnap] Successfully created 'Stop2_TheEllFamily' hierarchy with complete UI and auto-wired EllFamilyController!");
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
