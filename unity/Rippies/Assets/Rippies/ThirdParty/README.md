# Licensed reveal asset

The enhanced pack reveal can use the locally licensed Fab asset
`Animated Card Loot Pack`.

Place the purchased GLB at:

```text
Assets/Resources/Rippies/ThirdParty/Local/animated_card_loot_pack.glb
```

The local asset directory is intentionally ignored because this repository is
public and must not redistribute purchased marketplace source files. Unity
glTFast imports the GLB as a native Unity asset. The authored card mesh receives
a deterministic face texture from the assigned reveal payload and becomes the
touch-rotatable inspect card after extraction. If the GLB is absent, the reveal
falls back to the checked-in procedural foil pack and generated card.
