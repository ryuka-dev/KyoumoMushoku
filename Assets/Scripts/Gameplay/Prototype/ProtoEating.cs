using KyoumoMushoku.Core.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 鞄の中の物を数字キー（1〜9）で飲み食いする。効果は本編と同じ Core の <see cref="Consumption"/>（腐った物は代償がある）。
    /// 鞄の何番目かは HUD の一覧の番号と同じ。
    /// </summary>
    public sealed class ProtoEating : MonoBehaviour
    {
        [SerializeField] ProtoHands _hands;
        [SerializeField] ProtoVitals _vitals;

        InputAction[] _slots;

        public void Configure(ProtoHands hands, ProtoVitals vitals)
        {
            _hands = hands;
            _vitals = vitals;
        }

        void Awake()
        {
            _slots = new InputAction[9];
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new InputAction("Eat" + (i + 1), InputActionType.Button, $"<Keyboard>/{i + 1}");
            }
        }

        void OnEnable()
        {
            foreach (var slot in _slots)
            {
                slot.Enable();
            }
        }

        void OnDisable()
        {
            foreach (var slot in _slots)
            {
                slot.Disable();
            }
        }

        void OnDestroy()
        {
            foreach (var slot in _slots)
            {
                slot?.Dispose();
            }
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
                    Consume(i);
                    return;
                }
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
            var result = Consumption.TryConsume(bag, index, _vitals.Vitals, out var consumed);
            switch (result)
            {
                case ConsumeResult.Consumed:
                    _hands.Say(ProtoText.Consumed(consumed.DisplayName, consumed.IsFood && FoodKnowledge.IsDangerous(state)));
                    break;
                case ConsumeResult.NotConsumable:
                    if (bag.TryGetDefinition(index, out var definition))
                    {
                        _hands.Say(ProtoText.NotEdible(definition.DisplayName));
                    }

                    break;
            }
        }
    }
}
