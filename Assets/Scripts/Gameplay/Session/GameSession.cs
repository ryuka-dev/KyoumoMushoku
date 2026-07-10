using System.Collections.Generic;
using KyoumoMushoku.Core.Persistence;
using KyoumoMushoku.Core.Police;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Economy;
using KyoumoMushoku.Gameplay.Items;
using KyoumoMushoku.Gameplay.Persistence;
using KyoumoMushoku.Gameplay.Police;
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

        readonly Dictionary<string, IRespawnPoint> _respawnPoints = new();

        ISaveStore _store;
        Rigidbody2D _playerBody;
        PlayerVitals _vitals;
        PlayerWallet _wallet;
        PlayerInventory _inventory;
        Hospital _hospital;
        ZoneAlertDirector _alerts;
        IRng _rng;

        public void Configure(GameClockDriver clock, Transform player) => (_clock, _player) = (clock, player);

        void Awake()
        {
            _store ??= new FileSaveStore();
            _rng ??= new SystemRng();
            _alerts = FindFirstObjectByType<ZoneAlertDirector>();

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

            // 日付の切り替わりは就寝の瞬間だけに起こる（第二節）。警戒度の減衰もここで行う。
            // 日付をポーリングしないのは、ロード時にも日付が変わって見え、二重に減衰するためである。
            _clock?.Clock?.BeginNextDay();
            _alerts?.BeginNextDay();

            // 今夜ここで無料の寝床を使った、という事実は朝まで残る。だから減衰のあとに数える。
            // 静穏ゾーンではこれが積み上がってベンチの撤去を招く（第五節・SleepSpotClosure）。
            if (spot.IsFree)
            {
                _alerts?.Raise(spot.Zone, ZoneAlertTuning.FreeSleepRaise);
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

            // SAN の回復だけは崩壊時に下がるが、下限を割らない（第三節）。
            vitals.Apply(new VitalsDelta
            {
                Hp = spot.HpRecovery,
                Thirst = spot.ThirstRecovery,
                Hunger = spot.HungerRecovery,
                Sanity = SanityScale.SleepRecovery(spot.SanityRecovery, vitals.Sanity),
            });
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

            if (!string.IsNullOrEmpty(save.SleepSpotId) &&
                _respawnPoints.TryGetValue(save.SleepSpotId, out var point))
            {
                TeleportTo(point.SpawnPosition);
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
