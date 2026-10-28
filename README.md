# 今日も無職。

無職の主人公が、街のゴミ箱を漁って三日間を生き延びる一人称のサバイバルゲームです。

ゴミ箱は叩いてふたを開けるまで、何が出てくるか分かりません。弁当が出れば一日しのげますが、腐った物を食べれば体を壊します。気分が沈むほど、当たりか外れかの見分けがつかなくなります。拾った缶はコンビニで売れますし、レジ打ちで日銭も稼げます。ただし警官に見つかると追い払われ、捕まれば公園の外れや商店街の外まで連れて行かれます。

## 操作

| キー | 動作 |
| --- | --- |
| W / A / S / D | 移動 |
| Shift | 走る |
| マウス | 視点 |
| 左クリック | 叩く・掴む・使う |
| 数字キー | カバンの物を飲食・使用 |
| Q＋数字キー | カバンの物を捨てる |
| G | 背負った物を降ろす |
| Esc | ポーズ（音量・マウスの感度・やめる） |

## ダウンロード

[Releases ページ](https://github.com/ryuka-dev/KyoumoMushoku/releases) にあるのは、2026-07 の 2D 横スクロール版の体験版です。一人称版はまだ公開していません。

## ソースコードについて

Unity 6000.3.6f1 で開きます。一人称版のシーンは `Assets/Scenes/Slice_Chapter1.unity`、提出版の書き出しはメニューの `KyoumoMushoku/Build Submission (Windows)` です。

Unity Asset Store の素材は、利用規約により元のファイルを配れないので、このリポジトリには入れていません（`Assets/ThirdParty/` を外しています）。そのため、そのまま開くと街並みと小物の一部が欠け、効果音の一部が鳴りません。プログラムはすべて読めます。完全な状態で遊ぶには、書き出したゲーム（Windows 版）を使ってください。

含めていない素材（どれも Unity Asset Store で入手できます。`Assets/ThirdParty/AssetStore/` の下に置くと元どおりになります）：

| 置き場所 | 素材 |
| --- | --- |
| `BrokenVector/` | Low Poly Storage Pack（BrokenVector） |
| `Pandazole_Ultimate_Pack/` | Simple Game Low Poly Pack（Pandazole） |
| `Low Poly Simple Urban City 3D Asset Pack/` | Free Low Poly Simple Urban City 3D Asset Pack |
| `CelerisLab/` | The Complete UI Sound Effects Library（CelerisLab） |
| `Vefects/` | Stylized VFX Bundle（Vefects） |

素材ごとの出自と、どこに使っているかは [`Docs/素材台帳.md`](Docs/%E7%B4%A0%E6%9D%90%E5%8F%B0%E5%B8%B3.md) にあります。

## ゲームデザイン資料

企画と設計は [`Docs/GameDesign.md`](Docs/GameDesign.md)、一人称版の試作の記録は [`Docs/一人称試作.md`](Docs/%E4%B8%80%E4%BA%BA%E7%A7%B0%E8%A9%A6%E4%BD%9C.md) にまとめています。
