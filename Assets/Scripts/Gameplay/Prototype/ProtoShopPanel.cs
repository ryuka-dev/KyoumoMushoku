using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// コンビニの入口。左クリックで店に入り、棚の品を買い、廃品を売る。取引は <see cref="ProtoStore"/>（本編と同じ Core）。
    /// 店の建物は外側だけの模型なので、中は画面の上の一覧で表す。店にいる間は時間を止める。
    /// 品揃えは本編の店（Storefront の既定）と同じ。
    /// </summary>
    public sealed class ProtoShopPanel : MonoBehaviour, IProtoUsable
    {
        [SerializeField] ProtoStore _store;
        [SerializeField] ProtoSession _session;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] Font _font;
        [Tooltip("今夜の廃棄がまだなら、店員がそれをこぼす（山場の予告）。上機嫌（SAN 70 以上）のときだけ聞ける。")]
        [SerializeField] ProtoDisposal _disposal;
        [SerializeField] string[] _offerIds = { "water_bottle", "onigiri", "can_coffee", "backpack" };

        ProtoHands _hands;
        InputAction _leave;
        string _clerkLine;
        GUIStyle _style;
        GUIStyle _title;
        GUIStyle _button;
        Texture2D _white;

        public bool IsOpen { get; private set; }

        public void Configure(ProtoStore store, ProtoSession session, ItemDatabaseAsset catalog, Font font,
            ProtoDisposal disposal)
        {
            _disposal = disposal;
            _store = store;
            _session = session;
            _catalog = catalog;
            _font = font;
        }

        void Awake() => _leave = new InputAction("LeaveShop", InputActionType.Button, "<Keyboard>/escape");

        void OnEnable() => _leave.Enable();

        void OnDisable() => _leave.Disable();

        void OnDestroy()
        {
            _leave?.Dispose();
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
            IsOpen = true;
            return null;
        }

        void Update()
        {
            if (IsOpen && _leave.WasPressedThisFrame())
            {
                Close();
            }
        }

        void Close()
        {
            IsOpen = false;
            _session?.CloseModal();
        }

        void OnGUI()
        {
            if (!IsOpen || _hands == null)
            {
                return;
            }

            EnsureStyles();
            GUI.depth = -10;
            var w = Screen.width;
            var h = Screen.height;
            var panel = new Rect(w / 2f - 320f, h / 2f - 250f, 640f, 500f);
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(panel, _white);
            GUI.color = previous;

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

            GUI.Label(new Rect(panel.x + 32f, panel.yMax - 112f, panel.width - 64f, 40f), _clerkLine, _style);
            if (GUI.Button(new Rect(panel.x + panel.width / 2f - 100f, panel.yMax - 60f, 200f, 40f), ProtoText.ShopLeave, _button))
            {
                Close();
            }
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
            _button ??= new GUIStyle(GUI.skin.button) { font = _font, fontSize = 20, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 16, 4, 4) };
        }
    }
}
