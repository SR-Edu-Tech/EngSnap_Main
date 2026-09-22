# Unit 9 Stop 3 — Sentence Street: Setup Guide

## 1. Overview & Pedagogical Purpose

* **Activity Name:** Stop 3 — Sentence Street (Unit 9)
* **Goal:** The six book sentences printed in the reader (pp. 43–45), read by the child rather than to them.
* **Core Rationale:** Reading individual words in isolation is only a stepping stone; reading an entire sentence independently is the true milestone of becoming a reader. Handling non-decodable sight words transparently through the **Sight Word Pocket** prevents cognitive stalling and gives children the confidence that *"I can read by myself!"*

---

## 2. Activity Flow

```mermaid
graph TD
    A[Start Activity] --> B[Sight Word Pocket Intro (2 min)]
    B --> C[Introduce 6 Sight Words: he, she, his, her, was, with]
    C --> D[Cards Animate into Pocket]
    D --> E[Guided Left-to-Right Pocket Tapping]
    E --> F[Child Taps All 6 Pocket Cards Left-to-Right]
    F --> G[Sentence Strip Appears!]
    G --> H[Load Sentence: Left-to-Right Progressive Word Taps]
    H --> I{All words tapped?}
    I -- No --> H
    I -- Yes --> J[Read Whole Pass: Noun Badges Pop Up]
    J --> K[Left-to-Right Read Highlight with Leo Underneath]
    K --> L[Success Celebration + Stars]
    L --> M{More Sentences?}
    M -- Yes --> H
    M -- No --> N[Complete Stop 3: Stars + Sticker + Navigate to Stop 4]
```

1. **Sight Words First (2 mins):** 6 word cards enter the child's *Sight Word Pocket* (`he`, `she`, `his`, `her`, `was`, `with`). Each is presented plainly: *"Six more pocket words today. These ones you just know — no sounding out! Put it in your pocket!"*
2. **Pocket Practice (Left-to-Right):** Before the sentence strip appears, the child taps each word in their pocket in sequence from left to right (`he` ➔ `she` ➔ `his` ➔ `her` ➔ `was` ➔ `with`) to reinforce sight word recognition.
3. **Sentence Strip Revealed (Word-by-Word):** Once the pocket words are tapped left-to-right, the **Sentence Strip appears**. Words appear progressively from left to right as the child taps each word to read it.
4. **The 6 Book Sentences (pp. 43–45):**
   1. `The fig is in the bin.`
   2. `The kid with a wig sat in a pit.`
   3. `This is a dog and its name is Tom.`
   4. `A fox sat on a log.`
   5. `The cub rubs the pup.`
   6. `The pup is in the tub.`
5. **Read It Whole:** After word-by-word tapping, the sentence is read again as one flowing line. Illustrated noun badges pop up above nouns (`fig`, `bin`, `kid`, `wig`, `pit`, `dog`, `Tom`, `fox`, `log`, `cub`, `pup`, `tub`). Leo reads along quietly underneath so struggling learners are supported.
6. **Reward & Transition:** Confetti, star meter animation, and sticker award before proceeding to Stop 4 (Story Time).

---

## 3. Unity UI & Hierarchy Setup

Create the following hierarchy under your UI Canvas (identical structure to Unit 8 Stop 3):

```text
Canvas (or Unit9_UIPanel)
└── Stop3_SentenceStreet [SentenceStreetController]
    ├── TopBar
    │   ├── ProgressRing (Image - ImageType: Filled, Radial 360)
    │   ├── ProgressText (TextMeshProUGUI - e.g., "0%")
    │   └── StarMeter (RectTransform for scale bounce)
    │
    ├── LeoMascotContainer
    │   └── LeoMascot (Image / Spine / Sprite animation)
    │
    ├── SightWordPocketUI [SightWordPocketUI]
    │   ├── PocketOpenButton (Button + Icon)
    │   └── CardsContainer (HorizontalLayoutGroup / GridLayoutGroup)
    │       ├── Card_0 (Button + TextMeshProUGUI + Image)
    │       ├── Card_1 (Button + TextMeshProUGUI + Image)
    │       ├── Card_2 (Button + TextMeshProUGUI + Image)
    │       ├── Card_3 (Button + TextMeshProUGUI + Image)
    │       ├── Card_4 (Button + TextMeshProUGUI + Image)
    │       └── Card_5 (Button + TextMeshProUGUI + Image)
    │
    ├── PocketIntroPanel (Modal overlay for initial 2 min intro)
    │   ├── BackgroundDim (Image - Dark translucent)
    │   ├── IntroCardDisplay (Button + RectTransform)
    │   └── IntroCardTMP (TextMeshProUGUI)
    │
    ├── SentenceStripContainer [SentenceStripUI]
    │   ├── StripBackground (Image / Rounded panel)
    │   └── WordsContainer (HorizontalLayoutGroup, Child Alignment: Middle Center)
    │       ├── WordSlot_0
    │       │   ├── NounBadge (Image - positioned above word)
    │       │   ├── WordButton (Button)
    │       │   │   └── WordText (TextMeshProUGUI)
    │       │   └── WordHighlightGlow (Image - glowing border outline)
    │       ├── WordSlot_1 ... WordSlot_9 (Identical structure up to 10 slots)
    │
    ├── DialogueUI
    │   ├── DialogueBubble (Image / CanvasGroup)
    │   └── DialogueTMP (TextMeshProUGUI)
    │
    ├── ControlButtons
    │   ├── ReadWholeButton (Button + TMP "Read Whole")
    │   └── NextSentenceButton (Button + TMP "Next Sentence ➔")
    │
    ├── AudioSources
    │   ├── VoiceAudioSource (AudioSource - 2D, SpatialBlend: 0)
    │   └── SFXAudioSource (AudioSource - 2D, SpatialBlend: 0)
    │
    └── RewardsContainer
        ├── ConfettiParticles (ParticleSystem / UI Particle)
        ├── RewardPopup (GameObject)
        ├── StickerPopup (GameObject)
        └── ContinueButton (Button)
```

---

## 4. Inspector Wire-Up Reference

### A. `SentenceStreetController`
| Field | Target GameObject / Asset |
| :--- | :--- |
| **Unit ID** | `"Unit9"` |
| **Topic Name** | `"SentenceStreet"` |
| **Activity Data** | `SentenceStreetData_Unit9` (ScriptableObject) |
| **Voice Audio Source** | `VoiceAudioSource` |
| **SFX Audio Source** | `SFXAudioSource` |
| **Dialogue Text** | `DialogueTMP` |
| **Dialogue Canvas Group** | `DialogueBubble` |
| **Sight Word Pocket UI** | `SightWordPocketUI` |
| **Pocket Intro Panel** | `PocketIntroPanel` |
| **Intro Card TMP** | `IntroCardTMP` |
| **Intro Card Button** | `IntroCardDisplay` |
| **Sentence Strip UI** | `SentenceStripContainer` |
| **Read Whole Button** | `ReadWholeButton` |
| **Next Sentence Button** | `NextSentenceButton` |
| **Progress Ring Fill Image** | `ProgressRing` |
| **Progress Text** | `ProgressText` |
| **Star Meter Rect** | `StarMeter` |
| **Leo Mascot Object** | `LeoMascot` |
| **Confetti Particles** | `ConfettiParticles` |
| **Reward Popup** | `RewardPopup` |
| **Sticker Popup** | `StickerPopup` |
| **Continue Button** | `ContinueButton` |
| **Next Panel** | `Stop4_StoryTime` |

### B. `SentenceStripUI`
Bind the 10 WordSlot children into the serialized arrays:
* **Word Containers (0..9):** `WordSlot_0` ... `WordSlot_9`
* **Word Buttons (0..9):** `WordSlot_0/WordButton` ... `WordSlot_9/WordButton`
* **Word Texts (0..9):** `WordSlot_0/WordButton/WordText` ... `WordSlot_9/WordButton/WordText`
* **Word Highlight Glows (0..9):** `WordSlot_0/WordHighlightGlow` ... `WordSlot_9/WordHighlightGlow`
* **Noun Picture Images (0..9):** `WordSlot_0/NounBadge` ... `WordSlot_9/NounBadge`

### C. `SightWordPocketUI`
* **Pocket Open Button:** `PocketOpenButton`
* **Cards Container:** `CardsContainer`
* **Card Buttons (0..5):** `Card_0` ... `Card_5`
* **Card Texts (0..5):** `Card_0/TMP` ... `Card_5/TMP`
* **Audio Source:** `AudioSource` on Pocket
* **Card Pop SFX:** `card_pop.wav`

---

## 5. ScriptableObject Configuration (`SentenceStreetData`)

Use Unity Editor Menu: **`EngSnap > Phonics2 > Create Sentence Street Data (Unit 9 Stop 3)`** to auto-generate the asset at:
`Assets/Resources/Phonics2/Unit9/SentenceStreetData_Unit9.asset`.

### Data Breakdown:

#### 1. Sight Words (6 items)
| Index | Word | Intro Script Clip Description |
| :---: | :--- | :--- |
| **0** | `he` | *"This one says 'he'. Pop it in your pocket!"* |
| **1** | `she` | *"This is 'she'. Pop it in your pocket!"* |
| **2** | `his` | *"This one says 'his'. Pop it in your pocket!"* |
| **3** | `her` | *"This one says 'her'. Pop it in your pocket!"* |
| **4** | `was` | *"This one says 'was'. Pop it in your pocket!"* |
| **5** | `with` | *"This one says 'with'. Pop it in your pocket!"* |

#### 2. Sentences (6 items)
1. **Sentence 1:** `"The fig is in the bin."`
   * Tokens: `The` (Sight), `fig` (Noun: `fig_sprite`), `is` (Sight), `in` (Sight), `the` (Sight), `bin.` (Noun: `bin_sprite`)
2. **Sentence 2:** `"The kid with a wig sat in a pit."`
   * Tokens: `The` (Sight), `kid` (Noun: `kid_sprite`), `with` (Sight), `a` (Sight), `wig` (Noun: `wig_sprite`), `sat` (Decodable), `in` (Sight), `a` (Sight), `pit.` (Noun: `pit_sprite`)
3. **Sentence 3:** `"This is a dog and its name is Tom."`
   * Tokens: `This` (Sight), `is` (Sight), `a` (Sight), `dog` (Noun: `dog_sprite`), `and` (Sight), `its` (Sight), `name` (Sight), `is` (Sight), `Tom.` (Noun: `tom_sprite`)
4. **Sentence 4:** `"A fox sat on a log."`
   * Tokens: `A` (Sight), `fox` (Noun: `fox_sprite`), `sat` (Decodable), `on` (Sight), `a` (Sight), `log.` (Noun: `log_sprite`)
5. **Sentence 5:** `"The cub rubs the pup."`
   * Tokens: `The` (Sight), `cub` (Noun: `cub_sprite`), `rubs` (Decodable), `the` (Sight), `pup.` (Noun: `pup_sprite`)
6. **Sentence 6:** `"The pup is in the tub."`
   * Tokens: `The` (Sight), `pup` (Noun: `pup_sprite`), `is` (Sight), `in` (Sight), `the` (Sight), `tub.` (Noun: `tub_sprite`)

---

## 6. Voice & Sound Script Manifest

Use this audio manifest to feed AI voice generation tools (e.g., ElevenLabs):

| Asset Filename | Voice / Cue | Script / Text | Prompt / Context Note |
| :--- | :--- | :--- | :--- |
| `leo_u9s3_pocket_intro.wav` | **Leo** | *"Six more pocket words today. These ones you just know — no sounding out!"* | Warm, encouraging teacher tone. Sight word intro. |
| `leo_u9s3_sentence_intro.wav` | **Leo** | *"Here comes your first sentence. Tap each word and read it!"* | Enthusiastic call-to-action. |
| `leo_u9s3_read_whole_invitation.wav` | **Leo** | *"Now let us read it all together, nice and smooth."* | Gentle invitation for whole-sentence reading. |
| `leo_u9s3_sentence_success.wav` | **Leo** | *"You read a SENTENCE. A whole sentence, by yourself!"* | Huge celebration, proud and joyful. |
| `sw_he.wav` | Sight Word | *"he"* | Clear, plain pronunciation. |
| `sw_she.wav` | Sight Word | *"she"* | Clear, plain pronunciation. |
| `sw_his.wav` | Sight Word | *"his"* | Clear, plain pronunciation. |
| `sw_her.wav` | Sight Word | *"her"* | Clear, plain pronunciation. |
| `sw_was.wav` | Sight Word | *"was"* | Clear, plain pronunciation. |
| `sw_with.wav` | Sight Word | *"with"* | Clear, plain pronunciation. |
| `sent_u9_1_whole.wav` | Sentence | *"The fig is in the bin."* | Natural reading pace, warm cadence. |
| `sent_u9_1_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 1)* | Underneath audio to support struggling child. |
| `sent_u9_2_whole.wav` | Sentence | *"The kid with a wig sat in a pit."* | Natural reading pace. |
| `sent_u9_2_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 2)* | Underneath audio. |
| `sent_u9_3_whole.wav` | Sentence | *"This is a dog and its name is Tom."* | Natural reading pace. |
| `sent_u9_3_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 3)* | Underneath audio. |
| `sent_u9_4_whole.wav` | Sentence | *"A fox sat on a log."* | Natural reading pace. |
| `sent_u9_4_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 4)* | Underneath audio. |
| `sent_u9_5_whole.wav` | Sentence | *"The cub rubs the pup."* | Natural reading pace. |
| `sent_u9_5_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 5)* | Underneath audio. |
| `sent_u9_6_whole.wav` | Sentence | *"The pup is in the tub."* | Natural reading pace. |
| `sent_u9_6_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 6)* | Underneath audio. |

---

## 7. Required Visual Assets

1. **Sight Word Pocket UI:** Pocket pouch graphic + 6 styled word card tiles.
2. **Noun Badges (Sprites):**
   * `fig.png` — Fresh purple fig fruit.
   * `bin.png` — Clean green or blue recycling/storage bin.
   * `kid.png` — Smiling young child avatar.
   * `wig.png` — Funny curly costume wig.
   * `pit.png` — Sandy play pit / ground pit.
   * `dog.png` — Friendly cartoon dog.
   * `tom.png` — Friendly boy named Tom.
   * `fox.png` — Bright orange playful fox.
   * `log.png` — Wooden tree log.
   * `cub.png` — Cute bear/lion cub.
   * `pup.png` — Playful little puppy.
   * `tub.png` — Warm bubbly bath tub.
3. **Sentence Strip UI:** Rounded background strip, word box container, glowing golden outline overlay.

---

## 8. Verification & QA Checklist

- [ ] **Pocket Intro Sequence:** When launching Stop 3 for the first time, does Leo introduce the 6 sight words and pop them one by one into the pocket?
- [ ] **Persistent Pocket:** Can the child tap the Pocket icon at any point to hear any of the 6 sight words spoken aloud?
- [ ] **Word-by-Word Tap Flow:** Does tapping each word trigger its audio clip and glow highlight?
- [ ] **Read Whole Unlock:** Does the `"Read Whole"` button appear only after all words in the sentence have been tapped?
- [ ] **Read Whole Flow:** Does tapping `"Read Whole"` reveal the noun illustrations above `fig`, `bin`, `kid`, `wig`, `pit`, `dog`, `Tom`, `fox`, `log`, `cub`, `pup`, `tub` and animate the left-to-right glow reading line?
- [ ] **Sentence Progression:** Do all 6 sentences cycle seamlessly in sequence?
- [ ] **Completion & Navigation:** At the end of sentence 6, do celebration particles, stars, and the sticker popup appear, cleanly progressing to Stop 4 (Story Time)?
