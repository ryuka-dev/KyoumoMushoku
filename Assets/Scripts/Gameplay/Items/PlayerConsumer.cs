using System;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Survival;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Items
{
    /// <summary>
    /// カバンの中身を飲食・使用する唯一の入口。UI はここを呼び、Core の <see cref="Consumption"/> に橋渡しする。
    /// 状態（Vitals）と持ち物（Inventory）の両方に触れるため、その合流点をひとつに保つ。
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    [RequireComponent(typeof(PlayerVitals))]
    public sealed class PlayerConsumer : MonoBehaviour
    {
        PlayerInventory _inventory;
        PlayerVitals _vitals;

        /// <summary>飲食・使用が成立したときに、消費した定義を添えて発火する。</summary>
        public event Action<ItemDefinition> Consumed;

        void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
            _vitals = GetComponent<PlayerVitals>();
        }

        public ConsumeResult TryConsume(int index)
        {
            var result = Consumption.TryConsume(_inventory.Inventory, index, _vitals.Vitals, out var consumed);
            if (result == ConsumeResult.Consumed)
            {
                Consumed?.Invoke(consumed);
            }

            return result;
        }
    }
}
