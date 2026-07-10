using KyoumoMushoku.Gameplay.Interaction;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 今そばにある物体に対して何ができるかを、その場の言葉で示す。
    /// 使える時は行動とキー、使えない時は理由（「カバンが一杯だ」など）を出す。
    /// これにより「見えていない」と「作られていない」をプレイヤーが取り違えない（第十四節）。
    /// </summary>
    public sealed class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] PlayerInteractor _interactor;

        public void Configure(PlayerInteractor interactor, TMP_Text text)
        {
            _interactor = interactor;
            _text = text;
        }

        void Update()
        {
            if (_text == null || _interactor == null)
            {
                return;
            }

            var current = _interactor.Current;
            if (current == null)
            {
                _text.text = string.Empty;
                return;
            }

            var description = current.Describe(_interactor.Context);
            _text.text = current.CanInteract(_interactor.Context)
                ? $"{description}　［E］"
                : description;
        }
    }
}
