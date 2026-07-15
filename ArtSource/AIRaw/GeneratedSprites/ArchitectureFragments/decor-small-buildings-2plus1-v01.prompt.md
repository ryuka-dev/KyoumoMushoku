# Decorative Small Building 2+1 View Sets AI Raw v01

Purpose: matched front/left/right decorative foreground building modules similar in role to `filler-building-corrugated-patched-transparent-v01.png`.

Generated: 2026-07-15

Final output:

- Patched corrugated utility shed:
  - `decor-building-corrugated-utility-shed-2plus1-front-transparent-v01.png`
  - `decor-building-corrugated-utility-shed-2plus1-left-transparent-v01.png`
  - `decor-building-corrugated-utility-shed-2plus1-right-transparent-v01.png`
- Electrical meter / utility control room:
  - `decor-building-meter-room-2plus1-front-transparent-v01.png`
  - `decor-building-meter-room-2plus1-left-transparent-v01.png`
  - `decor-building-meter-room-2plus1-right-transparent-v01.png`
- Closed guard kiosk / street management booth:
  - `decor-building-guard-kiosk-2plus1-front-transparent-v01.png`
  - `decor-building-guard-kiosk-2plus1-left-transparent-v01.png`
  - `decor-building-guard-kiosk-2plus1-right-transparent-v01.png`
- Squat shuttered maintenance shed:
  - `decor-building-shutter-maintenance-2plus1-front-transparent-v01.png`
  - `decor-building-shutter-maintenance-2plus1-left-transparent-v01.png`
  - `decor-building-shutter-maintenance-2plus1-right-transparent-v01.png`

Source generation:

- Built-in Codex image generation.
- Corrugated utility shed source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_040433d588996b27016a578dd316888194a7be3704a8487cc0.png`
- Meter room source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_040433d588996b27016a578e0e1f5c819491e94d903106ace3.png`
- Guard kiosk source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_040433d588996b27016a578e474e948194a14e26b634addabd.png`
- Shutter maintenance source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_040433d588996b27016a578e86fb908194bd4ffa0346645431.png`

Post-processing:

- Split each generated triptych into three equal vertical panels: front, left side, right side.
- Removed bright magenta chroma-key background.
- Cropped each panel to subject bounds.
- Resized each view into a `160x180` canvas with nearest-neighbor sampling.
- Quantized to a 48-color palette without dithering.
- Saved transparent PNGs with hard 0/255 alpha.

Shared prompt constraints:

```text
Generate one matched 2+1 view set for a small decorative foreground building. The sheet must show exactly three separate views of the same small building: FRONT view, LEFT SIDE view, RIGHT SIDE view. Match the established foreground filler style: muted low-saturation Japanese urban pixel art, tired city survival mood, dry black humor, everyday exhaustion, same scale as convenience-store-height filler buildings. TRUE low-resolution pixel art, crisp square pixels, limited palette, no anti-aliasing, no smooth gradients, no high-resolution pixel-style filter. One triptych sheet on a flat #ff00ff chroma-key background, three equal vertical panels from left to right. No characters, cars, full street scene, sky, distant buildings, readable text, fake text labels, UI, or watermark.
```
