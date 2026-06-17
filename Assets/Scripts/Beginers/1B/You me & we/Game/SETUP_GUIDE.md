# You & We Game — Unity Setup Guide
## Scripts (all suffixed _YouAndWeGame)

| File | Purpose |
|---|---|
| `YouAndWeGameController_YouAndWeGame.cs` | Root controller, implements `IUnitCompletable` |
| `PronounDragScreen_YouAndWeGame.cs` | Screen 1 logic (falling cards, drop detection) |
| `PronounCard_YouAndWeGame.cs` | Individual draggable card — fall, drag, snap |
| `PronounHouse_YouAndWeGame.cs` | Tree-house drop zone — glow, correct flash |
| `BubblePopScreen_YouAndWeGame.cs` | Screen 2 logic (target prompt, round loop) |
| `BubbleItem_YouAndWeGame.cs` | Individual rising bubble — tap, wobble, pop |

---

## Scene Hierarchy

```
YouAndWeGame                         ← YouAndWeGameController_YouAndWeGame
  ├─ Screen1_PronounDrag             ← PronounDragScreen_YouAndWeGame
  │    ├─ CardSpawnArea              (RectTransform — full screen or top strip)
  │    ├─ HousesRow                  (HorizontalLayoutGroup)
  │    │    ├─ House_I               ← PronounHouse_YouAndWeGame (pronoun = "I")
  │    │    ├─ House_He              ← PronounHouse_YouAndWeGame (pronoun = "He")
  │    │    ├─ House_She             ← PronounHouse_YouAndWeGame (pronoun = "She")
  │    │    ├─ House_It              ← PronounHouse_YouAndWeGame (pronoun = "It")
  │    │    └─ House_We              ← PronounHouse_YouAndWeGame (pronoun = "We")
  │    ├─ RobinSpeechBubble          (TMP_Text)
  │    ├─ NextButton                 (Button)
  │    └─ SfxSource                  (AudioSource)
  │
  └─ Screen2_BubblePop              ← BubblePopScreen_YouAndWeGame
       ├─ BubbleSpawnArea            (RectTransform — full screen)
       ├─ TargetPromptBG             (Image)
       │    └─ TargetText            (TMP_Text)
       ├─ RobinSpeech                (TMP_Text)
       ├─ SparkleRoot                (RectTransform — empty, for sparkle pool)
       ├─ NextButton                 (Button)
       └─ SfxSource                  (AudioSource)
```

---

## Prefabs to Create

### PronounCard_Prefab
- Root: RectTransform (~180×220px) + CanvasGroup + Image (card background) + `PronounCard_YouAndWeGame`
- Children:
  - `CardImage` — Image (the picture, ~160×160px)
  - `CardLabel` — TMP_Text (word label, optional, bottom of card)
- **Leave scale at (1,1,1)**. Script manages all scale animation.

### Bubble_Prefab
- Root: RectTransform (~140×140px) + CanvasGroup + Image (bubble sprite) + Button + `BubbleItem_YouAndWeGame`
- Children:
  - `WordText` — TMP_Text (centered, large bold font, 28-34px)
- Set Button `Transition = None` (script handles visual feedback)

### Sparkle_Prefab  (simple)
- Root: RectTransform (~30×30px) + CanvasGroup + Image (star/sparkle sprite)
- No script needed — `BubblePopScreen` animates and destroys it.

### House_Prefab  (or create directly in scene)
- Root: RectTransform (~160×220px) + `PronounHouse_YouAndWeGame`
- Children:
  - `HouseImage` — Image (tree-house sprite, full size)
  - `GlowImage` — Image (bright outline, set alpha=0 in Inspector)
  - `PronounLabel` — TMP_Text ("I", "He", etc.)
  - `DropZone` — RectTransform (~120×80px, positioned over the door area)

---

## Inspector Wiring

### YouAndWeGameController_YouAndWeGame
| Field | Drag |
|---|---|
| screen1GO | Screen1_PronounDrag GO |
| screen2GO | Screen2_BubblePop GO |
| screen1Script | PronounDragScreen component |
| screen2Script | BubblePopScreen component |

### PronounDragScreen_YouAndWeGame
| Field | Value |
|---|---|
| cardPrefab | PronounCard_Prefab |
| cardSpawnArea | CardSpawnArea RT |
| houses[0..4] | House_I … House_We components |
| robinSpeech | RobinSpeechBubble TMP_Text |
| nextButton | NextButton |
| sfxSource | SfxSource |
| correctClip | chime .wav |
| wrongClip | buzz .wav |
| allDoneClip | fanfare .wav |
| fallSpeed | 80 (default) |
| cardSpacing | 3.5s (default) |
| **cardData** | Fill all 6-8 entries (see below) |

#### PronounCardData entries (example 8 cards)
| cardSprite | correctPronoun | cardLabel |
|---|---|---|
| [girl sprite] | She | Girl |
| [boy sprite] | He | Boy |
| [self/me sprite] | I | Me |
| [sun sprite] | It | Sun |
| [group/family sprite] | We | Family |
| [cat sprite] | It | Cat |
| [teacher sprite] | She | Teacher |
| [friends sprite] | We | Friends |

### BubblePopScreen_YouAndWeGame
| Field | Value |
|---|---|
| bubblePrefab | Bubble_Prefab |
| sparklePrefab | Sparkle_Prefab |
| spawnArea | BubbleSpawnArea RT |
| sparkleRoot | SparkleRoot RT |
| targetText | TargetText TMP_Text |
| robinSpeech | RobinSpeech TMP_Text |
| nextButton | NextButton |
| sfxSource | SfxSource |
| popClip | pop .wav |
| wrongClip | wobble/buzz .wav |
| allDoneClip | fanfare .wav |
| words | strong, smart, kind, happy, brave, helpful, lucky, the best |
| decoysPerRound | 2 |
| riseSpeed | 55 |
| bubbleSpawnInterval | 0.6 |

---

## Wiring into TopicData_BB2

In your topic's `TopicData_BB2` component, add one entry to `unitEntries`:

```
unitEntries[n]
  unitType              = Game          ← UnitType_BB1.Game
  contentGameObject     = YouAndWeGame  ← drag the root GO
  unitDisplayName       = "Game"
```

That's it — `SharedUnitPanelController` will call `OnUnitStart()` on the root GO,
which resets and starts from Screen 1 every single time.

---

## Flow Summary

```
TopicSelectorButton clicked
  → TopicSelectorRegistry.OpenTopic()
    → SharedUnitPanelController.Open()
      → SharedUnitButton "Game" clicked
        → SharedUnitPanelController.StartUnit()
          → YouAndWeGameController.OnUnitStart()   ← IUnitCompletable
            → Screen 1 starts fresh
              → all cards placed → Next enabled
                → Screen 2 starts fresh
                  → all bubbles popped → Next enabled
                    → YouAndWeGameController.OnGameComplete()
                      → SharedUnitPanelController.UnitFinished()
                        → badge ticked
                        → reward panel if all units done
```

Opening the game again **always** starts from Screen 1 (OnUnitStart resets everything).

---

## SFX Recommendations
- **correctClip** — bright xylophone chime (C5, short)
- **wrongClip** — soft low buzz or "boing" (gentle, not harsh for kids)
- **allDoneClip (Screen 1)** — short upbeat fanfare (2s)
- **popClip** — satisfying bubble pop + sparkle sound
- **allDoneClip (Screen 2)** — longer celebration fanfare

Both screens have their own `AudioSource` so sounds never cut each other off.
