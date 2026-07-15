# Convenience-Height Filler Buildings AI Raw v01

Purpose: foreground filler architecture modules with approximately the same visual height as the convenience store frontage.

Generated: 2026-07-15

Final output:

- `filler-building-wood-service-wall-transparent-v01.png`
- `filler-building-shutter-storage-bay-transparent-v01.png`
- `filler-building-concrete-utility-annex-transparent-v01.png`
- `filler-building-corrugated-patched-transparent-v01.png`

Reference images:

- User-provided cropped screenshot of the brown wooden/corrugated filler wall near the trash area.
- User-provided cropped screenshot of the gray shuttered filler bay beside the convenience store.

Source generation:

- Built-in Codex image generation.
- Wood service wall source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0f846a3354534ad0016a57858d7c708191bac301ce1b8b22fc.png`
- Shutter storage bay source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0f846a3354534ad0016a5785e162888191b69b4f912c3d55eb.png`
- Concrete utility annex source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0f846a3354534ad0016a5786376ff4819185c8538696fafd01.png`
- Corrugated patched wall source: `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_0f846a3354534ad0016a57868a12a08191b3aad8d769e07cb2.png`

Post-processing:

- Removed bright magenta chroma-key background.
- Cropped each module to subject bounds.
- Resized each sprite to exactly `160x180` with nearest-neighbor sampling.
- Quantized to a 48-color palette without dithering.
- Saved transparent PNGs with hard 0/255 alpha.

Shared prompt constraints:

```text
Foreground filler architecture module for a 2D side-scrolling Unity game, approximately the same height as the convenience store frontage. Muted low-saturation Japanese urban pixel art, tired city survival mood, dry black humor, everyday exhaustion, same scale and lighting as the convenience store area. TRUE low-resolution pixel art, crisp square pixels, limited palette, no high-resolution pixel-style filter. Isolated straight-on orthographic facade on a flat #ff00ff chroma-key background. No ground plane, cast shadow, sky, characters, cars, full street scene, readable text, logos, fake Japanese text, UI, or watermark.
```
