using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 試作の昼夜。本編の時計は持ち込まず、N キーで切り替えるだけ。産出テーブルの昼／夜を確かめるため。
    /// </summary>
    public sealed class ProtoDayNight : MonoBehaviour
    {
        [SerializeField] Light _sun;
        [SerializeField] Color _dayColor = new Color(1f, 0.96f, 0.9f);
        [SerializeField] Color _nightColor = new Color(0.35f, 0.42f, 0.7f);
        [SerializeField, Min(0f)] float _dayIntensity = 1.2f;
        [SerializeField, Min(0f)] float _nightIntensity = 0.25f;

        InputAction _toggle;

        public bool IsNight { get; private set; }

        public event Action Changed;

        public void Configure(Light sun) => _sun = sun;

        void Awake() => _toggle = new InputAction("ToggleNight", InputActionType.Button, "<Keyboard>/n");

        void OnEnable()
        {
            _toggle.Enable();
            Apply();
        }

        void OnDisable() => _toggle.Disable();

        void OnDestroy() => _toggle?.Dispose();

        void Update()
        {
            if (!_toggle.WasPressedThisFrame())
            {
                return;
            }

            IsNight = !IsNight;
            Apply();
            Changed?.Invoke();
        }

        void Apply()
        {
            if (_sun == null)
            {
                return;
            }

            _sun.color = IsNight ? _nightColor : _dayColor;
            _sun.intensity = IsNight ? _nightIntensity : _dayIntensity;
        }
    }
}
