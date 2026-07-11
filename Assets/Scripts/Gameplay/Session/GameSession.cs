using System.Collections.Generic;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Persistence;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Progress;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Knacks;
using KyoumoMushoku.Gameplay.Persistence;
using KyoumoMushoku.Gameplay.Police;
using KyoumoMushoku.Gameplay.Progress;
using KyoumoMushoku.Gameplay.Shop;
using KyoumoMushoku.Gameplay.Survival;
using KyoumoMushoku.Gameplay.World;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Session
{
    /// <summary>
    /// セーブ・ロード・就寝・死亡搬送の単一の所有者。プレイヤーの状態を持つ各コンポーネントは
    /// 自分の値を保持するが、それらを一貫した1つのセーブに束ね、就寝の瞬間に書き出すのはここだけ。
    ///
    /// 就寝すると就寝場所を問わずオートセーブする（第二節）。死亡は搬送であってセーブ地点ではない。
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] GameClockDriver _clock;
        [SerializeField] Transform _player;

        [Tooltip("初めて安宿に泊まった証として渡すアイテムの識別子（第十一節・安宿の鍵）。")]
        [SerializeField] string _innKeyItemId = "inn_key";

        readonly Dictionary<string, IRespawnPoint> _respawnPoints = new();
        readonly Dictionary<string, StashSpot> _stashSpots = new();

        ISaveStore _store;
        Rigidbody2D _playerBody;
        PlayerVitals _vitals;
        PlayerWallet _wallet;
        PlayerInventory _inventory;
        PlayerKnacks _knacks;
        PlayerMilestones _milestones;
        PlayerCarry _carry;
        Hospital _hospital;
        ZoneAlertDirector _alerts;
        Storefront _storefront;
        StashDirector _stashDirector;
        IRng _rng;

        public void Configure(GameClockDriver clock, Transform player) => (_clock, _player) = (clock, player);

        void Awake()
        {
            _store ??= new FileSaveStore();
            _rng ??= new SystemRng();
            _alerts = FindFirstObjectByType<ZoneAlertDirector>();
            _storefront = FindFirstObjectByType<Storefront>();
            _stashDirector = FindFirstObjectByType<StashDirector>();

            if (_player == null && _vitals == null)
            {
                _vitals = FindFirstObjectByType<PlayerVitals>();
                if (_vitals != null)
                {
                    _player = _vitals.transform;
                }
            }

            if (_player != null)
            {
                _vitals ??= _player.GetComponent<PlayerVitals>();
                _wallet = _player.GetComponent<PlayerWallet>();
                _inventory = _player.GetComponent<PlayerInventory>();
                _knacks = _player.GetComponent<PlayerKnacks>();
                _milestones = _player.GetComponent<PlayerMilestones>();
                _carry = _player.GetComponent<PlayerCarry>();
                _playerBody = _player.GetComponent<Rigidbody2D>();
            }
        }

        void Start()
        {
            CollectRespawnPoints();

            if (_vitals != null)
            {
                _vitals.Died += OnPlayerDied;
            }

            LoadOrBeginAnew();
        }

        void OnDestroy()
        {
            if (_vitals != null)
            {
                _vitals.Died -= OnPlayerDied;
            }
        }

        void CollectRespawnPoints()
        {
            _respawnPoints.Clear();
            _stashSpots.Clear();

            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is IRespawnPoint point)
                {
                    _respawnPoints[point.RespawnId] = point;
                }

                if (behaviour is Hospital hospital)
                {
                    _hospital = hospital;
                }

                if (behaviour is SleepSpot spot)
                {
                    spot.BindSession(this);
                }

                if (behaviour is StashSpot stashSpot)
                {
                    _stashSpots[stashSpot.StashSpotId] = stashSpot;
                }
            }
        }

        /// <summary>就寝。回復・日付送り・オートセーブをこの順で行う（第二節）。</summary>
        public void SleepAt(SleepSpot spot)
        {
            if (spot == null || _vitals == null)
            {
                return;
            }

            if (spot.CostYen > 0 && (_wallet == null || !_wallet.Wallet.TrySpend(spot.CostYen)))
            {
                return;
            }

            ApplySleepRecovery(spot);

            // 泊まったという事実は日付が変わる前の出来事として数える（第八節）。
            // 安宿＝金を払う就寝場所。初めての宿泊が段階目標になり、その証として鍵が残る（第十一節）。
            if (!spot.IsFree && _milestones != null && _milestones.RecordInnStay())
            {
                GrantInnKeepsake();
            }

            // 日付の切り替わりは就寝の瞬間だけに起こる（第二節）。警戒度の減衰もここで行う。
            // 日付をポーリングしないのは、ロード時にも日付が変わって見え、二重に減衰するためである。
            _clock?.Clock?.BeginNextDay();
            _alerts?.BeginNextDay();

            // コンビニの買い取り枠も日境界で戻る（第十三節）。就寝でセーブされるため、この値は永続しない。
            _storefront?.BeginNextDay();

            // 今夜ここで無料の寝床を使った、という事実は朝まで残る。だから減衰のあとに数える。
            // 静穏ゾーンではこれが積み上がってベンチの撤去を招く（第五節・SleepSpotClosure）。
            if (spot.IsFree)
            {
                // 路上の寝方（第六節）：目立たない寝方を覚えると、無料就寝が残す警戒度が減る＝撤去を招きにくい。
                // 判定は習得前の状態で行う。覚えたその晩ではなく、次の晩から効く。
                var scale = _knacks != null && _knacks.Has(KnackId.StreetSleeper)
                    ? KnackTuning.StreetSleeperFreeSleepScale
                    : 1f;
                _alerts?.Raise(spot.Zone, ZoneAlertTuning.FreeSleepRaise * scale);

                // 野外で寝た回数を数える。2回で 路上の寝方 を覚える。
                _knacks?.RecordOutdoorSleep();
            }

            // 保管庫イベントは日境界で進む（第十二節）。警戒度の減衰・無料就寝の上昇を済ませたあとの警戒度を読む。
            if (_stashDirector != null && _clock != null && _clock.Clock != null)
            {
                _stashDirector.BeginNextDay(_clock.Clock.Day);
            }

            // 新しい日を迎えたことを段階目標に申告する（第八節）。4日目の朝＝3日間生存の達成であり、
            // 達成の通知（結算画面）はこの申告から発火する。セーブより前に記録し、達成済みで永続させる。
            if (_clock != null && _clock.Clock != null)
            {
                _milestones?.RecordDayBegan(_clock.Clock.Day);
            }

            Save(spot.RespawnId);
        }

        void ApplySleepRecovery(SleepSpot spot)
        {
            var vitals = _vitals.Vitals;

            if (spot.FullRestore)
            {
                // 完全回復。SAN も含めて満たす（安宿）。
                vitals.Apply(new VitalsDelta
                {
                    Hp = vitals.Tuning.MaxHp,
                    Thirst = vitals.Tuning.MaxThirst,
                    Hunger = vitals.Tuning.MaxHunger,
                    Sanity = vitals.Tuning.MaxSanity,
                });
                return;
            }

            // 路上の寝方（第六節）：野外の無料の寝床では回復が上がる。有料の安宿には効かない。
            var outdoorBonus = spot.IsFree && _knacks != null && _knacks.Has(KnackId.StreetSleeper)
                ? KnackTuning.StreetSleeperRecoveryBonus
                : 0f;

            // 一晩が過ぎたぶん、渇き・空腹を消費する。就寝は時間の経過でもあり、無料の寝床（ベンチ・地下通路）は
            // その代償を負う。安宿は上の完全回復経路で満タンに戻るのでここには来ない（＝免除）。
            // Vitals.Apply が 0 でクランプし、遡及ダメージは Advance でのみ起きるので、0 で目覚めても即罰しない（第三節）。
            var tuning = vitals.Tuning;

            // SAN の回復だけは崩壊時に下がるが、下限を割らない（第三節）。
            vitals.Apply(new VitalsDelta
            {
                Hp = spot.HpRecovery + outdoorBonus,
                Thirst = spot.ThirstRecovery - tuning.OvernightThirstDrain,
                Hunger = spot.HungerRecovery - tuning.OvernightHungerDrain,
                Sanity = SanityScale.SleepRecovery(spot.SanityRecovery + outdoorBonus, vitals.Sanity),
            });
        }

        /// <summary>
        /// 初めて安宿に泊まった証（第十一節・安宿の鍵）。カバンに空きが無ければ諦める。
        /// 達成の権威はあくまで段階目標のフラグであり、鍵はただの世界内の記念品である。
        /// </summary>
        void GrantInnKeepsake()
        {
            if (_inventory != null && _inventory.Inventory != null)
            {
                _inventory.Inventory.TryAdd(new ItemInstance(new ItemId(_innKeyItemId)));
            }
        }

        void OnPlayerDied()
        {
            var vitals = _vitals.Vitals;

            // SAN のペナルティは死亡直前の値で決まる。搬送より先に読む（第三節）。
            var sanityLoss = DeathPenalty.SanityLoss(vitals.Sanity);

            var fee = _hospital != null ? _hospital.MedicalFeeYen : 0;
            _wallet?.Wallet.SeizeUpTo(fee);

            // 渇き・空腹は死亡直前の値を維持する。回復するのは HP だけ。
            vitals.Apply(new VitalsDelta { Sanity = -sanityLoss });
            vitals.Revive();

            // 腐敗品の没収は警察の没収ルールを流用する（第三節）。取り上げられるという同じ出来事に、
            // 2つの規則を持たせない。
            Confiscate();

            if (_hospital != null)
            {
                TeleportTo(_hospital.SpawnPosition);
            }

            // 死亡はセーブ地点ではない。ここではセーブしない。
        }

        /// <summary>腐敗した食品の一部を取り上げられる。規則は警察と共通（<see cref="Confiscation"/>）。</summary>
        void Confiscate()
        {
            if (_inventory == null || _inventory.Inventory == null || _inventory.Catalog == null)
            {
                return;
            }

            var inventory = _inventory.Inventory;
            var seized = Confiscation.SelectSeized(inventory.Items, _inventory.Catalog, _rng);

            // 索引は降順なので、この順に抜けばずれない。
            foreach (var index in seized)
            {
                inventory.TryRemoveAt(index, out _);
            }
        }

        void LoadOrBeginAnew()
        {
            if (_store == null)
            {
                return;
            }

            if (!_store.TryLoad(out var save, out var error))
            {
                // セーブが存在するのに読めなかったときだけ警告する。存在しない＝新規開始は正常。
                if (_store.Exists)
                {
                    Debug.LogWarning($"{nameof(GameSession)}: セーブを読み込めなかったため新規開始する。理由：{error}");
                }

                return;
            }

            _vitals?.RestoreState(save.Vitals);
            _wallet?.RestoreState(save.WalletYen);
            _inventory?.RestoreState(save.Inventory);
            _clock?.RestoreState(save.Clock);
            _alerts?.RestoreState(save.ZoneAlerts);
            _knacks?.RestoreState(save.Knacks);
            _milestones?.RestoreState(save.Milestones);
            _carry?.RestoreState(save.CarrySlot);
            RestoreStashes(save.Stashes);
            _stashDirector?.RestoreState(save.PendingStashEvents);

            if (!string.IsNullOrEmpty(save.SleepSpotId) &&
                _respawnPoints.TryGetValue(save.SleepSpotId, out var point))
            {
                TeleportTo(point.SpawnPosition);
            }
        }

        /// <summary>置かれている保管庫をすべて集めてセーブへ束ねる。空の設置場所は載せない。</summary>
        List<StashState> CaptureStashes()
        {
            var list = new List<StashState>();
            foreach (var spot in _stashSpots.Values)
            {
                var state = spot.CaptureState();
                if (state != null)
                {
                    list.Add(state);
                }
            }

            return list;
        }

        /// <summary>
        /// セーブデータから保管庫を復元する。設置場所は識別子で対応づけ、世界に無い設置場所の保管庫は
        /// 外部入力として捨てる（地形が変わった等）。カバンのカタログを器の容量計算に手渡す。
        /// </summary>
        void RestoreStashes(List<StashState> stashes)
        {
            var catalog = _inventory != null ? _inventory.Catalog : null;

            // まずすべての設置場所を空に戻す。前の状態が残ったまま重ならないようにする。
            foreach (var spot in _stashSpots.Values)
            {
                spot.Restore(null, catalog);
            }

            if (stashes == null)
            {
                return;
            }

            foreach (var state in stashes)
            {
                if (state != null && _stashSpots.TryGetValue(state.SpotId, out var spot))
                {
                    spot.Restore(state, catalog);
                }
            }
        }

        void Save(string sleepSpotId)
        {
            if (_store == null)
            {
                return;
            }

            var save = new SaveGame
            {
                Version = SaveGame.CurrentVersion,
                Clock = _clock != null && _clock.Clock != null ? _clock.Clock.CaptureState() : new Core.DayCycle.GameClockState(),
                Vitals = _vitals != null ? _vitals.Vitals.CaptureState() : new VitalsState(),
                Inventory = _inventory != null ? _inventory.Inventory.CaptureState() : new Core.Items.InventoryState(),
                ZoneAlerts = _alerts != null ? _alerts.CaptureState() : new ZoneAlertState(),
                Knacks = _knacks != null ? _knacks.CaptureState() : new KnackState(),
                Milestones = _milestones != null ? _milestones.CaptureState() : new MilestoneState(),
                CarrySlot = _carry != null ? _carry.CaptureState() : new Core.Items.CarrySlotState(),
                Stashes = CaptureStashes(),
                PendingStashEvents = _stashDirector != null
                    ? _stashDirector.CaptureState()
                    : new List<PendingStashEvent>(),
                WalletYen = _wallet != null ? _wallet.Wallet.Yen : 0,
                SleepSpotId = sleepSpotId ?? string.Empty,
            };

            if (!SaveGameValidation.TryValidate(save, out var error))
            {
                Debug.LogError($"{nameof(GameSession)}: 生成したセーブが不正なため書き込みを中止した。理由：{error}");
                return;
            }

            _store.Save(save);
        }

        void TeleportTo(Vector3 position)
        {
            if (_player == null)
            {
                return;
            }

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
    }
}
