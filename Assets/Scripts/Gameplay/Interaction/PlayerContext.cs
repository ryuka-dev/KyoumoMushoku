using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Knacks;
using KyoumoMushoku.Gameplay.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Interaction
{
    /// <summary>
    /// インタラクトの相手にプレイヤー側の権威を手渡す束。
    /// 相手（<see cref="IInteractable"/>）はここを通してのみ状態に触れるため、
    /// 個々の相手がプレイヤーの内部構造に直接依存しない。
    /// </summary>
    public sealed class PlayerContext
    {
        public PlayerContext(Transform transform, PlayerVitals vitals, PlayerInventory inventory, PlayerWallet wallet,
            PlayerKnacks knacks)
        {
            Transform = transform;
            Vitals = vitals;
            Inventory = inventory;
            Wallet = wallet;
            Knacks = knacks;
        }

        public Transform Transform { get; }
        public PlayerVitals Vitals { get; }
        public PlayerInventory Inventory { get; }
        public PlayerWallet Wallet { get; }

        /// <summary>習得したコツ（第六節）。漁りの見立てなど、相手のルール変化を読むために使う。</summary>
        public PlayerKnacks Knacks { get; }
    }
}
