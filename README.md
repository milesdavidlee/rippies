# Rippies

Rippies is a mobile trading-card product with a native collection and commerce shell plus an embedded Unity 6 pack-opening experience.

The repository includes the working Unity vertical slice and an implemented
iOS React Native product pass:

- Discover, Collection, and Profile product tabs.
- Persistent deterministic fake inventory and reveal receipts.
- Unopened-pack browsing, local reveal fallback, and a collected-cards view.
- A six-pack collection grid with mouse and touch selection.
- A selected-pack transition into the reveal position.
- A JSON handoff through `NativeRevealBridge`.
- Procedural foil-pack geometry and left-to-right tearing.
- Two locally licensed Unity reveal experiences with a persistent Profile
  selector; the silver packet burst is the development default.
- A detached top strip, falling pack, five-card fan/grid reveal, rarity glow,
  and per-card 3D inspection.
- Immutable receipt-assigned card content and coordinated pack/card palettes.
- A checked-in Objective-C++ iOS Unity-as-a-Library host bridge.
- A typed React Native bridge contract matching the Unity reveal events.

## Repository layout

```text
.
├── AGENTS.md
├── artifacts/screenshots/
├── docs/
│   ├── IMPLEMENTATION_HANDOFF.md
│   └── trading-card-pack-rip-blueprint.md
├── mobile/
│   ├── ios/
│   ├── android/
│   └── src/
└── unity/Rippies/
    ├── Assets/
    ├── Packages/
    └── ProjectSettings/
```

## Open the Unity prototype

1. Install Unity Editor `6000.5.5f1` with iOS Build Support and Android Build Support.
2. In Unity Hub, choose **Add project from disk**.
3. Select `unity/Rippies`.
4. Open `Assets/Rippies/Scenes/PackReveal.unity`.
5. Enter Play Mode.

The demo starts in the product-style pack grid. Select a pack, let it move into the hero position, then tear from left to right.

## Run the mobile shell

The mobile app uses bare React Native and requires Node.js `22.11` or newer.

```sh
cd mobile
npm install
npm test
npm run ios
```

See [mobile/README.md](mobile/README.md) for the fake-data test flow and the
Unity host integration boundary. Android is intentionally deferred.

## Primary development environment

This project is developed with **Codex inside Visual Studio Code on macOS**. Install C# Dev Kit and Unity support for C# navigation and debugging. Unity generates its solution/project files locally; generated `.sln`, `.slnx`, and `.csproj` files are intentionally ignored.

In Unity, configure the editor under **Unity > Settings/Preferences > External Tools > External Script Editor**.

## Start here

Read [docs/IMPLEMENTATION_HANDOFF.md](docs/IMPLEMENTATION_HANDOFF.md) before changing architecture or creating the mobile shell.
