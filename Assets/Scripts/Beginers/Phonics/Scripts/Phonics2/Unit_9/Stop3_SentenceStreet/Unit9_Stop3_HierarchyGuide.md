# Unit 9 Stop 3 — Sentence Street: Unity Hierarchy Setup Guide

This guide details the exact step-by-step GameObject structure, RectTransform coordinates, components, and inspector assignments required to build **Unit 9 Stop 3 (Sentence Street)** in Unity (identical structure to Unit 8 Stop 3).

---

## 1. Canvas & Canvas Scaler Baseline

Ensure your Canvas uses standard UI reference settings:
* **Render Mode:** Screen Space - Overlay (or Camera)
* **Canvas Scaler:**
  * **UI Scale Mode:** `Scale With Screen Size`
  * **Reference Resolution:** `X: 1920, Y: 1080`
  * **Screen Match Mode:** `Match Width Or Height`
  * **Match:** `0.5`

---

## 2. Complete Hierarchy Tree Overview

```text
Canvas (or Unit9_UIPanel)
└── [Panel] Stop3_SentenceStreet                 <-- [SentenceStreetController, CanvasGroup, RectTransform: Stretch All]
    │
    ├── [Image] Background                       <-- [Image: Color/Sprite, RectTransform: Stretch All]
    │
    ├── [Empty] TopBar                           <-- [RectTransform: Top Stretch, PosY: -70, H: 120]
    │   ├── [Image] ProgressRing                 <-- [Image: Type=Filled, Radial 360, Color=Emerald Green]
    │   ├── [TMP] ProgressText                   <-- [TextMeshProUGUI: "0%", Font Size: 28, Bold]
    │   └── [Rect] StarMeter                     <-- [RectTransform: Right Anchored, Size: 220x60, 3 Star icons]
    │
    ├── [Empty] LeoMascotContainer               <-- [RectTransform: Bottom-Left, PosX: 180, PosY: 180, Size: 300x380]
    │   └── [Image] LeoMascot                    <-- [Image / Spine animation of Leo]
    │
    ├── [Panel] SightWordPocketUI                <-- [SightWordPocketUI, RectTransform: Top-Right]
    │   ├── [Button] PocketOpenButton            <-- [Button + Image (Pouch Icon), Size: 90x90]
    │   └── [Panel] CardsContainer               <-- [HorizontalLayoutGroup / GridLayoutGroup]
    │       ├── [Button] Card_0                  <-- [Button + Image + TextMeshProUGUI "he"]
    │       ├── [Button] Card_1                  <-- [Button + Image + TextMeshProUGUI "she"]
    │       ├── [Button] Card_2                  <-- [Button + Image + TextMeshProUGUI "his"]
    │       ├── [Button] Card_3                  <-- [Button + Image + TextMeshProUGUI "her"]
    │       ├── [Button] Card_4                  <-- [Button + Image + TextMeshProUGUI "was"]
    │       └── [Button] Card_5                  <-- [Button + Image + TextMeshProUGUI "with"]
    │
    ├── [Panel] PocketIntroPanel                 <-- [Modal overlay for initial 2 min intro, Starts Inactive]
    │   ├── [Image] BackgroundDim                <-- [Image: Dark translucent panel, Stretch All]
    │   ├── [Button] IntroCardDisplay            <-- [Button + RectTransform: Center 300x200]
    │   └── [TMP] IntroCardTMP                   <-- [TextMeshProUGUI: Center 60pt Bold]
    │
    ├── [Panel] SentenceStripContainer           <-- [SentenceStripUI, RectTransform: Middle Center, Size: 1400x320, PosY: 30]
    │   ├── [Image] StripBackground              <-- [Image: Rounded card panel, Color: Soft White/Cream, Stretch All]
    │   └── [Empty] WordsContainer               <-- [HorizontalLayoutGroup, Middle Center, Spacing: 25]
    │       ├── [Empty] WordSlot_0               <-- [RectTransform: Size: 130x160]
    │       │   ├── [Image] NounBadge            <-- [Image: PosY: 90, Size: 80x80, Starts Inactive]
    │       │   ├── [Button] WordButton          <-- [Button + Image (Tile), Size: 130x100]
    │       │   │   └── [TMP] WordText           <-- [TextMeshProUGUI: Font Size: 44, Color: Deep Blue/Black]
    │       │   └── [Image] WordHighlightGlow    <-- [Image: Glowing border outline, Starts Inactive]
    │       ├── [Empty] WordSlot_1 ... WordSlot_9 (Identical structure up to 10 slots)
    │
    ├── [Panel] DialogueUI                       <-- [CanvasGroup, RectTransform: Bottom Center, PosY: 70, Size: 1000x110]
    │   ├── [Image] DialogueBubble               <-- [Image: Rounded Speech Bubble, Color: White with 0.95 alpha]
    │   └── [TMP] DialogueTMP                    <-- [TextMeshProUGUI: Font Size: 32, Auto-wrap, Color: Dark Slate]
    │
    ├── [Empty] ControlButtons                   <-- [RectTransform: Middle Center / Bottom Center]
    │   ├── [Button] ReadWholeButton             <-- [Button + TMP "Read Whole 🔊", Size: 260x80, Inactive]
    │   └── [Button] NextSentenceButton          <-- [Button + TMP "Next Sentence ➔", Size: 260x80, Inactive]
    │
    ├── [Empty] AudioSources                     <-- [GameObject holding Audio Components]
    │   ├── [AudioSource] VoiceAudioSource       <-- [AudioSource: 2D, PlayOnAwake=False, SpatialBlend=0]
    │   └── [AudioSource] SFXAudioSource         <-- [AudioSource: 2D, PlayOnAwake=False, SpatialBlend=0]
    │
    └── [Panel] RewardsContainer                 <-- [RectTransform: Stretch All, RaycastTarget=False]
        ├── [Particle] ConfettiParticles         <-- [ParticleSystem / UI Particle, Inactive]
        ├── [Panel] RewardPopup                  <-- [Star reward modal popup, Inactive]
        ├── [Panel] StickerPopup                 <-- [Stop 3 celebration sticker modal, Inactive]
        └── [Button] ContinueButton              <-- [Button + TMP "Continue ➔", PosY: -350, Inactive]
```

---

## 3. Inspector Wire-Up Table (`SentenceStreetController`)

| Field Name | Target GameObject / Asset |
| :--- | :--- |
| **Unit ID** | `"Unit9"` |
| **Topic Name** | `"SentenceStreet"` |
| **Activity Data** | `SentenceStreetData_Unit9` (ScriptableObject) |
| **Voice Audio Source** | `AudioSources/VoiceAudioSource` |
| **SFX Audio Source** | `AudioSources/SFXAudioSource` |
| **Dialogue Text** | `DialogueUI/DialogueTMP` |
| **Dialogue Canvas Group** | `DialogueUI` |
| **Sight Word Pocket UI** | `SightWordPocketUI` |
| **Pocket Intro Panel** | `PocketIntroPanel` |
| **Intro Card TMP** | `PocketIntroPanel/IntroCardTMP` |
| **Intro Card Button** | `PocketIntroPanel/IntroCardDisplay` |
| **Sentence Strip UI** | `SentenceStripContainer` |
| **Read Whole Button** | `ControlButtons/ReadWholeButton` |
| **Next Sentence Button** | `ControlButtons/NextSentenceButton` |
| **Progress Ring Fill Image** | `TopBar/ProgressRing` |
| **Progress Text** | `TopBar/ProgressText` |
| **Star Meter Rect** | `TopBar/StarMeter` |
| **Leo Mascot Object** | `LeoMascotContainer/LeoMascot` |
| **Confetti Particles** | `RewardsContainer/ConfettiParticles` |
| **Reward Popup** | `RewardsContainer/RewardPopup` |
| **Sticker Popup** | `RewardsContainer/StickerPopup` |
| **Continue Button** | `RewardsContainer/ContinueButton` |
| **Next Panel** | `Stop4_StoryTime` |
| **Current Panel** | `Stop3_SentenceStreet` |

---

## 4. `SentenceStripUI` Slot Array Assignment

In the `SentenceStripUI` component on `SentenceStripContainer`, expand each array to size `10` and wire:

| Array Element | Word Containers | Word Buttons | Word Texts | Word Highlight Glows | Noun Picture Images |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **`[0]`** | `WordSlot_0` | `WordSlot_0/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[1]`** | `WordSlot_1` | `WordSlot_1/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[2]`** | `WordSlot_2` | `WordSlot_2/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[3]`** | `WordSlot_3` | `WordSlot_3/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[4]`** | `WordSlot_4` | `WordSlot_4/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[5]`** | `WordSlot_5` | `WordSlot_5/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[6]`** | `WordSlot_6` | `WordSlot_6/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[7]`** | `WordSlot_7` | `WordSlot_7/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[8]`** | `WordSlot_8` | `WordSlot_8/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
| **`[9]`** | `WordSlot_9` | `WordSlot_9/WordButton` | `.../WordButton/WordText` | `.../WordHighlightGlow` | `.../NounBadge` |
