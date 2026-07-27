# Rippies coding instructions

Before implementing changes, read:

1. `docs/IMPLEMENTATION_HANDOFF.md`
2. `docs/trading-card-pack-rip-blueprint.md`
3. `AGENTS.md`

Keep product UI and commerce in the mobile shell. Keep the 3D pack tear, cinematic reveal, and card inspection in Unity.

Never select or reroll the owned card in Unity. Unity receives a server-assigned, persisted reveal payload and only presents it.

The Unity project is `unity/Rippies`, uses editor `6000.5.5f1`, and opens at `Assets/Rippies/Scenes/PackReveal.unity`.

Preserve the native bridge messages:

- App to Unity: `PrepareReveal`, `BeginReveal`, `SkipReveal`, `PauseReveal`, `SetMuted`, `DisposeReveal`.
- Unity to app: `sceneReady`, `tearStarted`, `cardVisible`, `revealComplete`.

Do not commit Unity generated directories or generated IDE solution files.

