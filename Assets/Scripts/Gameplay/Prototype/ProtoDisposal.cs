using System.Collections.Generic;
using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Prototype
{
    /// <summary>
    /// コンビニの廃棄の時間（第 5 分の山場・作業計画 第一の二節）。
    /// 夜になってしばらくすると、店員が期限切れの袋を提げて店から出てきて、店の横のゴミ箱へ入れて戻っていく。
    /// そのゴミ箱を開けると、中身は一度に出ず、噴き出し続ける（<see cref="ProtoTrashCan.LoadDisposal"/>）。
    /// 鞄に入りきらない量なので、何を掴み、何を捨てて入れ替えるかが問われる。噴くたびに鳴るので、警官の耳にも届く。
    ///
    /// 予告は世界の中で、事前に行う（第十四節）：夕方、店の近くにいると店員のぼやきが聞こえる。昼の間に店に入れば、
    /// 店員が今夜の廃棄が多いことをこぼす（<see cref="ProtoShopPanel"/>）。
    /// 一晩に一度。日付が変わるとまた待つ。
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

        [Header("中身")]
        [SerializeField] string[] _items =
        {
            "bento", "onigiri", "bread", "onigiri", "bento", "bread", "onigiri", "bento", "onigiri", "bread", "onigiri",
        };

        [Tooltip("期限切れなので、多くは傷んでいる。残りは新鮮。腐った物は出さない（出したての廃棄）。")]
        [SerializeField, Range(0f, 1f)] float _staleChance = 0.6f;

        [Tooltip("噴き出す間隔（秒）。")]
        [SerializeField, Min(0.05f)] float _spewInterval = 0.45f;

        [Header("店員")]
        [SerializeField, Min(0.1f)] float _clerkSpeed = 1.4f;
        [SerializeField, Min(0f)] float _dropSeconds = 2f;
        [Tooltip("この距離より近くにいれば、店員の声が聞こえる。")]
        [SerializeField, Min(0f)] float _earshot = 16f;

        IRng _rng;
        Phase _phase;
        float _nightTimer;
        float _dropTimer;
        int _day;
        bool _muttered;

        /// <summary>今夜の廃棄がまだ出ていない（昼・夕方の店員の口ぶりに使う）。</summary>
        public bool PendingTonight => _phase == Phase.Waiting;

        public void Configure(ProtoDayNight dayNight, ProtoTrashCan bin, ProtoHands player, Transform clerk,
            Vector3 doorPoint, Vector3 binPoint)
        {
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
                // 新しい一日。今夜もまた出る。
                _day = clock.Day;
                _phase = Phase.Waiting;
                _nightTimer = 0f;
                _muttered = false;
            }

            var dt = Time.deltaTime;
            switch (_phase)
            {
                case Phase.Waiting:
                    if (clock.Phase == DayPhase.Dusk && !_muttered && PlayerWithin(_earshot))
                    {
                        _muttered = true;
                        Say(ProtoText.ClerkMutterAtDusk);
                    }

                    if (clock.Phase == DayPhase.Night)
                    {
                        _nightTimer += dt;
                        if (_nightTimer >= _delayAfterNightfall)
                        {
                            StepOut();
                        }
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
            var draws = new List<LootDraw>(_items.Length);
            foreach (var id in _items)
            {
                var state = _rng.NextDouble() < _staleChance ? FoodState.Stale : FoodState.Fresh;
                draws.Add(LootDraw.Of(new ItemId(id), state));
            }

            return draws;
        }

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
