using System;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Knacks;
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
        PlayerKnacks _knacks;

        /// <summary>飲食・使用が成立したときに、消費した定義を添えて発火する。</summary>
        public event Action<ItemDefinition> Consumed;

        void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
            _vitals = GetComponent<PlayerVitals>();
            _knacks = GetComponent<PlayerKnacks>();
        }

        public ConsumeResult TryConsume(int index)
        {
            // 腐敗した食品かどうかを消費の前に見ておく（鉄の胃袋の触発・第六節）。
            var wasRottenFood = IsRottenFood(index);

            // 鉄の胃袋を持つなら腐敗の代償が半減する。読み取り専用なので素の帳簿を渡す。
            var result = Consumption.TryConsume(_inventory.Inventory, index, _vitals.Vitals, out var consumed,
                _knacks != null ? _knacks.Book : null);
            if (result != ConsumeResult.Consumed)
            {
                return result;
            }

            Consumed?.Invoke(consumed);

            // 腐敗食を食べて、それでも生きていたら 鉄の胃袋 を覚える。代償で行き倒れたなら覚えない。
            if (wasRottenFood && _vitals.Vitals.IsAlive)
            {
                _knacks?.RecordSurvivedRotten();
            }

            return result;
        }

        bool IsRottenFood(int index)
        {
            var inventory = _inventory.Inventory;
            if (inventory == null || !inventory.TryGetDefinition(index, out var definition) || !definition.IsFood)
            {
                return false;
            }

            return inventory[index].Freshness == FoodState.Rotten;
        }
    }
}
