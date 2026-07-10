using KyoumoMushoku.Core.Zones;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 警戒ゾーンを実行時のデータとして表す。ゾーンはシーン資産ではなく論理的な区分であり、
    /// 将来シーンを分割してもゲームプレイ層には影響しない。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AlertZoneVolume : MonoBehaviour
    {
        [SerializeField] AlertZoneId _zone = AlertZoneId.None;

        public AlertZoneId Zone => _zone;

        public void Configure(AlertZoneId zone) => _zone = zone;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }
    }
}
