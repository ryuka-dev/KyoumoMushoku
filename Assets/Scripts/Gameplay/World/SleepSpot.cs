using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Session;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 就寝場所。ベンチ・地下通路・安宿の3種を1つの型で表す（第二節・第十三節）。
    /// 就寝すると日付が進み、就寝場所を問わずオートセーブされる。
    ///
    /// 差別化は回復量だけでなく、属する警戒ゾーンにもある（第十三節）。無料の就寝場所は
    /// そのゾーンに顔を覚えさせ、静穏ゾーンではそれが叩き起こされる確率になる。
    /// 生活ゾーンでの帰結（保管庫イベント）は Phase 5。
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

        [Tooltip("叩き起こされたときに警官が言う台詞を出す口。無くても成立する。")]
        [SerializeField] NpcSpeech _speech;

        GameSession _session;

        public string RespawnId => _respawnId;
        public Vector3 SpawnPosition => transform.position;

        public AlertZoneId Zone => _zone;
        public int CostYen => _costYen;
        public bool FullRestore => _fullRestore;
        public float HpRecovery => _hpRecovery;
        public float ThirstRecovery => _thirstRecovery;
        public float HungerRecovery => _hungerRecovery;
        public float SanityRecovery => _sanityRecovery;

        /// <summary>無料の寝床だけが顔を覚えられる。金を払う客は誰にも気に留められない（第五節）。</summary>
        public bool IsFree => _costYen <= 0;

        public void BindSession(GameSession session) => _session = session;

        public void BindSpeech(NpcSpeech speech) => _speech = speech;

        /// <summary>
        /// 叩き起こされた。見えない状態（静穏ゾーンの警戒度）が上がっていたことを、
        /// 事後に世界の側が教える（第十四節の事後説明の表）。
        /// </summary>
        public void SayWokenByOfficer()
        {
            if (_speech != null)
            {
                _speech.Say("またお前か。");
            }
        }

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

        public bool CanInteract(PlayerContext player)
        {
            if (player.Vitals == null || !player.Vitals.Vitals.IsAlive || _session == null)
            {
                return false;
            }

            return _costYen <= 0 || (player.Wallet != null && player.Wallet.Wallet.CanAfford(_costYen));
        }

        public string Describe(PlayerContext player)
        {
            if (_costYen > 0 && (player.Wallet == null || !player.Wallet.Wallet.CanAfford(_costYen)))
            {
                return $"{_label}（所持金が足りない）";
            }

            return _costYen > 0 ? $"{_label}（{_costYen}円）" : _label;
        }

        public void Interact(PlayerContext player)
        {
            _session.SleepAt(this);
        }
    }
}
