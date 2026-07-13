# 主人公 歩行 AI 原稿 01

用途: 主人公の歩行アニメーション検討用 AI 原稿。ゲーム内で直接使用する最終スプライトシートではない。

生成日: 2026-07-13

画像:

- `2026-07-13-main-character-walk-ai-raw-01.png`

## Prompt

```text
Use case: stylized-concept
Asset type: AI raw animation pose sheet for a 2D side-scrolling Unity game
Primary request: Create a walk-cycle pose sheet for the main protagonist of 今日も無職。 based on this established character design: a tiny unemployed Japanese man with messy dark hair, exhausted half-closed eyes, worn dark hoodie, faded pants, old sneakers, and a plain plastic grocery bag. This is an AI raw draft for later manual pixel-art cleanup.
Subject: same one-head-tall protagonist repeated across frames. Head takes about 70% of full body height, very short body, tiny legs, squat readable silhouette. He is tired, slouched, and surviving, not energetic.
Action: tired shuffling walk cycle facing right. Show 6 key poses in one horizontal row: idle/base, left foot tiny step forward with bag lagging back, compressed low point, right foot tiny step forward with bag swinging slightly forward, compressed mirrored low point, return-to-idle pose. Keep the face, hair, hoodie, pants, shoes, proportions, and plain plastic bag identical in every frame.
Style/medium: strict low-resolution pixel art sprite sheet, limited palette, crisp square pixels, no anti-aliasing, no painterly shading, no smooth gradients. Muted low-saturation urban colors. Readable silhouette at small size. Grounded everyday realism with dry black humor and social satire.
Composition/framing: 6 separate frames in one clean horizontal sprite-sheet row. Each frame fits a 48x48 canvas cell with equal spacing and alignment. Character centered in each cell, feet on the same baseline, facing right. Perfectly flat solid #00ff00 chroma-key background for later removal. No shadows, no ground plane, no UI, no labels, no text, no watermark.
Lighting/mood: flat readable game-sprite lighting, mundane Japanese city survival mood, tired and dry, not horror.
Constraints: maintain exact character identity across all frames. The movement is a small exhausted shuffle, not a cute bounce, not a heroic run. Keep details simple enough for manual cleanup.
Avoid: changing character design between frames, tall body proportions, 2-head body, 3-head body, cute chibi mascot, energetic bounce, running pose, detailed anime face, painterly illustration, soft shading, anti-aliased edges, smooth vector art, 3D render, realistic painting, fake Japanese text, logo text, cyberpunk, fantasy, horror monster, cluttered background.
```

## メモ

- 歩行の方向性は「小さく引きずるような歩き」として使う。
- AI 出力の頭身はまだ高めになりやすいため、最終化時に一頭身へ圧縮する。
- 袋の遅れ、肩の沈み、足元の小さい動きを残す。
- Aseprite 等で 48x48 の実セルへ切り直し、輪郭と色数を統一してから `Assets/Art/` へ移す。
