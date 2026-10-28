using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の移動と視点（試作）。本編の <c>PlayerMotor</c> とは混ぜない。
    /// Input System の型はこのクラスとこの名前空間の外へ出さない。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ProtoFirstPersonController : MonoBehaviour
    {
        [SerializeField] Transform _head;
        [SerializeField, Min(0.1f)] float _walkSpeed = 3.2f;
        [SerializeField, Min(0.1f)] float _runSpeed = 5.5f;
        [SerializeField, Min(0.01f)] float _lookSensitivity = 0.12f;
        [SerializeField] float _gravity = -18f;

        CharacterController _body;
        InputAction _move;
        InputAction _look;
        InputAction _run;
        InputAction _toggleCursor;
        float _pitch;
        float _verticalVelocity;
        ProtoVitals _vitals;
        ProtoHands _hands;

        /// <summary>走っているか（Shift を押して動いている）。渇き・空腹の消耗が増える。</summary>
        public bool IsRunning { get; private set; }

        public void Configure(Transform head) => _head = head;

        /// <summary>見上げ・見下ろしの角度（下向きが正）。</summary>
        public float Pitch => _pitch;

        /// <summary>
        /// 視点を外から向ける（警官に掴まれて顔を向けさせられる・連れて行かれた先で向き直る）。
        /// 左右は体ごと、上下は頭だけを回す。次にマウスを動かしたとき、ここから続く。
        /// </summary>
        public void SetView(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _pitch = Mathf.Clamp(pitch, -80f, 80f);
            if (_head != null)
            {
                _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        void Awake()
        {
            _body = GetComponent<CharacterController>();
            _vitals = GetComponent<ProtoVitals>();
            _hands = GetComponent<ProtoHands>();

            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            _run = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            _toggleCursor = new InputAction("ToggleCursor", InputActionType.Button, "<Keyboard>/escape");
        }

        void OnEnable()
        {
            _move.Enable();
            _look.Enable();
            _run.Enable();
            _toggleCursor.Enable();
            LockCursor(true);
        }

        void OnDisable()
        {
            _move.Disable();
            _look.Disable();
            _run.Disable();
            _toggleCursor.Disable();
            LockCursor(false);
        }

        void OnDestroy()
        {
            _move?.Dispose();
            _look?.Dispose();
            _run?.Dispose();
            _toggleCursor?.Dispose();
        }

        void Update()
        {
            if (_toggleCursor.WasPressedThisFrame())
            {
                LockCursor(Cursor.lockState != CursorLockMode.Locked);
            }

            // ヒットストップ中（timeScale 0）も視点だけは動かせると手応えが途切れない。
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var look = _look.ReadValue<Vector2>() * _lookSensitivity;
                transform.Rotate(0f, look.x, 0f, Space.Self);
                _pitch = Mathf.Clamp(_pitch - look.y, -80f, 80f);
                if (_head != null)
                {
                    _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                }
            }

            var input = _move.ReadValue<Vector2>();
            // 段ボールを背負っていると走れず、歩きも遅い（GameDesign 第十一節）。
            var canRun = _hands == null || _hands.CanRun;
            IsRunning = canRun && _run.IsPressed() && input.sqrMagnitude > 0.01f;
            var speed = IsRunning ? _runSpeed : _walkSpeed;
            if (_hands != null)
            {
                speed *= _hands.MoveSpeedMultiplier;
            }

            // 渇きがゼロのあいだは足が重い（本編と同じ規則）。
            if (_vitals != null && _vitals.Vitals != null)
            {
                speed *= _vitals.Vitals.SpeedMultiplier;
            }

            var planar = (transform.right * input.x + transform.forward * input.y) * speed;

            _verticalVelocity = _body.isGrounded ? -1f : _verticalVelocity + _gravity * Time.deltaTime;
            _body.Move((planar + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
