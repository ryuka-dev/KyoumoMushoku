# Park Series AI Raw v01

Purpose: park-area prop sprites and one repeatable park ground tile.

Generated: 2026-07-15

Final output:

- `StreetProps/park-slide-old-transparent-v01.png`
- `StreetProps/park-swing-set-transparent-v01.png`
- `StreetProps/park-sandbox-concrete-transparent-v01.png`
- `StreetProps/park-low-fence-railing-transparent-v01.png`
- `StreetProps/park-noticeboard-transparent-v01.png`
- `Ground/park-ground-grass-dirt-repeat-v01.png`

Existing park-adjacent assets intentionally not regenerated:

- `StreetProps/bench-park-transparent-v01.png`
- `StreetProps/street-tree-broadleaf-transparent-v01.png`
- `StreetProps/streetlamp-public-transparent-v01.png`
- `watertap-public-transparent-v01.png`

Source generation:

- Built-in Codex image generation.
- Park slide source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0227fb291d8b3c88016a578fae8bc88199b4348b01b0acab88.png`
- Swing set source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0227fb291d8b3c88016a578ffcdda081999d5dae9392e02cd8.png`
- Sandbox source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0227fb291d8b3c88016a579024a55481999906adce7e4f8ee3.png`
- Low fence source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0227fb291d8b3c88016a57905a656481999f912d413d5e9fb1.png`
- Noticeboard source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0227fb291d8b3c88016a579097dea881998100ab63e9558030.png`
- Grass/dirt tile source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0227fb291d8b3c88016a5790e8a6ec8199bd921f69f4f64218.png`

Post-processing:

- Removed bright magenta chroma-key background from prop sprites.
- Cropped each prop to subject bounds.
- Resized each prop into its target canvas with nearest-neighbor sampling.
- Quantized prop sprites to a 48-color palette without dithering.
- Saved prop sprites as transparent PNGs with hard 0/255 alpha.
- Cropped the ground tile to square, resized to exactly `64x64`, and quantized to 32 colors.

Shared prompt constraints:

```text
Park series asset for a 2D side-scrolling Unity game. Match 今日も無職。 established style: muted low-saturation Japanese urban pixel art, tired city survival mood, dry black humor, everyday exhaustion, readable silhouette, not cute, not heroic. TRUE low-resolution pixel art, crisp square pixels, limited palette, no anti-aliasing, no smooth gradients, no high-resolution pixel-style filter. No characters, readable text, logos, UI, or watermark.
```
