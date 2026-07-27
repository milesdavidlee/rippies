# Rippies implementation handoff

## Purpose

This document is the primary context file for **Codex running inside Visual Studio Code on macOS**. It can also be used by another engineer or coding agent continuing the Rippies mobile and Unity implementation.

Rippies is designed as a hybrid product:

- The normal mobile application owns the collection, store, account, inventory, navigation, and backend communication.
- Unity is embedded as a full-screen native view only for opening a selected pack and inspecting its revealed card.

The current working implementation is a Unity vertical slice. It intentionally includes a product-style 3D pack grid so the complete interaction can be tested before the native shell exists.

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

- Six animated 3D foil packs in a touch/click grid.
- Independent themes and coordinated palettes.
- Selected pack animates into the reveal anchor.
- Selected `packTypeId`, palette, and generated card payload pass through `NativeRevealBridge`.
- Left-to-right constrained tear interaction.
- Procedurally inflated foil geometry with crimping, wrinkles, and jagged seam.
- Top strip fully detaches and exits the frame.
- Pack falls below frame and is disabled.
- Card emerges with generated name, rarity, archetype, stats, serial, flavor text, and pattern art.
- Glow remains behind the card at inspect angles.
- Soft, clamped drag orbit and continuous idle motion.
- Return from completed reveal to the pack grid.

The last verified flow was:

```text
Browse
  -> select Chrome/Solar pack
  -> selected pack moves to hero position
  -> bridge prepares rippies_chrome or rippies_solar payload
  -> Unity Ready
  -> tear/reveal
  -> Complete
  -> return to Browse
```

## Important source files

```text
Assets/Rippies/Runtime/PackSelectionFlow.cs
Assets/Rippies/Runtime/NativeRevealBridge.cs
Assets/Rippies/Runtime/PackRipController.cs
Assets/Rippies/Runtime/SwipeTearInteractor.cs
Assets/Rippies/Runtime/FoilPackDeformer.cs
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

Production must validate the signature or trust a payload already validated by the native shell.

## Product-shell implementation

The Unity `PackSelectionFlow` is the interaction reference, not the final ownership boundary.

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
- Unmount or park Unity after `revealComplete`.

The cleanest initial stack, if no mobile shell exists, is bare React Native with Swift and Kotlin host modules. A managed-only Expo project is not recommended because Unity as a Library requires native iOS and Android project changes.

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

1. Create the bare mobile shell with checked-in `ios` and `android` projects.
2. Export Unity as a Library for iOS and Android.
3. Implement `PackRevealView` on each platform.
4. Recreate the collection grid in the shell.
5. Crossfade the selected native pack into Unity after `sceneReady`.
6. Add app lifecycle recovery and idempotent reveal restoration.
7. Connect real inventory and reveal APIs.
