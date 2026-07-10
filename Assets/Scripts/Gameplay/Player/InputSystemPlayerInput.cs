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
        InputAction _interact;

        public float Horizontal => _move?.ReadValue<float>() ?? 0f;

        public bool RunHeld => _run?.IsPressed() ?? false;

        public bool InteractPressed => _interact?.WasPressedThisFrame() ?? false;

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

            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Keyboard>/space");
        }

        void OnEnable()
        {
            _move.Enable();
            _run.Enable();
            _interact.Enable();
        }

        void OnDisable()
        {
            _move.Disable();
            _run.Disable();
            _interact.Disable();
        }

        void OnDestroy()
        {
            _move?.Dispose();
            _run?.Dispose();
            _interact?.Dispose();
        }
    }
}
