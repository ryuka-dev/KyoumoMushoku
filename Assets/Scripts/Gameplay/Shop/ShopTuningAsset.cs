using UnityEngine;

namespace KyoumoMushoku.Gameplay.Shop
{
    /// <summary>
    /// コンビニの取引・バイトの数値（第四・九・十三節）。すべて叩き台であり、プレイテストで調整する。
    /// <see cref="Storefront"/> のバランス数値をここへ集約する。店頭の品揃え（<c>_offerIds</c>）は
    /// 構造的な素性なので <c>GreyboxBuilder</c> 側に残す。
    ///
    /// バイトは最も SAN を削る（第四節）。時間もバイトの主要なコストの1つであり、1シフトで
    /// ソフトクロックが <see cref="JobShiftSeconds"/> だけまとまって進む。
    /// </summary>
    [CreateAssetMenu(fileName = "ShopTuning", menuName = "KyoumoMushoku/Shop Tuning")]
    public sealed class ShopTuningAsset : ScriptableObject
    {
        [Tooltip("1日あたりの買い取り上限（円）。店主は回収業者ではない（第十三節）。")]
        [SerializeField, Min(0)] int _buybackDailyCapYen = 300;

        [Tooltip("バイト（レジ打ち）のラウンド数。")]
        [SerializeField, Min(1)] int _jobRounds = 5;

        [Tooltip("バイト1回で削られる SAN。最も回復させづらい資源を大量に消費する（第四節）。")]
        [SerializeField, Min(0f)] float _jobSanityCost = 25f;

        [Tooltip("バイト1回で減る空腹。")]
        [SerializeField, Min(0f)] float _jobHungerCost = 15f;

        [Tooltip("バイト1回（1シフト）で進むソフトクロックの秒数。")]
        [SerializeField, Min(0f)] float _jobShiftSeconds = 90f;

        public int BuybackDailyCapYen => _buybackDailyCapYen;
        public int JobRounds => _jobRounds;
        public float JobSanityCost => _jobSanityCost;
        public float JobHungerCost => _jobHungerCost;
        public float JobShiftSeconds => _jobShiftSeconds;
    }
}
