# Unit 10 — Reading Peak: Complete Unity Hierarchy Setup Guide

This guide is the definitive, step-by-step master reference for creating and configuring the entire **Unit 10 (Reading Peak)** hierarchy in Unity. It covers all 4 stops, RectTransform coordinates, layout groups, component requirements, and exact inspector wire-up tables.

---

## 1. Canvas & Canvas Scaler Baseline

All Unit 10 UI panels are designed on a 16:9 full HD canvas reference:

* **Canvas:**
  * **Render Mode:** `Screen Space - Overlay` (or `Screen Space - Camera`)
* **Canvas Scaler:**
  * **UI Scale Mode:** `Scale With Screen Size`
  * **Reference Resolution:** `X: 1920, Y: 1080`
  * **Screen Match Mode:** `Match Width Or Height`
  * **Match:** `0.5`
* **Graphic Raycaster:** Enabled (Default)

---

## 2. Master Unit 10 Multi-Panel Architecture

```text
Canvas (1920 x 1080)
└── [Panel] Unit10_UIPanel                               [RectTransform: Stretch All (0,0,0,0)]
    │
    ├── [Panel] Stop1_ICanSee                           <-- [ICanSeeController, Active by default]
    ├── [Panel] Stop2_TheEllFamily                      <-- [EllFamilyController, Inactive initially]
    ├── [Panel] Stop3_TheDogInTheWell                   <-- [DogInTheWellController, Inactive initially]
    └── [Panel] Stop4_QuestionsAndGraduation            <-- [GraduationController, Inactive initially]
```

---

## 3. Stop 1 — "I Can See" Fluency & Sentence Maker

### 3.1 Hierarchy Tree Overview

```text
Stop1_ICanSee                                           [ICanSeeController, CanvasGroup, RectTransform: Stretch All]
├── [Image] Background                                  [Image: Soft Sky/Meadow, Stretch All]
│
├── [Empty] TopBar                                      [RectTransform: Top Stretch, PosY: -60, H: 100]
│   ├── [Image] ProgressRing                            [Image: Type=Filled, Radial 360, Color=Emerald Green, PosX: -840, Size: 70x70]
│   ├── [TMP] ProgressText                              [TextMeshProUGUI: "0%", Font Size: 24, Bold]
│   └── [Rect] StarMeter                                [RectTransform: Right Anchored, PosX: -120, Size: 220x60]
│
├── [Empty] LeoMascotContainer                          [RectTransform: Bottom-Left, PosX: 180, PosY: 180, Size: 300x380]
│   └── [Image] LeoMascot                               [Image / Mascot Animator, Size: 280x360]
│
├── [Panel] SentenceSetsPanel                           [RectTransform: Middle Center, PosY: 40, Size: 1300x620]
│   ├── [TMP] SetTitleTMP                               [TextMeshProUGUI: Top Center, PosY: -40, Font Size: 36, Bold]
│   ├── [Empty] SmoothMeterContainer                    [RectTransform: Top Right, PosX: -80, PosY: -40, Size: 240x36]
│   │   ├── [Image] SmoothMeterBg                       [Image: Dark Outline / Capsule]
│   │   └── [Image] SmoothMeterFill                     [Image: Type=Filled, Horizontal, Color=Cyan/Amber]
│   ├── [Panel] LineContainer_0                         [RectTransform: PosY: 100, Size: 1100x120, HorizontalLayoutGroup]
│   │   ├── [Image] LineIllustration_0                  [Image: Size: 100x100]
│   │   ├── [TMP] LineText_0                            [TextMeshProUGUI: "I can see Ben.", Font Size: 38]
│   │   └── [Button] LineSpeakerButton_0                [Button + Speaker Icon, Size: 70x70]
│   ├── [Panel] LineContainer_1                         [RectTransform: PosY: -30, Size: 1100x120, HorizontalLayoutGroup]
│   │   ├── [Image] LineIllustration_1                  [Image: Size: 100x100]
│   │   ├── [TMP] LineText_1                            [TextMeshProUGUI: "I can see a pen.", Font Size: 38]
│   │   └── [Button] LineSpeakerButton_1                [Button + Speaker Icon, Size: 70x70]
│   ├── [Panel] LineContainer_2                         [RectTransform: PosY: -160, Size: 1100x120, HorizontalLayoutGroup]
│   │   ├── [Image] LineIllustration_2                  [Image: Size: 100x100]
│   │   ├── [TMP] LineText_2                            [TextMeshProUGUI: "I can see Ben with a pen.", Font Size: 38]
│   │   └── [Button] LineSpeakerButton_2                [Button + Speaker Icon, Size: 70x70]
│   ├── [Button] RereadSetButton                        [Button + TMP "Read Again 🔄", PosX: -160, PosY: -260, Size: 220x65]
│   └── [Button] NextSetButton                          [Button + TMP "Next Set ➔", PosX: 160, PosY: -260, Size: 220x65]
│
├── [Panel] MakeYourOwnPanel                            [RectTransform: Middle Center, PosY: 40, Size: 1300x620, Inactive]
│   ├── [TMP] MakeYourOwnPromptTMP                      [TextMeshProUGUI: Top Center, PosY: -40, Font Size: 34]
│   ├── [Panel] SentenceDropArea                        [RectTransform: Center, PosY: 60, Size: 1000x140]
│   │   ├── [Image] DropAreaBg                          [Image: Rounded White Card]
│   │   ├── [Image] MakeYourOwnDropSlotImage            [Image: PosX: 260, Size: 100x100, Inactive]
│   │   └── [TMP] MakeYourOwnSentenceTMP                [TextMeshProUGUI: "I can see a ____", Font Size: 44, Bold]
│   ├── [Panel] ChoicesGridContainer                    [RectTransform: Center, PosY: -110, Size: 900x150, HorizontalLayoutGroup]
│   │   ├── [Button] ChoicePicture_0                    [Button + Image (cat) + TMP "cat", Size: 180x130]
│   │   ├── [Button] ChoicePicture_1                    [Button + Image (dog) + TMP "dog", Size: 180x130]
│   │   ├── [Button] ChoicePicture_2                    [Button + Image (pig) + TMP "pig", Size: 180x130]
│   │   └── [Button] ChoicePicture_3                    [Button + Image (bug) + TMP "bug", Size: 180x130]
│   └── [Button] ReadMySentenceButton                   [Button + TMP "Read Aloud 🔊", PosY: -240, Size: 280x70]
│
├── [Panel] DialogueUI                                  [CanvasGroup, Bottom Center, PosY: 70, Size: 1050x110]
│   ├── [Image] DialogueBubble                          [Image: Speech Bubble with 9-slice rounded corners]
│   └── [TMP] DialogueTMP                               [TextMeshProUGUI: Font Size: 32, Auto-wrap]
│
├── [Empty] AudioSources                                [Audio Components container]
│   ├── [AudioSource] VoiceAudioSource                  [2D, SpatialBlend: 0, PlayOnAwake: False]
│   └── [AudioSource] SFXAudioSource                    [2D, SpatialBlend: 0, PlayOnAwake: False]
│
└── [Panel] RewardsContainer                            [RectTransform: Stretch All, RaycastTarget: False]
    ├── [Particle] ConfettiParticles                    [ParticleSystem / UI Particle, Inactive]
    ├── [Panel] RewardPopup                             [Star celebration modal, Inactive]
    ├── [Panel] StickerPopup                            [Stop 1 trophy sticker modal, Inactive]
    └── [Button] ContinueButton                         [Button + TMP "Continue to Stop 2 ➔", PosY: -350, Inactive]
```

### 3.2 Inspector Wire-Up Table (`ICanSeeController`)

| Serialized Field | Reference Target |
| :--- | :--- |
| **Unit ID** | `"Unit10"` |
| **Topic Name** | `"ICanSee"` |
| **Activity Data** | `ICanSeeData_Unit10` |
| **Voice Audio Source** | `AudioSources/VoiceAudioSource` |
| **SFX Audio Source** | `AudioSources/SFXAudioSource` |
| **Dialogue Text** | `DialogueUI/DialogueTMP` |
| **Dialogue Canvas Group** | `DialogueUI` |
| **Sentence Sets Panel** | `SentenceSetsPanel` |
| **Set Title TMP** | `SentenceSetsPanel/SetTitleTMP` |
| **Line Containers `[0..2]`** | `LineContainer_0`, `LineContainer_1`, `LineContainer_2` |
| **Line Texts `[0..2]`** | `LineText_0`, `LineText_1`, `LineText_2` |
| **Line Illustrations `[0..2]`** | `LineIllustration_0`, `LineIllustration_1`, `LineIllustration_2` |
| **Line Speaker Buttons `[0..2]`**| `LineSpeakerButton_0`, `LineSpeakerButton_1`, `LineSpeakerButton_2` |
| **Smooth Meter Fill Image** | `SentenceSetsPanel/SmoothMeterContainer/SmoothMeterFill` |
| **Next Set Button** | `SentenceSetsPanel/NextSetButton` |
| **Reread Set Button** | `SentenceSetsPanel/RereadSetButton` |
| **Make Your Own Panel** | `MakeYourOwnPanel` |
| **Make Your Own Prompt TMP** | `MakeYourOwnPanel/MakeYourOwnPromptTMP` |
| **Make Your Own Drop Slot Image**| `MakeYourOwnPanel/SentenceDropArea/MakeYourOwnDropSlotImage` |
| **Make Your Own Sentence TMP** | `MakeYourOwnPanel/SentenceDropArea/MakeYourOwnSentenceTMP` |
| **Choice Picture Buttons `[0..3]`**| `ChoicePicture_0` .. `ChoicePicture_3` |
| **Choice Picture Images `[0..3]`** | `ChoicePicture_0/Image` .. `ChoicePicture_3/Image` |
| **Choice Picture Texts `[0..3]`** | `ChoicePicture_0/Text` .. `ChoicePicture_3/Text` |
| **Read My Sentence Button** | `MakeYourOwnPanel/ReadMySentenceButton` |
| **Progress Ring Fill Image** | `TopBar/ProgressRing` |
| **Progress Text** | `TopBar/ProgressText` |
| **Star Meter Rect** | `TopBar/StarMeter` |
| **Leo Mascot Object** | `LeoMascotContainer/LeoMascot` |
| **Confetti Particles** | `RewardsContainer/ConfettiParticles` |
| **Reward Popup** | `RewardsContainer/RewardPopup` |
| **Sticker Popup** | `RewardsContainer/StickerPopup` |
| **Continue Button** | `RewardsContainer/ContinueButton` |
| **Next Panel** | `Stop2_TheEllFamily` |
| **Current Panel** | `Stop1_ICanSee` |

---

## 4. Stop 2 — The -ell Family & Swap Machine

### 4.1 Hierarchy Tree Overview

```text
Stop2_TheEllFamily                                      [EllFamilyController, CanvasGroup, RectTransform: Stretch All]
├── [Image] Background                                  [Image: Phonics Workshop / Workshop Room, Stretch All]
│
├── [Empty] TopBar                                      [RectTransform: Top Stretch, PosY: -60, H: 100]
│   ├── [Image] ProgressRing                            [Image: Type=Filled, Radial 360, Color=Emerald Green]
│   ├── [TMP] ProgressText                              [TextMeshProUGUI: "0%"]
│   └── [Rect] StarMeter                                [RectTransform: Right Anchored, Size: 220x60]
│
├── [Empty] LeoMascotContainer                          [RectTransform: Bottom-Left, PosX: 180, PosY: 180, Size: 300x380]
│   └── [Image] LeoMascot                               [Image / Mascot Animator]
│
├── [Panel] SwapMachinePanel                            [RectTransform: Middle Center, PosY: 50, Size: 1200x560]
│   ├── [Image] MachineHousing                          [Image: Lever Machine frame]
│   ├── [Panel] OnsetSlot                               [Image: PosX: -260, Size: 180x180 + TMP OnsetLetterTMP: "w"]
│   ├── [Panel] PlusSign                                [TMP: "+", PosX: -110, Font Size: 50]
│   ├── [Panel] DoubleLSlot                             [Image: PosX: 50, Size: 220x180 + TMP DoubleLLetterTMP: "ell"]
│   ├── [Panel] BlendedWordSlot                         [Image: PosY: -120, Size: 440x120 + TMP BlendedWordTMP: "well"]
│   ├── [Image] WordIllustrationImage                   [Image: PosX: 380, PosY: 30, Size: 220x220]
│   ├── [Button] BlendLeverButton                       [Button + Image (Lever), PosX: 380, PosY: -120, Size: 160x100]
│   └── [Button] NextWordButton                         [Button + TMP "Next Word ➔", PosY: -230, Size: 240x65]
│
├── [Panel] MeetDellPanel                               [RectTransform: Middle Center, PosY: 50, Size: 1100x520, Inactive]
│   ├── [Image] DellCharacterImage                      [Image: Dell avatar with smiling face, Size: 280x320, PosX: -220]
│   ├── [TMP] DellCharacterTMP                          [TextMeshProUGUI: "Dell", Font Size: 64, Bold, PosX: 200, PosY: 60]
│   ├── [Button] DellPronounceButton                    [Button + Speaker Icon, PosX: 200, PosY: -30, Size: 200x70]
│   └── [Button] NextToWallButton                       [Button + TMP "Read Family Wall ➔", PosY: -180, Size: 280x70]
│
├── [Panel] FamilyWallPanel                             [RectTransform: Middle Center, PosY: 50, Size: 1200x540, Inactive]
│   ├── [Panel] WordButtonsContainer                    [GridLayoutGroup: 3 cols x 2 rows, Cell Size: 320x130, Spacing: 30x30]
│   │   ├── [Button] WallWord_0                         [Button + TMP "well" + Highlight Image]
│   │   ├── [Button] WallWord_1                         [Button + TMP "bell" + Highlight Image]
│   │   ├── [Button] WallWord_2                         [Button + TMP "fell" + Highlight Image]
│   │   ├── [Button] WallWord_3                         [Button + TMP "tell" + Highlight Image]
│   │   ├── [Button] WallWord_4                         [Button + TMP "yell" + Highlight Image]
│   │   └── [Button] WallWord_5                         [Button + TMP "sell" + Highlight Image]
│   └── [Button] FinishFamilyWallButton                 [Button + TMP "Ready for Story! ➔", PosY: -220, Size: 300x70]
│
├── [Panel] DialogueUI                                  [CanvasGroup, Bottom Center, PosY: 70, Size: 1050x110]
│   ├── [Image] DialogueBubble                          [Image: Speech Bubble]
│   └── [TMP] DialogueTMP                               [TextMeshProUGUI: Font Size: 32]
│
├── [Empty] AudioSources                                [Audio Components container]
│   ├── [AudioSource] VoiceAudioSource                  [2D, SpatialBlend: 0]
│   └── [AudioSource] SFXAudioSource                    [2D, SpatialBlend: 0]
│
└── [Panel] RewardsContainer                            [RectTransform: Stretch All]
    ├── [Particle] ConfettiParticles                    [ParticleSystem / UI Particle, Inactive]
    ├── [Panel] RewardPopup                             [Star modal, Inactive]
    ├── [Panel] StickerPopup                            [Stop 2 sticker modal, Inactive]
    └── [Button] ContinueButton                         [Button + TMP "Read The Story ➔", PosY: -350, Inactive]
```

### 4.2 Inspector Wire-Up Table (`EllFamilyController`)

| Serialized Field | Reference Target |
| :--- | :--- |
| **Unit ID** | `"Unit10"` |
| **Topic Name** | `"TheEllFamily"` |
| **Activity Data** | `EllFamilyData_Unit10` |
| **Voice Audio Source** | `AudioSources/VoiceAudioSource` |
| **SFX Audio Source** | `AudioSources/SFXAudioSource` |
| **Dialogue Text** | `DialogueUI/DialogueTMP` |
| **Dialogue Canvas Group** | `DialogueUI` |
| **Swap Machine Panel** | `SwapMachinePanel` |
| **Onset Letter TMP** | `SwapMachinePanel/OnsetSlot/OnsetLetterTMP` |
| **Double L Letter TMP** | `SwapMachinePanel/DoubleLSlot/DoubleLLetterTMP` |
| **Blended Word TMP** | `SwapMachinePanel/BlendedWordSlot/BlendedWordTMP` |
| **Word Illustration Image** | `SwapMachinePanel/WordIllustrationImage` |
| **Blend Lever Button** | `SwapMachinePanel/BlendLeverButton` |
| **Next Word Button** | `SwapMachinePanel/NextWordButton` |
| **Meet Dell Panel** | `MeetDellPanel` |
| **Dell Character TMP** | `MeetDellPanel/DellCharacterTMP` |
| **Dell Character Image** | `MeetDellPanel/DellCharacterImage` |
| **Dell Pronounce Button** | `MeetDellPanel/DellPronounceButton` |
| **Next To Wall Button** | `MeetDellPanel/NextToWallButton` |
| **Family Wall Panel** | `FamilyWallPanel` |
| **Wall Word Buttons `[0..5]`** | `FamilyWallPanel/WordButtonsContainer/WallWord_0` .. `WallWord_5` |
| **Wall Word Texts `[0..5]`** | `WallWord_0/Text` .. `WallWord_5/Text` |
| **Wall Word Highlights `[0..5]`**| `WallWord_0/Highlight` .. `WallWord_5/Highlight` |
| **Finish Family Wall Button** | `FamilyWallPanel/FinishFamilyWallButton` |
| **Progress Ring Fill Image** | `TopBar/ProgressRing` |
| **Progress Text** | `TopBar/ProgressText` |
| **Star Meter Rect** | `TopBar/StarMeter` |
| **Leo Mascot Object** | `LeoMascotContainer/LeoMascot` |
| **Confetti Particles** | `RewardsContainer/ConfettiParticles` |
| **Reward Popup** | `RewardsContainer/RewardPopup` |
| **Sticker Popup** | `RewardsContainer/StickerPopup` |
| **Continue Button** | `RewardsContainer/ContinueButton` |
| **Next Panel** | `Stop3_TheDogInTheWell` |
| **Current Panel** | `Stop2_TheEllFamily` |

---

## 5. Stop 3 — "The Dog in the Well" Story Reader

### 5.1 Hierarchy Tree Overview

```text
Stop3_TheDogInTheWell                                   [DogInTheWellController, CanvasGroup, RectTransform: Stretch All]
├── [Image] Background                                  [Image: Countryside / Well garden backdrop, Stretch All]
│
├── [Empty] TopBar                                      [RectTransform: Top Stretch, PosY: -60, H: 100]
│   ├── [Image] ProgressRing                            [Image: Type=Filled, Radial 360, Color=Emerald Green]
│   ├── [TMP] ProgressText                              [TextMeshProUGUI: "0%"]
│   └── [Rect] StarMeter                                [RectTransform: Right Anchored, Size: 220x60]
│
├── [Empty] LeoMascotContainer                          [RectTransform: Bottom-Left, PosX: 180, PosY: 180, Size: 300x380]
│   └── [Image] LeoMascot                               [Image / Mascot Animator]
│
├── [Panel] StoryPanel                                  [RectTransform: Middle Center, PosY: 40, Size: 1360x640]
│   ├── [Image] WellSceneImage                          [Image: Illustrated Scene Beat, PosY: 140, Size: 720x340]
│   ├── [Panel] LineReadingArea                         [RectTransform: PosY: -110, Size: 1300x160]
│   │   ├── [TMP] CurrentLineTMP                        [TextMeshProUGUI: Full Line Text, Font Size: 38, PosY: 40]
│   │   └── [Empty] WordsContainer                      [HorizontalLayoutGroup: Spacing: 15, PosY: -30]
│   │       ├── [Button] Word_0                         [Button + TMP + Highlight Glow Image]
│   │       ├── [Button] Word_1 ... Word_11             (12 Word Token Slots total)
│   ├── [Button] ReadLineHelpButton                     [Button + Speaker Icon, PosX: -540, PosY: -110, Size: 80x80]
│   └── [Button] NextLineTickButton                     [Button + Green Checkmark ✔, PosX: 540, PosY: -110, Size: 90x90]
│
├── [Panel] DialogueUI                                  [CanvasGroup, Bottom Center, PosY: 70, Size: 1050x110]
│   ├── [Image] DialogueBubble                          [Image: Speech Bubble]
│   └── [TMP] DialogueTMP                               [TextMeshProUGUI: Font Size: 32]
│
├── [Empty] AudioSources                                [Audio Components container]
│   ├── [AudioSource] VoiceAudioSource                  [2D, SpatialBlend: 0]
│   └── [AudioSource] SFXAudioSource                    [2D, SpatialBlend: 0]
│
└── [Panel] RewardsContainer                            [RectTransform: Stretch All]
    ├── [Particle] ConfettiParticles                    [ParticleSystem / UI Particle, Inactive]
    ├── [Panel] RewardPopup                             [Star modal, Inactive]
    ├── [Panel] StickerPopup                            [Stop 3 story trophy sticker modal, Inactive]
    └── [Button] ContinueButton                         [Button + TMP "Take The Graduation Quiz ➔", PosY: -350, Inactive]
```

### 5.2 Inspector Wire-Up Table (`DogInTheWellController`)

| Serialized Field | Reference Target |
| :--- | :--- |
| **Unit ID** | `"Unit10"` |
| **Topic Name** | `"TheDogInTheWell"` |
| **Activity Data** | `DogInTheWellData_Unit10` |
| **Voice Audio Source** | `AudioSources/VoiceAudioSource` |
| **SFX Audio Source** | `AudioSources/SFXAudioSource` |
| **Dialogue Text** | `DialogueUI/DialogueTMP` |
| **Dialogue Canvas Group** | `DialogueUI` |
| **Story Panel** | `StoryPanel` |
| **Well Scene Image** | `StoryPanel/WellSceneImage` |
| **Current Line TMP** | `StoryPanel/LineReadingArea/CurrentLineTMP` |
| **Word Buttons In Line `[0..11]`**| `StoryPanel/LineReadingArea/WordsContainer/Word_0` .. `Word_11` |
| **Word Texts In Line `[0..11]`** | `Word_0/Text` .. `Word_11/Text` |
| **Word Highlights In Line `[0..11]`**| `Word_0/Highlight` .. `Word_11/Highlight` |
| **Read Line Help Button** | `StoryPanel/ReadLineHelpButton` |
| **Next Line Tick Button** | `StoryPanel/NextLineTickButton` |
| **Progress Ring Fill Image** | `TopBar/ProgressRing` |
| **Progress Text** | `TopBar/ProgressText` |
| **Star Meter Rect** | `TopBar/StarMeter` |
| **Leo Mascot Object** | `LeoMascotContainer/LeoMascot` |
| **Confetti Particles** | `RewardsContainer/ConfettiParticles` |
| **Reward Popup** | `RewardsContainer/RewardPopup` |
| **Sticker Popup** | `RewardsContainer/StickerPopup` |
| **Continue Button** | `RewardsContainer/ContinueButton` |
| **Next Panel** | `Stop4_QuestionsAndGraduation` |
| **Current Panel** | `Stop3_TheDogInTheWell` |

---

## 6. Stop 4 — Questions & Grand Sound Island Graduation

### 6.1 Hierarchy Tree Overview

```text
Stop4_QuestionsAndGraduation                            [GraduationController, CanvasGroup, RectTransform: Stretch All]
├── [Image] Background                                  [Image: Grand Sound Island Horizon, Stretch All]
│
├── [Empty] TopBar                                      [RectTransform: Top Stretch, PosY: -60, H: 100]
│   ├── [Image] ProgressRing                            [Image: Type=Filled, Radial 360, Color=Emerald Green]
│   ├── [TMP] ProgressText                              [TextMeshProUGUI: "0%"]
│   └── [Rect] StarMeter                                [RectTransform: Right Anchored, Size: 220x60]
│
├── [Panel] QuestionsPanel                              [RectTransform: Middle Center, PosY: 40, Size: 1360x640]
│   ├── [Panel] PinnedStoryPanel                        [RectTransform: Top Stretch, PosY: -20, Size: 1300x260, VerticalLayoutGroup]
│   │   ├── [Image] PinnedStoryBg                       [Image: Parchment / White card backdrop]
│   │   ├── [Panel] PinnedLine_0                        [TMP PinnedStoryLineTexts[0] + Image Highlight]
│   │   ├── [Panel] PinnedLine_1 ... PinnedLine_7       (8 Pinned Story Lines total)
│   ├── [Panel] QuestionArea                            [RectTransform: Bottom Stretch, PosY: -40, Size: 1300x320]
│   │   ├── [TMP] QuestionPromptTMP                     [TextMeshProUGUI: "Who rang the bell?", Font Size: 36, Bold]
│   │   └── [Panel] ChoicesContainer                    [HorizontalLayoutGroup: Spacing: 30, Size: 1200x180]
│   │       ├── [Button] Choice_0                       [Button + ChoiceImage + ChoiceTMP, Size: 360x140]
│   │       ├── [Button] Choice_1                       [Button + ChoiceImage + ChoiceTMP, Size: 360x140]
│   │       └── [Button] Choice_2                       [Button + ChoiceImage + ChoiceTMP, Size: 360x140]
│
├── [Panel] GraduationPanel                             [RectTransform: Middle Center, Size: 1400x700, Inactive]
│   ├── [Image] SoundIslandMapBackground                [Image: Full Sound Island Map]
│   ├── [Empty] IslandWorldGlows                        [Container for 10 Glow Nodes]
│   │   ├── [Image] WorldGlow_0                         [Image: World 1 Glow (Unit 1), Inactive]
│   │   ├── [Image] WorldGlow_1 ... WorldGlow_9         (Worlds 2 to 10 Glows, Inactive)
│   ├── [Empty] AllFourMascotsContainer                 [RectTransform: Bottom Stretch, Size: 1200x320, Inactive]
│   │   ├── [Image] LeoMascot                           [Image: Leo cheering]
│   │   ├── [Image] GigiMascot                          [Image: Gigi cheering]
│   │   ├── [Image] TaraMascot                          [Image: Tara cheering]
│   │   └── [Image] MomoMascot                          [Image: Momo cheering]
│   └── [Particle] FireworksParticleSystem              [ParticleSystem: Colorful bursts, Inactive]
│
├── [Panel] PhonicsChampionBadgePopup                   [Modal overlay: Gold Champion Badge + Shimmer, Inactive]
│
├── [Panel] CertificateModalPopup                       [Modal overlay: Printable Certificate, Inactive]
│   ├── [Image] CertificateBorder                       [Image: Ornate Gold Frame]
│   ├── [TMP] CertificateTitleTMP                       [TextMeshProUGUI: "SOUND ISLAND GRADUATE"]
│   ├── [TMP] CertificateChildNameTMP                   [TextMeshProUGUI: "Star Reader", Font Size: 48, Bold]
│   ├── [TMP] CertificateWordCountTMP                   [TextMeshProUGUI: "100+ Words Mastered", Font Size: 28]
│   └── [Button] FinishGameButton                       [Button + TMP "Explore Island / Read Again ➔", PosY: -220, Size: 340x75]
│
├── [Panel] DialogueUI                                  [CanvasGroup, Bottom Center, PosY: 70, Size: 1050x110]
│   ├── [Image] DialogueBubble                          [Image: Speech Bubble]
│   └── [TMP] DialogueTMP                               [TextMeshProUGUI: Font Size: 32]
│
├── [Empty] AudioSources                                [Audio Components container]
│   ├── [AudioSource] VoiceAudioSource                  [2D, SpatialBlend: 0]
│   └── [AudioSource] SFXAudioSource                    [2D, SpatialBlend: 0]
│
└── [Panel] RewardsContainer                            [RectTransform: Stretch All]
    ├── [Particle] ConfettiParticles                    [ParticleSystem / UI Particle, Inactive]
    ├── [Panel] RewardPopup                             [Star modal, Inactive]
    ├── [Panel] StickerPopup                            [Graduation Crown sticker modal, Inactive]
    └── [Button] ContinueButton                         [Button + TMP "Finish ➔", PosY: -350, Inactive]
```

### 6.2 Inspector Wire-Up Table (`GraduationController`)

| Serialized Field | Reference Target |
| :--- | :--- |
| **Unit ID** | `"Unit10"` |
| **Topic Name** | `"Graduation"` |
| **Activity Data** | `GraduationData_Unit10` |
| **Voice Audio Source** | `AudioSources/VoiceAudioSource` |
| **SFX Audio Source** | `AudioSources/SFXAudioSource` |
| **Dialogue Text** | `DialogueUI/DialogueTMP` |
| **Dialogue Canvas Group** | `DialogueUI` |
| **Questions Panel** | `QuestionsPanel` |
| **Pinned Story Panel** | `QuestionsPanel/PinnedStoryPanel` |
| **Pinned Story Line Texts `[0..7]`**| `PinnedLine_0/Text` .. `PinnedLine_7/Text` |
| **Pinned Story Line Highlights `[0..7]`**| `PinnedLine_0/Highlight` .. `PinnedLine_7/Highlight` |
| **Question Prompt TMP** | `QuestionsPanel/QuestionArea/QuestionPromptTMP` |
| **Question Choice Buttons `[0..2]`**| `QuestionsPanel/QuestionArea/ChoicesContainer/Choice_0` .. `Choice_2` |
| **Question Choice Texts `[0..2]`** | `Choice_0/Text` .. `Choice_2/Text` |
| **Question Choice Images `[0..2]`**| `Choice_0/Image` .. `Choice_2/Image` |
| **Graduation Panel** | `GraduationPanel` |
| **Island World Glow Images `[0..9]`**| `GraduationPanel/IslandWorldGlows/WorldGlow_0` .. `WorldGlow_9` |
| **All Four Mascots Container** | `GraduationPanel/AllFourMascotsContainer` |
| **Leo Mascot** | `GraduationPanel/AllFourMascotsContainer/LeoMascot` |
| **Gigi Mascot** | `GraduationPanel/AllFourMascotsContainer/GigiMascot` |
| **Tara Mascot** | `GraduationPanel/AllFourMascotsContainer/TaraMascot` |
| **Momo Mascot** | `GraduationPanel/AllFourMascotsContainer/MomoMascot` |
| **Fireworks Particle System** | `GraduationPanel/FireworksParticleSystem` |
| **Phonics Champion Badge Popup** | `PhonicsChampionBadgePopup` |
| **Certificate Modal Popup** | `CertificateModalPopup` |
| **Certificate Child Name TMP** | `CertificateModalPopup/CertificateChildNameTMP` |
| **Certificate Word Count TMP** | `CertificateModalPopup/CertificateWordCountTMP` |
| **Finish Game Button** | `CertificateModalPopup/FinishGameButton` |
| **Progress Ring Fill Image** | `TopBar/ProgressRing` |
| **Progress Text** | `TopBar/ProgressText` |
| **Star Meter Rect** | `TopBar/StarMeter` |
| **Confetti Particles** | `RewardsContainer/ConfettiParticles` |
| **Reward Popup** | `RewardsContainer/RewardPopup` |
| **Sticker Popup** | `RewardsContainer/StickerPopup` |
| **Continue Button** | `RewardsContainer/ContinueButton` |
| **Next Panel** | `null` (or Main Island Map Panel) |
| **Current Panel** | `Stop4_QuestionsAndGraduation` |

---

## 7. Inter-Stop Navigation & State Progression Summary

| Stop Index | Stop GameObject | Controller Component | Trigger to Next | Next Target |
| :---: | :--- | :--- | :--- | :--- |
| **Stop 1** | `Stop1_ICanSee` | `ICanSeeController` | `CompleteStop1()` ➔ `ContinueButton` | `Stop2_TheEllFamily` |
| **Stop 2** | `Stop2_TheEllFamily` | `EllFamilyController` | `CompleteStop2()` ➔ `ContinueButton` | `Stop3_TheDogInTheWell` |
| **Stop 3** | `Stop3_TheDogInTheWell` | `DogInTheWellController` | `CompleteStop3()` ➔ `ContinueButton` | `Stop4_QuestionsAndGraduation` |
| **Stop 4** | `Stop4_QuestionsAndGraduation` | `GraduationController` | `StartGraduationCeremony()` ➔ `FinishGameButton` | Sound Island Map / Course Complete |
