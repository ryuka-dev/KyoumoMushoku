using KyoumoMushoku.Gameplay.Interaction;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 地上と地下通路をつなぐ階段。E で調べて昇り降りする交互門（第五節）。
    /// 移動距離そのものは短縮しない（地下通路は歩いて渡る必要がある）。到着点は連結先の
    /// 階段の口であり、階段はあくまで階層をつなぐだけである。
    ///
    /// 区域の帰属や無料就寝の判定はここでは扱わない。地下通路の区域は
    /// <see cref="AlertZoneVolume"/>／<c>ZoneTracker</c> が、就寝は <c>SleepSpot</c> が担う。
    /// この門はプレイヤーの位置を移すだけであり、それらの語義には触れない。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Stairwell : MonoBehaviour, IInteractable
    {
        [SerializeField] Stairwell _linked;
        [SerializeField] Transform _arrivalPoint;

        public void Configure(Stairwell linked, Transform arrivalPoint)
        {
            _linked = linked;
            _arrivalPoint = arrivalPoint;
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public bool CanInteract(PlayerContext player) =>
            _linked != null && _linked._arrivalPoint != null;

        public string Describe(PlayerContext player) =>
            _linked != null && _linked.transform.position.y < transform.position.y
                ? "階段を降りる"
                : "階段を昇る";

        public void Interact(PlayerContext player)
        {
            if (_linked == null || _linked._arrivalPoint == null
                || !player.Transform.TryGetComponent(out Rigidbody2D body))
            {
                return;
            }

            body.position = _linked._arrivalPoint.position;
            body.linearVelocity = Vector2.zero;
        }
    }
}
