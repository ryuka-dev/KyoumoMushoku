using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Survival;
using KyoumoMushoku.Gameplay.UI;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Police
{
    /// <summary>
    /// 巡回する警官（第五節）。接触した瞬間に攻撃してくる敵ではなく、段階的にエスカレートする
    /// 追い出し機構であり、都市そのものからの圧力として機能する。
    ///
    /// 注意 → 警告 → 追い出し。プレイヤーの応答は2つある。立ち去るか、漁りの完了に賭けるか。
    /// どちらも新しい入力を要さない。歩けば視界を外れて冷め、留まれば注目度が上がりきる。
    ///
    /// 注目度（この警官が向けている関心）は一過性であり、ここが所有する。
    /// 警戒度（ゾーンの記憶）は永続し、<see cref="ZoneAlertDirector"/> が所有する。
    /// この警官は警戒度を読み、捕まえたときに上昇を申告するだけである。
    /// </summary>
    public sealed class PoliceOfficer : MonoBehaviour
    {
        // 台詞は世界の中の情報である。誰が言ったのかが分かるよう、頭上に出す（第十四節）。
        const string NoticeLine = "……おい。";
        const string WarningLine = "ここは君の居場所じゃない。立ち去りなさい。";
        const string PursueLine = "待ちなさい！";
        const string CaptureLine = "二度とここで漁るな。";
        const string CaptureWithSeizureLine = "腐ったものは預かる。二度とここで漁るな。";

        [Tooltip("この警官が見ている警戒ゾーン。段階進行の速さはこのゾーンの警戒度が駆動する。")]
        [SerializeField] AlertZoneId _zone = AlertZoneId.Commercial;

        [SerializeField] float _patrolMinX = 110f;
        [SerializeField] float _patrolMaxX = 190f;

        [Tooltip("巡回の速さ。叩き台。")]
        [SerializeField] float _patrolSpeed = 2f;

        [Tooltip("追跡の速さ。走るプレイヤー（7.5）より遅く、歩くプレイヤー（4.5）より速い。叩き台。")]
        [SerializeField] float _chaseSpeed = 6.5f;

        [Tooltip("プレイヤーに気づく距離。")]
        [SerializeField] float _sightRadius = 10f;

        [Tooltip("視界の高さの幅。地下通路（Y=-20）は地上の警官から原理的に見えない。")]
        [SerializeField] float _sightHeight = 3f;

        [Tooltip("警告中に保つ距離。これ以上は近づかない。")]
        [SerializeField] float _warnDistance = 2.5f;

        [Tooltip("この距離まで追いつくと捕まえる。")]
        [SerializeField] float _captureDistance = 1.2f;

        [Tooltip("突き飛ばされて失う HP。叩き台。")]
        [SerializeField] float _captureHpLoss = 12f;

        [SerializeField] NpcSpeech _speech;

        [SerializeField] Color _calmTint = new Color(0.25f, 0.3f, 0.55f);
        [SerializeField] Color _alertTint = new Color(0.85f, 0.3f, 0.25f);

        SpriteRenderer _renderer;
        ZoneAlertDirector _alerts;
        AlertZoneVolume _area;

        Transform _player;
        Rigidbody2D _playerBody;
        PlayerVitals _vitals;
        PlayerInventory _inventory;
        PlayerInteractor _interactor;
        ZoneTracker _tracker;

        IRng _rng;
        float _suspicion;
        float _patrolDirection = 1f;
        bool _raisedResidentialThisPursuit;

        /// <summary>いまの段階。診断用の HUD が観測する。ゲームプレイの権威ではない。</summary>
        public PoliceStage Stage { get; private set; } = PoliceStage.Unaware;

        /// <summary>いまの注目度（0〜100）。この警官だけが持つ一過性の値。</summary>
        public float Suspicion => _suspicion;

        public AlertZoneId Zone => _zone;

        public void Configure(AlertZoneId zone, float patrolMinX, float patrolMaxX, NpcSpeech speech)
        {
            _zone = zone;
            _patrolMinX = Mathf.Min(patrolMinX, patrolMaxX);
            _patrolMaxX = Mathf.Max(patrolMinX, patrolMaxX);
            _speech = speech;
        }

        void Awake()
        {
            // 姿はスケールされた子（Body）が持つ。根は等倍のまま保ち、頭上の台詞が歪まないようにする。
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _rng = new SystemRng();
            _alerts = FindFirstObjectByType<ZoneAlertDirector>();

            _vitals = FindFirstObjectByType<PlayerVitals>();
            if (_vitals != null)
            {
                _player = _vitals.transform;
                _playerBody = _player.GetComponent<Rigidbody2D>();
                _inventory = _player.GetComponent<PlayerInventory>();
                _interactor = _player.GetComponent<PlayerInteractor>();
                _tracker = _player.GetComponent<ZoneTracker>();
            }

            _area = ResolveOwnArea();
            ApplyTint();
        }

        void OnEnable()
        {
            if (_tracker != null)
            {
                _tracker.ZoneChanged += OnPlayerZoneChanged;
            }
        }

        void OnDisable()
        {
            if (_tracker != null)
            {
                _tracker.ZoneChanged -= OnPlayerZoneChanged;
            }
        }

        /// <summary>
        /// 自分が立っているエリアの警戒ボリューム。追い出す先（エリアの端）はここから導く。
        /// レイアウトの定数を警官に埋め込まない。地形を動かしても追い出す先はついてくる。
        /// </summary>
        AlertZoneVolume ResolveOwnArea()
        {
            foreach (var volume in FindObjectsByType<AlertZoneVolume>(FindObjectsSortMode.None))
            {
                if (!volume.TryGetComponent(out Collider2D collider))
                {
                    continue;
                }

                var bounds = collider.bounds;
                if (transform.position.x >= bounds.min.x && transform.position.x <= bounds.max.x &&
                    volume.Zone == _zone)
                {
                    return volume;
                }
            }

            return null;
        }

        void Update()
        {
            if (_alerts == null || _player == null || _vitals == null)
            {
                return;
            }

            var delta = Time.deltaTime;
            var sees = _vitals.Vitals.IsAlive && CanSeePlayer();

            _suspicion = sees
                ? PoliceEscalation.Advance(_suspicion, ObservedGainPerSecond(),
                    PoliceEscalation.SpeedMultiplier(_zone, _alerts.Level(_zone)), delta)
                : PoliceEscalation.Relax(_suspicion, delta);

            var next = PoliceEscalation.NextStage(Stage, _suspicion);
            if (next != Stage)
            {
                EnterStage(next);
            }

            Act(delta);
        }

        /// <summary>
        /// 視界内で何をしているか。目立つ行為（<see cref="ISuspiciousAct"/>）はより速く注目を集める。
        /// 何もしていなくても、長く留まれば注意を招く（第五節・第1段階）。
        /// </summary>
        float ObservedGainPerSecond()
        {
            if (_interactor != null && _interactor.Channeling is ISuspiciousAct act)
            {
                return act.SuspicionPerSecond;
            }

            return PoliceEscalation.LoiterGainPerSecond;
        }

        /// <summary>
        /// 高さの幅を見ることで、地下通路（Y=-20）を通る限り警官の視界に入らない。
        /// 「速いが別のリスクがある経路」（第十三節）に、新しい規則を足さずに意味を与える。
        /// </summary>
        bool CanSeePlayer()
        {
            var offset = _player.position - transform.position;

            return Mathf.Abs(offset.y) <= _sightHeight &&
                   Mathf.Abs(offset.x) <= _sightRadius;
        }

        void EnterStage(PoliceStage next)
        {
            Stage = next;

            switch (next)
            {
                case PoliceStage.Noticing:
                    Say(NoticeLine);
                    break;
                case PoliceStage.Warning:
                    Say(WarningLine);
                    break;
                case PoliceStage.Pursuing:
                    _raisedResidentialThisPursuit = false;
                    Say(PursueLine);
                    break;
            }

            ApplyTint();
        }

        void Act(float delta)
        {
            switch (Stage)
            {
                case PoliceStage.Warning:
                    Approach(delta, _patrolSpeed, _warnDistance);
                    break;

                case PoliceStage.Pursuing:
                    Approach(delta, _chaseSpeed, _captureDistance);
                    if (Mathf.Abs(_player.position.x - transform.position.x) <= _captureDistance &&
                        Mathf.Abs(_player.position.y - transform.position.y) <= _sightHeight)
                    {
                        Capture();
                    }

                    break;

                default:
                    Patrol(delta);
                    break;
            }
        }

        void Patrol(float delta)
        {
            var x = transform.position.x + _patrolDirection * _patrolSpeed * delta;

            if (x <= _patrolMinX)
            {
                x = _patrolMinX;
                _patrolDirection = 1f;
            }
            else if (x >= _patrolMaxX)
            {
                x = _patrolMaxX;
                _patrolDirection = -1f;
            }

            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }

        /// <summary>プレイヤーへ寄る。追跡でも自分のゾーンの外までは出ない。</summary>
        void Approach(float delta, float speed, float stopDistance)
        {
            var toPlayer = _player.position.x - transform.position.x;
            if (Mathf.Abs(toPlayer) <= stopDistance)
            {
                return;
            }

            var x = transform.position.x + Mathf.Sign(toPlayer) * speed * delta;
            transform.position = new Vector3(ClampToArea(x), transform.position.y, transform.position.z);
        }

        float ClampToArea(float x)
        {
            if (_area == null || !_area.TryGetComponent(out Collider2D collider))
            {
                return x;
            }

            var bounds = collider.bounds;
            return Mathf.Clamp(x, bounds.min.x, bounds.max.x);
        }

        /// <summary>
        /// 商業ゾーンで追われて生活ゾーンへ逃げ帰る行為は、生活ゾーンの警戒度への最大の入力である（第十三節）。
        /// 自分の住処の近くで目立つと、自分の住処を失う。
        /// </summary>
        void OnPlayerZoneChanged(AlertZoneId zone)
        {
            if (Stage != PoliceStage.Pursuing || _raisedResidentialThisPursuit ||
                zone != AlertZoneId.Residential || _alerts == null)
            {
                return;
            }

            _raisedResidentialThisPursuit = true;
            _alerts.Raise(AlertZoneId.Residential, ZoneAlertTuning.FleeIntoResidentialRaise);
        }

        void Capture()
        {
            // 捕まったゾーンの警戒度は、その後どうなろうと必ず上がる（第五節）。
            _alerts.Raise(_zone, ZoneAlertTuning.CaptureRaise);

            _vitals.Vitals.Apply(new VitalsDelta { Hp = -_captureHpLoss });

            // 突き飛ばされて行き倒れたなら、そこからは死亡の経路（搬送・医療費・没収）が引き取る。
            // 没収を二度行わない。
            if (!_vitals.Vitals.IsAlive)
            {
                Reset();
                return;
            }

            var seized = Confiscate();
            EscortToAreaEdge();
            Say(seized > 0 ? CaptureWithSeizureLine : CaptureLine);

            Reset();
        }

        int Confiscate()
        {
            if (_inventory == null || _inventory.Inventory == null || _inventory.Catalog == null)
            {
                return 0;
            }

            var inventory = _inventory.Inventory;
            var seized = Confiscation.SelectSeized(inventory.Items, _inventory.Catalog, _rng);

            // 索引は降順なので、この順に抜けばずれない。
            foreach (var index in seized)
            {
                inventory.TryRemoveAt(index, out _);
            }

            return seized.Count;
        }

        /// <summary>現在のエリアの端（安全で貧しい側）まで連れて行かれる（第五節）。</summary>
        void EscortToAreaEdge()
        {
            if (_area == null || !_area.TryGetComponent(out Collider2D collider))
            {
                return;
            }

            var edgeX = collider.bounds.min.x + 1.5f;
            var position = new Vector3(edgeX, _player.position.y, _player.position.z);

            if (_playerBody != null)
            {
                _playerBody.position = position;
                _playerBody.linearVelocity = Vector2.zero;
            }
            else
            {
                _player.position = position;
            }
        }

        void Reset()
        {
            _suspicion = 0f;
            Stage = PoliceStage.Unaware;
            _raisedResidentialThisPursuit = false;
            ApplyTint();
        }

        void Say(string line)
        {
            if (_speech != null)
            {
                _speech.Say(line);
            }
        }

        void ApplyTint()
        {
            if (_renderer == null)
            {
                return;
            }

            // 灰箱段階の視覚的な手がかり。段階が上がるほど赤くなる。
            var t = Stage switch
            {
                PoliceStage.Noticing => 0.35f,
                PoliceStage.Warning => 0.7f,
                PoliceStage.Pursuing => 1f,
                _ => 0f,
            };

            _renderer.color = Color.Lerp(_calmTint, _alertTint, t);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.85f, 0.3f, 0.25f, 0.35f);
            Gizmos.DrawWireCube(transform.position, new Vector3(_sightRadius * 2f, _sightHeight * 2f, 0f));
        }
    }
}
