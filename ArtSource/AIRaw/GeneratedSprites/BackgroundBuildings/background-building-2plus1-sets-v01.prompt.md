# Background Building 2+1 View Sets AI Raw v01

Purpose: matched front/left/right building view sets for the side-scrolling city environment.

Generated: 2026-07-15

Final output:

- Old office/commercial block:
  - `background-building-office-old-2plus1-front-transparent-v01.png`
  - `background-building-office-old-2plus1-left-transparent-v01.png`
  - `background-building-office-old-2plus1-right-transparent-v01.png`
- Narrow mixed-use building:
  - `background-building-mixeduse-narrow-2plus1-front-transparent-v01.png`
  - `background-building-mixeduse-narrow-2plus1-left-transparent-v01.png`
  - `background-building-mixeduse-narrow-2plus1-right-transparent-v01.png`
- Low-rise apartment building:
  - `background-building-apartment-lowrise-2plus1-front-transparent-v01.png`
  - `background-building-apartment-lowrise-2plus1-left-transparent-v01.png`
  - `background-building-apartment-lowrise-2plus1-right-transparent-v01.png`

Source generation:

- Built-in Codex image generation.
- Style reference: `ArtSource/References/Style/2026-07-13-overall-gameplay-style-preview.png`
- Office source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0cfbcf91db27815b016a57805fb11c8199a76db5854bfe8c5b.png`
- Mixed-use source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0cfbcf91db27815b016a57801f40f881998bfde78885ea6d60.png`
- Apartment source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0cfbcf91db27815b016a577fdd5124819987c6c013275aed64.png`

Post-processing:

- Split each generated triptych into three equal vertical panels: front, left side, right side.
- Removed bright magenta chroma-key background.
- Cropped each panel to subject bounds.
- Resized each view to exactly `160x220` with nearest-neighbor sampling.
- Quantized to a 48-color palette without dithering.
- Saved transparent PNGs with hard 0/255 alpha.

Shared prompt constraints:

```text
Generate one matched 2+1 view set for a Japanese city building. The sheet must show exactly three separate views of the same building: FRONT view, LEFT SIDE view, RIGHT SIDE view. Use the provided overall gameplay style preview as style reference only. Muted low-saturation Japanese urban pixel art, side-scrolling city survival, dry black humor, everyday exhaustion, readable silhouettes. TRUE low-resolution pixel art, crisp square pixels, hard stair-step edges, limited palette, no anti-aliasing, no smooth gradients, no high-resolution pixel-style filter. One triptych sheet on a perfectly flat solid #ff00ff chroma-key background, three equal vertical panels from left to right: FRONT, LEFT SIDE, RIGHT SIDE. No characters, cars, full street scene, sky, distant buildings, readable text, fake text labels, UI, or watermark.
```
