# Licensed reveal assets

The enhanced pack reveal can use the locally licensed `Animated Card Loot Pack`
and `loot_packet_silver` assets.

Place the purchased GLBs at:

```text
Assets/Resources/Rippies/ThirdParty/Local/animated_card_loot_pack.glb
Assets/Resources/Rippies/ThirdParty/Local/loot_packet_silver.glb
```

The local asset directory is intentionally ignored because this repository is
public and must not redistribute purchased marketplace source files. Unity
glTFast imports each GLB as a native Unity asset. The original asset supplies
the beveled two-sided card visual. The silver asset supplies the default packet
blow-apart and four-card fan, with a fifth receipt card added before the shared
grid. Every card receives deterministic front/back artwork from its assigned
reveal payload and remains touch-rotatable after extraction. If an authored
asset is absent, its route falls back to the available authored or procedural
reveal.
