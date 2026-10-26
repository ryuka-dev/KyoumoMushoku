using System.Collections;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 叩く手と掴む手（試作）。左で叩き、右で宙の物を掴んで鞄へ入れる。
    /// 鞄は本編と同じ Core の <see cref="Inventory"/>（容量制）。入らない物は掴めない。
    /// </summary>
    public sealed class ProtoHands : MonoBehaviour
    {
        [SerializeField] Camera _camera;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField, Min(1)] int _bagCapacity = Inventory.DefaultCapacity;

        [Header("叩く")]
        [SerializeField, Min(0.5f)] float _punchReach = 2.4f;
        [SerializeField, Min(0f)] float _hitStopMax = 0.09f;
        [SerializeField, Min(0f)] float _shakeMax = 0.06f;

        [Header("掴む")]
        [SerializeField, Min(1f)] float _grabReach = 14f;
        [SerializeField, Range(1f, 30f)] float _grabConeDegrees = 10f;

        InputAction _punch;
        InputAction _grab;
        InputAction _refill;
        Transform _fist;
        Vector3 _fistRest;
        LineRenderer _arm;
        AudioSource _audio;
        Vector3 _cameraRest;
        float _punchT = 1f;
        float _shake;
        float _hitStopUntil;
        bool _hitStopRunning;
        ProtoLoot _grabbing;

        public Inventory Bag { get; private set; }
        public string Toast { get; private set; }
        public float ToastAge { get; private set; }

        public void Configure(Camera camera, ItemDatabaseAsset catalog)
        {
            _camera = camera;
            _catalog = catalog;
        }

        void Awake()
        {
            Bag = new Inventory(_catalog, _bagCapacity);
            _punch = new InputAction("Punch", InputActionType.Button, "<Mouse>/leftButton");
            _grab = new InputAction("Grab", InputActionType.Button, "<Mouse>/rightButton");
            _refill = new InputAction("Refill", InputActionType.Button, "<Keyboard>/r");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            BuildFistAndArm();
        }

        void OnEnable()
        {
            _punch.Enable();
            _grab.Enable();
            _refill.Enable();
        }

        void OnDisable()
        {
            _punch.Disable();
            _grab.Disable();
            _refill.Disable();
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            _punch?.Dispose();
            _grab?.Dispose();
            _refill?.Dispose();
        }

        void Update()
        {
            ToastAge += Time.unscaledDeltaTime;

            if (_punch.WasPressedThisFrame())
            {
                Punch();
            }

            if (_grab.WasPressedThisFrame() && _grabbing == null)
            {
                Grab();
            }

            if (_refill.WasPressedThisFrame())
            {
                foreach (var can in Object.FindObjectsByType<ProtoTrashCan>(FindObjectsSortMode.None))
                {
                    can.Refill();
                }

                Say(ProtoText.Refilled);
            }

            AnimateFist();
            AnimateArm();
            AnimateShake();
        }

        void Punch()
        {
            _punchT = 0f;
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _punchReach, ~0, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            var can = hit.collider.GetComponentInParent<ProtoTrashCan>();
            if (can == null)
            {
                return;
            }

            var strength = can.Hit(_camera.transform.forward, out var toast);
            if (toast != null)
            {
                Say(toast);
            }

            if (strength > 0f)
            {
                _shake = Mathf.Max(_shake, strength);
                HitStop(_hitStopMax * strength);
            }
        }

        void Grab()
        {
            var target = FindGrabTarget();
            _punchT = 0f;
            if (target == null)
            {
                return;
            }

            if (!Bag.CanAdd(target.Item))
            {
                Say(ProtoText.NoRoom(target.DisplayName));
                _audio.pitch = 0.6f;
                _audio.PlayOneShot(ProtoSounds.Clang, 0.4f);
                return;
            }

            _grabbing = target;
            target.PullTo(_fist, OnArrived);
        }

        void OnArrived(ProtoLoot loot)
        {
            _grabbing = null;
            if (Bag.TryAdd(loot.Item))
            {
                Say(ProtoText.Caught(loot.DisplayName));
                _audio.pitch = loot.Rarity == ProtoRarity.Rare ? 1.4f : 1f;
                _audio.PlayOneShot(ProtoSounds.Grab, 1f);
            }
            else
            {
                Say(ProtoText.NoRoom(loot.DisplayName));
            }
        }

        ProtoLoot FindGrabTarget()
        {
            ProtoLoot best = null;
            var bestAngle = _grabConeDegrees;
            var origin = _camera.transform.position;
            foreach (var loot in ProtoLoot.Active)
            {
                if (loot.IsBeingPulled)
                {
                    continue;
                }

                var toLoot = loot.transform.position - origin;
                if (toLoot.magnitude > _grabReach)
                {
                    continue;
                }

                var angle = Vector3.Angle(_camera.transform.forward, toLoot);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = loot;
                }
            }

            return best;
        }

        void HitStop(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + seconds);
            if (!_hitStopRunning)
            {
                StartCoroutine(HitStopRoutine());
            }
        }

        IEnumerator HitStopRoutine()
        {
            _hitStopRunning = true;
            Time.timeScale = 0f;
            while (Time.unscaledTime < _hitStopUntil)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            _hitStopRunning = false;
        }

        void Say(string text)
        {
            Toast = text;
            ToastAge = 0f;
        }

        void AnimateFist()
        {
            _punchT = Mathf.Min(1f, _punchT + Time.unscaledDeltaTime / 0.14f);
            // 前へ速く突いて、ゆっくり戻る。
            var jab = _punchT < 0.35f ? _punchT / 0.35f : 1f - (_punchT - 0.35f) / 0.65f;
            _fist.localPosition = _fistRest + new Vector3(-0.12f, 0.05f, 0.55f) * Mathf.Clamp01(jab);
        }

        void AnimateArm()
        {
            var reaching = _grabbing != null;
            _arm.enabled = reaching;
            if (reaching)
            {
                _arm.SetPosition(0, _fist.position);
                _arm.SetPosition(1, _grabbing.transform.position);
            }
        }

        void AnimateShake()
        {
            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 5f);
            _camera.transform.localPosition = _cameraRest + Random.insideUnitSphere * (_shakeMax * _shake * _shake);
        }

        void BuildFistAndArm()
        {
            _cameraRest = _camera != null ? _camera.transform.localPosition : Vector3.zero;

            var fist = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fist.name = "Fist";
            Destroy(fist.GetComponent<Collider>());
            fist.transform.SetParent(_camera.transform, false);
            fist.transform.localScale = new Vector3(0.08f, 0.08f, 0.11f);
            _fistRest = new Vector3(0.22f, -0.24f, 0.40f);
            fist.transform.localPosition = _fistRest;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.85f, 0.7f, 0.55f));
            fist.GetComponent<Renderer>().sharedMaterial = material;
            _fist = fist.transform;

            var armGo = new GameObject("RubberArm");
            armGo.transform.SetParent(transform, false);
            _arm = armGo.AddComponent<LineRenderer>();
            _arm.positionCount = 2;
            _arm.startWidth = 0.07f;
            _arm.endWidth = 0.05f;
            _arm.sharedMaterial = material;
            _arm.enabled = false;
        }
    }
}
