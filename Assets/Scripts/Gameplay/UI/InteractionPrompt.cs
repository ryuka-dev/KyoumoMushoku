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

            // 漁りなど時間のかかる調べものの最中は、進捗と中断の仕方を示す（第十節：探索時間の手応え）。
            if (_interactor.IsChanneling)
            {
                _text.text = HudText.Rummaging(ProgressBar(_interactor.ChannelProgress));
                return;
            }

            var current = _interactor.Current;
            if (current == null)
            {
                _text.text = string.Empty;
                return;
            }

            var description = current.Describe(_interactor.Context);
            var line = current.CanInteract(_interactor.Context)
                ? HudText.Interactable(description)
                : description;

            // 捨てる（G）は調べる（E）と独立した動詞。捨てられる場所では両方を並べて示す（第十四節・予告は事前に）。
            if (current is IDiscardStation)
            {
                line += "\n" + HudText.Discardable(DiscardText.Prompt);
            }

            _text.text = line;
        }

        /// <summary>
        /// 進捗の升目。ブロック要素（U+2591 など）は日本語フォントに無いことがあるため、
        /// JIS の範囲にある ■／□ を使う。豆腐を出さないこと自体が可読性の要請である（第三節）。
        /// </summary>
        static string ProgressBar(float progress)
        {
            const int cells = 10;
            var filled = Mathf.Clamp(Mathf.RoundToInt(progress * cells), 0, cells);
            return new string('■', filled) + new string('□', cells - filled);
        }
    }
}
