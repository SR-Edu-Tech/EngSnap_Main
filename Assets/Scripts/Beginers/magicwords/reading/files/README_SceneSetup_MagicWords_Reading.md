# Magic Words – Reading Unit  
## Unity Scene Setup Guide
### Script Suffix: `_MagicWords_Reading`

---

## 📁 Scripts in This Package

| Script | Purpose |
|--------|---------|
| `GameManager_MagicWords_Reading` | Central state machine; panel switching |
| `MagicWordData_MagicWords_Reading` | ScriptableObject data container per word |
| `Panel1_WordBubbles_MagicWords_Reading` | Panel 1 controller – bubble spawn & tap |
| `WordBubble_MagicWords_Reading` | Individual bubble – animation + interaction |
| `Panel2_SituationCards_MagicWords_Reading` | Panel 2 controller – card sequence |
| `SituationCard_MagicWords_Reading` | Individual card – slide-in + tap |
| `AudioManager_MagicWords_Reading` | BG music + SFX + voiceover ducking |
| `SparkleEffect_MagicWords_Reading` | Reusable sparkle burst controller |
| `AnimationHelper_MagicWords_Reading` | Static animation coroutine library |

---

## 🎨 Step 1 – Create ScriptableObject Data Assets

For each of the 5 magic words, create a **MagicWordData** asset:

> **Assets → Right-click → Create → MagicWords → MagicWordData**

Create 5 assets and name them:
- `MWD_Please`
- `MWD_Sorry`  
- `MWD_ThankYou`
- `MWD_ExcuseMe`
- `MWD_Welcome`

### Fill in each asset:

| Field | PLEASE | SORRY | THANK YOU | EXCUSE ME | WELCOME |
|-------|--------|-------|-----------|-----------|---------|
| **magicWord** | PLEASE | SORRY | THANK YOU | EXCUSE ME | WELCOME |
| **accentColor** | Yellow `#FFD740` | Orange `#FF6E40` | Green `#69F0AE` | Blue `#40C4FF` | Purple `#EA80FC` |
| **bubbleIntroAudio** | `audio_please_intro` | `audio_sorry_intro` | `audio_thankyou_intro` | `audio_excuseme_intro` | `audio_welcome_intro` |
| **situationAudio** | `audio_please_situation` | `audio_sorry_situation` | `audio_thankyou_situation` | `audio_excuseme_situation` | `audio_welcome_situation` |
| **wordOnlyAudio** | `audio_please_word` | `audio_sorry_word` | `audio_thankyou_word` | `audio_excuseme_word` | `audio_welcome_word` |
| **situationIllustration** | Child + parent + cookie | Child bumping someone | Child receiving gift | Child in crowd | Child responding thanks |
| **cardAutoPlayDelay** | 0.6 | 0.6 | 0.6 | 0.6 | 0.6 |

---

## 🏗️ Step 2 – Scene Hierarchy

```
Scene Root
│
├── GameManager              ← GameManager_MagicWords_Reading
│                              AudioManager_MagicWords_Reading
│
├── Canvas (Screen Space – Camera)
│   │
│   ├── Panel1_WordBubbles   ← Panel1_WordBubbles_MagicWords_Reading
│   │   ├── Background
│   │   ├── BubbleContainer  ← assign to bubbleContainer
│   │   └── NextButton       ← assign to nextButton + nextButtonObject
│   │
│   └── Panel2_SituationCards ← Panel2_SituationCards_MagicWords_Reading
│       ├── Background
│       ├── CardContainer    ← assign to cardContainer
│       └── BottomButtons    ← assign to bottomButtonsObject
│           ├── NextButton   ← assign to nextButton
│           └── ReplayButton ← assign to replayButton
│
└── EventSystem
```

---

## 🫧 Step 3 – WordBubble Prefab

Create a prefab in `Assets/Prefabs/` named **WordBubble**:

```
WordBubble (RectTransform)
├── Components: Button, WordBubble_MagicWords_Reading, AudioSource
├── BubbleBackground  (Image – circle sprite, coloured)
├── BubbleIcon        (Image – decorative emoji/icon, optional)
├── WordLabel         (TMP_Text – large bold text, centred)
├── SparkleEmitter    (ParticleSystem – burst on tap)
├── TappedGlowRing    (Image – ring shape, hidden by default)
└── CheckmarkObject   (contains ✓ image, hidden by default)
```

**Recommended size:** 200×200 pixels  
**Word font size:** 28–36pt, bold, white or dark depending on accent  
**TappedGlowRing:** Semi-transparent ring image, alpha 0.55  

---

## 🃏 Step 4 – SituationCard Prefab

Create a prefab named **SituationCard**:

```
SituationCard (RectTransform)
├── Components: CanvasGroup, SituationCard_MagicWords_Reading
├── CardBackground   (Image – white rounded rect)
├── AccentStripe     (Image – coloured bar at top, 8px tall)
├── IllustrationArea (Button + Image)
│   └── IllustrationImage  (Image – fill the top 55% of card)
├── Divider          (Image – thin horizontal line)
├── WordArea         (Button)
│   └── MagicWordText (TMP_Text – 48–60pt, bold, centred)
└── TapSparkle       (ParticleSystem)
```

**Recommended card size:** 320×480 pixels  
**Illustration area:** Top 55% of card  
**Word area:** Bottom 45% – word in big colourful text  

---

## 🔊 Step 5 – Audio Files Required

Place in `Assets/Audio/MagicWords/`:

```
Voiceovers/
  audio_please_intro.wav    "PLEASE — we say this when we need something."
  audio_sorry_intro.wav     "SORRY — we say this when we do something wrong."
  audio_thankyou_intro.wav  "THANK YOU — we say this when we get something."
  audio_excuseme_intro.wav  "EXCUSE ME — we say this when we need to pass or interrupt."
  audio_welcome_intro.wav   "WELCOME — we say this when someone says thank you."

  audio_please_situation.wav    "When you need something — say PLEASE!"
  audio_sorry_situation.wav     "When you do something wrong — say SORRY!"
  audio_thankyou_situation.wav  "When someone gives you something — say THANK YOU!"
  audio_excuseme_situation.wav  "When you need to pass someone — say EXCUSE ME!"
  audio_welcome_situation.wav   "When someone says thank you — say YOU ARE WELCOME!"

  audio_please_word.wav     "PLEASE"
  audio_sorry_word.wav      "SORRY"
  audio_thankyou_word.wav   "THANK YOU"
  audio_excuseme_word.wav   "EXCUSE ME"
  audio_welcome_word.wav    "WELCOME"

SFX/
  sfx_bubble_pop.wav        Short satisfying pop (30–80ms)
  sfx_bubble_tap.wav        Soft bounce boing
  sfx_all_tapped.wav        Cheerful fanfare / chime (1–2s)
  sfx_card_slide.wav        Whoosh slide in (100–200ms)
  sfx_card_tap.wav          Soft tap click
  sfx_all_cards_done.wav    Big celebration jingle
  sfx_panel_transition.wav  Magical swish
  sfx_intro_jingle.wav      Opening happy tune (2–3s)
  sfx_unit_complete.wav     Full celebration fanfare

Music/
  bgm_magic_words.wav       Upbeat, loopable background music (60–90 BPM)
```

---

## ✨ Step 6 – Sparkle Particle System Settings

Configure the **SparkleEmitter** ParticleSystem on each bubble/card:

| Parameter | Value |
|-----------|-------|
| Duration | 0.5 |
| Looping | ❌ Off |
| Start Lifetime | 0.6–1.2 |
| Start Speed | 150–350 |
| Start Size | 8–20 |
| Start Color | Random from palette (set in code) |
| Gravity Modifier | 0.5 |
| Max Particles | 25 |
| Emission – Burst Count | 18 |
| Shape | Sphere, Radius 0.1 |
| Renderer – Render Mode | Billboard |
| Renderer – Material | Sprites-Default (or star sprite) |

**Also create a larger sparkle for the NEXT button reveal** – double the burst count.

---

## 📐 Step 7 – Bubble Container Layout

In Panel1, `BubbleContainer` should be a plain RectTransform (not a LayoutGroup).

Set 5 anchored positions for the 5 bubbles to create a fun scattered arrangement:

```
Index 0 (PLEASE)    : (-280, +80)   — top left
Index 1 (SORRY)     : (+270, +120)  — top right
Index 2 (THANK YOU) : (0,    -20)   — centre
Index 3 (EXCUSE ME) : (-240, -160)  — bottom left
Index 4 (WELCOME)   : (+260, -180)  — bottom right
```

Adjust for your canvas resolution (design at 1080×1920 for portrait or 1920×1080 landscape).

---

## 🎮 Step 8 – Final Inspector Assignments

### GameManager
- `panel1WordBubbles` → Panel1_WordBubbles GameObject
- `panel2SituationCards` → Panel2_SituationCards GameObject
- `sfxIntroJingle` / `sfxPanelTransition` / `sfxUnitComplete` → SFX clips

### Panel1_WordBubbles_MagicWords_Reading
- `magicWords[0..4]` → MWD_Please, MWD_Sorry, MWD_ThankYou, MWD_ExcuseMe, MWD_Welcome
- `wordBubblePrefab` → WordBubble prefab
- `bubbleContainer` → BubbleContainer RectTransform
- `bubblePositions[0..4]` → positions as listed above
- `sparkleFXPrefab` → SparkleEffect prefab
- `nextButton` / `nextButtonObject` → NEXT button refs
- All SFX clips

### Panel2_SituationCards_MagicWords_Reading
- `magicWords[0..4]` → same 5 ScriptableObjects
- `situationCardPrefab` → SituationCard prefab
- `cardContainer` → CardContainer RectTransform
- `nextButton` / `replayButton` / `bottomButtonsObject` → button refs
- All SFX clips

### AudioManager_MagicWords_Reading
- `backgroundMusic` → bgm_magic_words.wav

---

## 💡 Design Tips for 3–4 Year Olds

- **Touch targets ≥ 120px** – small fingers miss small buttons
- **Never punish a wrong tap** – every tap on a bubble replays the word
- **Bright, saturated colours** – each word has a distinct colour identity
- **Voice first** – audio plays automatically; visuals reinforce, never replace
- **Celebrate everything** – sparkles on every tap, fanfares on completion
- **No timers, no pressure** – student moves at their own pace

---

## 🚀 Extending the System

To add more words beyond 5:
1. Create additional `MagicWordData` ScriptableObjects
2. Add them to the arrays in Panel1 and Panel2 controllers
3. Add new bubble positions in Panel1's `bubblePositions` array
4. No code changes required

To connect to your curriculum system, replace the `GoToNextUnit()` stub in `GameManager_MagicWords_Reading` with your scene loading / progress tracking code.
