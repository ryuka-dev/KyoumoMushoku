using System.Text;
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
        readonly StringBuilder _text = new StringBuilder();

        public void Configure(ProtoHands hands, ProtoDayNight dayNight, ProtoVitals vitals, ProtoSession session, Font font)
        {
            _hands = hands;
            _dayNight = dayNight;
            _vitals = vitals;
            _session = session;
            _font = font;
        }

        void OnDestroy()
        {
            if (_white != null)
            {
                Destroy(_white);
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

            GUI.Label(new Rect(w / 2f - 10f, h / 2f - 12f, 20f, 24f), "+", _center);

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
                GUI.Label(new Rect(0f, h / 2f + 24f, w, 32f), _hands.AimPrompt, _center);
            }

            if (!string.IsNullOrEmpty(_hands.Toast) && _hands.ToastAge < 2.2f)
            {
                GUI.Label(new Rect(0f, h * 0.68f, w, 40f), _hands.Toast, _center);
            }
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

            GUI.Label(new Rect(0f, h * 0.28f, w, 48f),
                morning ? ProtoText.MorningTitle(day) : ProtoText.CollapsedTitle, _title);
            GUI.Label(new Rect(0f, h * 0.28f + 60f, w, 120f),
                morning
                    ? ProtoText.MorningBody(_session.CaughtYesterday, _hands.Wallet != null ? _hands.Wallet.Yen : 0, mood)
                    : ProtoText.CollapsedBody(day),
                _center);

            var y = h * 0.28f + 200f;
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

            _style ??= new GUIStyle(GUI.skin.label) { font = _font, fontSize = 18, normal = { textColor = Color.white } };
            _center ??= new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            _title ??= new GUIStyle(_center) { fontSize = 40 };
            _button ??= new GUIStyle(GUI.skin.button) { font = _font, fontSize = 20 };
        }
    }
}
