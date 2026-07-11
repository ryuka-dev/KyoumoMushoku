# CLAUDE.md（プロジェクト固有）

このファイルは KyoumoMushoku プロジェクト固有のルールと基本情報を定義する。
ユーザーのグローバル CLAUDE.md（エンジニアリング原則）と併せて適用すること。内容が矛盾する場合は、本ファイルの方がこのプロジェクトに関しては優先される。

## 言語ポリシー

- 本プロジェクトの主言語は **日本語** である。
- 設計ドキュメント、コミットメッセージ本文、ゲーム内テキスト（UI・シナリオ・アイテム名など）は日本語を基本とする。
- コード上の識別子（クラス名・変数名・メソッド名など）は英語で構わない。
- 会話・作業ログでの言語は問わないが、リポジトリに残る成果物（コード内コメント、コミットメッセージ、ドキュメント、ゲーム内テキスト）は日本語を優先する。

## プロジェクト基本情報

| 項目 | 内容 |
| --- | --- |
| Unity プロジェクトフォルダ名 | KyoumoMushoku |
| GitHub リポジトリ名 | KyoumoMushoku |
| 製品名（Product Name） | 今日も無職。 |
| アセンブリ名 | KyoumoMushoku |
| ルート名前空間 | KyoumoMushoku |
| セーブフォルダ名 | KyoumoMushoku |
| 実行ファイル名 | KyoumoMushoku.exe |
| 作者名 | Ryuka |
| ジャンル | 2D 横スクロール型サバイバル探索ゲーム |
| エンジン | Unity（2D テンプレート） |

上記の Product Name / アセンブリ名 / ルート名前空間 / 実行ファイル名は、Unity Editor 上で実プロジェクトを作成した後、Project Settings（Player / Assembly Definition）で実際に設定すること。現時点ではリポジトリと設計資料のみを整備済みで、Unity プロジェクト本体（Assets / Packages / ProjectSettings 等）はまだ生成されていない。

## ゲームデザイン資料

企画・設計内容は [`Docs/GameDesign.md`](Docs/GameDesign.md) にまとめている。実装前に必ず参照すること。今後の設計変更・追加もこのファイルに追記していく。

特に次の3つは、個別のシステムを超えて全体に適用される決定である。

1. **SAN は「都市を読む力」である**（第三節）。3本目の体力ゲージではない。SAN が関わる仕様を追加するときは、必ずこの定義に照らすこと。
2. **因果は世界の中で閉じる**（第十四節）。予告は事前に、説明は事後に、いずれも世界の中の情報として行う。見えない状態を追加するときは、誰がそれを説明するかを同時に決めること。
3. **情報を奪ってよいが、嘘をついてはならない**（第三節）。`??` は用いるが、誤った数値を真実として見せてはならない。

実装の優先順位（Phase 0〜5）は第九節に定義してある。垂直スライスが証明すべき4つの命題に寄与しない機能を、Phase 0〜4 に持ち込まないこと。

## 数値とローカライズの複用ルール（seam）

新しくプレイヤー向け文字列や平衡数値を追加するときは、既存の集約の仕組みを必ず複用する。バラした直書きを増やさない。

### ローカライズ（プレイヤー向け文字列）

- プレイヤーの目に触れる文字列は、域別のテキストモジュール経由でのみ書く。モジュールは `Assets/Scripts/Gameplay/UI` にあり、命名規約は `*Text.cs` / `*TextLabels.cs`（`WorldText`／`ShopText`／`StashText`／`PoliceText`／`ForageText`／`HudText`／`GameTextLabels`／`FoodCardText`）。新モジュールもこの命名に従えば自動で許可される。
- 後端は今は日本語ベタ書きのメソッド本体でよい。将来の多言語化は本体をテーブル参照へ差し替えるだけで済む。
- 例外（テキストモジュールを通さなくてよいもの）：Core 層（純粋ロジック・診断のみ）、Inspector 属性（`[Tooltip]`／`[Header]`）、ログ・例外メッセージ、`ItemDatabaseAsset` のようなデータ seed（アイテムの表示名など）。就寝場所や水源の呼び名は設置ごとの data（`[SerializeField]` 既定値＋builder の Configure）であり、これもデータ扱い。
- この規約は `LocalizationConventionTests`（EditMode）が自動で守る。テキストモジュール外に日本語リテラルを直書きするとテストが赤になる。真にデータ／診断なら、テストの許可一覧に理由付きで加える。

### 数値（平衡・チューニング）

- プレイテストで調整する平衡数値は `Assets/Config/*.asset`（ScriptableObject）に置く。Inspector・Play 中に可変。既存：`VitalsTuning`／`DaySchedule`／`ItemDatabase`／`TrashCanLoot`／`SleepTuning`／`ShopTuning`／`WaterTuning`。
- 純粋 Core ロジックがコンパイル時に参照する構造的定数（SAN 閾値・警戒度規則・コツ・保管庫イベント）は、従来どおり per-domain の静的 `*Tuning` クラス。
- `GreyboxBuilder` に生の数値字面量を直接書かない。builder は資産を読み、各コンポーネントを Configure するだけにする（位置・色・sorting などの構造値は除く）。
- 落とし穴：`Ensure*Asset()` は存在しなければ生成するだけで、既存 `Assets/Config/*.asset` は再ビルドで上書きされず実行時の権威になる。コード既定値を変えたら、当該 `.asset` を削除して `KyoumoMushoku/Build Greybox Scene` で再生成する（Inspector で直接編集してもよい）。`VitalsTests` は `new VitalsTuning()` のコード既定値を検証し厳密値を直書きするため、掉率変更時は期待値の追随が要る。

## 備考

- 本ファイルはプロジェクトの土台整備段階（Git リポジトリ初期化・企画資料整理）で作成されたものであり、開発の進行に応じて更新すること。
