# 美術ソース運用

`ArtSource/` は、Unity の実行時アセットとして直接使わない美術資料を置く場所である。
ゲーム内で実際に使用する素材は `Assets/Art/` に置く。

## ディレクトリ構成

- `References/`: 画風、構図、比率、材質、実在物などの参考資料。
- `References/Style/`: 全体画風、キャラクター比率、シーンの空気感などのスタイル参考。
- `AIRaw/`: AI が直接生成した未加工の原稿。原則として prompt も一緒に残す。
- `AIRaw/GeneratedSprites/`: AI 原稿を題材別に分類して置く。直下に画像を散らかさない。
- `WorkInProgress/`: 人間が修正・分解・再描画している途中の作業ファイル。
- `Assets/Art/`: 手作業で整え、Unity に取り込んでゲーム内で使う最終素材。

### `AIRaw/GeneratedSprites/` の分類

| 分類 | 内容 |
| --- | --- |
| `Characters/` | 主人公、NPC など人物。 |
| `StreetProps/` | 自販機、ベンチ、街灯、ゴミ箱など、街路に置く独立した物体。 |
| `ArchitectureFragments/` | 壁パネル、雨樋、外階段など建物の部位。 |
| `BackgroundBuildings/` | 遠景として使う建物全体。 |
| `Walls/` | フェンス、塀など、面として繋ぐ壁モジュール。 |
| `Ground/` | 路面、歩道、縁石、排水溝など、地面のモジュールと汚れ decal。 |
| `UtilityEquipment/` | 室外機、電力量計、ガスボンベなど設備機器。 |
| `HouseholdClutter/` | 布団、ブルーシート、ケーブルなど生活の残骸。 |
| `UI/` | HUD 枠、アイコンなど UI 素材。 |

## 基本フロー

1. 画風探索や構図確認の画像は `ArtSource/References/` に入れる。
2. AI 生成直後の未加工画像は `ArtSource/AIRaw/GeneratedSprites/` の該当分類に入れる。
3. Aseprite、PSD、Krita などの作業中ファイルは `ArtSource/WorkInProgress/` に入れる。同名の原稿が `AIRaw/` にあっても、`WorkInProgress/` 側は加工後の別物である。上書き・重複整理の対象にしない。
4. 切り出し、透明化、減色、命名、サイズ調整が済んだ最終素材だけを `Assets/Art/` に入れる。

## 命名規則

- 参考画像: `YYYY-MM-DD-topic-preview.png`
- AI 原稿（クロマキー背景つき）: `asset-name-chromakey-v01.png`
- AI 原稿（背景を抜いたもの）: `asset-name-transparent-v01.png`
- 作業中ファイル: `asset-name-working-v01.aseprite`
- ゲーム内素材: `asset-name.png` または `asset-name-sheet.png`

AI 原稿は `chromakey` と `transparent` を対で残す。作り直したら `v02`、`v03` と版を上げ、旧版は消さない。

初期に生成した一部の原稿は `YYYY-MM-DD-asset-name-ai-raw-01.png` と、対応する `.prompt.md` の形で残っている。これは prompt を併記していた頃の名残であり、分類だけ揃えて名前はそのままにしてある。新しく足す原稿には使わない。

`ArtSource/` 側の画像は高解像度でもよい。`Assets/Art/` に入れる素材は、ゲーム内で使う実寸、透明度、スプライト分割、色数、読みやすさを確認してから配置する。
