using System.Text;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片の表示（IMGUI）。照準・状態のバー・日付と気分・鞄・照準先の案内・一言・朝と倒れたときの区切り。
    /// HUD はポスト処理の後に描かれるので、SAN の退色はかからない（文字は常に読める。欠落は `??` でのみ起こす）。
    ///
    /// SAN は数値のバーでは見せない。気分の言葉と、世界の色で伝える（第三節：SAN は三本目の体力ゲージではない）。
    /// </summary>
    public sealed class ProtoHud : MonoBehaviour
    {
        [SerializeField] ProtoHands _hands;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] ProtoVitals _vitals;
        [SerializeField] ProtoSession _session;
        [SerializeField] Font _font;

        GUIStyle _style;
        GUIStyle _center;
        GUIStyle _title;
        GUIStyle _button;
        Texture2D _white;
        Texture2D _ring;
        ProtoPolice[] _police = new ProtoPolice[0];
        readonly StringBuilder _text = new StringBuilder();

        public void Configure(ProtoHands hands, ProtoDayNight dayNight, ProtoVitals vitals, ProtoSession session, Font font)
        {
            _hands = hands;
            _dayNight = dayNight;
            _vitals = vitals;
            _session = session;
            _font = font;
        }

        void Start() => _police = FindObjectsByType<ProtoPolice>(FindObjectsSortMode.None);

        void OnDestroy()
        {
            if (_white != null)
            {
                Destroy(_white);
            }

            if (_ring != null)
            {
                Destroy(_ring);
            }
        }

        void OnGUI()
        {
            if (_hands == null || _hands.Bag == null || _vitals == null || _vitals.Vitals == null)
            {
                return;
            }

            EnsureStyles();
            var w = Screen.width;
            var h = Screen.height;
            var vitals = _vitals.Vitals;

            if (_session != null && _session.State != ProtoSession.SessionState.Playing)
            {
                DrawPanel(w, h);
                return;
            }

            DrawCrosshair(w, h);

            // 左上：日付・所持金・気分、その下に HP・喉・空腹のバー。
            var clock = _dayNight != null ? _dayNight.Clock : null;
            var status = ProtoText.Status(clock != null ? clock.Day : 1,
                clock != null ? GameTextLabels.Phase(clock.Phase) : HudText.Unknown,
                _hands.Wallet != null ? _hands.Wallet.Yen : 0,
                GameTextLabels.SanityTier(vitals.SanityTier));
            GUI.Label(new Rect(16f, 12f, 360f, 84f), status, _style);
            DrawBar(new Rect(16f, 100f, 200f, 14f), HudText.HpLabel, vitals.HpFraction, new Color(0.85f, 0.3f, 0.3f));
            DrawBar(new Rect(16f, 122f, 200f, 14f), HudText.ThirstLabel, vitals.ThirstFraction, new Color(0.35f, 0.65f, 0.95f));
            DrawBar(new Rect(16f, 144f, 200f, 14f), HudText.HungerLabel, vitals.HungerFraction, new Color(0.95f, 0.7f, 0.3f));

            // 一人称では警官が背後にいることもあるので、最も強い段階だけを一行で知らせる。
            var stage = PoliceStage.Unaware;
            foreach (var officer in _police)
            {
                if (officer != null && officer.Stage > stage)
                {
                    stage = officer.Stage;
                }
            }

            if (stage != PoliceStage.Unaware)
            {
                var previous = GUI.contentColor;
                GUI.contentColor = stage == PoliceStage.Pursuing ? new Color(1f, 0.45f, 0.4f) : new Color(1f, 0.85f, 0.4f);
                GUI.Label(new Rect(240f, 98f, 300f, 28f), ProtoText.PoliceStatus(GameTextLabels.PoliceStage(stage)), _style);
                GUI.contentColor = previous;
            }

            // 鞄：番号は数字キーと同じ。食品の状態は SAN によって `??` になる（FoodCardText）。
            var bag = _hands.Bag;
            _text.Clear();
            _text.AppendLine(HudText.BagHeader(bag.UsedSlots, bag.Capacity));
            if (bag.Count == 0)
            {
                _text.AppendLine(HudText.BagEmpty);
            }

            for (var i = 0; i < bag.Count; i++)
            {
                var headline = bag.TryGetDefinition(i, out var definition)
                    ? FoodCardText.Headline(definition, bag[i].Freshness, vitals.Sanity)
                    : bag[i].Id;
                _text.AppendLine(HudText.InventoryItemLine(i + 1, headline));
            }

            if (bag.Count > 0)
            {
                _text.Append(HudText.EatHint);
            }

            GUI.Label(new Rect(16f, 172f, 420f, h - 220f), _text.ToString(), _style);
            GUI.Label(new Rect(16f, h - 36f, w - 32f, 28f), ProtoText.Controls, _style);

            if (!string.IsNullOrEmpty(_hands.AimPrompt))
            {
                // 押して何かが起きるときだけ操作を添える。
                DrawBackedLabel(h / 2f + 30f, w, _hands.Aim == ProtoHands.AimState.InReach
                    ? ProtoText.ClickPrompt(_hands.AimPrompt)
                    : _hands.AimPrompt);
            }

            if (!string.IsNullOrEmpty(_hands.Toast) && _hands.ToastAge < _hands.ToastSeconds)
            {
                DrawBackedLabel(h * 0.68f, w, _hands.Toast);
            }
        }

        /// <summary>
        /// 照準。何も無ければ小さな「+」。叩ける物・使える物を見ていれば輪になり、手が届けば暖色ではっきり、
        /// 届かなければ薄く小さく出す（遠くからでも「あれは何かできる」と分かる。何ができるかは近づいてから言う）。
        /// </summary>
        void DrawCrosshair(float w, float h)
        {
            var center = new Vector2(w / 2f, h / 2f);
            switch (_hands.Aim)
            {
                case ProtoHands.AimState.InReach:
                    DrawRing(center, 40f, new Color(1f, 0.78f, 0.35f, 0.95f));
                    break;
                case ProtoHands.AimState.Distant:
                    DrawRing(center, 26f, new Color(1f, 1f, 1f, 0.5f));
                    break;
                default:
                    GUI.Label(new Rect(center.x - 10f, center.y - 12f, 20f, 24f), "+", _center);
                    break;
            }
        }

        /// <summary>画面中央に、薄暗い帯を敷いた一行（明るい壁や白いゴミ箱の上でも読めるように）。</summary>
        void DrawBackedLabel(float y, float w, string text)
        {
            var content = new GUIContent(text);
            var size = _center.CalcSize(content);
            var back = new Rect((w - size.x) / 2f - 12f, y - 2f, size.x + 24f, size.y + 4f);
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(back, _white);
            GUI.color = previous;
            GUI.Label(new Rect(0f, y, w, size.y), content, _center);
        }

        void DrawRing(Vector2 center, float size, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), _ring);
            GUI.color = previous;
        }

        static Texture2D BuildRing()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    // 半径 26・太さ 5 の輪（64px 中）。縁は 1px で滑らかに落とす。
                    var a = Mathf.Clamp01(3.5f - Mathf.Abs(d - 26f));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        void DrawPanel(float w, float h)
        {
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), _white);
            GUI.color = previous;

            var vitals = _vitals.Vitals;
            var day = _dayNight != null && _dayNight.Clock != null ? _dayNight.Clock.Day : 1;
            var mood = GameTextLabels.SanityTier(vitals.SanityTier);
            var morning = _session.State == ProtoSession.SessionState.Morning;
            var final = _session.State == ProtoSession.SessionState.Final;
            var yen = _hands.Wallet != null ? _hands.Wallet.Yen : 0;

            string title;
            string body;
            if (final)
            {
                title = ProtoText.FinalTitle(_session.FinalDay);
                body = ProtoText.FinalBody(_session.FinalDay, _session.CaughtTotal, yen, _hands.Bag.Capacity, mood);
            }
            else if (morning)
            {
                title = ProtoText.MorningTitle(day);
                body = ProtoText.MorningBody(_session.CaughtYesterday, yen, mood);
            }
            else
            {
                title = ProtoText.CollapsedTitle;
                body = ProtoText.CollapsedBody(day);
            }

            GUI.Label(new Rect(0f, h * 0.24f, w, 48f), title, _title);
            GUI.Label(new Rect(0f, h * 0.24f + 60f, w, 200f), body, _center);

            var y = h * 0.24f + 280f;
            if (morning)
            {
                if (GUI.Button(new Rect(w / 2f - 210f, y, 200f, 44f), ProtoText.ContinueButton, _button))
                {
                    _session.Continue();
                }
            }
            else if (GUI.Button(new Rect(w / 2f - 210f, y, 200f, 44f), ProtoText.RestartButton, _button))
            {
                _session.Restart();
            }

            if (GUI.Button(new Rect(w / 2f + 10f, y, 200f, 44f), ProtoText.QuitButton, _button))
            {
                _session.Quit();
            }
        }

        void DrawBar(Rect rect, string label, float fraction, Color color)
        {
            GUI.Label(new Rect(rect.x, rect.y + rect.height / 2f - 14f, 52f, 28f), label, _style);
            var bar = new Rect(rect.x + 52f, rect.y, rect.width - 52f, rect.height);
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(bar, _white);
            GUI.color = color;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(fraction), bar.height), _white);
            GUI.color = previous;
        }

        void EnsureStyles()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }

            if (_ring == null)
            {
                _ring = BuildRing();
            }

            _style ??= new GUIStyle(GUI.skin.label) { font = _font, fontSize = 18, normal = { textColor = Color.white } };
            _center ??= new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            _title ??= new GUIStyle(_center) { fontSize = 40 };
            _button ??= new GUIStyle(GUI.skin.button) { font = _font, fontSize = 20 };
        }
    }
}
