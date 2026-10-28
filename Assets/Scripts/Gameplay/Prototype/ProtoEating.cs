using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 鞄の中の物を数字キー（1〜9）で飲み食いする。効果は本編と同じ Core の <see cref="Consumption"/>（腐った物は代償がある）。
    /// Q を押しながら数字キーなら、その物を捨てる（鞄が一杯のとき、入れ替えるため）。
    /// 鞄の何番目かは HUD の一覧の番号と同じ。
    ///
    /// 読む物（週刊誌・文庫本）と削る物（スクラッチくじ）は、使う間だけ時計が進み、その間の渇きと空腹が減る
    /// （<see cref="SliceTuningAsset.UseClockSecondsOf"/>。SAN と時間の交換）。くじの当否は削った瞬間に決まる。
    /// </summary>
    public sealed class ProtoEating : MonoBehaviour
    {
        [SerializeField] ProtoHands _hands;
        [SerializeField] ProtoVitals _vitals;
        [SerializeField] SliceTuningAsset _slice;
        [SerializeField] ProtoDayNight _dayNight;

        InputAction[] _slots;
        InputAction _discard;
        readonly IRng _rng = new SystemRng();

        public void Configure(ProtoHands hands, ProtoVitals vitals, SliceTuningAsset slice, ProtoDayNight dayNight)
        {
            _hands = hands;
            _vitals = vitals;
            _slice = slice;
            _dayNight = dayNight;
        }

        void Awake()
        {
            _slots = new InputAction[9];
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new InputAction("Eat" + (i + 1), InputActionType.Button, $"<Keyboard>/{i + 1}");
            }

            _discard = new InputAction("Discard", InputActionType.Button, "<Keyboard>/q");
        }

        void OnEnable()
        {
            foreach (var slot in _slots)
            {
                slot.Enable();
            }

            _discard.Enable();
        }

        void OnDisable()
        {
            foreach (var slot in _slots)
            {
                slot.Disable();
            }

            _discard.Disable();
        }

        void OnDestroy()
        {
            foreach (var slot in _slots)
            {
                slot?.Dispose();
            }

            _discard?.Dispose();
        }

        void Update()
        {
            if (_hands == null || !_hands.enabled || _hands.Bag == null || _vitals == null || _vitals.Vitals == null)
            {
                return;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].WasPressedThisFrame())
                {
                    if (_discard.IsPressed())
                    {
                        Discard(i);
                    }
                    else
                    {
                        Consume(i);
                    }

                    return;
                }
            }
        }

        void Discard(int index)
        {
            var bag = _hands.Bag;
            if (index >= bag.Count)
            {
                return;
            }

            var name = bag.TryGetDefinition(index, out var definition) ? definition.DisplayName : bag[index].Id;
            if (bag.TryRemoveAt(index, out _))
            {
                _hands.Say(ProtoText.Discarded(name));
            }
        }

        void Consume(int index)
        {
            var bag = _hands.Bag;
            if (index >= bag.Count)
            {
                return;
            }

            var state = bag[index].Freshness;
            var wasRottenFood = state == FoodState.Rotten && bag.TryGetDefinition(index, out var food) && food.IsFood;

            // 鉄の胃袋（第六節）を持つなら腐敗の代償が半減する（本編の PlayerConsumer と同じ）。
            var knacks = _hands.Knacks;
            var result = Consumption.TryConsume(bag, index, _vitals.Vitals, out var consumed, knacks != null ? knacks.Book : null);
            switch (result)
            {
                case ConsumeResult.Consumed:
                    _hands.Say(AfterUse(consumed, state));
                    // 腐った物を食べて、それでも生きていたら覚える。代償で倒れたなら覚えない。覚えた独り言が上の一言に代わる。
                    if (wasRottenFood && _vitals.Vitals.IsAlive && knacks != null)
                    {
                        knacks.RecordSurvivedRotten();
                    }

                    break;
                case ConsumeResult.NotConsumable:
                    if (bag.TryGetDefinition(index, out var definition))
                    {
                        _hands.Say(definition.Category == ItemCategory.Equipment
                            ? ProtoText.EquipmentInBag(definition.DisplayName)
                            : ProtoText.NotEdible(definition.DisplayName));
                    }

                    break;
            }
        }

        /// <summary>使い終えた後：時間を払い、くじなら当否を決める。返すのは独り言。</summary>
        string AfterUse(ItemDefinition used, FoodState state)
        {
            var seconds = _slice != null ? _slice.UseClockSecondsOf(used.Id) : 0f;
            if (seconds > 0f)
            {
                _dayNight?.Skip(seconds);
                _vitals.Vitals.DrainTime(seconds);
            }

            if (_slice != null && _slice.IsScratch(used.Id))
            {
                var prize = _slice.ScratchPrize(_rng.NextDouble());
                if (prize > 0)
                {
                    _hands.Wallet?.Add(prize);
                }

                return ProtoText.Scratched(prize);
            }

            return seconds > 0f
                ? ProtoText.ReadThrough(used.DisplayName)
                : ProtoText.Consumed(used.DisplayName, used.IsFood && FoodKnowledge.IsDangerous(state));
        }
    }
}
