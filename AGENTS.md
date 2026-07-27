# Rippies agent instructions

Read `docs/IMPLEMENTATION_HANDOFF.md` and `docs/trading-card-pack-rip-blueprint.md` before changing the Unity reveal or creating the mobile shell.

The primary agent workflow is Codex inside Visual Studio Code on macOS.

## Architectural boundaries

- The mobile product shell owns accounts, commerce, inventory, the collection grid, navigation, recovery UI, and API calls.
- Unity owns only the full-screen pack-opening and card-inspection experience.
- The server selects and persists the card result. Never add client-side ownership or prize-selection logic to Unity.
- Preserve the bridge contract documented in `docs/IMPLEMENTATION_HANDOFF.md`.
- The Unity pack grid is an interaction prototype. Production mobile work should recreate that grid in the shell and hand the selected pack payload to Unity.

## Unity project

- Required editor: Unity `6000.5.5f1`.
- Project: `unity/Rippies`.
- Main scene: `unity/Rippies/Assets/Rippies/Scenes/PackReveal.unity`.
- Runtime namespace: `Rippies.Reveal`.
- Use URP and the Input System.
- Keep reveal progression in the explicit `RipState` state machine.
- Use `MaterialPropertyBlock` for per-pack and per-card palette changes.

## Repository hygiene

- Commit `Assets`, `Packages`, and `ProjectSettings`.
- Never commit `Library`, `Temp`, `Logs`, `UserSettings`, `Build`, generated Xcode/Gradle exports, `.sln`, `.slnx`, or `.csproj`.
- Preserve `.meta` files alongside Unity assets.
- Do not overwrite unrelated local work or regenerate all Unity assets unnecessarily.
- Validate scripts, enter Play Mode, run grid → handoff → reveal → collection return, and check the Unity console before completing reveal changes.

## Mobile shell

- If no existing mobile framework is selected, prefer a bare React Native app with checked-in `ios` and `android` projects. Do not use an Expo-managed-only project for Unity as a Library.
- Keep platform-specific Unity host code thin.
- Treat every reveal ID as idempotent and recoverable.
- Do not consider a reveal complete until Unity emits `revealComplete`.
