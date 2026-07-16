# Commercial Street Stall Side-View Props v01

Generated: 2026-07-16

Purpose: strict profile side-view outdoor stall modules for the busiest commercial street of a modest regional city.

Final output:

- `commercial-stall-folding-table-goods-transparent-v01.png`
- `commercial-stall-plastic-crates-produce-transparent-v01.png`
- `commercial-stall-yatai-food-cart-transparent-v01.png`
- `commercial-stall-clothing-rack-sale-transparent-v01.png`
- `commercial-stall-cardboard-clearance-boxes-transparent-v01.png`
- `commercial-stall-tarp-canopy-table-transparent-v01.png`

Each transparent PNG also has a matching `*-chromakey-v01.png` file using solid `#ff00ff` outside the prop.

Preview:

- `commercial-stalls-side-view-v01-preview.png`

Source generation:

- Built-in Codex image generation.
- Source file kept under `C:\Users\LingYun67\.codex\generated_images\019f6881-ab16-7652-a5c6-f07f058f0eb1\ig_02ca8b2cc9f6f3ad016a588afbd9e8819986f2685e8d5ea75c.png`

Post-processing:

- Regenerated after the first pass looked too front-facing; split the strict profile side-view 3x2 asset sheet into six stall modules.
- Removed bright magenta chroma-key background.
- Cropped each module to subject bounds.
- Resized with nearest-neighbor sampling.
- Saved final canvases at exactly `192x144`.
- Quantized to a 48-color palette without dithering.
- Transparent images use hard 0/255 alpha only.

Prompt:

```text
Regenerate the outdoor commercial-street stall assets as TRUE SIDE VIEW ONLY. Generate a sheet of outdoor street-stall assets for the busiest commercial street of a modest third-tier Japanese city. Only pure profile side-view props, no front/back/2+1, no front-facing stall displays, no angled 3/4 view. These are the little stall parts placed outside shop buildings: temporary tables, crates, carts, racks, and small canopies. They should feel busy for a small regional commercial street, but tired, cheap, and slightly worn, not Tokyo, not a festival, not a polished market.

Exactly six separate side-view stall modules in one clean sheet, arranged in a 3 columns x 2 rows grid with generous spacing: folding table with bargain goods and small blank price-card shapes; stacked plastic crates with vegetables or packaged goods; small old food cart / yatai-like pushcart with no readable text; cheap clothing rack with hanging dull clothes; cardboard-box clearance stall with random goods; simple tarp/canopy stall frame with low table below.

TRUE low-resolution pixel art sprite, crisp square pixels, hard stair-step edges, limited palette, no anti-aliasing, no painterly shading, no smooth gradients, no high-resolution illustration look. Match KyoumoMushoku established muted Japanese urban pixel art: tired city survival, dry black humor, everyday exhaustion, readable silhouettes, not cute, not heroic.

One asset sheet on a perfectly flat solid #ff00ff chroma-key background. Side-view only, each stall centered inside its cell with padding. No labels, no text, no UI, no watermark. Isolated props only, no ground plane, no cast shadow, no sky, no storefront facade, no full street scene. No characters, no readable Japanese or English text, no logos, no brand names, no cars, no bicycles, no perspective view.
```

