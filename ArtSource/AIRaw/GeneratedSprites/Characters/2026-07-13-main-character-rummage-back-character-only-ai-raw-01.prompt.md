# 主人公 ゴミ箱漁り背面人物のみ AI 原稿 01

用途: ゴミ箱漁りアニメーションの人物側フレーム原稿。人物は背面または斜め背面で、シーン側のゴミ箱に向かって漁る前提。

生成日: 2026-07-13

画像:

- `2026-07-13-main-character-rummage-back-character-only-ai-raw-01.png`

## Prompt

```text
Use case: stylized-concept
Asset type: AI raw character animation pose sheet for a 2D side-scrolling Unity game
Primary request: Regenerate only the protagonist's trash-rummaging character animation frames for 今日も無職。 The character should be seen from the back or three-quarter back, facing an imaginary trash can in front of him. Do not draw the trash can. The trash can is a separate scene object.
Style reference: match the established overall gameplay style preview stored in ArtSource/References/Style: muted low-saturation Japanese urban pixel art, dry black humor, poor everyday city survival, readable gameplay silhouettes, not cute, not heroic.
Subject: the same tiny unemployed Japanese male protagonist: messy dark hair seen from behind, worn dark hoodie, faded pants, old sneakers, plain plastic grocery bag with no text. Tired and poor, but readable. His face is mostly not visible because he is facing away from the camera toward the trash can.
Action: character-only rummaging animation, back view or three-quarter back view, intended to play in front of a stationary scene trash can. Show 8 clean key frames in one horizontal row: tired standing back view, step closer and bend forward, both arms reach forward/down into the imaginary trash can, rummage loop pose 1 with shoulders compressed, rummage loop pose 2 with small arm shift, deeper reach with head lowered, pull hand back, inspect result while still mostly back-facing then return toward tired idle. The animation sheet presentation must be clean, readable, and practical, while the character's movement itself remains tired and awkward.
Style/medium: strict low-resolution pixel art sprite-sheet draft, limited palette, crisp square pixels, no anti-aliasing, no painterly shading, no smooth gradients. Muted low-saturation urban colors. Readable silhouette at small size.
Composition/framing: one clean horizontal row, 8 separate frames, equal cell size, feet aligned on the same baseline, consistent character height and proportions across frames. Flat solid #00ff00 chroma-key background. No trash can, no scene, no ground, no shadow, no UI, no labels, no text, no watermark.
Character proportions: short squat game character, close to one-head-tall but with arms and legs readable enough for animation. Keep same hoodie, pants, shoes, hair mass, and bag across every frame.
Constraints: prioritize animation usability, clear frame separation, consistent pivot, and clean silhouette. Hands and upper body should move into the same imaginary interaction zone directly in front of the character so the animation can align with a separate trash can sprite. No actual trash can.
Avoid: side-view rummaging, profile-only pose, trash can, garbage bags, props other than the plain grocery bag, background, UI, labels, watermark, fake Japanese text, changing character design between frames, tall realistic body, cute mascot, energetic comedy, slapstick pose, clean heroic movement, painterly illustration, anti-aliased edges, smooth gradients, 3D render, anime portrait, cyberpunk, fantasy, horror.
```

## メモ

- 横向きではなく、背面または斜め背面でゴミ箱に向かう案。
- ゴミ箱はシーン側に置き、人物アニメには含めない。
- 最終実装では、人物の手元がゴミ箱の開口部に合うようにピボットと重なり順を調整する。
- 立ち絵や歩行とは向きが異なるため、ゴミ箱漁り専用の interaction animation として扱う。
