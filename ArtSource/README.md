# 美術ソース運用

`ArtSource/` は、Unity の実行時アセットとして直接使わない美術資料を置く場所である。
ゲーム内で実際に使用する素材は `Assets/Art/` に置く。

## 目录構成

- `References/`: 画風、構図、比率、材質、実在物などの参考資料。
- `References/Style/`: 全体画風、キャラクター比率、シーンの空気感などのスタイル参考。
- `AIRaw/`: AI が直接生成した未加工の原稿。原則として prompt も一緒に残す。
- `WorkInProgress/`: 人間が修正・分解・再描画している途中の作業ファイル。
- `Assets/Art/`: 手作業で整え、Unity に取り込んでゲーム内で使う最終素材。

## 基本フロー

1. 画風探索や構図確認の画像は `ArtSource/References/` に入れる。
2. AI 生成直後の未加工画像は `ArtSource/AIRaw/` に入れる。
3. Aseprite、PSD、Krita などの作業中ファイルは `ArtSource/WorkInProgress/` に入れる。
4. 切り出し、透明化、減色、命名、サイズ調整が済んだ最終素材だけを `Assets/Art/` に入れる。

## 命名規則

- 参考画像: `YYYY-MM-DD-topic-preview.png`
- AI 原稿: `YYYY-MM-DD-asset-name-ai-raw-01.png`
- 作業中ファイル: `asset-name-working-v01.aseprite`
- ゲーム内素材: `asset-name.png` または `asset-name-sheet.png`

`ArtSource/` 側の画像は高解像度でもよい。`Assets/Art/` に入れる素材は、ゲーム内で使う実寸、透明度、スプライト分割、色数、読みやすさを確認してから配置する。
