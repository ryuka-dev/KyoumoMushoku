# Small Clinic Hospital Building AI Raw v01

Purpose: AI raw background building sprite for a 2D side-scrolling Unity game.

Generated: 2026-07-16

Final output:

- `background-building-hospital-small-clinic-chromakey-v01.png`
- `background-building-hospital-small-clinic-transparent-v01.png`

Source generation:

- Built-in Codex image generation.
- Source file kept under `C:\Users\LingYun67\.codex\generated_images\019f6881-ab16-7652-a5c6-f07f058f0eb1\ig_0fefb0d2f0866dcb016a58323992688191907bc23010b1493c.png`
- Style reference: `ArtSource/References/Style/2026-07-13-overall-gameplay-style-preview.png`

Post-processing:

- Removed bright magenta chroma-key background.
- Cropped to subject bounds.
- Resized with nearest-neighbor sampling.
- Quantized to a 48-color palette without dithering.
- Saved final canvases at exactly `320x260`.
- Transparent image uses hard 0/255 alpha only.

Prompt:

```text
Use case: stylized-concept
Asset type: AI raw pixel sprite for a 2D side-scrolling Unity game, final processed canvas target 320 x 260 pixels.
Primary request: Generate one small Japanese urban neighborhood hospital / clinic building facade for the game 今日も無職。 This is a single isolated background building asset, not a full street scene and not an illustration.
Style reference: match the existing project style: muted low-saturation Japanese urban pixel art, tired city survival mood, dry black humor, everyday exhaustion, readable gameplay silhouette, not cute, not heroic. Similar production intent to low-resolution props where a trash can final canvas is 140 x 180 and building fronts are about 160 x 220, but this hospital should be a larger believable building.
Subject: front-facing compact hospital building, about three floors plus a small roof utility area, modest city clinic rather than huge modern medical center. Include a small entrance canopy, sliding glass doors, rows of simple square windows, a muted medical cross sign panel without readable text, small wall-mounted vents and pipes, slightly stained off-white concrete, faded blue-gray window glass, dull green-gray trim, a few blank notice panels near the entrance. No readable Japanese or English text, no logo, no people, no vehicles.
Style/medium: TRUE low-resolution pixel art sprite, crisp square pixels, hard stair-step edges, limited palette, no anti-aliasing, no painterly shading, no smooth gradients, no high-resolution illustration look. Every visible detail should be made from blocky pixels suitable for direct 1px=1px use after cleanup.
Composition/framing: centered upright facade on a perfectly flat solid #ff00ff chroma-key background. Keep the building fully separated from the background with generous padding. The building should occupy most of a 320 x 260 final canvas, wider than a small prop, with believable architectural proportions for a small hospital. Orthographic front view with very slight side-scrolling readability, no perspective street scene.
Color palette: muted off-white concrete, dirty gray, faded blue-gray glass, desaturated teal/green medical accents, dark charcoal outlines, tiny dull red warning/accent only if useful. Must remain readable when desaturated.
Constraints: flat #ff00ff background only; no ground plane, no cast shadow, no contact shadow, no sky, no surrounding buildings, no full street scene, no characters, no cars, no ambulances, no UI, no labels, no readable text, no watermark. Keep details simple enough for manual pixel cleanup and Unity sprite import.
Avoid: high resolution pixel-style filter, soft edges, anti-aliasing, smooth gradients, 3D render, photorealism, painterly illustration, anime gloss, cute mascot style, cyberpunk neon, fantasy, horror hospital, gore, excessive clutter, readable generated text.
```
