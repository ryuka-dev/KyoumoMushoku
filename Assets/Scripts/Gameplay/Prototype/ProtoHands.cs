using System.Collections;
using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Gameplay.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の手（試作）。右手だけを見せる（作者 2026-10-26：両手は要らない）。操作は左クリック一つ。
    ///
    /// 左クリックの解釈：
    /// 1. 照準がまだ開いていないゴミ箱に当たっていれば、拳で叩く。
    /// 2. そうでなく、照準が使える物（<see cref="IProtoUsable"/>：店の台・水道・寝床）に当たっていれば、それを使う。
    /// 3. そうでなく、照準の近くに宙の物があれば、手を開いて伸ばし、握って引き戻す（伸びる腕）。
    /// 4. どれでもなければ、空を打つ（開いた箱なら「もう何も出ない」）。
    ///
    /// 手は指を回さない。握り（拳）と開きの 2 つの模型を差し替える（前の版の「指の向きが逆」を繰り返さない）。
    /// 模型は第三者素材 FPS_Psx_Hands（CC0）から <c>Tools/Models/gen/hand_psx.py</c> が作る。原点は肘、前は +Z。
    /// 鞄は本編と同じ Core の <see cref="Inventory"/>（容量制）。入らない物は掴めない。
    /// 試作ではプレイヤーの持ち物（鞄と所持金）をここが持つ。
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
        [SerializeField] GameObject _fistModel;
        [SerializeField] GameObject _openModel;
        [SerializeField] Material _armMaterial;
        [SerializeField, Min(1)] int _bagCapacity = Inventory.DefaultCapacity;
        [Tooltip("始めの所持金（円）。本編の PlayerWallet の既定と同じ。")]
        [SerializeField, Min(0)] int _startingYen = 500;

        [Header("手の構え（カメラ基準・原点は肘）")]
        [SerializeField] Vector3 _restPosition = new Vector3(0.20f, -0.26f, -0.10f);
        [SerializeField] Vector3 _restEuler = new Vector3(-8f, -8f, -80f);
        [Tooltip("肘から掌の中心まで（模型の寸法）。掴んだ物はここへ寄せる。")]
        [SerializeField, Min(0.05f)] float _palmDistance = 0.38f;

        [Header("叩く")]
        [SerializeField, Min(0.5f)] float _punchReach = 2.4f;
        [SerializeField, Min(0f)] float _punchDistance = 0.30f;
        [SerializeField, Min(0f)] float _hitStopMax = 0.09f;
        [SerializeField, Min(0f)] float _shakeMax = 0.06f;
        [Tooltip("これより短い間隔で叩き続けると、音は最も大きい（連打）。")]
        [SerializeField, Min(0.01f)] float _mashInterval = 0.25f;
        [Tooltip("これより間を空けて叩けば、音は最も小さい。")]
        [SerializeField, Min(0.02f)] float _calmInterval = 0.9f;
        [Tooltip("間を空けて叩いたときの音の大きさ（連打を 1 として）。")]
        [SerializeField, Range(0f, 1f)] float _calmLoudness = 0.45f;

        [Header("案内")]
        [Tooltip("この距離までの叩ける物・使える物は、届かなくても照準が薄く変わる（何かがあると分かる）。")]
        [SerializeField, Min(0.5f)] float _hintReach = 6f;

        [Header("掴む")]
        [SerializeField, Min(1f)] float _grabReach = 14f;
        [SerializeField, Range(1f, 30f)] float _grabConeDegrees = 10f;
        [SerializeField, Min(0.02f)] float _reachSeconds = 0.12f;
        [SerializeField, Min(0.02f)] float _retractSeconds = 0.2f;

        InputAction _use;
        Transform _hand;
        GameObject _fist;
        GameObject _open;
        Transform _palm;
        LineRenderer _arm;
        AudioSource _audio;
        Vector3 _cameraRest;
        float _punchT = 1f;
        float _shake;
        float _hitStopUntil;
        float _lastPunchAt = -10f;
        bool _hitStopRunning;

        ProtoLoot _grabbing;
        GrabPhase _phase;
        float _grabT;
        Vector3 _reachFrom;
        Quaternion _reachFromRotation;

        public Inventory Bag { get; private set; }
        public ProtoVitals Vitals { get; private set; }

        /// <summary>宙の物を掴んで鞄に入れた。拾った物に反応する世界の側（独り言など）が聞く。</summary>
        public event System.Action<ItemInstance> Grabbed;
        public Wallet Wallet { get; private set; }

        public enum AimState
        {
            None,
            /// <summary>叩ける物・使える物を見ているが、まだ手が届かない。</summary>
            Distant,
            /// <summary>左クリックで何かが起きる。</summary>
            InReach,
        }

        /// <summary>照準の先。HUD の照準の形を決める。</summary>
        public AimState Aim { get; private set; }

        /// <summary>左クリックで起きることの案内（押す操作は含まない）。<see cref="AimState.InReach"/> のときだけ。無ければ null。</summary>
        public string AimPrompt { get; private set; }

        /// <summary>手の届くところで照準が合っているゴミ箱（袋を含む）。無ければ null。</summary>
        public ProtoTrashCan AimedTrashCan { get; private set; }

        /// <summary>左クリックで使われる物（開いていないゴミ箱か使える物）。無ければ null。縁取りが「狙われている」を知るのに使う。</summary>
        public Component AimedObject { get; private set; }

        /// <summary>前に <see cref="TakeCaughtCount"/> してから掴んで鞄に入れた数。</summary>
        int _caught;
        public string Toast { get; private set; }
        public float ToastAge { get; private set; }

        public void Configure(Camera camera, ItemDatabaseAsset catalog, GameObject fistModel, GameObject openModel,
            Material armMaterial)
        {
            _camera = camera;
            _catalog = catalog;
            _fistModel = fistModel;
            _openModel = openModel;
            _armMaterial = armMaterial;
        }

        void Awake()
        {
            Bag = new Inventory(_catalog, _bagCapacity);
            Wallet = new Wallet(_startingYen);
            Vitals = GetComponent<ProtoVitals>();
            _use = new InputAction("Use", InputActionType.Button, "<Mouse>/leftButton");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _cameraRest = _camera.transform.localPosition;
            BuildHand();
        }

        void OnEnable()
        {
            _use.Enable();
        }

        void OnDisable()
        {
            _use.Disable();
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            _use?.Dispose();
        }

        void Update()
        {
            ToastAge += Time.unscaledDeltaTime;
            UpdateAim();

            if (_use.WasPressedThisFrame())
            {
                Use();
            }
        }

        void LateUpdate()
        {
            // 視点（ProtoFirstPersonController.Update）が動いた後に手を置く。
            AnimateHand();
            AnimateShake();
        }

        void Use()
        {
            if (_phase != GrabPhase.None)
            {
                return;
            }

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

            var usable = hit.collider != null ? UsableAt(hit.collider) : null;
            if (usable != null)
            {
                _punchT = 0f;
                Say(usable.Use(this));
                _audio.pitch = 1.2f;
                _audio.PlayOneShot(ProtoSounds.Grab, 0.8f);
                return;
            }

            var target = FindGrabTarget();
            if (target != null)
            {
                Grab(target);
                return;
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

            // 連打するほどうるさい。叩く回数は同じなので、連打すれば早く開くが、警官に聞こえやすい。
            var interval = Time.unscaledTime - _lastPunchAt;
            _lastPunchAt = Time.unscaledTime;
            var mash = 1f - Mathf.Clamp01((interval - _mashInterval) / Mathf.Max(0.01f, _calmInterval - _mashInterval));
            var strength = can.Hit(_camera.transform.forward, Mathf.Lerp(_calmLoudness, 1f, mash), Vitals != null ? Vitals.Vitals : null, out var toast);
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
            _reachFrom = _hand.position;
            _reachFromRotation = _hand.rotation;
            SetOpen(true);
        }

        void OnArrived(ProtoLoot loot)
        {
            if (Bag.TryAdd(loot.Item))
            {
                _caught++;
                Say(ProtoText.Caught(loot.DisplayName));
                Grabbed?.Invoke(loot.Item);
                _audio.pitch = loot.Tier >= KyoumoMushoku.Core.Foraging.LootTier.Rare ? 1.4f : 1f;
                _audio.PlayOneShot(ProtoSounds.Grab, 1f);
            }
            else
            {
                Say(ProtoText.NoRoom(loot.DisplayName));
            }
        }

        /// <summary>
        /// 照準の案内を決める。順番は <see cref="Use"/> の解釈と同じ（案内と、押して起きることを食い違わせない）：
        /// 開いていないゴミ箱 → 使える物 → 宙の物。開いたゴミ箱は案内しない（叩いても何も起きない）。
        /// 届かない距離の物は照準の形だけで知らせる。
        /// </summary>
        void UpdateAim()
        {
            Aim = AimState.None;
            AimPrompt = null;
            AimedTrashCan = null;
            AimedObject = null;

            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            ProtoTrashCan can = null;
            IProtoUsable usable = null;
            var inReach = false;
            if (Physics.Raycast(ray, out var hit, _hintReach, ~0, QueryTriggerInteraction.Ignore))
            {
                can = hit.collider.GetComponentInParent<ProtoTrashCan>();
                usable = UsableAt(hit.collider);
                inReach = hit.distance <= _punchReach;
            }

            if (inReach && can != null)
            {
                AimedTrashCan = can;
            }

            if (inReach && can != null && !can.IsOpened)
            {
                SetAim(can.Prompt);
                AimedObject = can;
                return;
            }

            if (inReach && usable != null)
            {
                SetAim(usable.Prompt(this));
                AimedObject = usable as Component;
                return;
            }

            var loot = _phase == GrabPhase.None ? FindGrabTarget() : null;
            if (loot != null)
            {
                SetAim(ProtoText.GrabPrompt(loot.DisplayName));
                return;
            }

            // 開いた箱はもう触れる物ではないので、近くても遠くても照準は元の「+」に戻す。
            if ((can != null && !can.IsOpened) || usable != null)
            {
                Aim = AimState.Distant;
            }
        }

        /// <summary>照準の先の使う物。閉まっている店は触れる物ではないので、無いのと同じに扱う（第十七節 規則 1）。</summary>
        static IProtoUsable UsableAt(Collider collider)
        {
            var usable = collider.GetComponentInParent<IProtoUsable>();
            return usable is IProtoClosable closable && !closable.IsOpenNow ? null : usable;
        }

        void SetAim(string prompt)
        {
            Aim = AimState.InReach;
            AimPrompt = prompt;
        }

        /// <summary>掴んだ数を受け取り、数え直す。<see cref="ProtoSession"/> が一日の区切りで呼ぶ。</summary>
        public int TakeCaughtCount()
        {
            var count = _caught;
            _caught = 0;
            return count;
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

        /// <summary>一言を出す既定の秒数。</summary>
        public const float DefaultToastSeconds = 2.2f;

        /// <summary>いまの一言を出しておく秒数。</summary>
        public float ToastSeconds { get; private set; } = DefaultToastSeconds;

        /// <summary>画面の一言。手の動作の結果のほか、飲食などほかの部品も使う。読ませたい独り言は長めに出す。</summary>
        public void Say(string text, float seconds = DefaultToastSeconds)
        {
            Toast = text;
            ToastAge = 0f;
            ToastSeconds = seconds;
        }

        void SetOpen(bool open)
        {
            _open.SetActive(open);
            _fist.SetActive(!open);
        }

        void AnimateHand()
        {
            var camera = _camera.transform;

            // 構え＋拳の突き（前へ速く、ゆっくり戻る）。
            _punchT = Mathf.Min(1f, _punchT + Time.unscaledDeltaTime / 0.16f);
            var jab = _punchT < 0.3f ? _punchT / 0.3f : 1f - (_punchT - 0.3f) / 0.7f;
            jab = Mathf.Clamp01(jab);
            var restPosition = camera.TransformPoint(_restPosition + new Vector3(-0.06f, 0.07f, _punchDistance) * jab);
            var restRotation = camera.rotation * Quaternion.Euler(_restEuler) * Quaternion.Euler(-4f * jab, -6f * jab, 0f);

            switch (_phase)
            {
                case GrabPhase.Reach:
                {
                    if (_grabbing == null)
                    {
                        BeginRetract();
                        break;
                    }

                    _grabT += Time.unscaledDeltaTime / _reachSeconds;
                    var t = Mathf.Clamp01(_grabT);
                    var eased = 1f - (1f - t) * (1f - t);
                    var target = _grabbing.transform.position;
                    var toTarget = target - restPosition;
                    var direction = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized : camera.forward;
                    // 肘を動かして、掌が目標に届くようにする。
                    var elbow = target - direction * _palmDistance;
                    _hand.position = Vector3.Lerp(_reachFrom, elbow, eased);
                    _hand.rotation = Quaternion.Slerp(_reachFromRotation,
                        Quaternion.LookRotation(direction, camera.up) * Quaternion.Euler(0f, 0f, _restEuler.z), eased);
                    if (_grabT >= 1f)
                    {
                        SetOpen(false);
                        _grabbing.PullTo(_palm, OnArrived);
                        _grabbing = null;
                        BeginRetract();
                    }

                    break;
                }

                case GrabPhase.Retract:
                {
                    _grabT += Time.unscaledDeltaTime / _retractSeconds;
                    var t = Mathf.Clamp01(_grabT);
                    var eased = t * t;
                    _hand.position = Vector3.Lerp(_reachFrom, restPosition, eased);
                    _hand.rotation = Quaternion.Slerp(_reachFromRotation, restRotation, eased);
                    if (_grabT >= 1f)
                    {
                        _phase = GrabPhase.None;
                    }

                    break;
                }

                default:
                    _hand.position = restPosition;
                    _hand.rotation = restRotation;
                    break;
            }

            // 伸びる腕：構えの位置から、いまの肘まで袖色の紐を張る。
            var stretched = Vector3.Distance(restPosition, _hand.position) > 0.08f;
            _arm.enabled = stretched;
            if (stretched)
            {
                _arm.SetPosition(0, restPosition);
                _arm.SetPosition(1, _hand.position);
            }
        }

        void BeginRetract()
        {
            _phase = GrabPhase.Retract;
            _grabT = 0f;
            _reachFrom = _hand.position;
            _reachFromRotation = _hand.rotation;
        }

        void AnimateShake()
        {
            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 5f);
            _camera.transform.localPosition = _cameraRest + Random.insideUnitSphere * (_shakeMax * _shake * _shake);
        }

        void BuildHand()
        {
            // 模型の根は FBX の軸変換の回転を持つので、空の容器の下に置き、容器を動かす。
            var hand = new GameObject("RightHand");
            _hand = hand.transform;
            _fist = Instantiate(_fistModel, _hand, false);
            _open = Instantiate(_openModel, _hand, false);
            foreach (var r in hand.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            _palm = new GameObject("Palm").transform;
            _palm.SetParent(_hand, false);
            _palm.localPosition = new Vector3(0f, 0f, _palmDistance);
            SetOpen(false);

            var armGo = new GameObject("RubberArm");
            armGo.transform.SetParent(transform, false);
            _arm = armGo.AddComponent<LineRenderer>();
            _arm.positionCount = 2;
            _arm.startWidth = 0.07f;
            _arm.endWidth = 0.06f;
            _arm.numCapVertices = 2;
            _arm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _arm.sharedMaterial = _armMaterial;
            _arm.enabled = false;
        }
    }
}
