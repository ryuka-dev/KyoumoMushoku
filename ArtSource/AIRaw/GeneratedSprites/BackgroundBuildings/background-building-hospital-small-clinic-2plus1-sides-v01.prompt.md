# Small Clinic Hospital Side Views AI Raw v01

Purpose: left and right side-view companions for `background-building-hospital-small-clinic-transparent-v01.png`.

Generated: 2026-07-16

Final output:

- `background-building-hospital-small-clinic-2plus1-left-chromakey-v01.png`
- `background-building-hospital-small-clinic-2plus1-left-transparent-v01.png`
- `background-building-hospital-small-clinic-2plus1-right-chromakey-v01.png`
- `background-building-hospital-small-clinic-2plus1-right-transparent-v01.png`

Source generation:

- Built-in Codex image generation.
- Source file kept under `C:\Users\LingYun67\.codex\generated_images\019f6881-ab16-7652-a5c6-f07f058f0eb1\ig_040b962d8dd4cbf3016a58386ce5408191b4d7ce5725378d18.png`
- Front master for style/proportion reference: `background-building-hospital-small-clinic-transparent-v01.png`

Post-processing:

- Split the generated side-view sheet into left and right panels.
- Removed bright magenta chroma-key background.
- Cropped each side view to subject bounds.
- Resized with nearest-neighbor sampling.
- Saved final canvases at exactly `320x260`.
- Quantized to a 48-color palette without dithering.
- Transparent images use hard 0/255 alpha only.
- Chroma-key images use solid `#ff00ff` outside the building.

Construction notes:

- The side views keep the same height, roof line, muted hospital palette, window rhythm, and AI raw wall texture language as the front view.
- The side walls are intentionally flat: no exterior AC unit, no exposed side wiring, no exterior water pipe, no drain pipe, no conduit, no ladder, and no other protruding wall fixtures.
- Side details are limited to flush windows, painted grime, floor bands, a flat wall sign/cross panel, and a shallow flat door/recess.

Prompt:

```text
Use case: stylized-concept
Asset type: matched left and right side-view AI raw pixel sprites for the existing small Japanese clinic hospital building in a 2D side-scrolling Unity game, final processed as two 320 x 260 assets.
Primary request: Redraw the LEFT SIDE and RIGHT SIDE views for the same small hospital/clinic building whose established FRONT view is a muted low-resolution Japanese urban pixel-art hospital facade. The side views must visually match that front asset's AI raw pixel-art style: dense imperfect pixel texture, stained off-white concrete, faded blue-gray glass, dark charcoal pixel outlines, muted teal/green medical accents, urban exhaustion mood. The previous side views looked too clean and programmatic; make these feel like the original AI raw hospital facade, not a flat vector diagram.
Subject: exactly two separate side-view building sprites on one sheet: LEFT SIDE on the left half, RIGHT SIDE on the right half. Each side is a flat side wall of the same hospital: same height, same roofline feeling, same concrete material, same window scale and hospital palette as the front. Side walls have flush rectangular windows, subtle grime, painted wall bands, small flat painted medical cross/sign panels, simple shallow flush side door/recess if needed.
Critical constraint: the side walls must have NO protruding parts. Do not draw exterior AC units, no exposed electric wires, no water pipes, no drainpipes, no conduits, no vents sticking out, no ladders, no awnings, no balconies, no cables, no utility boxes, no wall-mounted machinery. All visible details must be flat on the wall surface only.
Style/medium: TRUE low-resolution pixel art sprite, crisp square pixels, hard stair-step edges, limited palette, no anti-aliasing, no painterly shading, no smooth gradients, no high-resolution illustration look. Every detail should be blocky pixels suitable for direct 1px=1px use after cleanup.
Composition/framing: one horizontal two-panel sheet on a perfectly flat solid #ff00ff chroma-key background. Left half contains only the LEFT SIDE view, right half contains only the RIGHT SIDE view. No labels, no text, no arrows, no UI, no watermark. Keep each building fully separated from the background with generous padding. Orthographic side view, not perspective street scene.
Color palette: muted off-white concrete, dirty gray, faded blue-gray glass, desaturated teal/green medical accents, dark charcoal outlines, dull beige shadows. Must remain readable when desaturated.
Constraints: no ground plane, no cast shadow, no contact shadow, no sky, no surrounding buildings, no characters, no cars, no ambulances, no readable text, no logos. Match the established hospital front style and proportions; side surfaces only.
Avoid: clean vector rectangles, overly simple block icons, high resolution pixel-style filter, soft edges, anti-aliasing, smooth gradients, 3D render, photorealism, painterly illustration, anime gloss, cute mascot style, cyberpunk neon, horror hospital, gore, excessive clutter, readable generated text, any protruding AC/wires/pipes/vents.
```
