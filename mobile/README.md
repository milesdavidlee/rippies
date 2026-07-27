# Rippies mobile shell

This is a bare React Native `0.86.2` application with checked-in Swift and
Kotlin projects. React Native owns collection, commerce, inventory,
navigation, API communication, and recovery. Unity will be embedded only as a
full-screen pack reveal and card-inspection view.

## Requirements

- Node.js `22.11` or newer
- Xcode and CocoaPods for iOS
- Android Studio and JDK 17 for Android

## Commands

```sh
npm install
npm test
npm run lint
npm run ios
npm run android
```

For iOS, install Ruby dependencies and pods before the first build:

```sh
bundle install
bundle exec pod install --project-directory=ios
```

## Unity integration boundary

`src/reveal/contracts.ts` mirrors the immutable reveal payload and Unity event
contract in `../docs/IMPLEMENTATION_HANDOFF.md`.
`src/bridge/UnityRevealBridge.ts` is the TypeScript-facing contract for the
future thin Swift and Kotlin host modules.

The native implementation must:

1. Restore or request the server-assigned reveal payload.
2. Mount and warm the Unity view.
3. Send `PrepareReveal(payloadJson)`.
4. Present only after `sceneReady`.
5. Treat a reveal as complete only after `revealComplete`.
6. Preserve the receipt for idempotent recovery after interruption.
