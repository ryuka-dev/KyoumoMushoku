# Sewer Underpass Floor Tiles v01

Purpose: AIRaw 64x64 ground tiles for the KyoumoMushoku 2D fixed-camera sewer / underground passage scene.

Generated: 2026-07-16

Images:

- `sewer-floor-wet-concrete-repeat-v01.png`
- `sewer-floor-water-channel-center-v01.png`
- `sewer-floor-water-channel-left-bank-v01.png`
- `sewer-floor-water-channel-right-bank-v01.png`
- `sewer-floor-water-channel-top-edge-v01.png`
- `sewer-floor-water-channel-bottom-edge-v01.png`
- `sewer-floor-drain-grate-v01.png`
- `sewer-floor-slimy-step-edge-v01.png`
- `sewer-underpass-floor-tiles-v01-preview.png`
- `sewer-underpass-set-v01-preview.png`

Production notes:

- Drawn directly at `64x64` with Pillow; no high-resolution source and no downscale pass.
- Opaque `RGB` tiles for Unity Tilemap use.
- Limited muted sewer palette, hard pixels, no anti-aliasing.
- Water-channel tiles use a very shallow top-plane cue for a fixed camera looking horizontally with about a 3 degree downward angle.

Prompt/spec:

```text
Use case: stylized-concept
Asset type: AIRaw 64x64 ground tile set for a 2D side-scrolling Unity game.
Primary request: underground passage / sewer floor tiles with classic sewer water channel. Camera is mostly horizontal with a small downward viewing angle, about 3 degrees, so the floor should use a subtle fake 3D perspective rather than a full top-down view.
Subject: wet concrete floor slabs, dark water channel, left/right banks, channel start/end edges, drain grate, slimy step edge.
Style/medium: TRUE low-resolution pixel art, direct 1px=1px sprite pixels, limited palette, no anti-aliasing, no smooth gradients, no high-resolution pixel-style filter.
Color palette: dark damp concrete, green-black water, sickly moss/slime highlights, dull rust, low saturation.
Constraints: 64x64, opaque RGB, tilemap-friendly, no text, no logos, no characters, no prop clutter that would break repetition.
Avoid: photorealism, 3D render, painterly texture, bright fantasy sewer colors, strong top-down perspective.
```
