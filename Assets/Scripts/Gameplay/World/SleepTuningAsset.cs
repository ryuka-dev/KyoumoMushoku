using System;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 就寝場所ごとの宿代と回復量（第二節・第十三節）。すべて叩き台であり、プレイテストで調整する。
    /// <see cref="SleepSpot"/> の回復数値だけをここへ集約する。呼び名・警戒ゾーンは設置場所の構造的な
    /// 素性なので <c>GreyboxBuilder</c> 側に残す。差別化の本体（属する警戒ゾーン）は第十三節を参照。
    ///
    /// 血量は「金で治す怪我」であり（第三節）、無料の寝床では小さくしか回復しない。数晩かけてゆっくり治り、
    /// 早く戻したければ病院で金を払う（<see cref="Hospital"/>）。安宿だけが完全回復する。
    /// </summary>
    [CreateAssetMenu(fileName = "SleepTuning", menuName = "KyoumoMushoku/Sleep Tuning")]
    public sealed class SleepTuningAsset : ScriptableObject
    {
        [Serializable]
        public struct Recovery
        {
            [Min(0)] public int costYen;

            [Tooltip("安宿だけ true。四維をすべて満たす。")]
            public bool fullRestore;

            [Tooltip("無料の寝床の HP 回復は小さい＝怪我はゆっくりしか治らない（第三節）。")]
            public float hp;

            public float thirst;
            public float hunger;
            public float sanity;
        }

        [Header("公園のベンチ（静穏ゾーン・無料）")]
        [SerializeField]
        Recovery _bench = new Recovery
        {
            costYen = 0, fullRestore = false, hp = 5f, thirst = 0f, hunger = 0f, sanity = 10f,
        };

        [Header("地下通路（生活ゾーン・無料・SAN 回復が悪い）")]
        [SerializeField]
        Recovery _underpass = new Recovery
        {
            costYen = 0, fullRestore = false, hp = 5f, thirst = 0f, hunger = 0f, sanity = 5f,
        };

        [Header("安宿（商業ゾーン・有料・完全回復＋セーブ）")]
        [SerializeField]
        Recovery _inn = new Recovery
        {
            costYen = 1500, fullRestore = true, hp = 0f, thirst = 0f, hunger = 0f, sanity = 0f,
        };

        public Recovery Bench => _bench;
        public Recovery Underpass => _underpass;
        public Recovery Inn => _inn;
    }
}
