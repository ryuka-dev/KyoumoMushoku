using System.Text;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 試作の最小限の表示（照準・鞄・所持金・照準先の案内・一言・操作説明）。灰箱なので IMGUI で足りる。本編の HUD とは別物。
    /// </summary>
    public sealed class ProtoHud : MonoBehaviour
    {
        [SerializeField] ProtoHands _hands;
        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] Font _font;

        GUIStyle _style;
        GUIStyle _center;
        readonly StringBuilder _bag = new StringBuilder();

        public void Configure(ProtoHands hands, ProtoDayNight dayNight, Font font)
        {
            _hands = hands;
            _dayNight = dayNight;
            _font = font;
        }

        void OnGUI()
        {
            if (_hands == null || _hands.Bag == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { font = _font, fontSize = 18, normal = { textColor = Color.white } };
            _center ??= new GUIStyle(_style) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };

            var w = Screen.width;
            var h = Screen.height;
            GUI.Label(new Rect(w / 2f - 10f, h / 2f - 12f, 20f, 24f), "+", _center);

            var bag = _hands.Bag;
            _bag.Clear();
            if (_hands.Wallet != null)
            {
                _bag.AppendLine(ProtoText.Money(_hands.Wallet.Yen));
            }

            _bag.AppendLine(ProtoText.Bag(bag.UsedSlots, bag.Capacity));
            for (var i = 0; i < bag.Count; i++)
            {
                _bag.AppendLine(ProtoText.BagLine(bag.TryGetDefinition(i, out var definition) ? definition.DisplayName : bag[i].Id));
            }

            if (_dayNight != null)
            {
                _bag.AppendLine(ProtoText.TimeOfDay(_dayNight.IsNight));
            }

            GUI.Label(new Rect(16f, 16f, 360f, 520f), _bag.ToString(), _style);

            if (!string.IsNullOrEmpty(_hands.AimPrompt))
            {
                GUI.Label(new Rect(0f, h / 2f + 24f, w, 32f), _hands.AimPrompt, _center);
            }
            GUI.Label(new Rect(16f, h - 36f, w - 32f, 28f), ProtoText.Controls, _style);

            if (!string.IsNullOrEmpty(_hands.Toast) && _hands.ToastAge < 2.2f)
            {
                GUI.Label(new Rect(0f, h * 0.68f, w, 40f), _hands.Toast, _center);
            }
        }
    }
}
