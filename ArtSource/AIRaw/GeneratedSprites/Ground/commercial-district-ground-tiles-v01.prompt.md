# Commercial District Ground Tiles v01

Generated: 2026-07-16

Purpose: top-down square ground tiles for the busiest commercial district.

Target style: low-resolution pixel art matching the existing KyoumoMushoku muted urban street / convenience-store ground asset style. Final assets are true `64x64` pixel tiles, not high-resolution pixel-style renders.

Post-processing:

- Generated as deterministic low-resolution pixel tiles.
- Saved each final tile at exactly `64x64`.
- Quantized the full set to one shared 24-color palette with no dithering.
- Saved as opaque RGB PNG.

Base repeat tiles:

- `commercial-district-asphalt-repeat-v01.png` - cleaner commercial-road asphalt for repeated copies.
- `commercial-district-asphalt-worn-repeat-v01.png` - worn asphalt variant for sparse road variation.
- `commercial-district-sidewalk-concrete-repeat-v01.png` - main commercial sidewalk concrete.
- `commercial-district-sidewalk-concrete-stained-v01.png` - stained sidewalk concrete variant.
- `commercial-district-plaza-paver-repeat-v01.png` - denser plaza / shopping-street paver pattern.
- `commercial-district-tactile-paving-repeat-v01.png` - Japanese tactile paving tile strip for busy pedestrian flow.

Transition and functional tiles:

- `commercial-district-sidewalk-asphalt-edge-horizontal-v01.png` - sidewalk to asphalt horizontal curb.
- `commercial-district-sidewalk-asphalt-edge-vertical-v01.png` - sidewalk to asphalt vertical curb.
- `commercial-district-sidewalk-asphalt-corner-v01.png` - curb corner transition.
- `commercial-district-paver-concrete-edge-horizontal-v01.png` - paver to concrete horizontal transition.
- `commercial-district-paver-concrete-edge-vertical-v01.png` - paver to concrete vertical transition.
- `commercial-district-tactile-paver-edge-horizontal-v01.png` - tactile paving to paver horizontal transition.
- `commercial-district-tactile-paver-edge-vertical-v01.png` - tactile paving to paver vertical transition.
- `commercial-district-crosswalk-asphalt-horizontal-v01.png` - crosswalk marking over asphalt.
- `commercial-district-crosswalk-asphalt-vertical-v01.png` - crosswalk marking over asphalt.
- `commercial-district-sidewalk-drainage-grate-v01.png` - sidewalk drainage grate detail tile.

Preview:

- `commercial-district-ground-tiles-v01-preview.png`

Design constraints:

- Top-down orthographic square ground tiles only.
- Full square canvas filled edge to edge.
- No perspective, cast shadow, transparent background, text, logo, characters, props, UI, or watermark.
- Use repeated base tiles for identical road / sidewalk sections and separate transition tiles where materials meet.
