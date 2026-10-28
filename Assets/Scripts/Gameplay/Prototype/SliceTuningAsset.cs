using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Foraging;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片（甲子園の提出版）だけが使う数値。凍結中の 2D 版と共有する資産（VitalsTuning など）は書き換えない。
    /// 切片で値を変えたい共有の数値は、資産を複製して切片のシーンから参照する（ItemDatabase_Slice・SleepTuning_Slice・
    /// DaySchedule_Slice・VitalsTuning_Slice）。ここには、共有の資産に場所が無い切片の規則だけを置く。すべて叩き台で、遊んで決める。
    ///
    /// 決定の出典：作業計画 第一の二節 決定事項「切片の SAN」と K3、作者の指示（三日目で切片を終える）。
    /// </summary>
    [CreateAssetMenu(fileName = "SliceTuning", menuName = "KyoumoMushoku/Slice Tuning")]
    public sealed class SliceTuningAsset : ScriptableObject
    {
        [Header("始まり")]
        [Tooltip("一日目の朝の SAN。満タンにしない＝一日目から擦り減っている（決定事項「切片の SAN」①）。")]
        [SerializeField, Range(0f, 100f)] float _startingSanity = 60f;

        [Header("夜更かし（決定事項「切片の SAN」③）")]
        [Tooltip("夜になってから、SAN の減りが急になり始めるまでの秒数。廃棄の時間（夜から 25 秒）より前に置く＝廃棄を待つ間から削れ始める。")]
        [SerializeField, Min(0f)] float _lateNightStartSeconds = 20f;

        [Tooltip("夜更かしが始まってから、1 秒ごとに増える SAN の減り（/秒）。遅くなるほど急になる。")]
        [SerializeField, Min(0f)] float _lateNightRampPerSecond = 0.015f;

        [Tooltip("夜更かしによる SAN の減りの上限（/秒）。")]
        [SerializeField, Min(0f)] float _lateNightMaxDrainPerSecond = 0.4f;

        [Header("バイト（レジ打ちのタイミングバー）")]
        [Tooltip("1日に入れるシフトの数。寝て日付が変わると戻る。")]
        [SerializeField, Min(0)] int _jobShiftsPerDay = 1;

        [Tooltip("カーソルが往復する速さ（毎秒、バーの幅に対する割合）。本編のレジ打ちと同じ 1.1 から。")]
        [SerializeField, Min(0.1f)] float _jobCursorSpeed = 1.1f;

        [Tooltip("命中帯の半幅（バーの幅に対する割合）。")]
        [SerializeField, Range(0.01f, 0.5f)] float _jobHalfBand = 0.14f;

        [Tooltip("精神崩壊（値札が読めない帯）での命中帯の半幅。手元が狂う（第三節）。")]
        [SerializeField, Range(0.01f, 0.5f)] float _jobBrokenHalfBand = 0.06f;

        [Header("廃棄の時間（コンビニの大噴出）")]
        [Tooltip("その夜に廃棄が出る確率。日付が変わった時点で決まり、夕方の店員の口ぶりで分かる（予告は事前に・第十四節）。")]
        [SerializeField, Range(0f, 1f)] float _disposalChance = 0.5f;

        [Tooltip("一度の廃棄で噴き出す回数（抽選の回数）。")]
        [SerializeField, Min(1)] int _disposalDraws = 8;

        [Tooltip("廃棄の中身。重みが出やすさ。珍しさは品の段（下の「品の珍しさ」）で決まる。")]
        [SerializeField] TrashCanLootAsset.Table _disposalTable = DefaultDisposalTable();

        [Header("品の珍しさ（Docs/切片の品目.md 第三節）")]
        [Tooltip("品ごとの段。ここに無い品は「並」。出やすさからは導かない（同じ品が表によって珍しくなったり並になったりしない）。")]
        [SerializeField] List<ItemTierEntry> _itemTiers = DefaultItemTiers();

        [Header("リサイクルショップ（Docs/切片の品目.md 第六節）")]
        [Tooltip("アーケードのリサイクルショップが買う品と値段（円）。一日の上限は無い。ここに無い品は買わない。品のデータの売値は 0 にしておく（コンビニの店主は買わない）。")]
        [SerializeField] List<RecyclePriceEntry> _recyclePrices = DefaultRecyclePrices();

        [Header("鞄から使う物（Docs/切片の品目.md 第四節）")]
        [Tooltip("使うと時計が進む品（読む・削る）と、その秒数。その間の渇きと空腹も減る。ここに無い品は一瞬で使い終わる。")]
        [SerializeField] List<ItemSecondsEntry> _useClockSeconds = DefaultUseClockSeconds();

        [Tooltip("スクラッチくじの品の id。使うと削り、当否はその瞬間に決まる（それまで誰にも分からないので、合図も無い）。")]
        [SerializeField] string _scratchItemId = "scratch";

        [Tooltip("スクラッチくじの当たり（円）と重み。0 円ははずれ。")]
        [SerializeField] List<PrizeEntry> _scratchPrizes = DefaultScratchPrizes();

        [Tooltip("小銭入れの品の id。掴むと鞄に入れず、中身がそのまま財布へ入る。")]
        [SerializeField] string _purseItemId = "coin_purse";

        [Tooltip("小銭入れの中身（円）。掴んだ瞬間に、この中から一つが同じ確からしさで決まる。")]
        [SerializeField] int[] _purseYen = { 100, 200, 300, 500, 800 };

        [Header("SAN の保底（Docs/切片の品目.md 第十節）")]
        [Tooltip("SAN が低いほど出やすくなる、気を晴らす品。ゴミ箱の中身を引き直すとき（夜への切り替わり・日付の変わり目）だけ効く。")]
        [SerializeField] string[] _pityItemIds = { "cigarette", "weekly_mag", "paperback", "sweet_bun", "can_coffee" };

        [Tooltip("SAN がこの値を下回ると、上の品の重みにこの倍率を掛ける。下の段ほど低い SAN にする（一番低い段が勝つ）。")]
        [SerializeField] List<PityBand> _pityBands = new List<PityBand>
        {
            new PityBand { belowSanity = 50f, scale = 1.5f },
            new PityBand { belowSanity = 30f, scale = 2.5f },
        };

        [Tooltip("落ちぶれると拾うようになる品（吸い殻）。SAN がこの値を下回ったときだけ、ゴミ箱の表に加わる。")]
        [SerializeField] string _buttItemId = "cig_butt";
        [SerializeField, Range(0f, 100f)] float _buttBelowSanity = 50f;
        [SerializeField, Min(0f)] float _buttWeight = 8f;

        [Header("道具（Docs/切片の品目.md 第四節）")]
        [Tooltip("軍手の品の id。鞄にある間、ふたのあるゴミ箱が一打早く開く（速く、静か）。")]
        [SerializeField] string _glovesItemId = "work_gloves";

        [Tooltip("キャップの品の id。鞄にある間、警官の目に気づかれるのが遅い。")]
        [SerializeField] string _capItemId = "cap";

        [Tooltip("キャップがあるときの、警官の目に気づかれる速さの倍率。")]
        [SerializeField, Range(0.1f, 1f)] float _capNoticeMultiplier = 0.6f;

        [Header("最初の大当たり（切片の演出）")]
        [Tooltip("ゲームを始めてから、この数だけ箱や袋を開け終えた後、次に初めて叩く箱へ大当たりを一つ入れておく（つまり 10 なら 10 個目）。0 なら入れない。一局に一度だけ。")]
        [SerializeField, Min(0)] int _guaranteedJackpotAfterOpens = 9;

        [Tooltip("入れる大当たりの品（この中から一つ）。")]
        [SerializeField] string[] _guaranteedJackpotItems = { "wristwatch", "handheld" };

        [Header("宙の物が消えるまで（段ごと・秒）")]
        [Tooltip("珍しい物ほど長く地面に残る（取り逃しても、大当たりなら走って戻れる）。")]
        [SerializeField, Min(1f)] float _commonLifetime = 3f;
        [SerializeField, Min(1f)] float _uncommonLifetime = 6f;
        [SerializeField, Min(1f)] float _rareLifetime = 10f;
        [SerializeField, Min(1f)] float _jackpotLifetime = 20f;

        [Tooltip("大当たりの音。空なら合成の「キュイン」で代用する。素材を買ったら Assets/ThirdParty/ に置いてここへ入れる（再配布できない素材は公開リポジトリに入れない）。")]
        [SerializeField] AudioClip _jackpotSound;

        [Header("品の模型（Assets/Art/Models/Props/Items）")]
        [Tooltip("宙に飛ぶ品の見た目。ここに無い品は色付きの立方体で飛ぶ。")]
        [SerializeField] List<ItemModelEntry> _itemModels = new List<ItemModelEntry>();

        [Header("段ボール（Docs/切片の品目.md 第五節）")]
        [Tooltip("背負っている間の歩きの速さの倍率。走れない（GameDesign 第十一節）。")]
        [SerializeField, Range(0.1f, 1f)] float _carrySpeedMultiplier = 0.75f;

        [Tooltip("背負っている間、警官の目に気づかれる速さの倍率（目立つ）。")]
        [SerializeField, Min(1f)] float _carryNoticeMultiplier = 1.5f;

        [Tooltip("背負ったままベンチで寝たときの回復（敷く）。段ボールは朝には湿って使えない。")]
        [SerializeField] SleepTuningAsset.Recovery _cardboardBedding = new SleepTuningAsset.Recovery { hp = 10f, sanity = 12f };

        [Header("終わり")]
        [Tooltip("この日の夜に寝たら、翌朝に切片の結算を見せて終える。")]
        [SerializeField, Min(1)] int _finalDay = 3;

        public float StartingSanity => _startingSanity;
        public float LateNightStartSeconds => _lateNightStartSeconds;
        public float LateNightRampPerSecond => _lateNightRampPerSecond;
        public float LateNightMaxDrainPerSecond => _lateNightMaxDrainPerSecond;
        public int JobShiftsPerDay => _jobShiftsPerDay;
        public float JobCursorSpeed => _jobCursorSpeed;
        public float JobHalfBand => _jobHalfBand;
        public float JobBrokenHalfBand => _jobBrokenHalfBand;
        public int FinalDay => _finalDay;
        public float DisposalChance => _disposalChance;
        public int DisposalDraws => _disposalDraws;
        public TrashCanLootAsset.Table DisposalTable => _disposalTable;
        public float CarrySpeedMultiplier => _carrySpeedMultiplier;
        public AudioClip JackpotSound => _jackpotSound;
        public int GuaranteedJackpotAfterOpens => _guaranteedJackpotAfterOpens;

        /// <summary>最初の大当たりに入れる品を、0 以上 1 未満の <paramref name="roll01"/> で選ぶ。無ければ空の id。</summary>
        public ItemId GuaranteedJackpotItem(double roll01) =>
            _guaranteedJackpotItems == null || _guaranteedJackpotItems.Length == 0
                ? default
                : new ItemId(_guaranteedJackpotItems[Mathf.Clamp((int)(roll01 * _guaranteedJackpotItems.Length), 0, _guaranteedJackpotItems.Length - 1)]);

        /// <summary>宙の物が消えるまでの秒数。珍しい物ほど長い。</summary>
        public float LootLifetimeOf(LootTier tier) => tier switch
        {
            LootTier.Jackpot => _jackpotLifetime,
            LootTier.Rare => _rareLifetime,
            LootTier.Uncommon => _uncommonLifetime,
            _ => _commonLifetime,
        };
        public float CapNoticeMultiplier => _capNoticeMultiplier;
        public bool IsGloves(ItemId item) => item.Value == _glovesItemId;
        public bool IsCap(ItemId item) => item.Value == _capItemId;
        public float CarryNoticeMultiplier => _carryNoticeMultiplier;
        public SleepTuningAsset.Recovery CardboardBedding => _cardboardBedding;

        [Serializable]
        public struct ItemTierEntry
        {
            public string id;
            public LootTier tier;
        }

        /// <summary>品の段。表に無い品は並。</summary>
        public LootTier TierOf(ItemId item)
        {
            foreach (var entry in _itemTiers)
            {
                if (entry.id == item.Value)
                {
                    return entry.tier;
                }
            }

            return LootTier.Common;
        }

        [Serializable]
        public struct ItemModelEntry
        {
            public string id;
            public GameObject model;
        }

        /// <summary>品の模型。無ければ null（立方体で飛ぶ）。</summary>
        public GameObject ModelOf(ItemId item)
        {
            foreach (var entry in _itemModels)
            {
                if (entry.id == item.Value)
                {
                    return entry.model;
                }
            }

            return null;
        }

        [Serializable]
        public struct PityBand
        {
            [Range(0f, 100f)] public float belowSanity;
            [Min(0f)] public float scale;
        }

        /// <summary>吸い殻（落ちぶれると拾うようになる品）か。</summary>
        public bool IsButt(ItemId item) => item.Value == _buttItemId;

        /// <summary>
        /// ゴミ箱の表を、叩く人の SAN に合わせて作り直す（保底）。気を晴らす品の重みを上げ、低ければ吸い殻を加える。
        /// 確率を上げるだけで、出ることは約束しない。読めなさ（HitTellSight）はそのまま＝助けはあるが、どの箱かは見えない。
        /// </summary>
        public LootTable AdjustForSanity(LootTable table, float sanity)
        {
            if (table == null)
            {
                return null;
            }

            var scale = 1f;
            var lowest = float.MaxValue;
            foreach (var band in _pityBands)
            {
                if (sanity < band.belowSanity && band.belowSanity < lowest)
                {
                    lowest = band.belowSanity;
                    scale = band.scale;
                }
            }

            var addButt = sanity < _buttBelowSanity && _buttWeight > 0f && !string.IsNullOrEmpty(_buttItemId);
            if (Mathf.Approximately(scale, 1f) && !addButt)
            {
                return table;
            }

            var extra = addButt ? new[] { new LootTable.Entry(new ItemId(_buttItemId), _buttWeight) } : null;
            return table.Reweighted(item => IsPityItem(item) ? scale : 1f, extra);
        }

        bool IsPityItem(ItemId item)
        {
            foreach (var id in _pityItemIds)
            {
                if (id == item.Value)
                {
                    return true;
                }
            }

            return false;
        }

        [Serializable]
        public struct ItemSecondsEntry
        {
            public string id;
            [Min(0f)] public float seconds;
        }

        [Serializable]
        public struct PrizeEntry
        {
            [Min(0)] public int yen;
            [Min(0f)] public float weight;
        }

        /// <summary>使うと時計が何秒進むか。進まない品は 0。</summary>
        public float UseClockSecondsOf(ItemId item)
        {
            foreach (var entry in _useClockSeconds)
            {
                if (entry.id == item.Value)
                {
                    return entry.seconds;
                }
            }

            return 0f;
        }

        public bool IsScratch(ItemId item) => item.Value == _scratchItemId;

        public bool IsPurse(ItemId item) => item.Value == _purseItemId;

        /// <summary>くじの当たりを、0 以上 1 未満の <paramref name="roll01"/> で引く。</summary>
        public int ScratchPrize(double roll01)
        {
            var total = 0f;
            foreach (var prize in _scratchPrizes)
            {
                total += prize.weight;
            }

            var pick = (float)roll01 * total;
            foreach (var prize in _scratchPrizes)
            {
                if (pick < prize.weight)
                {
                    return prize.yen;
                }

                pick -= prize.weight;
            }

            return 0;
        }

        /// <summary>小銭入れの中身を、0 以上 1 未満の <paramref name="roll01"/> で引く。</summary>
        public int PurseYen(double roll01) =>
            _purseYen == null || _purseYen.Length == 0
                ? 0
                : _purseYen[Mathf.Clamp((int)(roll01 * _purseYen.Length), 0, _purseYen.Length - 1)];

        static List<ItemSecondsEntry> DefaultUseClockSeconds() => new List<ItemSecondsEntry>
        {
            new ItemSecondsEntry { id = "weekly_mag", seconds = 20f },
            new ItemSecondsEntry { id = "paperback", seconds = 40f },
            new ItemSecondsEntry { id = "scratch", seconds = 5f },
        };

        static List<PrizeEntry> DefaultScratchPrizes() => new List<PrizeEntry>
        {
            new PrizeEntry { yen = 0, weight = 75f },
            new PrizeEntry { yen = 100, weight = 15f },
            new PrizeEntry { yen = 500, weight = 8f },
            new PrizeEntry { yen = 3000, weight = 2f },
        };

        [Serializable]
        public struct RecyclePriceEntry
        {
            public string id;
            [Min(0)] public int yen;
        }

        /// <summary>リサイクルショップの買値。買わない品は 0。</summary>
        public int RecyclePriceOf(ItemId item)
        {
            foreach (var entry in _recyclePrices)
            {
                if (entry.id == item.Value)
                {
                    return entry.yen;
                }
            }

            return 0;
        }

        static List<RecyclePriceEntry> DefaultRecyclePrices() => new List<RecyclePriceEntry>
        {
            new RecyclePriceEntry { id = "brand_box", yen = 200 },
            new RecyclePriceEntry { id = "game_soft", yen = 600 },
            new RecyclePriceEntry { id = "wristwatch", yen = 900 },
            new RecyclePriceEntry { id = "handheld", yen = 1500 },
            new RecyclePriceEntry { id = "paperback", yen = 150 },
        };

        static List<ItemTierEntry> DefaultItemTiers()
        {
            var list = new List<ItemTierEntry>();
            void Add(LootTier tier, params string[] ids)
            {
                foreach (var id in ids)
                {
                    list.Add(new ItemTierEntry { id = id, tier = tier });
                }
            }

            Add(LootTier.Uncommon, "umbrella_broken", "sweet_bun", "weekly_mag", "cigarette", "copper_wire", "can_coffee", "karaage", "scratch");
            Add(LootTier.Rare, "paperback", "brand_box", "coin_purse", "work_gloves", "cap", "game_soft");
            Add(LootTier.Jackpot, "wristwatch", "handheld");
            return list;
        }

        /// <summary>
        /// 廃棄の叩き台。食べ物が中心だが、店の物は何でも混じる。宿の鍵とバックパックは入れない（三日の目標を廃棄で飛ばさない）。
        /// 期限切れなので食べ物の多くは傷んでいる。腐った物は出さない（出したての廃棄）。
        /// </summary>
        static TrashCanLootAsset.Table DefaultDisposalTable() => new TrashCanLootAsset.Table
        {
            emptyWeight = 6f,
            items = new List<TrashCanLootAsset.WeightedItem>
            {
                new TrashCanLootAsset.WeightedItem { id = "bento", weight = 14f },
                new TrashCanLootAsset.WeightedItem { id = "onigiri", weight = 14f },
                new TrashCanLootAsset.WeightedItem { id = "bread", weight = 14f },
                new TrashCanLootAsset.WeightedItem { id = "karaage", weight = 10f },
                new TrashCanLootAsset.WeightedItem { id = "sweet_bun", weight = 6f },
                new TrashCanLootAsset.WeightedItem { id = "can_aluminum", weight = 10f },
                new TrashCanLootAsset.WeightedItem { id = "water_bottle", weight = 10f },
                new TrashCanLootAsset.WeightedItem { id = "half_bottle", weight = 6f },
                new TrashCanLootAsset.WeightedItem { id = "bottle_empty", weight = 6f },
                new TrashCanLootAsset.WeightedItem { id = "can_coffee", weight = 6f },
                new TrashCanLootAsset.WeightedItem { id = "cigarette", weight = 4f },
                new TrashCanLootAsset.WeightedItem { id = "umbrella_broken", weight = 3f },
                new TrashCanLootAsset.WeightedItem { id = "cardboard", weight = 3f },
                new TrashCanLootAsset.WeightedItem { id = "scratch", weight = 3f },
            },
            freshWeight = 4f,
            staleWeight = 6f,
            rottenWeight = 0f,
        };
    }
}
