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
        [Tooltip("カーソルが往復する速さ（毎秒、バーの幅に対する割合）。本編のレジ打ちと同じ 1.1 から。")]
        [SerializeField, Min(0.1f)] float _jobCursorSpeed = 1.1f;

        [Tooltip("命中帯の半幅（バーの幅に対する割合）。")]
        [SerializeField, Range(0.01f, 0.5f)] float _jobHalfBand = 0.14f;

        [Tooltip("精神崩壊（値札が読めない帯）での命中帯の半幅。手元が狂う（第三節）。")]
        [SerializeField, Range(0.01f, 0.5f)] float _jobBrokenHalfBand = 0.06f;

        [Header("終わり")]
        [Tooltip("この日の夜に寝たら、翌朝に切片の結算を見せて終える。")]
        [SerializeField, Min(1)] int _finalDay = 3;

        public float StartingSanity => _startingSanity;
        public float LateNightStartSeconds => _lateNightStartSeconds;
        public float LateNightRampPerSecond => _lateNightRampPerSecond;
        public float LateNightMaxDrainPerSecond => _lateNightMaxDrainPerSecond;
        public float JobCursorSpeed => _jobCursorSpeed;
        public float JobHalfBand => _jobHalfBand;
        public float JobBrokenHalfBand => _jobBrokenHalfBand;
        public int FinalDay => _finalDay;
    }
}
