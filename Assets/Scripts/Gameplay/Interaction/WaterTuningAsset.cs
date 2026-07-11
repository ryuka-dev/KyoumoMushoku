using UnityEngine;

namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// 無料の水源の回復量（第十三節）。すべて叩き台であり、プレイテストで調整する。
    /// <see cref="WaterSource"/> の数値だけをここへ集約する。呼び名は構造的な素性なので
    /// <c>GreyboxBuilder</c> 側に残す。
    ///
    /// 水は軸の最も安全で最も貧しい端にのみ無料で在る（第十三節）。公衆トイレの水は無料だが
    /// SAN を削る＝尊厳と引き換えの生存。
    /// </summary>
    [CreateAssetMenu(fileName = "WaterTuning", menuName = "KyoumoMushoku/Water Tuning")]
    public sealed class WaterTuningAsset : ScriptableObject
    {
        [Header("公園の水道（無料・SAN を削らない）")]
        [SerializeField] float _tapThirstRestored = 35f;

        [Header("公衆トイレ（無料・尊厳と引き換え）")]
        [SerializeField] float _toiletThirstRestored = 35f;

        [Tooltip("トイレの水を飲むと SAN が減る。負値。")]
        [SerializeField] float _toiletSanityCost = -6f;

        public float TapThirstRestored => _tapThirstRestored;
        public float ToiletThirstRestored => _toiletThirstRestored;
        public float ToiletSanityCost => _toiletSanityCost;
    }
}
