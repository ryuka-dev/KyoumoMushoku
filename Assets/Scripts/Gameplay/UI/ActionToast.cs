using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Knacks;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 調べもの（漁りなど）の結果や、コツを覚えた瞬間を、短いあいだ画面に出して消す。
    /// 何が起きたのかを世界の言葉で事後に伝える経路であり（第十四節）、
    /// SAN を問わず必ず見える。食品の状態そのものは食品カードで読む（第三節）ため、ここには出さない。
    ///
    /// コツの習得通知は、先輩ホームレス（Phase 5b）が登場するまでのあいだ、ここが引き受ける。
    /// </summary>
    public sealed class ActionToast : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] PlayerInteractor _interactor;
        [SerializeField] PlayerKnacks _knacks;
        [SerializeField] float _holdSeconds = 2.5f;
        [SerializeField] float _fadeSeconds = 0.6f;

        float _remaining;

        public void Configure(PlayerInteractor interactor, PlayerKnacks knacks, TMP_Text text)
        {
            Unsubscribe();
            _interactor = interactor;
            _knacks = knacks;
            _text = text;
            Subscribe();
        }

        void OnEnable() => Subscribe();

        void OnDisable() => Unsubscribe();

        void Start()
        {
            if (_text != null)
            {
                SetAlpha(0f);
            }
        }

        void Subscribe()
        {
            if (_interactor != null)
            {
                _interactor.ActionReported += OnActionReported;
            }

            if (_knacks != null)
            {
                _knacks.Acquired += OnKnackAcquired;
            }
        }

        void Unsubscribe()
        {
            if (_interactor != null)
            {
                _interactor.ActionReported -= OnActionReported;
            }

            if (_knacks != null)
            {
                _knacks.Acquired -= OnKnackAcquired;
            }
        }

        void OnActionReported(string message) => Show(message);

        // 習得の瞬間は明示的に通知する（第六節）。習得したのはルールであって数値ではない。
        void OnKnackAcquired(KnackId id) => Show($"コツを覚えた：{GameTextLabels.Knack(id)}");

        void Show(string message)
        {
            if (_text == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            _text.text = message;
            _remaining = _holdSeconds + _fadeSeconds;
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
            var color = _text.color;
            color.a = alpha;
            _text.color = color;
        }
    }
}
