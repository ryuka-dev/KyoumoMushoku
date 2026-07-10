using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Player
{
    /// <summary>
    /// Input System のアダプタ。InputSystem 型はこのクラスの外へ漏らさない。
    /// </summary>
    public sealed class InputSystemPlayerInput : MonoBehaviour, IPlayerInput
    {
        InputAction _move;
        InputAction _run;

        public float Horizontal => _move?.ReadValue<float>() ?? 0f;

        public bool RunHeld => _run?.IsPressed() ?? false;

        void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");

            _run = new InputAction("Run", InputActionType.Button);
            _run.AddBinding("<Keyboard>/leftShift");
            _run.AddBinding("<Keyboard>/rightShift");
        }

        void OnEnable()
        {
            _move.Enable();
            _run.Enable();
        }

        void OnDisable()
        {
            _move.Disable();
            _run.Disable();
        }

        void OnDestroy()
        {
            _move?.Dispose();
            _run?.Dispose();
        }
    }
}
