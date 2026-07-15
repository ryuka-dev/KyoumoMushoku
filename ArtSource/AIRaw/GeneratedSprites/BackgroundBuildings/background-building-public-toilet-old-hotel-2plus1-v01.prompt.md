# Public Toilet and Old Hotel 2+1 View Sets AI Raw v01

Purpose: matched front/left/right building view sets for the side-scrolling city environment.

Generated: 2026-07-15

Final output:

- Public toilet:
  - `background-building-public-toilet-2plus1-front-transparent-v01.png`
  - `background-building-public-toilet-2plus1-left-transparent-v01.png`
  - `background-building-public-toilet-2plus1-right-transparent-v01.png`
- Old cheap hotel / business hotel:
  - `background-building-old-hotel-2plus1-front-transparent-v01.png`
  - `background-building-old-hotel-2plus1-left-transparent-v01.png`
  - `background-building-old-hotel-2plus1-right-transparent-v01.png`

Source generation:

- Built-in Codex image generation.
- Public toilet source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0fd58bc0f660801c016a578781f6b881919944ad02bab46a27.png`
- Old hotel source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0fd58bc0f660801c016a5787c49f6c819180e52411d9dd5679.png`

Post-processing:

- Split each generated triptych into three equal vertical panels: front, left side, right side.
- Removed bright magenta chroma-key background.
- Cropped each panel to subject bounds.
- Resized each view into a `160x220` canvas with nearest-neighbor sampling.
- Preserved relative silhouette proportions: the public toilet remains visually shorter than the old hotel.
- Quantized to a 48-color palette without dithering.
- Saved transparent PNGs with hard 0/255 alpha.

Shared prompt constraints:

```text
Generate one matched 2+1 view set for a Japanese city building. The sheet must show exactly three separate views of the same building: FRONT view, LEFT SIDE view, RIGHT SIDE view. Muted low-saturation Japanese urban pixel art, side-scrolling city survival, dry black humor, everyday exhaustion, readable silhouettes. TRUE low-resolution pixel art, crisp square pixels, hard stair-step edges, limited palette, no anti-aliasing, no smooth gradients, no high-resolution pixel-style filter. One triptych sheet on a perfectly flat solid #ff00ff chroma-key background, three equal vertical panels from left to right: FRONT, LEFT SIDE, RIGHT SIDE. No characters, cars, full street scene, sky, distant buildings, readable text, fake text labels, UI, or watermark.
```
