# ゴミ箱 シーンプロップ AI 原稿 01

用途: シーン側に配置するゴミ箱プロップの AI 原稿。人物アニメーションには含めない。

生成日: 2026-07-13

画像:

- `2026-07-13-trash-can-scene-prop-ai-raw-01.png`

## Prompt

```text
Use case: stylized-concept
Asset type: AI raw scene prop for a 2D side-scrolling Unity game
Primary request: Generate the scene-side trash can prop for 今日も無職。 This is not an animation and contains no character.
Style reference: match the established overall gameplay style preview stored in ArtSource/References/Style: muted low-saturation Japanese urban pixel art, dry black humor, poor everyday city survival, readable gameplay silhouettes, not cute, not heroic.
Subject: one side-view Japanese urban trash can used as an interactable scene object. It should feel like a convenience-store or alley trash bin: cheap plastic/metal body, slightly dented, dirty bags peeking out, a few abstract wrappers visible, no readable text, no logo. It must be readable as a gameplay object at small size.
Style/medium: strict low-resolution pixel art prop draft, limited palette, crisp square pixels, no anti-aliasing, no painterly shading, no smooth gradients. Muted gray-green, dirty blue-gray, faded plastic colors, tiny red/yellow accent only if useful. Readable even when desaturated to grayscale.
Composition/framing: single prop centered on a perfectly flat solid #00ff00 chroma-key background for later removal. Side-view with slight front face if needed, but usable in a side-scrolling scene. Generous padding, no shadow, no ground plane, no UI, no labels, no text, no watermark. Intended final game prop size around 48x48 or 64x48 after manual cleanup.
Constraints: clean production layout, isolated prop, stable silhouette, no character, no animation frames, no scene background. Keep details simple enough for manual pixel cleanup and Unity sprite import.
Avoid: character, hands, rummaging pose, full background scene, readable Japanese text, logos, cute mascot style, glossy anime, painterly illustration, anti-aliased edges, smooth gradients, 3D render, photorealism, cyberpunk neon, fantasy, horror gore, excessive clutter.
```

## メモ

- ゴミ箱はシーン側のオブジェクトとして扱う。
- 人物の漁りアニメーションにはゴミ箱を描き込まない。
- 最終化時は背景を透過し、ピボットと接触位置を決めて `Assets/Art/` へ移す。
