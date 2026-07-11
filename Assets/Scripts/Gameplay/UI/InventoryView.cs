using System.Text;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Survival;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// カバンの中身を一覧し、食品カードには SAN に応じた `??` を反映する（第三節・第十一節）。
    /// 数字キー（1〜9）でその番号のものを飲食・使用する。飲食は決して封鎖しない。
    ///
    /// 読めなさは食品カードの中だけに閉じ、画面全体を覆わない。
    /// </summary>
    public sealed class InventoryView : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] PlayerInventory _inventory;
        [SerializeField] PlayerVitals _vitals;
        [SerializeField] PlayerConsumer _consumer;

        readonly StringBuilder _builder = new StringBuilder();

        // モーダル（店パネル・段ボール箱パネル）が数字キーを使う間は飲食を黙らせ、キーの取り合いを避ける。
        IInputModal[] _modals = System.Array.Empty<IInputModal>();

        public void Configure(PlayerInventory inventory, PlayerVitals vitals, PlayerConsumer consumer, TMP_Text text)
        {
            _inventory = inventory;
            _vitals = vitals;
            _consumer = consumer;
            _text = text;
        }

        /// <summary>数字キーを占有するモーダルを結びつける。どれか1つでも開いている間は飲食入力を読まない。</summary>
        public void BindModal(params IInputModal[] modals) => _modals = modals ?? System.Array.Empty<IInputModal>();

        bool AnyModalOpen()
        {
            foreach (var modal in _modals)
            {
                if (modal != null && modal.IsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        void Update()
        {
            if (_inventory == null || _inventory.Inventory == null || _vitals == null || _vitals.Vitals == null)
            {
                return;
            }

            HandleEatInput();

            if (_text != null)
            {
                _text.text = Compose();
            }
        }

        void HandleEatInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _consumer == null || AnyModalOpen())
            {
                return;
            }

            for (var slot = 0; slot < 9; slot++)
            {
                if (DigitPressed(keyboard, slot + 1))
                {
                    _consumer.TryConsume(slot);
                    break;
                }
            }
        }

        static bool DigitPressed(Keyboard keyboard, int digit)
        {
            var key = digit switch
            {
                1 => Key.Digit1,
                2 => Key.Digit2,
                3 => Key.Digit3,
                4 => Key.Digit4,
                5 => Key.Digit5,
                6 => Key.Digit6,
                7 => Key.Digit7,
                8 => Key.Digit8,
                _ => Key.Digit9,
            };

            return keyboard[key].wasPressedThisFrame;
        }

        string Compose()
        {
            var inventory = _inventory.Inventory;
            var sanity = _vitals.Vitals.Sanity;

            _builder.Clear();
            _builder.AppendLine(HudText.BagHeader(inventory.UsedSlots, inventory.Capacity));

            if (inventory.Count == 0)
            {
                _builder.AppendLine(HudText.BagEmpty);
                return _builder.ToString();
            }

            for (var i = 0; i < inventory.Count; i++)
            {
                if (!inventory.TryGetDefinition(i, out var definition))
                {
                    continue;
                }

                var item = inventory[i];
                _builder.AppendLine(HudText.InventoryItemLine(i + 1, FoodCardText.Headline(definition, item.Freshness, sanity)));

                var effect = FoodCardText.EffectLines(definition, item.Freshness, sanity);
                if (!string.IsNullOrEmpty(effect))
                {
                    foreach (var line in effect.Split('\n'))
                    {
                        if (line.Length > 0)
                        {
                            _builder.AppendLine("    " + line);
                        }
                    }
                }
            }

            _builder.AppendLine(HudText.EatHint);
            return _builder.ToString();
        }
    }
}
