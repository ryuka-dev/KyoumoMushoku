# Urban Public Trash Can AI Raw v01

Purpose: AI raw street prop for a 2D side-scrolling Unity game.

Generated: 2026-07-15

Images:

- `trashcan-urban-public-chromakey-v01.png`
- `trashcan-urban-public-transparent-v01.png`

Source generation:

- Built-in Codex image generation
- Source file kept under `C:\Users\LingYun67\.codex\generated_images\019f63f6-05cd-78e0-a0b9-99ec3794e7f8\ig_08f5730b653f871d016a5708dd444c8191b6ab6261bc9dc1d5.png`

Post-processing:

- Removed bright green chroma-key background.
- Cropped to subject bounds.
- Resized with nearest-neighbor sampling.
- Quantized to a 32-color palette without dithering.
- Saved final canvases at exactly `140x180`.
- Transparent image uses hard 0/255 alpha only.

Prompt:

```text
Use case: stylized-concept
Asset type: AI raw pixel sprite for a 2D side-scrolling Unity game, saved later as a 140 x 180 canvas.
Primary request: Generate one Japanese urban public trash can / garbage bin prop for the game. This is a single isolated scene object, not a background and not an animation.
Style reference: muted low-saturation Japanese city survival pixel art, dry black humor, poor everyday city mood, readable gameplay silhouette, not cute, not heroic.
Subject: one side-view public trash can used as an interactable street prop. Cheap plastic/metal body, slightly dented, dirty lid, small black garbage bag peeking out, subtle grime, abstract wrapper shapes only, no readable text, no logo.
Style/medium: TRUE low-resolution pixel art sprite, crisp square pixels, hard pixel stair-step edges, limited palette, no anti-aliasing, no painterly shading, no smooth gradients, no high-resolution illustration look. Every visible detail should be built from blocky pixels suitable for direct 1px=1px use after cleanup.
Composition/framing: centered upright prop on a perfectly flat solid #00ff00 chroma-key background. Target final canvas is exactly 140 pixels wide by 180 pixels high, with the trash can occupying most of the height but leaving transparent padding around it. Slight 3/4 front angle is okay only if it remains readable in a side-scrolling scene.
Color palette: muted gray-green, dirty blue-gray, dark charcoal bag, dull rust and grime accents, readable even when desaturated.
Constraints: no character, no hands, no rummaging pose, no ground plane, no cast shadow, no contact shadow, no UI, no labels, no text, no watermark. Keep the silhouette simple enough for manual pixel cleanup and Unity sprite import.
Avoid: high resolution pixel-style filter, soft edges, anti-aliasing, smooth gradients, 3D render, photorealism, painterly illustration, anime gloss, cute mascot, readable Japanese text, logos, cyberpunk neon, fantasy, horror gore, excessive clutter.
```
