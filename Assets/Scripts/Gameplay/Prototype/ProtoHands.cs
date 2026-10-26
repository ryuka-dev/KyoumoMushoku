using System.Collections;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の両手（試作）。操作は左クリック一つ（作者 2026-10-26：右で掴むのは直感に反する）。
    ///
    /// 左クリックの解釈：
    /// 1. 照準がまだ開いていないゴミ箱に当たっていれば、右手で叩く。
    /// 2. そうでなく、照準の近くに宙の物があれば、左手を伸ばして掴む（伸びる腕）。
    /// 3. どちらでもなければ、右手で空を打つ（開いた箱なら「もう何も出ない」）。
    ///
    /// 鞄は本編と同じ Core の <see cref="Inventory"/>（容量制）。入らない物は掴めない。
    /// 手の模型は <c>Tools/Models/gen/hands.py</c>。左手は右手の鏡像。
    /// </summary>
    public sealed class ProtoHands : MonoBehaviour
    {
        enum GrabPhase
        {
            None,
            Reach,
            Retract,
        }

        [SerializeField] Camera _camera;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] GameObject _handModel;
        [SerializeField] Material _armMaterial;
        [SerializeField, Min(1)] int _bagCapacity = Inventory.DefaultCapacity;

        [Header("手の構え（カメラ基準・原点は肘）")]
        [SerializeField] Vector3 _rightRestPosition = new Vector3(0.17f, -0.30f, 0.02f);
        [SerializeField] Vector3 _rightRestEuler = new Vector3(-6f, -7f, 0f);
        [SerializeField] Vector3 _leftRestPosition = new Vector3(-0.19f, -0.34f, -0.02f);
        [SerializeField] Vector3 _leftRestEuler = new Vector3(-2f, 9f, 0f);

        [Header("叩く")]
        [SerializeField, Min(0.5f)] float _punchReach = 2.4f;
        [SerializeField, Min(0f)] float _punchDistance = 0.32f;
        [SerializeField, Min(0f)] float _hitStopMax = 0.09f;
        [SerializeField, Min(0f)] float _shakeMax = 0.06f;

        [Header("掴む")]
        [SerializeField, Min(1f)] float _grabReach = 14f;
        [SerializeField, Range(1f, 30f)] float _grabConeDegrees = 10f;
        [SerializeField, Min(0.02f)] float _reachSeconds = 0.12f;
        [SerializeField, Min(0.02f)] float _retractSeconds = 0.2f;

        InputAction _use;
        InputAction _refill;
        Transform _right;
        Transform _left;
        ProtoViewHand _rightHand;
        ProtoViewHand _leftHand;
        LineRenderer _arm;
        AudioSource _audio;
        Vector3 _cameraRest;
        float _punchT = 1f;
        float _shake;
        float _hitStopUntil;
        bool _hitStopRunning;

        ProtoLoot _grabbing;
        GrabPhase _phase;
        float _grabT;
        Vector3 _reachFrom;
        Vector3 _reachTo;

        public Inventory Bag { get; private set; }
        public string Toast { get; private set; }
        public float ToastAge { get; private set; }

        public void Configure(Camera camera, ItemDatabaseAsset catalog, GameObject handModel, Material armMaterial)
        {
            _camera = camera;
            _catalog = catalog;
            _handModel = handModel;
            _armMaterial = armMaterial;
        }

        void Awake()
        {
            Bag = new Inventory(_catalog, _bagCapacity);
            _use = new InputAction("Use", InputActionType.Button, "<Mouse>/leftButton");
            _refill = new InputAction("Refill", InputActionType.Button, "<Keyboard>/r");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _cameraRest = _camera.transform.localPosition;
            BuildHands();
        }

        void OnEnable()
        {
            _use.Enable();
            _refill.Enable();
        }

        void OnDisable()
        {
            _use.Disable();
            _refill.Disable();
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            _use?.Dispose();
            _refill?.Dispose();
        }

        void Update()
        {
            ToastAge += Time.unscaledDeltaTime;

            if (_use.WasPressedThisFrame())
            {
                Use();
            }

            if (_refill.WasPressedThisFrame())
            {
                foreach (var can in Object.FindObjectsByType<ProtoTrashCan>(FindObjectsSortMode.None))
                {
                    can.Refill();
                }

                Say(ProtoText.Refilled);
            }
        }

        void LateUpdate()
        {
            // 視点（ProtoFirstPersonController.Update）が動いた後に手を置く。左手は世界座標なので遅れると揺れる。
            AnimateRight();
            AnimateLeft();
            AnimateShake();
        }

        void Use()
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            ProtoTrashCan can = null;
            if (Physics.Raycast(ray, out var hit, _punchReach, ~0, QueryTriggerInteraction.Ignore))
            {
                can = hit.collider.GetComponentInParent<ProtoTrashCan>();
            }

            if (can != null && !can.IsOpened)
            {
                Punch(can);
                return;
            }

            if (_phase == GrabPhase.None)
            {
                var target = FindGrabTarget();
                if (target != null)
                {
                    Grab(target);
                    return;
                }
            }

            Punch(can);
        }

        void Punch(ProtoTrashCan can)
        {
            _punchT = 0f;
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

        void Grab(ProtoLoot target)
        {
            if (!Bag.CanAdd(target.Item))
            {
                Say(ProtoText.NoRoom(target.DisplayName));
                _audio.pitch = 0.6f;
                _audio.PlayOneShot(ProtoSounds.Clang, 0.4f);
                return;
            }

            _grabbing = target;
            _phase = GrabPhase.Reach;
            _grabT = 0f;
            _reachFrom = _left.position;
            _leftHand.SetCurl(0f);
        }

        void OnArrived(ProtoLoot loot)
        {
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

        void AnimateRight()
        {
            _punchT = Mathf.Min(1f, _punchT + Time.unscaledDeltaTime / 0.16f);
            // 前へ速く突いて、ゆっくり戻る。
            var jab = _punchT < 0.3f ? _punchT / 0.3f : 1f - (_punchT - 0.3f) / 0.7f;
            jab = Mathf.Clamp01(jab);
            var rest = Quaternion.Euler(_rightRestEuler);
            _right.localRotation = rest * Quaternion.Euler(-4f * jab, -6f * jab, 0f);
            _right.localPosition = _rightRestPosition + new Vector3(-0.06f, 0.07f, _punchDistance) * jab;
        }

        void AnimateLeft()
        {
            var anchor = _camera.transform.TransformPoint(_leftRestPosition);
            var restRotation = _camera.transform.rotation * Quaternion.Euler(_leftRestEuler);

            switch (_phase)
            {
                case GrabPhase.Reach:
                {
                    if (_grabbing == null)
                    {
                        _phase = GrabPhase.Retract;
                        _grabT = 0f;
                        break;
                    }

                    _grabT += Time.unscaledDeltaTime / _reachSeconds;
                    var eased = 1f - (1f - Mathf.Clamp01(_grabT)) * (1f - Mathf.Clamp01(_grabT));
                    var target = _grabbing.transform.position;
                    var toTarget = target - anchor;
                    var direction = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : _camera.transform.forward;
                    // 肘の位置を動かして、掌が目標に届くようにする（掌は肘から 0.33m 前）。
                    _reachTo = target - direction * 0.33f;
                    _left.position = Vector3.Lerp(_reachFrom, _reachTo, eased);
                    _left.rotation = Quaternion.LookRotation(direction, _camera.transform.up);
                    if (_grabT >= 1f)
                    {
                        _leftHand.SetCurl(1f);
                        _grabbing.PullTo(_leftHand.Palm, OnArrived);
                        _grabbing = null;
                        _phase = GrabPhase.Retract;
                        _grabT = 0f;
                        _reachFrom = _left.position;
                    }

                    break;
                }

                case GrabPhase.Retract:
                {
                    _grabT += Time.unscaledDeltaTime / _retractSeconds;
                    var eased = Mathf.Clamp01(_grabT);
                    eased *= eased;
                    _left.position = Vector3.Lerp(_reachFrom, anchor, eased);
                    _left.rotation = Quaternion.Slerp(_left.rotation, restRotation, eased);
                    if (_grabT >= 1f)
                    {
                        _phase = GrabPhase.None;
                        _leftHand.SetCurl(0.35f);
                    }

                    break;
                }

                default:
                    _left.position = anchor;
                    _left.rotation = restRotation;
                    break;
            }

            // 伸びる腕：構えの位置から、いまの肘まで袖色の紐を張る。
            var stretched = Vector3.Distance(anchor, _left.position) > 0.05f;
            _arm.enabled = stretched;
            if (stretched)
            {
                _arm.SetPosition(0, anchor);
                _arm.SetPosition(1, _left.position);
            }
        }

        void AnimateShake()
        {
            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 5f);
            _camera.transform.localPosition = _cameraRest + Random.insideUnitSphere * (_shakeMax * _shake * _shake);
        }

        void BuildHands()
        {
            // 模型の親は軸変換の回転（FBX 由来）を持つので、それを上書きしないよう空の容器の下に置く。
            var right = new GameObject("RightHand");
            right.transform.SetParent(_camera.transform, false);
            Instantiate(_handModel, right.transform, false);
            _right = right.transform;
            _right.localPosition = _rightRestPosition;
            _right.localRotation = Quaternion.Euler(_rightRestEuler);
            _rightHand = right.AddComponent<ProtoViewHand>();
            _rightHand.SnapCurl(1f);

            // 左手は世界座標で動かす（伸びる腕が照準の先まで届くため）。鏡像は負の拡大で作る。
            var left = new GameObject("LeftHand");
            Instantiate(_handModel, left.transform, false);
            _left = left.transform;
            _left.localScale = new Vector3(-1f, 1f, 1f);
            _leftHand = left.AddComponent<ProtoViewHand>();
            _leftHand.SnapCurl(0.35f);

            var armGo = new GameObject("RubberArm");
            armGo.transform.SetParent(transform, false);
            _arm = armGo.AddComponent<LineRenderer>();
            _arm.positionCount = 2;
            _arm.startWidth = 0.10f;
            _arm.endWidth = 0.08f;
            _arm.numCapVertices = 2;
            _arm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _arm.sharedMaterial = _armMaterial;
            _arm.enabled = false;
        }
    }
}
