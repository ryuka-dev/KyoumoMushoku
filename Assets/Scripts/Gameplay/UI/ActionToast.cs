using KyoumoMushoku.Gameplay.Interaction;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.UI
{
    /// <summary>
    /// 調べもの（漁りなど）の結果を、短いあいだ画面に出して消す。
    /// 何が起きたのかを世界の言葉で事後に伝える経路であり（第十四節）、
    /// SAN を問わず必ず見える。食品の状態そのものは食品カードで読む（第三節）ため、ここには出さない。
    /// </summary>
    public sealed class ActionToast : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] PlayerInteractor _interactor;
        [SerializeField] float _holdSeconds = 2.5f;
        [SerializeField] float _fadeSeconds = 0.6f;

        float _remaining;

        public void Configure(PlayerInteractor interactor, TMP_Text text)
        {
            Unsubscribe();
            _interactor = interactor;
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
        }

        void Unsubscribe()
        {
            if (_interactor != null)
            {
                _interactor.ActionReported -= OnActionReported;
            }
        }

        void OnActionReported(string message)
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
