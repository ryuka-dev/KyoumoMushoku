using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 世界の中の発話。話者の頭上に短いあいだ文字を出して消す。
    ///
    /// 見えない状態が変化したとき、その原因を後から世界の側が教える（第十四節）。
    /// システムのポップアップではなく、誰が言ったのかが画面上で分かる形にするために、
    /// 画面座標ではなく話者の位置に置く。SAN がどれだけ落ちてもこの経路は死なない。
    ///
    /// 警官が使い、Phase 5 の先輩ホームレスが同じものを使う。
    /// </summary>
    public sealed class NpcSpeech : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] float _fadeSeconds = 0.5f;

        float _remaining;
        float _holdSeconds;

        public void Configure(TMP_Text text)
        {
            _text = text;
            SetAlpha(0f);
        }

        void Start()
        {
            SetAlpha(0f);
        }

        /// <summary>言う。前の台詞が残っていても、新しい台詞が上書きする。</summary>
        public void Say(string line, float seconds = 2.5f)
        {
            if (_text == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            _text.text = line;
            _holdSeconds = Mathf.Max(0.1f, seconds);
            _remaining = _holdSeconds;
            SetAlpha(1f);
        }

        void Update()
        {
            if (_text == null || _remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.deltaTime;

            var alpha = _fadeSeconds > 0f ? Mathf.Clamp01(_remaining / _fadeSeconds) : (_remaining > 0f ? 1f : 0f);
            SetAlpha(alpha);
        }

        void SetAlpha(float alpha)
        {
            if (_text == null)
            {
                return;
            }

            var color = _text.color;
            color.a = alpha;
            _text.color = color;
        }
    }
}
