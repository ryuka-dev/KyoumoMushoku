using System.Collections.Generic;
using KyoumoMushoku.Gameplay.Foraging;
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

        [Tooltip("廃棄の中身。重みが出やすさ。全体に対する割合が小さい品ほど「珍しい」と出る（ゴミ箱と同じ境）。")]
        [SerializeField] TrashCanLootAsset.Table _disposalTable = DefaultDisposalTable();

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
                new TrashCanLootAsset.WeightedItem { id = "can_aluminum", weight = 12f },
                new TrashCanLootAsset.WeightedItem { id = "water_bottle", weight = 10f },
                new TrashCanLootAsset.WeightedItem { id = "half_bottle", weight = 8f },
                new TrashCanLootAsset.WeightedItem { id = "bottle_empty", weight = 8f },
                new TrashCanLootAsset.WeightedItem { id = "can_coffee", weight = 6f },
                new TrashCanLootAsset.WeightedItem { id = "cigarette", weight = 4f },
                new TrashCanLootAsset.WeightedItem { id = "umbrella_broken", weight = 3f },
                new TrashCanLootAsset.WeightedItem { id = "cardboard", weight = 3f },
            },
            freshWeight = 4f,
            staleWeight = 6f,
            rottenWeight = 0f,
        };
    }
}
