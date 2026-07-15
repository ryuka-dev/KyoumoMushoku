using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Police;
using KyoumoMushoku.Gameplay.Session;
using KyoumoMushoku.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 就寝場所。ベンチ・地下通路・安宿の3種を1つの型で表す（第二節・第十三節）。
    /// 就寝すると日付が進み、就寝場所を問わずオートセーブされる。
    ///
    /// 差別化は回復量だけでなく、属する警戒ゾーンにもある（第十三節）。無料の就寝場所は
    /// そのゾーンの警戒度を上げる。静穏ゾーンの無料の寝床（＝公園のベンチ）では、それが積み上がると
    /// 苦情の貼り紙が出て、やがてベンチが撤去される（第五節・敵対的建築）。撤去は恒久ではなく、
    /// 警戒度が下がれば数日で戻る。生活ゾーンでの帰結（保管庫イベント）は Phase 5。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SleepSpot : MonoBehaviour, IInteractable, IRespawnPoint
    {
        [SerializeField] string _respawnId = "bench_park";
        [SerializeField] string _label = "ベンチで寝る";
        [SerializeField] AlertZoneId _zone = AlertZoneId.Quiet;

        [Tooltip("安宿など有料の就寝場所の宿代。無料なら 0。")]
        [SerializeField] int _costYen;

        [Tooltip("安宿は完全回復する。ベンチ・地下通路は下の回復量を使う。")]
        [SerializeField] bool _fullRestore;

        [SerializeField] float _hpRecovery = 20f;
        [SerializeField] float _thirstRecovery;
        [SerializeField] float _hungerRecovery;

        [Tooltip("SAN の基準回復量。精神崩壊時は下がるが下限がある（第三節）。")]
        [SerializeField] float _sanityRecovery = 12f;

        [Tooltip("苦情の貼り紙・撤去を世界の中で示すラベル。撤去対象の無料就寝場所にだけ意味を持つ。無くても成立する。")]
        [SerializeField] TMP_Text _notice;

        [SerializeField] Color _openTint = new Color(0.55f, 0.45f, 0.35f);
        [SerializeField] Color _warnedTint = new Color(0.62f, 0.5f, 0.28f);
        [SerializeField] Color _removedTint = new Color(0.3f, 0.3f, 0.3f);

        [Tooltip("夜になったかを読む時計。就寝可否の権威はこちらにあり、就寝場所は観測するだけ。")]
        [SerializeField] GameClockDriver _clock;

        GameSession _session;
        ZoneAlertDirector _alerts;
        SpriteRenderer _renderer;
        SleepSpotState _state = SleepSpotState.Open;

        public string RespawnId => _respawnId;
        public Vector3 SpawnPosition => transform.position;

        public AlertZoneId Zone => _zone;
        public int CostYen => _costYen;
        public bool FullRestore => _fullRestore;
        public float HpRecovery => _hpRecovery;
        public float ThirstRecovery => _thirstRecovery;
        public float HungerRecovery => _hungerRecovery;
        public float SanityRecovery => _sanityRecovery;

        /// <summary>無料の寝床だけがそのゾーンの警戒度を上げる。金を払う客は誰にも気に留められない（第五節）。</summary>
        public bool IsFree => _costYen <= 0;

        /// <summary>
        /// 撤去の対象になるのは、静穏ゾーンの無料の寝床だけである（第五節）。
        /// 地下通路（生活ゾーン）は撤去されない。危ういのは寝る本人ではなく彼の財産のほうであり、
        /// その帰結は Phase 5 に入る。安宿（有料）は誰も気に留めない。
        /// </summary>
        bool SubjectToClosure => IsFree && _zone == AlertZoneId.Quiet;

        /// <summary>診断・演出用。ゲームプレイの権威ではない。</summary>
        public SleepSpotState State => _state;

        public void BindSession(GameSession session) => _session = session;

        public void BindNotice(TMP_Text notice) => _notice = notice;

        public void Configure(string respawnId, string label, AlertZoneId zone, int costYen, bool fullRestore,
            float hpRecovery, float thirstRecovery, float hungerRecovery, float sanityRecovery)
        {
            _respawnId = respawnId;
            _label = label;
            _zone = zone;
            _costYen = costYen;
            _fullRestore = fullRestore;
            _hpRecovery = hpRecovery;
            _thirstRecovery = thirstRecovery;
            _hungerRecovery = hungerRecovery;
            _sanityRecovery = sanityRecovery;
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            _alerts = FindFirstObjectByType<ZoneAlertDirector>();

            if (_clock == null)
            {
                _clock = FindFirstObjectByType<GameClockDriver>();
            }

            RefreshState(force: true);
        }

        /// <summary>
        /// 夜が来たか。眠れるのは夜だけであり、昼はもちろん薄暮でもまだ寝られない（第二節）。
        /// 時刻の権威は時計にあり、ここは観測するだけ。
        ///
        /// 時計が居ない場面（時計を持たない検証用の場面など）では時刻で拒まない。
        /// 就寝という既存の振る舞いを、読めない情報を理由に塞がないための構えである。
        /// </summary>
        bool IsNight => _clock == null || _clock.Clock == null || _clock.Clock.Phase == DayPhase.Night;

        void Update()
        {
            // 撤去対象のベンチだけが警戒度を読む。他の就寝場所は毎フレーム何もしない。
            if (SubjectToClosure)
            {
                RefreshState(force: false);
            }
        }

        void RefreshState(bool force)
        {
            var next = SubjectToClosure && _alerts != null
                ? SleepSpotClosure.For(_alerts.Level(_zone))
                : SleepSpotState.Open;

            if (!force && next == _state)
            {
                return;
            }

            _state = next;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (_renderer != null)
            {
                _renderer.color = _state switch
                {
                    SleepSpotState.Removed => _removedTint,
                    SleepSpotState.Warned => _warnedTint,
                    _ => _openTint,
                };
            }

            if (_notice != null)
            {
                // 貼り紙は予告、撤去そのものは事後説明（第十四節）。いずれも世界の中の情報として出す。
                _notice.text = _state switch
                {
                    SleepSpotState.Removed => WorldText.SleepNoticeRemoved,
                    SleepSpotState.Warned => WorldText.SleepNoticeWarned,
                    _ => string.Empty,
                };
            }
        }

        public bool CanInteract(PlayerContext player)
        {
            if (player.Vitals == null || !player.Vitals.Vitals.IsAlive || _session == null)
            {
                return false;
            }

            // 撤去されたベンチはもう無い。寝られない（第五節）。
            if (_state == SleepSpotState.Removed)
            {
                return false;
            }

            // 夜になるまでは、どの就寝場所でも寝られない。
            if (!IsNight)
            {
                return false;
            }

            return _costYen <= 0 || (player.Wallet != null && player.Wallet.Wallet.CanAfford(_costYen));
        }

        public string Describe(PlayerContext player)
        {
            if (_state == SleepSpotState.Removed)
            {
                return WorldText.SleepRemoved(_label);
            }

            // 押しても何も起きない理由は、押す前に世界の言葉で告げる（第十四節・予告は事前に）。
            if (!IsNight)
            {
                return WorldText.SleepTooEarly(_label);
            }

            if (_costYen > 0 && (player.Wallet == null || !player.Wallet.Wallet.CanAfford(_costYen)))
            {
                return WorldText.SleepCannotAfford(_label);
            }

            if (_costYen > 0)
            {
                return WorldText.SleepCost(_label, _costYen);
            }

            return _state == SleepSpotState.Warned ? WorldText.SleepWarned(_label) : _label;
        }

        public void Interact(PlayerContext player)
        {
            _session.SleepAt(this);
        }
    }
}
