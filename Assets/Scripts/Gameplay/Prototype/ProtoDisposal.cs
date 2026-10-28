using System.Collections.Generic;
using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Gameplay.Foraging;
using KyoumoMushoku.Gameplay.Items;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// コンビニの廃棄の時間（第 5 分の山場・作業計画 第一の二節）。
    /// 夜になってしばらくすると、店員が期限切れの袋を提げて店から出てきて、店の横のゴミ箱へ入れて戻っていく。
    /// そのゴミ箱を開けると、中身は一度に出ず、噴き出し続ける（<see cref="ProtoTrashCan.LoadDisposal"/>）。
    /// 鞄に入りきらない量なので、何を掴み、何を捨てて入れ替えるかが問われる。噴くたびに鳴るので、警官の耳にも届く。
    ///
    /// 毎晩は出ない。出るかどうかは日付が変わった時点で決まる（<see cref="SliceTuningAsset.DisposalChance"/>）。
    /// 中身は店の物なら何でも混じり、品ごとの出やすさは <see cref="SliceTuningAsset.DisposalTable"/>、珍しさは品の段（<see cref="SliceTuningAsset.TierOf"/>）が決める。
    ///
    /// 予告は世界の中で、事前に行う（第十四節）：夕方、店の近くにいると店員のぼやきが聞こえる。昼の間に店に入れば、
    /// 店員が今夜の廃棄の多い少ないをこぼす（<see cref="ProtoShopPanel"/>）。出ない夜は、出ないと言う（嘘をつかない）。
    /// 一晩に一度。日付が変わるとまた決め直す。
    /// </summary>
    public sealed class ProtoDisposal : MonoBehaviour
    {
        enum Phase
        {
            Waiting,
            WalkingOut,
            Dropping,
            WalkingBack,
            Done,
        }

        [SerializeField] ProtoDayNight _dayNight;
        [SerializeField] ProtoTrashCan _bin;
        [SerializeField] ProtoHands _player;
        [SerializeField] Transform _clerk;
        [Tooltip("店員が出てくる所（入口の前）。")]
        [SerializeField] Vector3 _doorPoint;
        [Tooltip("店員が袋を入れるときに立つ所（ゴミ箱の前）。")]
        [SerializeField] Vector3 _binPoint;

        [Header("時刻")]
        [Tooltip("夜になってから店員が出てくるまでの秒数。")]
        [SerializeField, Min(0f)] float _delayAfterNightfall = 25f;

        [Header("中身（確率・回数・品の表は SliceTuning）")]
        [SerializeField] SliceTuningAsset _slice;
        [SerializeField] ItemDatabaseAsset _catalog;

        [Tooltip("噴き出す間隔（秒）。")]
        [SerializeField, Min(0.05f)] float _spewInterval = 0.45f;

        [Header("店員")]
        [SerializeField, Min(0.1f)] float _clerkSpeed = 1.4f;
        [SerializeField, Min(0f)] float _dropSeconds = 2f;
        [Tooltip("この距離より近くにいれば、店員の声が聞こえる。")]
        [SerializeField, Min(0f)] float _earshot = 16f;

        IRng _rng;
        Phase _phase;
        float _dropTimer;
        int _day;
        bool _comingTonight;
        bool _muttered;

        /// <summary>今夜の廃棄がまだ出ていない（昼・夕方の店員の口ぶりに使う）。</summary>
        public bool PendingTonight => _comingTonight && _phase == Phase.Waiting;

        /// <summary>今夜は廃棄が出ないと決まっている。</summary>
        public bool NoneTonight => !_comingTonight;

        public void Configure(ProtoDayNight dayNight, ProtoTrashCan bin, ProtoHands player, Transform clerk,
            Vector3 doorPoint, Vector3 binPoint, SliceTuningAsset slice, ItemDatabaseAsset catalog)
        {
            _slice = slice;
            _catalog = catalog;
            _dayNight = dayNight;
            _bin = bin;
            _player = player;
            _clerk = clerk;
            _doorPoint = doorPoint;
            _binPoint = binPoint;
        }

        void Awake()
        {
            _rng = new SystemRng();
            if (_clerk != null)
            {
                _clerk.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            var clock = _dayNight != null ? _dayNight.Clock : null;
            if (clock == null || _bin == null)
            {
                return;
            }

            if (clock.Day != _day)
            {
                // 新しい一日。今夜出るかどうかを、ここで先に決める（予告が嘘にならないように）。
                _day = clock.Day;
                _comingTonight = _slice == null || _rng.NextDouble() < _slice.DisposalChance;
                _phase = _comingTonight ? Phase.Waiting : Phase.Done;
                _muttered = false;
            }

            if (clock.Phase == DayPhase.Dusk && !_muttered && PlayerWithin(_earshot))
            {
                _muttered = true;
                Say(_comingTonight ? ProtoText.ClerkMutterAtDusk : ProtoText.ClerkMutterNoDisposal);
            }

            var dt = Time.deltaTime;
            switch (_phase)
            {
                case Phase.Waiting:
                    // 夜からの経過は時計から読む。バイトのように時計がまとまって進んでも、廃棄の時刻がずれない。
                    if (_dayNight.IsNight && _dayNight.SecondsSinceNightfall >= _delayAfterNightfall)
                    {
                        StepOut();
                    }

                    break;

                case Phase.WalkingOut:
                    if (Walk(_binPoint, dt))
                    {
                        _phase = Phase.Dropping;
                        _dropTimer = _dropSeconds;
                        _bin.LoadDisposal(Bags(), _spewInterval);
                        if (PlayerWithin(_earshot))
                        {
                            Say(ProtoText.ClerkDropsDisposal);
                        }
                    }

                    break;

                case Phase.Dropping:
                    _dropTimer -= dt;
                    if (_dropTimer <= 0f)
                    {
                        _phase = Phase.WalkingBack;
                    }

                    break;

                case Phase.WalkingBack:
                    if (Walk(_doorPoint, dt))
                    {
                        _clerk.gameObject.SetActive(false);
                        _phase = Phase.Done;
                    }

                    break;
            }
        }

        void StepOut()
        {
            _phase = Phase.WalkingOut;
            if (_clerk != null)
            {
                _clerk.position = _doorPoint;
                _clerk.gameObject.SetActive(true);
            }
        }

        /// <summary>店員を歩かせる。着いたら true。</summary>
        bool Walk(Vector3 target, float dt)
        {
            if (_clerk == null)
            {
                return true;
            }

            var offset = target - _clerk.position;
            offset.y = 0f;
            var step = _clerkSpeed * dt;
            if (offset.magnitude <= step)
            {
                _clerk.position = new Vector3(target.x, _clerk.position.y, target.z);
                return true;
            }

            _clerk.position += offset.normalized * step;
            _clerk.rotation = Quaternion.LookRotation(offset.normalized);
            return false;
        }

        List<LootDraw> Bags()
        {
            var count = _slice != null ? _slice.DisposalDraws : 0;
            var draws = new List<LootDraw>(count);
            if (_slice == null)
            {
                return draws;
            }

            // 毎晩組み直す（Play 中に SliceTuning を変えても次の廃棄から効く）。
            var table = TrashCanLootAsset.Build(_slice.DisposalTable);
            for (var i = 0; i < count; i++)
            {
                draws.Add(table.Draw(_rng, _catalog));
            }

            return draws;
        }

        /// <summary>廃棄の1回の抽選で、その品が出る確率。ゴミ箱は珍しさをこれで決める。</summary>

        /// <summary>店員（出ていなければ入口）の声が届く所にいるか。</summary>
        bool PlayerWithin(float distance)
        {
            if (_player == null)
            {
                return false;
            }

            var source = _clerk != null && _clerk.gameObject.activeSelf ? _clerk.position : _doorPoint;
            return (_player.transform.position - source).sqrMagnitude <= distance * distance;
        }

        void Say(string line)
        {
            if (_player != null)
            {
                _player.Say(line);
            }
        }
    }
}
