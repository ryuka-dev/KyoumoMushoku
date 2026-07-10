using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Session;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 就寝場所。ベンチ・地下通路・安宿の3種を1つの型で表す（第二節・第十三節）。
    /// 就寝すると日付が進み、就寝場所を問わずオートセーブされる。
    ///
    /// 差別化は回復量だけでなく、属する警戒ゾーンにもある（第十三節）。ゾーンごとの帰結
    /// （起こされる確率・保管庫イベント・段階進行）は Phase 3 以降がこの値を読む。
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

        public void BindSession(GameSession session) => _session = session;

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
