using System.Text;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 捨てるパネル。ゴミ箱（<see cref="IDiscardStation"/>）の前で G を押すと開き、
    /// 数字キー（1〜9）でカバンの品を、0 で背負っている段ボールを捨てる。
    /// 開いている間は移動と通常のインタラクトを止めて入力を占有する（<see cref="IInputModal"/>）。
    ///
    /// 捨てるのは無条件・無代償である。漁り（E）とは別の動詞であり、ゴミ箱の残量にも影響しない。
    /// 捨てた物はただ消える。世界には何も起きない（IDiscardStation を参照）。
    /// </summary>
    public sealed class DiscardPanel : MonoBehaviour, IInputModal
    {
        [SerializeField] PlayerMotor _motor;
        [SerializeField] PlayerInteractor _interactor;
        [SerializeField] TMP_Text _text;

        readonly StringBuilder _sb = new StringBuilder();

        PlayerContext _ctx;
        bool _open;
        bool _skipInputThisFrame;
        string _feedback = string.Empty;

        public bool IsOpen => _open;

        public void Configure(PlayerMotor motor, PlayerInteractor interactor, TMP_Text text)
        {
            _motor = motor;
            _interactor = interactor;
            _text = text;
            HideText();
        }

        void OnDisable()
        {
            if (_open)
            {
                // どんな終わり方でも操作を返す。モーダルに閉じ込めたまま消えない。
                SetPlayerControl(true);
                _open = false;
            }
        }

        void Update()
        {
            if (_text == null || _interactor == null)
            {
                return;
            }

            if (!_open)
            {
                TryOpen();
                return;
            }

            // 捨てている最中に飢え死にしたら、搬送に任せてモーダルを畳む。閉じ込めない。
            if (_ctx?.Vitals == null || _ctx.Vitals.Vitals == null || !_ctx.Vitals.Vitals.IsAlive ||
                _ctx.Inventory == null || _ctx.Inventory.Inventory == null)
            {
                Close();
                return;
            }

            var keyboard = Keyboard.current;
            if (_skipInputThisFrame)
            {
                _skipInputThisFrame = false;
                keyboard = null; // このフレームは描画だけ行い、入力は読まない。
            }

            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame ||
                    keyboard.gKey.wasPressedThisFrame)
                {
                    Close();
                    return;
                }

                if (keyboard.digit0Key.wasPressedThisFrame)
                {
                    DiscardCarried();
                }
                else
                {
                    for (var i = 0; i < 9; i++)
                    {
                        if (DigitPressed(keyboard, i + 1))
                        {
                            DiscardAt(i);
                            break;
                        }
                    }
                }
            }

            _text.text = Compose();
        }

        /// <summary>
        /// 閉じているときの入口。インタラクタが有効（＝他のモーダルが開いていない）で、
        /// いまの対象がゴミ箱で、漁りの最中でなければ、G で開く。
        /// </summary>
        void TryOpen()
        {
            if (!_interactor.isActiveAndEnabled || _interactor.IsChanneling ||
                !(_interactor.Current is IDiscardStation))
            {
                return;
            }

            var ctx = _interactor.Context;
            if (ctx?.Vitals == null || ctx.Vitals.Vitals == null || !ctx.Vitals.Vitals.IsAlive ||
                ctx.Inventory == null || ctx.Inventory.Inventory == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.gKey.wasPressedThisFrame)
            {
                return;
            }

            _ctx = ctx;
            _open = true;
            _feedback = string.Empty;
            _skipInputThisFrame = true; // 開く一打（G）を同フレームで拾い直さない。
            SetPlayerControl(false);
            StopPlayer();
            _text.text = Compose();
        }

        void Close()
        {
            _open = false;
            _ctx = null;
            SetPlayerControl(true);
            HideText();
        }

        void DiscardAt(int index)
        {
            var inventory = _ctx.Inventory.Inventory;
            if (index >= inventory.Count)
            {
                return;
            }

            if (inventory.TryRemoveAt(index, out var removed))
            {
                _feedback = DiscardText.Discarded(NameOf(removed));
            }
        }

        // 背負っている段ボールも捨てられる。下ろすのは既存の唯一の口（CarrySlot.TryTakeOut）を通す。
        void DiscardCarried()
        {
            var slot = _ctx.Carry != null ? _ctx.Carry.Slot : null;
            if (slot == null || !slot.IsOccupied)
            {
                return;
            }

            if (slot.TryTakeOut(out var item))
            {
                _feedback = DiscardText.Discarded(NameOf(item));
            }
        }

        string NameOf(ItemInstance item)
        {
            var catalog = _ctx.Inventory != null ? _ctx.Inventory.Catalog : null;
            return catalog != null && catalog.TryGet(item.ItemId, out var definition)
                ? definition.DisplayName
                : item.Id;
        }

        string Compose()
        {
            var inventory = _ctx.Inventory.Inventory;

            _sb.Clear();
            _sb.AppendLine(DiscardText.PanelHeader);
            _sb.AppendLine();

            _sb.AppendLine(DiscardText.BagHeading(inventory.UsedSlots, inventory.Capacity));
            if (inventory.Count == 0)
            {
                _sb.AppendLine(DiscardText.BagEmpty);
            }
            else
            {
                for (var i = 0; i < inventory.Count; i++)
                {
                    var name = inventory.TryGetDefinition(i, out var definition)
                        ? definition.DisplayName
                        : inventory[i].Id;
                    _sb.AppendLine(DiscardText.ItemLine(i + 1, name));
                }
            }

            var slot = _ctx.Carry != null ? _ctx.Carry.Slot : null;
            if (slot != null && slot.IsOccupied)
            {
                _sb.AppendLine();
                _sb.AppendLine(DiscardText.CarryLine(NameOf(slot.Item)));
            }

            if (!string.IsNullOrEmpty(_feedback))
            {
                _sb.AppendLine();
                _sb.AppendLine(_feedback);
            }

            _sb.AppendLine();
            _sb.AppendLine(DiscardText.CloseHint);
            return _sb.ToString();
        }

        void SetPlayerControl(bool enabled)
        {
            if (_motor != null)
            {
                _motor.enabled = enabled;
            }

            if (_interactor != null)
            {
                _interactor.enabled = enabled;
            }
        }

        void StopPlayer()
        {
            if (_ctx?.Transform != null && _ctx.Transform.TryGetComponent(out Rigidbody2D body))
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
        }

        void HideText()
        {
            if (_text != null)
            {
                _text.text = string.Empty;
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
    }
}
