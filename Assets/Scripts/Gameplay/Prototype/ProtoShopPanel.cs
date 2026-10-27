using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// コンビニの入口。左クリックで店に入り、棚の品を買い、廃品を売り、レジ打ちのバイトをする。取引は <see cref="ProtoStore"/>（本編と同じ Core）。
    /// 店の建物は外側だけの模型なので、中は画面の上の一覧で表す。店にいる間は時間を止める。
    /// 品揃えは本編の店（Storefront の既定）と同じ。
    ///
    /// レジ打ちは本編と同じタイミングバー：カーソルが往復し、帯に入った瞬間に打つ。ラウンド数は ShopTuning、
    /// カーソルの速さと帯の幅は切片の数値（<see cref="SliceTuningAsset"/>）。時間が止まっているので、カーソルは実時間で動かす。
    /// 途中でやめれば何も消費しない。全ラウンドを打ち終えたときだけ精算する（<see cref="ProtoStore.Work"/>）。
    /// </summary>
    public sealed class ProtoShopPanel : MonoBehaviour, IProtoUsable
    {
        enum Mode { Menu, Working }

        [SerializeField] ProtoStore _store;
        [SerializeField] ProtoSession _session;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] SliceTuningAsset _slice;
        [SerializeField] Font _font;
        [Tooltip("今夜の廃棄がまだなら、店員がそれをこぼす（山場の予告）。上機嫌（SAN 70 以上）のときだけ聞ける。")]
        [SerializeField] ProtoDisposal _disposal;
        [SerializeField] string[] _offerIds = { "water_bottle", "onigiri", "can_coffee", "backpack" };

        ProtoHands _hands;
        InputAction _leave;
        InputAction _strike;
        AudioSource _audio;
        string _clerkLine;
        GUIStyle _style;
        GUIStyle _title;
        GUIStyle _center;
        GUIStyle _button;
        Texture2D _white;

        // レジ打ちの進行。
        Mode _mode;
        int _round;
        int _hits;
        float _cursor;
        int _direction = 1;
        string _feedback;
        float _feedbackUntil;
        bool _lastHit;

        public bool IsOpen { get; private set; }

        public void Configure(ProtoStore store, ProtoSession session, ItemDatabaseAsset catalog, SliceTuningAsset slice,
            Font font, ProtoDisposal disposal)
        {
            _disposal = disposal;
            _store = store;
            _session = session;
            _catalog = catalog;
            _slice = slice;
            _font = font;
        }

        void Awake()
        {
            _leave = new InputAction("LeaveShop", InputActionType.Button, "<Keyboard>/escape");
            _strike = new InputAction("StrikeRegister", InputActionType.Button, "<Keyboard>/space");
            _strike.AddBinding("<Mouse>/leftButton");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
        }

        void OnEnable()
        {
            _leave.Enable();
            _strike.Enable();
        }

        void OnDisable()
        {
            _leave.Disable();
            _strike.Disable();
        }

        void OnDestroy()
        {
            _leave?.Dispose();
            _strike?.Dispose();
            if (_white != null)
            {
                Destroy(_white);
            }
        }

        public string Prompt(ProtoHands hands) => ProtoText.ShopDoorPrompt;

        public string Use(ProtoHands hands)
        {
            if (_session == null || !_session.TryOpenModal())
            {
                return null;
            }

            _hands = hands;
            // 上機嫌の報酬は情報だけ（第三節）：機嫌よく生きていれば、街の人が教えてくれる。
            // 夕方のぼやき（ProtoDisposal）は SAN に依らず届くので、予告が誰にも届かない夜は無い。
            _clerkLine = _disposal != null && _disposal.PendingTonight && SanityScale.CanHearRumors(Sanity())
                ? ProtoText.ShopGreetingBeforeDisposal
                : ProtoText.ShopGreeting;
            _mode = Mode.Menu;
            IsOpen = true;
            return null;
        }

        void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (_mode == Mode.Working)
            {
                TickWork();
                return;
            }

            if (_leave.WasPressedThisFrame())
            {
                Close();
            }
        }

        void Close()
        {
            IsOpen = false;
            _mode = Mode.Menu;
            _session?.CloseModal();
        }

        // ── バイト（レジ打ちのタイミングバー） ───────────────────

        void StartWork()
        {
            if (_store == null || _store.JobRounds <= 0)
            {
                return;
            }

            _mode = Mode.Working;
            _round = 1;
            _hits = 0;
            _cursor = 0f;
            _direction = 1;
            _feedback = null;
        }

        void TickWork()
        {
            if (_leave.WasPressedThisFrame())
            {
                // 途中でシフトを抜ける。中断は何も消費しない（本編と同じ）。
                _mode = Mode.Menu;
                return;
            }

            // 店の中は Time.timeScale = 0 なので、カーソルは実時間で動かす。
            _cursor += _direction * CursorSpeed * Time.unscaledDeltaTime;
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

            if (!_strike.WasPressedThisFrame())
            {
                return;
            }

            var half = HalfBand();
            _lastHit = Mathf.Abs(_cursor - 0.5f) <= half;
            if (_lastHit)
            {
                _hits++;
            }

            _feedback = _lastHit ? ProtoText.WorkHit : ProtoText.WorkMiss;
            _feedbackUntil = Time.unscaledTime + 0.5f;
            _audio.PlayOneShot(_lastHit ? ProtoSounds.Beep : ProtoSounds.Clang, _lastHit ? 0.6f : 0.3f);

            _round++;
            if (_round > _store.JobRounds)
            {
                FinishWork();
            }
        }

        void FinishWork()
        {
            var performance = _store.JobRounds > 0 ? (float)_hits / _store.JobRounds : 0f;
            var vitals = _hands.Vitals != null ? _hands.Vitals.Vitals : null;
            _clerkLine = _store.Work(performance, vitals, _hands.Wallet) ?? _clerkLine;
            _mode = Mode.Menu;
        }

        float CursorSpeed => _slice != null ? _slice.JobCursorSpeed : 1.1f;

        /// <summary>命中帯の半幅。精神崩壊の帯（値札が読めないのと同じ境）では狭まる（本編と同じ）。</summary>
        float HalfBand()
        {
            var broken = !SanityScale.CanReadPrices(Sanity());
            if (_slice == null)
            {
                return broken ? 0.06f : 0.14f;
            }

            return broken ? _slice.JobBrokenHalfBand : _slice.JobHalfBand;
        }

        // ── 描画 ─────────────────────────────────────────────

        void OnGUI()
        {
            if (!IsOpen || _hands == null)
            {
                return;
            }

            EnsureStyles();
            GUI.depth = -10;
            if (_mode == Mode.Working)
            {
                DrawWork();
            }
            else
            {
                DrawMenu();
            }
        }

        void DrawMenu()
        {
            var w = Screen.width;
            var h = Screen.height;
            var panel = new Rect(w / 2f - 320f, h / 2f - 280f, 640f, 560f);
            Fill(panel, new Color(0f, 0f, 0f, 0.85f));

            GUI.Label(new Rect(panel.x, panel.y + 16f, panel.width, 40f), ProtoText.ShopTitle, _title);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 64f, panel.width - 64f, 30f),
                ProtoText.ShopWallet(_hands.Wallet.Yen, _hands.Bag.UsedSlots, _hands.Bag.Capacity), _style);

            var y = panel.y + 108f;
            foreach (var id in _offerIds)
            {
                var label = _store.BuyPrompt(id, _hands.Bag, Sanity());
                if (label == null)
                {
                    continue;
                }

                if (GUI.Button(new Rect(panel.x + 32f, y, panel.width - 64f, 40f), label, _button))
                {
                    _clerkLine = _store.Buy(id, _hands.Bag, _hands.Wallet);
                }

                y += 48f;
            }

            y += 8f;
            if (GUI.Button(new Rect(panel.x + 32f, y, panel.width - 64f, 40f), _store.SellPrompt(_hands.Bag), _button))
            {
                _clerkLine = _store.Sell(_hands.Bag, _hands.Wallet);
            }

            y += 56f;
            if (GUI.Button(new Rect(panel.x + 32f, y, panel.width - 64f, 40f), ProtoText.WorkPrompt, _button))
            {
                StartWork();
            }

            GUI.Label(new Rect(panel.x + 32f, panel.yMax - 112f, panel.width - 64f, 40f), _clerkLine, _style);
            if (GUI.Button(new Rect(panel.x + panel.width / 2f - 100f, panel.yMax - 60f, 200f, 40f), ProtoText.ShopLeave, _button))
            {
                Close();
            }
        }

        /// <summary>レジ打ち。店の一覧は閉じ、画面の下寄りにバーだけを出す（一人称の景色の上で打つ）。</summary>
        void DrawWork()
        {
            var w = Screen.width;
            var h = Screen.height;
            var panel = new Rect(w / 2f - 360f, h * 0.6f, 720f, 200f);
            Fill(panel, new Color(0f, 0f, 0f, 0.7f));

            GUI.Label(new Rect(panel.x, panel.y + 12f, panel.width, 32f),
                ShopText.WorkHeader(Mathf.Min(_round, _store.JobRounds), _store.JobRounds), _center);

            var bar = new Rect(panel.x + 40f, panel.y + 60f, panel.width - 80f, 40f);
            Fill(bar, new Color(0.25f, 0.25f, 0.27f, 1f));
            var half = HalfBand();
            Fill(new Rect(bar.x + bar.width * (0.5f - half), bar.y, bar.width * half * 2f, bar.height),
                new Color(0.35f, 0.7f, 0.4f, 1f));
            Fill(new Rect(bar.x + bar.width * _cursor - 4f, bar.y - 8f, 8f, bar.height + 16f), Color.white);

            var feedback = _feedback != null && Time.unscaledTime < _feedbackUntil ? _feedback : ShopText.WorkHits(_hits);
            var previous = GUI.contentColor;
            if (_feedback != null && Time.unscaledTime < _feedbackUntil)
            {
                GUI.contentColor = _lastHit ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.5f);
            }

            GUI.Label(new Rect(panel.x, panel.y + 112f, panel.width, 32f), feedback, _center);
            GUI.contentColor = previous;
            GUI.Label(new Rect(panel.x, panel.y + 152f, panel.width, 32f), ProtoText.WorkControls, _center);
        }

        void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _white);
            GUI.color = previous;
        }

        float Sanity() =>
            _hands.Vitals != null && _hands.Vitals.Vitals != null ? _hands.Vitals.Vitals.Sanity : 100f;

        void EnsureStyles()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }

            _style ??= new GUIStyle(GUI.skin.label) { font = _font, fontSize = 20, normal = { textColor = Color.white } };
            _title ??= new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
            _center ??= new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter };
            _button ??= new GUIStyle(GUI.skin.button) { font = _font, fontSize = 20, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 16, 4, 4) };
        }
    }
}
