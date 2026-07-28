# Rippies iOS product pass

This is a bare React Native `0.86.2` application with checked-in Swift and
Kotlin projects. The current implemented target is iOS; Android is intentionally
out of scope for this pass.

React Native owns the product shell, navigation, fake inventory, card
collection, and reveal recovery. Unity owns the full-screen physical pack
reveal when a local `UnityFramework.framework` export is embedded.

## What is implemented

- Discover, Collection, and Profile tabs with a shared visual language and a
  regular native iOS 26 Liquid Glass tab bar whose bright selection capsule
  animates with the active tab (regular material fallback on earlier iOS).
- Six deterministic fake inventory records with five assigned cards per pack.
- Selectable unopened packs and a real Cards collection segment.
- Tappable stored cards that reopen the exact card directly in Unity's
  front-facing 3D inspector without replaying the pack tear.
- A full-screen local fallback plus the real embedded Unity tear animation.
- Persistent reveal receipts and opened-pack state through AsyncStorage.
- Resume-safe behavior: the assigned card group and presentation state never reroll.
- An iOS Objective-C++ host module matching the Unity bridge contract.
- Optional locally licensed authored pack animation with deterministic
  full-face front and pack-specific back artwork, live extraction camera
  tracking, and a five-card fan/grid/inspect handoff.
- A persistent **Profile → Pack animation** development selector for the
  default **Silver burst** and original **Loot pack** experiences.
- Automatic local fallback when the Unity framework is not embedded.
- Shared color, spacing, radius, motion, and pack tokens in
  `../shared/rippies-design-tokens.json`.

## Requirements

- Node.js `22.11` or newer
- Xcode and CocoaPods for iOS

## Commands

```sh
npm install
npm test
npm run lint
npm run ios
```

For iOS, install Ruby dependencies and pods before the first build:

```sh
bundle install
bundle exec pod install --project-directory=ios
```

## Test the fake-data product flow

1. Run `npm start` in `mobile/`.
2. In another terminal, run `npm run ios`.
3. In **Profile → Pack animation**, choose **Silver burst** or **Loot pack**.
   Silver burst is the default and the choice persists across launches.
4. Open **Collection** and select an unopened pack.
5. Swipe the Unity seal left-to-right. In `LOCAL REVEAL` fallback mode, swipe or
   tap the reveal track. VoiceOver users can activate **Rip pack** to invoke
   Unity's accessible skip-to-reveal path.
6. Verify the assigned cards follow the selected authored animation into the
   five-card group, then watch the group fan into a 3/2 grid.
7. Tap each card to lift it forward, drag horizontally to rotate through the
   front, edge, and custom back, then tap it again to return it to the same slot.
8. Select **View collection** on the Unity completion canvas and verify the
   coordinated close returns to the populated **Cards** segment.
9. In **Collection → Cards**, tap any stored card and verify it opens directly,
   front-facing and correctly framed, in Unity. Drag horizontally through its
   face, edge, and back, then select **Back to collection** and verify the same
   Cards grid returns without changing the receipt.
10. Switch among Discover, Collection, and Profile and verify the bright Liquid
   Glass selection capsule follows the active tab.
11. Reopen the same receipt before confirming, or restart the app, to verify the
   five assigned cards are restored.
12. Without restarting the app, open a second and third pack and verify the
   authored card group keeps the same size, centered pivots, and reachable
   collection action.
13. Use **Profile → Reset fake collection** to replay from a clean state.

To test the real Unity handoff, export the simulator player before running the
app:

```sh
ios/scripts/export-unity-ios.sh simulator
```

For both licensed authored reveals, place the purchased GLBs at:

```text
../unity/Rippies/Assets/Resources/Rippies/ThirdParty/Local/animated_card_loot_pack.glb
../unity/Rippies/Assets/Resources/Rippies/ThirdParty/Local/loot_packet_silver.glb
```

Those files are intentionally ignored and are never redistributed by this
public repository. If an authored asset is missing, Unity falls back to the
available authored or checked-in procedural reveal.

The Xcode target then builds and embeds `UnityFramework` automatically. It
displays `UNITY CONNECTED`, crossfades to the selected Unity pack after
`sceneReady`, and returns after `revealComplete`. Without an export, the app
displays `LOCAL REVEAL` and uses the React Native fallback.

## Unity integration boundary

`src/reveal/contracts.ts` mirrors the immutable reveal payload and Unity event
contract in `../docs/IMPLEMENTATION_HANDOFF.md`.
`src/bridge/UnityRevealBridge.ts` is the TypeScript-facing contract.
`ios/RippiesMobile/RippiesUnityReveal.mm` is the thin iOS host implementation.

The iOS host:

1. Dynamically finds an embedded `UnityFramework.framework`.
2. Warms Unity while the React Native shell remains visible.
3. Sends `PrepareReveal(payloadJson)`.
4. Promotes Unity only after `sceneReady`.
5. Keeps the five-card grid and selected 3D card interactive in Unity after
   `revealComplete`.
6. Emits `collectionRequested` only after the user chooses **View collection**
   and Unity finishes its closing motion.
7. Reuses the same host for direct stored-card inspection, then disposes it
   through **Back to collection** without completing or altering a reveal.
8. Restores the React Native window on disposal or interruption.

Generated Unity Xcode exports remain local and must not be committed. Use
`ios/scripts/export-unity-ios.sh device` for a physical-device export. The
checked-in Xcode build phase builds, embeds, copies Unity `Data`, and signs the
framework for the active iOS platform.
