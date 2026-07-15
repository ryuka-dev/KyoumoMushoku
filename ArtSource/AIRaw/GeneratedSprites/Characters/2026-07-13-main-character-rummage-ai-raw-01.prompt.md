# 主人公 ゴミ箱漁り AI 原稿 01

用途: 主人公のゴミ箱漁りアニメーション検討用 AI 原稿。ゲーム内で直接使用する最終スプライトシートではない。

生成日: 2026-07-13

画像:

- `2026-07-13-main-character-rummage-ai-raw-01.png`

## Prompt

```text
Use case: stylized-concept
Asset type: AI raw animation pose sheet for a 2D side-scrolling Unity game
Primary request: Create a trash-rummaging action pose sheet for the main protagonist of 今日も無職。 based on this established character design: a tiny unemployed Japanese man with messy dark hair, exhausted half-closed eyes, worn dark hoodie, faded pants, old sneakers, and a plain plastic grocery bag. This is an AI raw draft for later manual pixel-art cleanup.
Subject: same one-head-tall protagonist repeated across frames, interacting with one simple side-view trash can. Head takes about 70% of full body height, very short body, tiny legs, squat readable silhouette. Keep identity, clothes, hair, and proportions consistent across every frame.
Action: rummaging through a trash can, facing right. Show 10 key poses in one horizontal row: tired idle near trash can, step closer, lean forward, reach hand into trash can, rummage loop pose 1, rummage loop pose 2 with shoulders shaking, deeper reach, pull hand out, inspect result with empty tired expression, return to slouched idle. Include a simple trash can in the same relative position for action clarity.
Style/medium: strict low-resolution pixel art sprite sheet, limited palette, crisp square pixels, no anti-aliasing, no painterly shading, no smooth gradients. Muted low-saturation urban colors. Readable silhouette at small size. Grounded everyday realism with dry black humor and social satire.
Composition/framing: 10 separate frames in one clean horizontal sprite-sheet row. Each frame fits a 64x48 canvas cell with equal spacing and alignment. Feet on the same baseline, side view facing right. Perfectly flat solid #00ff00 chroma-key background for later removal. No shadows, no ground plane, no UI, no labels, no text, no watermark.
Lighting/mood: flat readable game-sprite lighting, mundane Japanese city survival mood, tired and dry, not horror, not slapstick comedy.
Constraints: maintain exact character identity across all frames. The rummaging is exhausted, practical, and slightly shameful, not cute, not energetic. The trash can is a simple readable game prop, not oversized. Keep details simple enough for manual cleanup.
Avoid: changing character design between frames, tall body proportions, 2-head body, 3-head body, cute chibi mascot, energetic cartoon rummaging, slapstick pose, detailed anime face, painterly illustration, soft shading, anti-aliased edges, smooth vector art, 3D render, realistic painting, fake Japanese text, logo text, cyberpunk, fantasy, horror monster, cluttered background.
```

## メモ

- 動作段階の参考として使う。
- `start`、`loop`、`finish`、`interrupted` に分解して最終アニメーションへ整理する。
- ゴミ箱の位置とプレイヤーの足元基準を最終素材で固定する。
- 警察の警告で中断される仕様に備え、手を引っ込める中断フレームを別途作る。
