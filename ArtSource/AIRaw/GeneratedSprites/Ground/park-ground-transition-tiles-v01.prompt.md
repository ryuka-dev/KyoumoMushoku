# Park Ground Transition Tiles v01

Generated: 2026-07-15

Target style: low-resolution pixel art matching the existing KyoumoMushoku muted urban park / street asset style. Final assets are true 64 x 64 pixel tiles, not high-resolution pixel-style renders.

Post-processing:
- Center-cropped each generated bitmap to a square.
- Resized to 64 x 64 with nearest-neighbor sampling.
- Harmonized repeat tile edges for easier Tilemap repetition.
- Composited transition tiles from the same grass, dirt, gravel, and stone paver base materials.
- Quantized the full set to one shared 32-color palette with no dithering.
- Saved as opaque RGB PNG.

Final files:
- `park-ground-grass-repeat-v01.png` - repeatable dull grass tile.
- `park-ground-dirt-repeat-v01.png` - repeatable compacted dirt tile.
- `park-ground-gravel-repeat-v01.png` - repeatable gravel tile.
- `park-ground-stone-paver-repeat-v01.png` - repeatable old stone paver tile.
- `park-ground-grass-dirt-edge-horizontal-v01.png` - horizontal grass to dirt transition.
- `park-ground-grass-dirt-edge-vertical-v01.png` - vertical grass to dirt transition.
- `park-ground-grass-gravel-edge-horizontal-v01.png` - horizontal grass to gravel transition.
- `park-ground-grass-gravel-edge-vertical-v01.png` - vertical grass to gravel transition.
- `park-ground-grass-paver-edge-horizontal-v01.png` - horizontal grass to stone paver transition.
- `park-ground-grass-paver-edge-vertical-v01.png` - vertical grass to stone paver transition.

Source generation order:
1. `ig_06a07a980be7efd4016a57931847908199882d5b7256575925.png` -> grass repeat
2. `ig_06a07a980be7efd4016a579350b78c8199ae94d788894a78c3.png` -> dirt repeat
3. `ig_06a07a980be7efd4016a579389c22c819997a2d6facd0ed331.png` -> gravel repeat
4. `ig_06a07a980be7efd4016a5793e7190481999a4ae5c241fb09b1.png` -> stone paver repeat
5. `ig_06a07a980be7efd4016a5794298358819985e5a877f80f3e69.png` -> grass dirt horizontal
6. `ig_06a07a980be7efd4016a57948b058c8199b1720c2420104033.png` -> grass dirt vertical
7. `ig_06a07a980be7efd4016a5794ec617881998e863e434c5eaadf.png` -> grass gravel horizontal
8. `ig_06a07a980be7efd4016a57952cc1d08199b67d1d59ef437fb6.png` -> grass paver horizontal
9. `ig_06a07a980be7efd4016a57959a55dc819986abdd1a6723300e.png` -> grass gravel vertical
10. `ig_06a07a980be7efd4016a5796067e5081998103c0c904dd2495.png` -> grass paver vertical

