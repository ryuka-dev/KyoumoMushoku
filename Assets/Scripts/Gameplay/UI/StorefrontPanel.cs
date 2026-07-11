using System.Text;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Shop;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Player;
using KyoumoMushoku.Gameplay.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// コンビニの店パネル。購入・売却・バイトを1枚で扱うモーダルであり、開いている間は移動と通常のインタラクトを
    /// 止めて入力を占有する（<see cref="IInputModal"/>）。開くのは <see cref="Storefront.Entered"/> の合図。
    ///
    /// バイトはレジ打ちのタイミングバー（第十節：リアクション系ミニゲーム1種類）。低 SAN では帯が狭まり、
    /// さらに報酬倍率も暴落する（第三節）。ただし操作そのものは決して奪わない。欠落は用いるが、封鎖はしない。
    /// 価格は SAN 20 未満で `??` になる（第三節：商品の価格）。買う行為は封鎖しない。
    /// </summary>
    public sealed class StorefrontPanel : MonoBehaviour, IInputModal
    {
        enum Mode { Closed, Menu, Working }

        [SerializeField] Storefront _store;
        [SerializeField] TMP_Text _text;
        [SerializeField] PlayerMotor _motor;
        [SerializeField] PlayerInteractor _interactor;

        [Tooltip("タイミングバーのカーソルが往復する速さ（毎秒の割合）。叩き台。")]
        [SerializeField] float _cursorSpeed = 1.1f;

        readonly StringBuilder _sb = new StringBuilder();

        Mode _mode = Mode.Closed;
        PlayerContext _ctx;
        bool _skipInputThisFrame;

        // レジ打ちの進行。
        int _round;
        int _hits;
        float _cursor;
        int _direction = 1;

        public bool IsOpen => _mode != Mode.Closed;

        public void Configure(Storefront store, PlayerMotor motor, PlayerInteractor interactor, TMP_Text text)
        {
            Unsubscribe();
            _store = store;
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
            if (IsOpen)
            {
                // どんな終わり方でも操作を返す。モーダルに閉じ込めたまま消えない。
                SetPlayerControl(true);
                _mode = Mode.Closed;
            }
        }

        void Subscribe()
        {
            if (_store != null)
            {
                _store.Entered += OnEntered;
            }
        }

        void Unsubscribe()
        {
            if (_store != null)
            {
                _store.Entered -= OnEntered;
            }
        }

        // インタラクタが有効なのはパネルが閉じているときだけなので、この合図は常に「開く」を意味する。
        void OnEntered(PlayerContext ctx) => Open(ctx);

        void Open(PlayerContext ctx)
        {
            _ctx = ctx;
            _mode = Mode.Menu;
            _skipInputThisFrame = true; // 開く一打（E/Space）を同フレームで拾い直さない。
            SetPlayerControl(false);
            StopPlayer();
        }

        void Close()
        {
            _mode = Mode.Closed;
            SetPlayerControl(true);
            HideText();
        }

        void Update()
        {
            if (_mode == Mode.Closed || _text == null)
            {
                return;
            }

            // 買い物中に飢え死にしたら、搬送に任せてモーダルを畳む。閉じ込めない。
            if (_ctx?.Vitals == null || _ctx.Vitals.Vitals == null || !_ctx.Vitals.Vitals.IsAlive)
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

            if (_mode == Mode.Working)
            {
                TickWork(keyboard);
            }
            else
            {
                TickMenu(keyboard);
            }
        }

        // ── メニュー ─────────────────────────────────────────────

        void TickMenu(Keyboard keyboard)
        {
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
                {
                    Close();
                    return;
                }

                if (keyboard.sKey.wasPressedThisFrame)
                {
                    Sell();
                }
                else if (keyboard.wKey.wasPressedThisFrame)
                {
                    StartWork();
                    return;
                }
                else
                {
                    for (var i = 0; i < _store.OfferIds.Count && i < 9; i++)
                    {
                        if (DigitPressed(keyboard, i + 1))
                        {
                            Buy(i);
                            break;
                        }
                    }
                }
            }

            _text.text = ComposeMenu();
        }

        void Buy(int offerIndex)
        {
            var id = new ItemId(_store.OfferIds[offerIndex]);
            var result = _store.TryBuy(id, _ctx, out var bought);
            _store.Speak(result switch
            {
                PurchaseResult.Bought => ShopText.BoughtThanks(bought != null ? bought.DisplayName : null),
                PurchaseResult.CannotAfford => ShopText.CannotAfford,
                PurchaseResult.InventoryFull => ShopText.InventoryFull,
                PurchaseResult.AlreadyOwned => ShopText.AlreadyOwned,
                _ => ShopText.NotSold,
            });
        }

        void Sell()
        {
            var result = _store.SellSalvage(_ctx);
            if (result.SoldCount > 0)
            {
                _store.Speak(ShopText.SalvageSold(result.SoldCount, result.PaidYen, result.CapReached));
            }
            else
            {
                _store.Speak(result.CapReached ? ShopText.BuybackCapReached : ShopText.NothingToSell);
            }
        }

        string ComposeMenu()
        {
            var sanity = _ctx.Vitals.Vitals.Sanity;
            var catalog = _ctx.Inventory != null ? _ctx.Inventory.Catalog : null;
            var inventory = _ctx.Inventory != null ? _ctx.Inventory.Inventory : null;
            var canReadPrices = SanityScale.CanReadPrices(sanity);

            _sb.Clear();
            _sb.AppendLine(ShopText.MenuHeader);
            _sb.AppendLine();
            _sb.AppendLine(ShopText.MenuBuyHeading);

            for (var i = 0; i < _store.OfferIds.Count; i++)
            {
                var id = new ItemId(_store.OfferIds[i]);
                if (catalog == null || !catalog.TryGet(id, out var def) || !def.IsForSale)
                {
                    continue;
                }

                string tail;
                if (def.CapacityBonus > 0 && inventory != null &&
                    inventory.Capacity >= Inventory.DefaultCapacity + def.CapacityBonus)
                {
                    tail = ShopText.OfferOwned;
                }
                else if (canReadPrices)
                {
                    tail = ShopText.OfferPrice(def.BuyPriceYen);
                }
                else
                {
                    tail = ShopText.OfferPriceUnreadable;
                }

                _sb.AppendLine(ShopText.MenuOfferLine(i + 1, def.DisplayName, tail));
            }

            if (!canReadPrices)
            {
                _sb.AppendLine(ShopText.PriceUnreadableNote);
            }

            _sb.AppendLine();
            var remaining = _store.Ledger.RemainingToday(_store.BuybackDailyCapYen);
            _sb.AppendLine(ShopText.MenuSellHeading(remaining));
            _sb.AppendLine();
            _sb.AppendLine(ShopText.MenuWorkHeading);

            return _sb.ToString();
        }

        // ── バイト（レジ打ちのタイミングバー） ───────────────────

        void StartWork()
        {
            _mode = Mode.Working;
            _round = 1;
            _hits = 0;
            _cursor = 0f;
            _direction = 1;
        }

        void TickWork(Keyboard keyboard)
        {
            // カーソルは 0↔1 を往復する。命中判定に依存しない実時間で動くので、そのぶんソフトクロックも進む。
            _cursor += _direction * _cursorSpeed * Time.deltaTime;
            if (_cursor >= 1f)
            {
                _cursor = 1f;
                _direction = -1;
            }
            else if (_cursor <= 0f)
            {
                _cursor = 0f;
                _direction = 1;
            }

            var sanity = _ctx.Vitals.Vitals.Sanity;
            var halfBand = SanityScale.CanReadPrices(sanity) ? 0.14f : 0.06f; // 崩壊帯では帯が狭まる（第三節）
            var low = 0.5f - halfBand;
            var high = 0.5f + halfBand;

            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    // 途中でシフトを抜ける。中断は何も消費しない（漁りの中断と同じ）。
                    _mode = Mode.Menu;
                    return;
                }

                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
                {
                    if (_cursor >= low && _cursor <= high)
                    {
                        _hits++;
                    }

                    _round++;
                    if (_round > _store.JobRounds)
                    {
                        FinishWork();
                        return;
                    }
                }
            }

            _text.text = ComposeWork(low, high);
        }

        void FinishWork()
        {
            var performance = _store.JobRounds > 0 ? (float)_hits / _store.JobRounds : 0f;
            var paid = _store.ApplyJobOutcome(performance, _ctx);
            _store.Speak(ShopText.JobDone(paid));
            _mode = Mode.Menu;
        }

        string ComposeWork(float low, float high)
        {
            _sb.Clear();
            _sb.AppendLine(ShopText.WorkHeader(_round, _store.JobRounds));
            _sb.AppendLine();
            // 等幅を強制する。比例フォントのままだと '-' と '=' の幅が違い、カーソル位置が実際とずれて見える。
            _sb.AppendLine($"<mspace=0.6em>{BuildBar(_cursor, low, high)}</mspace>");
            _sb.AppendLine();
            _sb.AppendLine(ShopText.WorkControls);
            _sb.AppendLine();
            _sb.AppendLine(ShopText.WorkHits(_hits));
            return _sb.ToString();
        }

        /// <summary>
        /// タイミングバーを ASCII で描く（豆腐を避けるため全角記号は使わない）。帯は | | で囲み = で満たし、
        /// 帯外は -、カーソルは # で示す。
        /// </summary>
        static string BuildBar(float cursor, float low, float high)
        {
            const int cells = 26;
            var chars = new char[cells];
            var lowCell = Mathf.RoundToInt(low * (cells - 1));
            var highCell = Mathf.RoundToInt(high * (cells - 1));
            var cursorCell = Mathf.Clamp(Mathf.RoundToInt(cursor * (cells - 1)), 0, cells - 1);

            for (var i = 0; i < cells; i++)
            {
                if (i == lowCell || i == highCell)
                {
                    chars[i] = '|';
                }
                else
                {
                    chars[i] = i > lowCell && i < highCell ? '=' : '-';
                }
            }

            chars[cursorCell] = '#';
            return "[" + new string(chars) + "]";
        }

        // ── 入力の占有 ───────────────────────────────────────────

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
