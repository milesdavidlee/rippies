# Rippies implementation handoff

## Purpose

This document is the primary context file for **Codex running inside Visual Studio Code on macOS**. It can also be used by another engineer or coding agent continuing the Rippies mobile and Unity implementation.

Rippies is designed as a hybrid product:

- The normal mobile application owns the collection, store, account, inventory, navigation, and backend communication.
- Unity is embedded as a full-screen native view only for opening a selected pack and inspecting its five revealed cards.

The current working implementation includes the iOS React Native product shell
and the Unity vertical slice as a real Unity-as-a-Library handoff. Android is
intentionally out of scope for the current product pass.

## Current working state

Unity editor:

```text
6000.5.5f1
```

Unity project:

```text
unity/Rippies
```

Main scene:

```text
Assets/Rippies/Scenes/PackReveal.unity
```

Implemented:

- Bare React Native iOS shell with Discover, Collection, and Profile tabs.
- iOS 26 navigation uses native `UIGlassEffect` Liquid Glass in dark mode,
  with an ultra-thin dark material fallback on earlier iOS releases.
- Deterministic fake inventory, immutable five-card reveal receipts, and resume-safe presentation state.
- Automatic simulator Unity export/build/embed phase in the iOS app target.
- Native Objective-C++ host that warms Unity behind React Native.
- Crossfade to Unity only after the selected reveal emits `sceneReady`.
- Native Unity-to-app event delivery for `tearStarted`, `cardVisible`, and `revealComplete`.
- Unity-hosted completion and inspect surface using the React Native design
  tokens, followed by an explicit collection CTA and coordinated crossfade.
- Six animated 3D foil packs in a touch/click grid.
- Independent themes and coordinated palettes.
- Selected pack animates into the reveal anchor.
- Optional licensed Fab GLB drives the centered arrival, swipe-scrubbed tear,
  wrapper release, and card extraction; the procedural pack remains the
  repository-safe fallback.
- Selected `packTypeId`, palette, and generated card payload pass through `NativeRevealBridge`.
- Left-to-right constrained tear interaction.
- Procedurally inflated foil geometry with crimping, wrinkles, and jagged seam.
- Top strip fully detaches and exits the frame.
- Pack falls below frame and is disabled.
- The purchased animation's primary card mesh emerges continuously from the wrapper,
  receives a deterministic receipt-specific Rippies face texture, detaches at
  the authored settle frame, and becomes the anchor for four additional
  receipt-assigned cards.
- All five cards rise together, fan apart, and settle into a balanced 3/2 grid.
  Tapping any card lifts it into a centered hero pose; horizontal drag rotates
  it freely in 3D, and tapping it again returns it to its exact grid slot.
- Without the local licensed GLB, the generated fallback card still emerges
  with name, rarity, archetype, stats, serial, flavor text, and pattern art.
- Glow remains behind the card at inspect angles.
- Touch-driven 3D card inspection around each card's renderer center, with
  unlimited horizontal turns, bounded vertical tilt, and continuous idle
  motion.
- Fully oval native-shell and Unity completion actions.
- Return from completed reveal to the native collection grid.

The last verified iOS simulator flow was:

```text
Collection tab
  -> select an unopened fake-data pack
  -> React Native restores its immutable receipt
  -> Unity warms behind the native pack surface
  -> selected payload reaches NativeRevealBridge
  -> matching Unity pack crossfades full screen
  -> swipe the seal
  -> Unity animates strip, pack, glow, and the primary assigned card
  -> five assigned cards fan out and settle into a 3/2 grid
  -> revealComplete keeps Unity visible in inspect mode
  -> user taps any card, rotates it in 3D, and taps again to return it
  -> View collection closes the group and emits collectionRequested
  -> native crossfade reveals the React Native Cards segment
```

## Important source files

```text
Assets/Rippies/Runtime/PackSelectionFlow.cs
Assets/Rippies/Runtime/NativeRevealBridge.cs
Assets/Rippies/Runtime/PackRipController.cs
Assets/Rippies/Runtime/SwipeTearInteractor.cs
Assets/Rippies/Runtime/FoilPackDeformer.cs
Assets/Rippies/Runtime/AuthoredPackDriver.cs
Assets/Rippies/Runtime/CardFaceTextureFactory.cs
Assets/Rippies/Runtime/CardGroupPresentation.cs
Assets/Rippies/Runtime/RevealDirector.cs
Assets/Rippies/Runtime/RevealGlowPulse.cs
Assets/Rippies/Runtime/GeneratedCardPresenter.cs
Assets/Rippies/Runtime/RevealData.cs
Assets/Rippies/Runtime/SoftOrbitCamera.cs
Assets/Rippies/Shaders/PackFoil.shader
Assets/Rippies/Shaders/CardGlow.shader
```

## Architecture to preserve

```text
Mobile collection grid
  -> user selects an owned/unopened pack
  -> app requests or restores immutable reveal receipt
  -> app preloads Unity and required assets
  -> app calls PrepareReveal(payloadJson)
  -> Unity emits sceneReady
  -> app presents Unity full screen
  -> user tears or app calls BeginReveal/SkipReveal
  -> Unity emits tearStarted, cardVisible, revealComplete
  -> app records presentation progress and returns to collection
```

The animation never decides which card the user owns. The backend must assign and persist the result before Unity becomes interactive.

## Licensed authored reveal asset

The enhanced reveal uses the purchased `Animated Card Loot Pack` GLB through
Unity glTFast. Because this repository is public, the marketplace binary is
kept local and ignored by Git.

Place it at:

```text
unity/Rippies/Assets/Resources/Rippies/ThirdParty/Local/animated_card_loot_pack.glb
```

`AuthoredPackDriver` maps the source clip into product-controlled phases:

```text
0.04s–3.88s  centered arrival / presentation
5.88s–6.84s  user-controlled swipe scrub
6.84s–7.48s  committed wrapper tear
7.48s–8.84s  card extraction and settle
```

At reveal preparation, `CardFaceTextureFactory` derives deterministic front
and back artwork from the immutable `CardPayload` and selected `packTypeId`.
`AuthoredPackDriver` composites both faces into the purchased model's original
UV atlas. The generated art fills each complete flat face while preserving the
model's physical bevel and edge thickness; the back uses the same orbit,
gradient, foil-band, and typography language as the React Native pack covers.

During extraction, `SoftOrbitCamera` continuously measures and tracks the
animated card's live renderer bounds, blending from the pack framing into the
inspection framing so the card stays centered and fully visible throughout the
authored motion. At the settle frame the authored card is detached without
rescaling, wrapped in a pivot at its renderer center, and remains under the
same camera framing for direct touch inspection.

When the user chooses **View collection**, inspection input stops but the
focused camera stays locked through Unity's closing beat. The presentation
pivot then moves in camera-space down and away—never in the licensed model's
rotated local axes—before `collectionRequested` starts the native crossfade
back to the React Native card grid.

Before every subsequent reveal, the card is restored to its authored parent
and exact original local scale before a new payload is applied. This reset
ordering is required because Unity remains resident between pack openings; it
prevents the closing-pivot scale from accumulating across a multi-pack session.
The generated Unity card is not displayed when the licensed asset is
available.

## Bridge contract

### App to Unity

Unity target object:

```text
NativeRevealBridge
```

Methods:

```text
PrepareReveal(payloadJson)
BeginReveal("")
SkipReveal("")
PauseReveal("true" | "false")
SetMuted("true" | "false")
DisposeReveal("")
```

### Unity to app

Unity emits JSON events through `NativeRevealBridge.Emit`:

```json
{
  "eventName": "sceneReady",
  "value": "rev_123"
}
```

Supported events:

```text
sceneReady       value = revealId
tearStarted      value = revealId
cardVisible      value = cardId
revealComplete   value = revealId
collectionRequested value = revealId
```

On iOS, Unity calls the native `RippiesUnityEvent` symbol.

On Android, Unity calls `onUnityRevealEvent(payload)` on the current Unity host activity.

### Reveal payload

```json
{
  "orderId": "ord_123",
  "revealId": "rev_456",
  "packTypeId": "rippies_prism",
  "assetVersion": "prototype-2",
  "cards": [
    {
      "id": "card_789",
      "name": "Prism Titan",
      "grade": "PROTOTYPE 112",
      "rarityTier": "rare",
      "archetype": "Wildcard",
      "accentHex": "#B96CFF",
      "flavorText": "Nothing stays sealed forever.",
      "attack": 67,
      "defense": 70,
      "speed": 60,
      "luck": 77,
      "frontImageUrl": "",
      "backImageUrl": ""
    },
    {
      "id": "card_790",
      "name": "Spectrum Ace",
      "grade": "PROTOTYPE 113",
      "rarityTier": "common",
      "archetype": "Runner",
      "accentHex": "#B96CFF",
      "flavorText": "Outrun the impossible.",
      "attack": 77,
      "defense": 83,
      "speed": 88,
      "luck": 89,
      "frontImageUrl": "",
      "backImageUrl": ""
    },
    {
      "id": "card_791",
      "name": "Violet Oracle",
      "grade": "PROTOTYPE 114",
      "rarityTier": "rare",
      "archetype": "Oracle",
      "accentHex": "#B96CFF",
      "flavorText": "Luck favors the luminous.",
      "attack": 72,
      "defense": 80,
      "speed": 66,
      "luck": 91,
      "frontImageUrl": "",
      "backImageUrl": ""
    },
    {
      "id": "card_792",
      "name": "Prism Phantom",
      "grade": "PROTOTYPE 115",
      "rarityTier": "rare",
      "archetype": "Sentinel",
      "accentHex": "#B96CFF",
      "flavorText": "Protect the signal.",
      "attack": 84,
      "defense": 92,
      "speed": 60,
      "luck": 78,
      "frontImageUrl": "",
      "backImageUrl": ""
    },
    {
      "id": "card_793",
      "name": "Iris Runner",
      "grade": "PROTOTYPE 116",
      "rarityTier": "ultra",
      "archetype": "Wildcard",
      "accentHex": "#B96CFF",
      "flavorText": "Every pull changes the story.",
      "attack": 90,
      "defense": 88,
      "speed": 94,
      "luck": 90,
      "frontImageUrl": "",
      "backImageUrl": ""
    }
  ],
  "card": {
    "id": "card_789",
    "name": "Prism Titan",
    "grade": "PROTOTYPE 112",
    "rarityTier": "rare",
    "archetype": "Wildcard",
    "accentHex": "#B96CFF",
    "flavorText": "Nothing stays sealed forever.",
    "attack": 67,
    "defense": 70,
    "speed": 60,
    "luck": 77,
    "frontImageUrl": "",
    "backImageUrl": ""
  },
  "receiptSignature": "signed-by-server"
}
```

`cards` is the immutable five-card result. `card` mirrors `cards[0]` as a
temporary backward-compatible primary-hit alias for older stored receipts.
Unity must present the array exactly as supplied and must never add, remove, or
reroll cards.

Production must validate the signature or trust a payload already validated by the native shell.

## Product-shell implementation

The Unity `PackSelectionFlow` remains the interaction reference, not the
ownership boundary. The iOS shell now implements this boundary with fake data;
replace the fake receipt store with backend APIs without moving assignment logic
into Unity.

Production shell responsibilities:

- Fetch and display the user’s actual unopened packs.
- Render the collection grid using native or React Native UI.
- Animate the selected tile into a full-screen hero position.
- Request or restore the immutable reveal payload.
- Mount/warm the Unity view behind or beneath the selected tile.
- Send `PrepareReveal`.
- Wait for `sceneReady`.
- Crossfade the native selected tile to the matching Unity pack.
- Route lifecycle, mute, skip, and recovery actions.
- Keep Unity active for five-card grid and hero inspection after `revealComplete`.
- Park Unity after `collectionRequested` completes the return transition.

The checked-in mobile stack is bare React Native. A managed-only Expo project
is not recommended because Unity as a Library requires native iOS project
changes.

## Build and test the iOS product

Generated Unity exports remain local under `unity/Rippies/Build` and are ignored
by Git.

From the repository root:

```sh
mobile/ios/scripts/export-unity-ios.sh simulator
cd mobile
npm start
```

In a second terminal:

```sh
cd mobile
npm run ios
```

The Xcode target runs `mobile/ios/scripts/embed-unity.sh`, builds
`UnityFramework`, embeds its `Data` directory, and signs the simulator
framework. If no local export exists, the app keeps the React Native fallback
so normal shell work remains possible.

## VS Code and Codex setup

The primary development environment is:

- macOS
- Visual Studio Code
- Codex inside Visual Studio Code
- C# Dev Kit and Unity tooling for C# navigation and debugging

Unity generates IDE project files locally. Do not commit them.

To regenerate:

1. Open `unity/Rippies` in Unity Hub.
2. Open the main scene.
3. Choose the external editor under Unity External Tools settings.
4. Use **Assets > Open C# Project** or open any C# script.

## Git and GitHub

Expected remote:

```text
https://github.com/milesdavidlee/rippies.git
```

Commit Unity source:

```text
unity/Rippies/Assets
unity/Rippies/Packages
unity/Rippies/ProjectSettings
```

Never commit:

```text
Library
Temp
Logs
UserSettings
Build
Builds
*.csproj
*.sln
*.slnx
```

All Unity `.meta` files must be committed.

## Verification checklist

Before merging Unity reveal work:

1. Open the scene in Unity `6000.5.5f1`.
2. Enter Play Mode.
3. Confirm six pack options render with distinct palettes.
4. Select a pack with mouse and touch input.
5. Confirm it animates to the hero position.
6. Confirm the payload `packTypeId` and palette match the selection.
7. Tear left to right.
8. Confirm the strip and pack leave the frame.
9. Confirm glow does not intersect the angled card.
10. Confirm the state reaches `Complete`.
11. Return to the pack grid.
12. Confirm the Unity console contains no errors.

## Next implementation milestone

1. Replace fake inventory and receipts with authenticated backend APIs.
2. Add production asset-version download and cache management.
3. Reduce the Unity player size and measure warm-start/memory behavior on a physical iPhone.
4. Continue visual and motion refinement in the Unity source scene.
