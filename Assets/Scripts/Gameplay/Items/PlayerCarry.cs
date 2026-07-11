using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Player;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Items
{
    /// <summary>
    /// 背負いスロット（第十一節）をシーンに接続する所有者。段ボールを1個だけ担ぐ。
    ///
    /// 背負っている間の代償は、新しい仕組みを足さずに既存の境界へ流し込む：
    /// 移動は <see cref="IMovementSpeedModifier"/> で -25%、走行は <see cref="IRunInhibitor"/> で不可、
    /// 警察の注目は <see cref="SuspicionPerSecond"/> を警官が読む（第十二節：段ボールを背負って歩くこと自体がリスク）。
    ///
    /// セーブ・ロードの単一の所有者は <c>GameSession</c>。
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class PlayerCarry : MonoBehaviour, IMovementSpeedModifier, IRunInhibitor
    {
        [Tooltip("段ボールを背負っている間の移動速度倍率（第十一節・叩き台）。")]
        [SerializeField, Range(0f, 1f)] float _speedMultiplierWhileCarrying = 0.75f;

        [Tooltip("背負って歩く姿を見られている間、毎秒どれだけ警察の注目が上がるか（叩き台）。")]
        [SerializeField, Min(0f)] float _suspicionPerSecond = 22f;

        CarrySlot _slot;

        public CarrySlot Slot => _slot;

        public bool IsCarrying => _slot != null && _slot.IsOccupied;

        /// <summary>背負って歩く姿は目立つ（第十二節）。警官がこれを読む。</summary>
        public float SuspicionPerSecond => _suspicionPerSecond;

        public float SpeedMultiplier => IsCarrying ? _speedMultiplierWhileCarrying : 1f;

        public bool InhibitsRun => IsCarrying;

        void Awake()
        {
            var inventory = GetComponent<PlayerInventory>();
            if (inventory == null || inventory.Catalog == null)
            {
                Debug.LogError($"{nameof(PlayerCarry)}: アイテムカタログが得られない。", this);
                enabled = false;
                return;
            }

            _slot = new CarrySlot(inventory.Catalog);
        }

        public CarrySlotState CaptureState() => _slot != null ? _slot.CaptureState() : new CarrySlotState();

        /// <summary>セーブデータから復元する。ロードの単一の所有者だけが呼ぶ。</summary>
        public void RestoreState(CarrySlotState state) => _slot?.Restore(state);
    }
}
