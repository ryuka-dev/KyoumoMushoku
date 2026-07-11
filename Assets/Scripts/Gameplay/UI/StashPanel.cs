using System.Text;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.World;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 段ボール箱の出し入れパネル（第十二節）。開いている間は移動と通常のインタラクトを止めて入力を占有する
    /// （<see cref="IInputModal"/>）。開くのは <see cref="StashSpot.Opened"/> の合図。
    ///
    /// 種別による取り出し制限は設けない。効くのは空きマスだけである（第十一節）：預けるときは箱の空き、
    /// 取り出すときはカバンの空き。Tab で「預ける／引き出す」を切り替え、数字キーでその側の品を動かす。
    /// </summary>
    public sealed class StashPanel : MonoBehaviour, IInputModal
    {
        enum Side { Deposit, Withdraw }

        [SerializeField] PlayerMotor _motor;
        [SerializeField] PlayerInteractor _interactor;
        [SerializeField] TMP_Text _text;

        readonly StringBuilder _sb = new StringBuilder();

        StashSpot _spot;
        Stash _stash;
        PlayerContext _ctx;
        bool _open;
        bool _skipInputThisFrame;
        Side _side = Side.Deposit;
        string _feedback = string.Empty;

        public bool IsOpen => _open;

        public void Configure(StashSpot spot, PlayerMotor motor, PlayerInteractor interactor, TMP_Text text)
        {
            Unsubscribe();
            _spot = spot;
            _motor = motor;
            _interactor = interactor;
            _text = text;
            Subscribe();
            HideText();
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            Unsubscribe();
            if (_open)
            {
                // どんな終わり方でも操作を返す。モーダルに閉じ込めたまま消えない。
                SetPlayerControl(true);
                _open = false;
            }
        }

        void Subscribe()
        {
            if (_spot != null)
            {
                _spot.Opened += OnOpened;
            }
        }

        void Unsubscribe()
        {
            if (_spot != null)
            {
                _spot.Opened -= OnOpened;
            }
        }

        // インタラクタが有効なのはパネルが閉じているときだけなので、この合図は常に「開く」を意味する。
        void OnOpened(PlayerContext ctx, Stash stash) => Open(ctx, stash);

        void Open(PlayerContext ctx, Stash stash)
        {
            _ctx = ctx;
            _stash = stash;
            _open = true;
            _side = Side.Deposit;
            _feedback = string.Empty;
            _skipInputThisFrame = true; // 開く一打（E/Space）を同フレームで拾い直さない。
            SetPlayerControl(false);
            StopPlayer();
        }

        void Close()
        {
            _open = false;
            _stash = null;
            SetPlayerControl(true);
            HideText();
        }

        void Update()
        {
            if (!_open || _text == null)
            {
                return;
            }

            // 出し入れ中に飢え死にしたら、搬送に任せてモーダルを畳む。閉じ込めない。
            if (_ctx?.Vitals == null || _ctx.Vitals.Vitals == null || !_ctx.Vitals.Vitals.IsAlive ||
                _stash == null || _ctx.Inventory == null || _ctx.Inventory.Inventory == null)
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
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
                {
                    Close();
                    return;
                }

                if (keyboard.tabKey.wasPressedThisFrame)
                {
                    _side = _side == Side.Deposit ? Side.Withdraw : Side.Deposit;
                    _feedback = string.Empty;
                }
                else
                {
                    for (var i = 0; i < 9; i++)
                    {
                        if (DigitPressed(keyboard, i + 1))
                        {
                            Act(i);
                            break;
                        }
                    }
                }
            }

            _text.text = Compose();
        }

        void Act(int index)
        {
            if (_side == Side.Deposit)
            {
                Deposit(index);
            }
            else
            {
                Withdraw(index);
            }
        }

        void Deposit(int index)
        {
            var inventory = _ctx.Inventory.Inventory;
            if (index >= inventory.Count)
            {
                return;
            }

            var item = inventory[index];
            if (!_stash.CanDeposit(item))
            {
                _feedback = "箱がいっぱいだ。";
                return;
            }

            // 空きは先に確かめてある。抜いてから預ける。
            inventory.TryRemoveAt(index, out var removed);
            _stash.TryDeposit(removed);
            _feedback = $"{NameOf(removed)}を箱に入れた。";
        }

        void Withdraw(int index)
        {
            var inventory = _ctx.Inventory.Inventory;
            if (index >= _stash.Count)
            {
                return;
            }

            var item = _stash[index];
            if (!inventory.CanAdd(item))
            {
                _feedback = "カバンがいっぱいだ。";
                return;
            }

            _stash.TryWithdrawAt(index, out var taken);
            inventory.TryAdd(taken);
            _feedback = $"{NameOf(taken)}を取り出した。";
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
            _sb.AppendLine("＝ 段ボール箱 ＝　［Tab：預ける↔引き出す］　［E／Esc：閉じる］");
            _sb.AppendLine();

            var depositActive = _side == Side.Deposit;

            _sb.AppendLine($"{(depositActive ? "▶" : "　")} 預ける（カバン → 箱）　カバン {inventory.UsedSlots}/{inventory.Capacity}マス");
            AppendItems(inventory.Count, depositActive, i => inventory.TryGetDefinition(i, out var d) ? d.DisplayName : inventory[i].Id, "（カバンは空）");
            _sb.AppendLine();

            _sb.AppendLine($"{(!depositActive ? "▶" : "　")} 引き出す（箱 → カバン）　箱 {_stash.UsedSlots}/{_stash.Capacity}マス");
            AppendItems(_stash.Count, !depositActive, i => _stash.TryGetDefinition(i, out var d) ? d.DisplayName : _stash[i].Id, "（箱は空）");

            if (!string.IsNullOrEmpty(_feedback))
            {
                _sb.AppendLine();
                _sb.AppendLine(_feedback);
            }

            return _sb.ToString();
        }

        void AppendItems(int count, bool numbered, System.Func<int, string> nameAt, string emptyLine)
        {
            if (count == 0)
            {
                _sb.AppendLine($"　　{emptyLine}");
                return;
            }

            for (var i = 0; i < count; i++)
            {
                // 数字が効くのは選ばれている側だけ。効かない側は数字を出さず取り違えを避ける。
                var head = numbered ? $"{i + 1}." : "・";
                _sb.AppendLine($"　　{head} {nameAt(i)}");
            }
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
