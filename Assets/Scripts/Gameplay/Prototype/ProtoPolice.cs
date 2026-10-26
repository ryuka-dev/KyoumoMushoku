using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// 一人称の切片の警官。巡回し、目と耳でプレイヤーに気づき、段階的に追い出す（第五節）。
    /// 段階と注目度の規則は本編と同じ Core の <see cref="PoliceEscalation"/>、捕まったときの没収は <see cref="Confiscation"/>、
    /// 台詞は本編の <see cref="PoliceText"/>。捕まると HP が減り、腐った物を取り上げられ、公園の端へ連れて行かれる（本編と同じ）。
    ///
    /// 目：視界の円錐の中で、建物に遮られずに見えているときだけ。路地や建物の陰に入れば見えない。
    /// 耳：ゴミ箱を叩く音（<see cref="ProtoNoise"/>）は、見えていなくても届く。うるさいほど遠くまで届き、注目度が上がる。
    /// 聞きつけた警官は音のした所へ見に来る。叩くたびに、手早く済ませるか、見つかる危険を取るかが問われる。
    ///
    /// 追跡の速さはプレイヤーの走りより遅い。視線を切って逃げ切れる（本編の地下通路の代わり）。
    /// 街区は一つなので、ゾーンの警戒度（ZoneAlertLevels）は入れていない。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ProtoPolice : MonoBehaviour
    {
        [SerializeField] ProtoHands _player;
        [SerializeField] ItemDatabaseAsset _catalog;
        [SerializeField] Transform _escortPoint;
        [SerializeField] Vector3[] _patrol = new Vector3[0];

        [Header("動き")]
        [SerializeField, Min(0f)] float _patrolSpeed = 1.6f;
        [SerializeField, Min(0f)] float _chaseSpeed = 4.8f;
        [SerializeField, Min(0f)] float _warnDistance = 2.5f;
        [SerializeField, Min(0f)] float _captureDistance = 1.3f;
        [SerializeField, Min(0f)] float _lookAroundSeconds = 4f;

        [Header("目と耳")]
        [SerializeField, Min(0f)] float _sightRadius = 10f;
        [SerializeField, Range(10f, 180f)] float _sightAngle = 110f;
        [Tooltip("最もうるさい音（1）が届く距離。小さい音ほど近くまでしか届かない。")]
        [SerializeField, Min(0f)] float _hearingRadius = 20f;
        [Tooltip("聞こえた一打で上がる注目度（大きさ 1 のとき）。")]
        [SerializeField, Min(0f)] float _heardGain = 15f;
        [Tooltip("叩くところを見られたときに、さらに上がる注目度。")]
        [SerializeField, Min(0f)] float _seenHitGain = 20f;

        [Header("捕まったとき")]
        [SerializeField, Min(0f)] float _captureHpLoss = 12f;

        const float EyeHeight = 1.6f;

        CharacterController _body;
        IRng _rng;
        float _suspicion;
        int _patrolIndex;
        bool _investigating;
        Vector3 _investigateAt;
        float _lookAroundLeft;
        Vector3 _lastSeen;

        public PoliceStage Stage { get; private set; }
        public float Suspicion => _suspicion;

        public void Configure(ProtoHands player, ItemDatabaseAsset catalog, Transform escortPoint, Vector3[] patrol)
        {
            _player = player;
            _catalog = catalog;
            _escortPoint = escortPoint;
            _patrol = patrol ?? new Vector3[0];
        }

        void Awake()
        {
            _body = GetComponent<CharacterController>();
            _rng = new SystemRng();
        }

        void OnEnable() => ProtoNoise.Made += OnNoise;

        void OnDisable() => ProtoNoise.Made -= OnNoise;

        void Update()
        {
            var vitals = _player != null ? _player.Vitals : null;
            if (vitals == null || vitals.Vitals == null)
            {
                return;
            }

            var delta = Time.deltaTime;
            if (delta <= 0f)
            {
                return;
            }

            var sees = vitals.Vitals.IsAlive && CanSeePlayer();
            if (sees)
            {
                _lastSeen = _player.transform.position;
            }

            _suspicion = sees
                ? PoliceEscalation.Advance(_suspicion, PoliceEscalation.LoiterGainPerSecond, 1f, delta)
                : PoliceEscalation.Relax(_suspicion, delta);

            var next = PoliceEscalation.NextStage(Stage, _suspicion);
            if (next != Stage)
            {
                Enter(next);
            }

            Act(sees, delta);
        }

        void OnNoise(Vector3 position, float loudness)
        {
            if (_player == null || !isActiveAndEnabled)
            {
                return;
            }

            var reach = _hearingRadius * Mathf.Lerp(0.4f, 1f, loudness);
            if ((position - transform.position).sqrMagnitude > reach * reach)
            {
                return;
            }

            var gain = _heardGain * loudness;
            if (CanSeePlayer())
            {
                gain += _seenHitGain;
            }

            _suspicion = PoliceEscalation.Clamp(_suspicion + gain);

            // まだ追い払いに来ていないなら、音のした所へ見に来る。
            if (Stage < PoliceStage.Warning)
            {
                _investigating = true;
                _investigateAt = position;
                _lookAroundLeft = _lookAroundSeconds;
            }
        }

        void Enter(PoliceStage stage)
        {
            var previous = Stage;
            Stage = stage;
            switch (stage)
            {
                case PoliceStage.Noticing when previous == PoliceStage.Unaware:
                    Say(PoliceText.Notice);
                    break;
                case PoliceStage.Warning when previous < PoliceStage.Warning:
                    Say(PoliceText.Warning);
                    break;
                case PoliceStage.Pursuing:
                    _investigating = false;
                    Say(PoliceText.Pursue);
                    break;
            }
        }

        void Act(bool sees, float delta)
        {
            var player = _player.transform.position;
            switch (Stage)
            {
                case PoliceStage.Pursuing:
                    MoveTowards(_lastSeen, _chaseSpeed, sees ? _captureDistance * 0.8f : 0.3f);
                    if (sees && Flat(player - transform.position).magnitude <= _captureDistance)
                    {
                        Capture();
                    }

                    break;

                case PoliceStage.Warning:
                    if (sees)
                    {
                        MoveTowards(player, _patrolSpeed * 1.4f, _warnDistance);
                    }
                    else
                    {
                        MoveTowards(_lastSeen, _patrolSpeed * 1.4f, 0.5f);
                    }

                    break;

                default:
                    if (sees && Stage == PoliceStage.Noticing)
                    {
                        Face(player, delta);
                    }
                    else if (_investigating)
                    {
                        Investigate(delta);
                    }
                    else
                    {
                        Patrol();
                    }

                    break;
            }
        }

        void Investigate(float delta)
        {
            if (MoveTowards(_investigateAt, _patrolSpeed * 1.3f, 1.5f))
            {
                // 着いたら、しばらく辺りを見回す。
                transform.Rotate(0f, 70f * delta, 0f);
                _lookAroundLeft -= delta;
                if (_lookAroundLeft <= 0f)
                {
                    _investigating = false;
                }
            }
        }

        void Patrol()
        {
            if (_patrol.Length == 0)
            {
                return;
            }

            if (MoveTowards(_patrol[_patrolIndex], _patrolSpeed, 0.4f))
            {
                _patrolIndex = (_patrolIndex + 1) % _patrol.Length;
            }
        }

        /// <summary>目標へ歩く。着いていれば true。</summary>
        bool MoveTowards(Vector3 target, float speed, float stopDistance)
        {
            var offset = Flat(target - transform.position);
            if (offset.magnitude <= stopDistance)
            {
                _body.SimpleMove(Vector3.zero);
                return true;
            }

            var direction = offset.normalized;
            _body.SimpleMove(direction * speed);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                1f - Mathf.Exp(-8f * Time.deltaTime));
            return false;
        }

        void Face(Vector3 target, float delta)
        {
            _body.SimpleMove(Vector3.zero);
            var offset = Flat(target - transform.position);
            if (offset.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(offset),
                    1f - Mathf.Exp(-6f * delta));
            }
        }

        /// <summary>
        /// 見えているか。視界の円錐の中で、間に遮る物が無いこと。警告・追跡中はこちらを向いているので円錐を問わない。
        /// </summary>
        bool CanSeePlayer()
        {
            var eye = transform.position + Vector3.up * EyeHeight;
            var target = _player.transform.position + Vector3.up * 1.5f;
            var offset = target - eye;
            if (offset.sqrMagnitude > _sightRadius * _sightRadius)
            {
                return false;
            }

            if (Stage < PoliceStage.Warning && Vector3.Angle(transform.forward, Flat(offset)) > _sightAngle * 0.5f)
            {
                return false;
            }

            if (Physics.Raycast(eye, offset.normalized, out var hit, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<ProtoHands>() != null;
            }

            return true;
        }

        void Capture()
        {
            var vitals = _player.Vitals.Vitals;
            vitals.Apply(new VitalsDelta { Hp = -_captureHpLoss });

            // 突き飛ばされて倒れたなら、そこからは倒れたときの流れ（ProtoSession）が引き取る。
            if (!vitals.IsAlive)
            {
                ResetEncounter();
                return;
            }

            var seized = Confiscate();
            Escort();
            Say(seized > 0 ? PoliceText.CaptureWithSeizure : PoliceText.Capture);
            ResetEncounter();
        }

        int Confiscate()
        {
            var bag = _player.Bag;
            if (bag == null || _catalog == null)
            {
                return 0;
            }

            var seized = Confiscation.SelectSeized(bag.Items, _catalog, _rng);
            // 索引は降順なので、この順に抜けばずれない。
            foreach (var index in seized)
            {
                bag.TryRemoveAt(index, out _);
            }

            return seized.Count;
        }

        /// <summary>公園の端（街の外れ）まで連れて行かれる。</summary>
        void Escort()
        {
            if (_escortPoint == null)
            {
                return;
            }

            var body = _player.GetComponent<CharacterController>();
            if (body != null)
            {
                body.enabled = false;
            }

            _player.transform.position = _escortPoint.position;
            if (body != null)
            {
                body.enabled = true;
            }
        }

        void ResetEncounter()
        {
            _suspicion = 0f;
            Stage = PoliceStage.Unaware;
            _investigating = false;
        }

        void Say(string line) => _player.Say(ProtoText.PoliceSays(line));

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
