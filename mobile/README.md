# Rippies iOS product pass

This is a bare React Native `0.86.2` application with checked-in Swift and
Kotlin projects. The current implemented target is iOS; Android is intentionally
out of scope for this pass.

React Native owns the product shell, navigation, fake inventory, card
collection, and reveal recovery. Unity owns the full-screen physical pack
reveal when a local `UnityFramework.framework` export is embedded.

## What is implemented

- Discover, Collection, and Profile tabs with a shared visual language and a
  native iOS 26 Liquid Glass dark-mode tab bar (dark material fallback on
  earlier iOS).
- Six deterministic fake inventory records with assigned card payloads.
- Selectable unopened packs and a real Cards collection segment.
- A full-screen local fallback plus the real embedded Unity tear animation.
- Persistent reveal receipts and opened-pack state through AsyncStorage.
- Resume-safe behavior: the assigned card and presentation state never reroll.
- An iOS Objective-C++ host module matching the Unity bridge contract.
- Optional locally licensed authored pack animation with a deterministic
  receipt-specific card face and direct 3D card handoff.
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
3. Open **Collection** and select an unopened pack.
4. Swipe the Unity seal left-to-right. In `LOCAL REVEAL` fallback mode, swipe or
   tap the reveal track. VoiceOver users can activate **Rip pack** to invoke
   Unity's accessible skip-to-reveal path.
5. Verify the card coming out of the authored wrapper is the same object that
   remains on screen, then drag it horizontally and vertically to inspect it in
   3D.
6. Select **View collection** on the Unity completion canvas and verify the
   coordinated close returns to the populated **Cards** segment.
7. Reopen the same receipt before confirming, or restart the app, to verify the
   assigned card is restored.
8. Without restarting the app, open a second and third pack and verify the
   authored card keeps the same size, center pivot, and reachable collection
   action.
9. Use **Profile → Reset fake collection** to replay from a clean state.

To test the real Unity handoff, export the simulator player before running the
app:

```sh
ios/scripts/export-unity-ios.sh simulator
```

For the licensed authored reveal, place the purchased GLB at:

```text
../unity/Rippies/Assets/Resources/Rippies/ThirdParty/Local/animated_card_loot_pack.glb
```

That file is intentionally ignored and is never redistributed by this public
repository. If it is missing, Unity uses the checked-in procedural pack and
generated card.

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
5. Keeps the card interactive in Unity after `revealComplete`.
6. Emits `collectionRequested` only after the user chooses **View collection**
   and Unity finishes its closing motion.
7. Restores the React Native window on disposal or interruption.

Generated Unity Xcode exports remain local and must not be committed. Use
`ios/scripts/export-unity-ios.sh device` for a physical-device export. The
checked-in Xcode build phase builds, embeds, copies Unity `Data`, and signs the
framework for the active iOS platform.
