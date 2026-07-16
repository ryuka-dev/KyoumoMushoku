# Japanese Coin Locker Two Views v01

Purpose: AIRaw street prop sprites for the KyoumoMushoku 2D side-scrolling Unity project.

Generated: 2026-07-16

Images:

- `coinlocker-japanese-front.png`
- `coinlocker-japanese-side.png`
- `coinlocker-japanese-front-chromakey-v01.png`
- `coinlocker-japanese-side-chromakey-v01.png`
- `coinlocker-japanese-2views-v01-preview.png`

Production notes:

- Drawn directly at final pixel dimensions with Pillow.
- No high-resolution source and no downscale pass were used.
- Transparent sprites use hard 0/255 alpha only.
- Front canvas: `220x180`, sized as a wide locker bank relative to the `140x180` trash can prop.
- Side canvas: `92x180`, sized as the narrower depth view of the same locker bank.
- Palette is intentionally muted and low-count for manual pixel cleanup and Unity nearest-neighbor import.

Prompt/spec:

```text
Use case: stylized-concept
Asset type: AIRaw true-pixel street prop sprites for a 2D side-scrolling Unity game.
Primary request: Generate a Japanese coin-operated station coin locker prop, with two views: front and side. It should read as the common Japanese coin locker / coin return locker bank used around train stations, arcades, and old shopping streets.
Style reference: match the established KyoumoMushoku low-saturation Japanese urban pixel art direction: worn everyday city objects, dry black-humor mood, readable silhouette, not cute, not heroic.
Subject: one bank of coin lockers with small repeated metal doors, keyholes, coin slots, dull instruction plates, grime, small rust chips, and no readable text or logos.
Style/medium: TRUE low-resolution pixel art, direct 1px=1px sprite pixels, crisp square edges, hard alpha, limited muted palette, no anti-aliasing, no painterly shading, no smooth gradients, no pixel-style filter from a high-resolution image.
Composition/framing: isolated prop on transparent background. Front view is a wide locker bank; side view shows the narrower cabinet depth and front lip. Bottom-aligned like other street props.
Color palette: dirty blue-gray metal, muted green-gray shadows, charcoal outlines, dull beige labels, tiny rust and old-key brass accents.
Constraints: no characters, no hands, no open doors, no ground plane, no cast shadow, no UI, no readable Japanese text, no logos, no watermark. Keep details simple enough for manual cleanup and Unity sprite import.
Avoid: high-resolution illustration, 3D render, photorealism, anti-aliased edges, smooth gradients, glossy anime, cyberpunk neon, fantasy, horror gore, excessive clutter.
```
