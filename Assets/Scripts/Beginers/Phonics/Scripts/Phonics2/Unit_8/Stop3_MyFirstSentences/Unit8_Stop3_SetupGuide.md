# Unit 8 Stop 3 — My First Sentences: Setup Guide

## 1. Overview & Pedagogical Purpose

* **Activity Name:** Stop 3 — My First Sentences (Unit 8)
* **Goal:** The four book sentences printed in the reader, read by the child rather than to them.
* **Core Rationale:** Reading individual words in isolation is only a stepping stone; reading an entire sentence independently is the true milestone of becoming a reader. Handling non-decodable sight words transparently through the **Sight Word Pocket** prevents cognitive stalling and gives children the confidence that *"I can read by myself!"*

---

## 2. Activity Flow

```mermaid
graph TD
    A[Start Activity] --> B[Sight Word Pocket Intro (2 min)]
    B --> C[Introduce 6 Sight Words: a, I, the, is, see, and]
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

1. **Sight Words First (2 mins):** 6 word cards enter the child's *Sight Word Pocket* (`a`, `I`, `the`, `is`, `see`, `and`). Each is presented plainly: *"This one you cannot sound out. It just says 'the'. Put it in your pocket!"*
2. **Pocket Practice (Left-to-Right):** Before the sentence strip appears, the child taps each word in their pocket in sequence from left to right (`a` ➔ `I` ➔ `the` ➔ `is` ➔ `see` ➔ `and`) to reinforce sight word recognition.
3. **Sentence Strip Revealed (Word-by-Word):** Once the pocket words are tapped left-to-right, the **Sentence Strip appears**. Words appear progressively from left to right as the child taps each word to read it.
4. **The 4 Book Sentences:**
   1. `I see a cap and a map.`
   2. `The rat ran with a hat.`
   3. `Ben writes ten with a pen.`
   4. `Here is a pet and it is wet.`
5. **Read It Whole:** After word-by-word tapping, the sentence is read again as one flowing line. Illustrated noun badges pop up above nouns (`cap`, `map`, `rat`, `hat`, `Ben`, `ten`, `pen`, `pet`, `wet`). Leo reads along quietly underneath so struggling learners are supported.
6. **Reward & Transition:** Confetti, star meter animation, and sticker award before proceeding to Stop 4 (Story Time).

---

## 3. Unity UI & Hierarchy Setup

Create the following hierarchy under your UI Canvas:

```text
Canvas (or Unit8_UIPanel)
└── Stop3_MyFirstSentences [MyFirstSentencesController]
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

### A. `MyFirstSentencesController`
| Field | Target GameObject / Asset |
| :--- | :--- |
| **Unit ID** | `"Unit8"` |
| **Topic Name** | `"MyFirstSentences"` |
| **Activity Data** | `MyFirstSentencesData_Unit8` (ScriptableObject) |
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

## 5. ScriptableObject Configuration (`MyFirstSentencesData`)

Use Unity Editor Menu: **`EngSnap > Phonics2 > Create My First Sentences Data (Unit 8 Stop 3)`** to auto-generate the asset at:
`Assets/Resources/Phonics2/Unit8/MyFirstSentencesData_Unit8.asset`.

### Data Breakdown:

#### 1. Sight Words (6 items)
| Index | Word | Intro Script Clip Description |
| :---: | :--- | :--- |
| **0** | `a` | *"This one says 'a'. Pop it in your pocket!"* |
| **1** | `I` | *"This is 'I'. Pop it in your pocket!"* |
| **2** | `the` | *"This one says 'the'. Not /t/-/h/-/e/ — just 'the'! Your pocket will remember it for you."* |
| **3** | `is` | *"This one says 'is'. Pop it in your pocket!"* |
| **4** | `see` | *"This one says 'see'. Pop it in your pocket!"* |
| **5** | `and` | *"This one says 'and'. Pop it in your pocket!"* |

#### 2. Sentences (4 items)
1. **Sentence 1:** `"I see a cap and a map."`
   * Tokens: `I` (Sight), `see` (Sight), `a` (Sight), `cap` (Noun: `cap_sprite`), `and` (Sight), `a` (Sight), `map.` (Noun: `map_sprite`)
2. **Sentence 2:** `"The rat ran with a hat."`
   * Tokens: `The` (Sight), `rat` (Noun: `rat_sprite`), `ran` (Decodable), `with` (Sight/Decodable), `a` (Sight), `hat.` (Noun: `hat_sprite`)
3. **Sentence 3:** `"Ben writes ten with a pen."`
   * Tokens: `Ben` (Noun: `ben_sprite`), `writes` (Sight/Story), `ten` (Noun: `ten_sprite`), `with` (Sight), `a` (Sight), `pen.` (Noun: `pen_sprite`)
4. **Sentence 4:** `"Here is a pet and it is wet."`
   * Tokens: `Here` (Sight), `is` (Sight), `a` (Sight), `pet` (Noun: `pet_sprite`), `and` (Sight), `it` (Sight), `is` (Sight), `wet.` (Noun: `wet_sprite`)

---

## 6. Voice & Sound Script Manifest

Use this audio manifest to feed AI voice generation tools (e.g., ElevenLabs):

| Asset Filename | Voice / Cue | Script / Text | Prompt / Context Note |
| :--- | :--- | :--- | :--- |
| `leo_u8s3_pocket_intro.wav` | **Leo** | *"Some little words cannot be sounded out. They are just… themselves! Pop them in your pocket."* | Warm, encouraging teacher tone. Sight word intro. |
| `leo_u8s3_word_the_intro.wav` | **Leo** | *"This one says 'the'. Not /t/-/h/-/e/ — just 'the'! Your pocket will remember it for you."* | Honest, friendly framing. |
| `leo_u8s3_sentence_intro.wav` | **Leo** | *"Here comes your first sentence. Tap each word and read it!"* | Enthusiastic call-to-action. |
| `leo_u8s3_read_whole_invitation.wav` | **Leo** | *"Now let us read it all together, nice and smooth."* | Gentle invitation for whole-sentence reading. |
| `leo_u8s3_sentence_success.wav` | **Leo** | *"You read a SENTENCE. A whole sentence, by yourself!"* | Huge celebration, proud and joyful. |
| `sw_a.wav` | Sight Word | *"a"* | Clear, plain pronunciation. |
| `sw_I.wav` | Sight Word | *"I"* | Clear, plain pronunciation. |
| `sw_the.wav` | Sight Word | *"the"* | Clear, plain pronunciation. |
| `sw_is.wav` | Sight Word | *"is"* | Clear, plain pronunciation. |
| `sw_see.wav` | Sight Word | *"see"* | Clear, plain pronunciation. |
| `sw_and.wav` | Sight Word | *"and"* | Clear, plain pronunciation. |
| `sent_1_whole.wav` | Sentence | *"I see a cap and a map."* | Natural reading pace, warm cadence. |
| `sent_1_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 1)* | Underneath audio to support struggling child. |
| `sent_2_whole.wav` | Sentence | *"The rat ran with a hat."* | Natural reading pace. |
| `sent_2_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 2)* | Underneath audio. |
| `sent_3_whole.wav` | Sentence | *"Ben writes ten with a pen."* | Natural reading pace. |
| `sent_3_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 3)* | Underneath audio. |
| `sent_4_whole.wav` | Sentence | *"Here is a pet and it is wet."* | Natural reading pace. |
| `sent_4_leo_quiet.wav` | Leo | *(Quiet / low-volume whisper-read of Sentence 4)* | Underneath audio. |

---

## 7. Required Visual Assets

1. **Sight Word Pocket UI:** Pocket pouch graphic for companion character + 6 styled word card tiles.
2. **Noun Badges (Sprites):**
   * `cap.png` — A baseball cap / cap icon.
   * `map.png` — An open treasure map.
   * `rat.png` — A cute cartoon rat.
   * `hat.png` — A top hat / sun hat.
   * `ben.png` — Character avatar of boy Ben.
   * `ten.png` — Number 10 graphic.
   * `pen.png` — Fountain / ballpoint pen.
   * `pet.png` — Friendly puppy / kitten.
   * `wet.png` — Splashing water droplet / wet puppy.
3. **Sentence Strip UI:** Rounded background strip, word box container, glowing golden outline overlay.

---

## 8. Verification & QA Checklist

- [ ] **Pocket Intro Sequence:** When launching Stop 3 for the first time, does Leo introduce the 6 sight words and pop them one by one into the pocket?
- [ ] **Persistent Pocket:** Can the child tap the Pocket icon at any point to hear any of the 6 sight words spoken aloud?
- [ ] **Word-by-Word Tap Flow:** Does tapping each word trigger its audio clip and glow highlight?
- [ ] **Read Whole Unlock:** Does the `"Read Whole"` button appear only after all words in the sentence have been tapped?
- [ ] **Read Whole Flow:** Does tapping `"Read Whole"` reveal the noun illustrations above `cap`, `map`, `rat`, `hat`, `Ben`, `ten`, `pen`, `pet`, `wet` and animate the left-to-right glow reading line?
- [ ] **Sentence Progression:** Do all 4 sentences cycle seamlessly in sequence?
- [ ] **Completion & Navigation:** At the end of sentence 4, do celebration particles, stars, and the sticker popup appear, cleanly progressing to Stop 4 (Story Time)?
