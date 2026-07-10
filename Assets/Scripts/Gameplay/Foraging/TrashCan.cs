using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Interaction;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Foraging
{
    /// <summary>
    /// ゴミ箱。漁ると、種類（A/B/C）と時間帯（昼／夜）に応じた産出テーブルから1個体を引く（第十三節）。
    /// 食品なら状態も同時に確定する（第十一節）。空振りもれっきとした結果である（第十節）。
    ///
    /// 1日に漁れる回数には上限があり、使い切ると「もう漁るものはない」。就寝で日付が変わると満タンに戻る
    /// （第一節：一部エリアの再湧き）。日付の変わり目は <see cref="GameClock.Day"/> の変化を canonical に読む。
    /// ロード時に時計そのものが差し替わる（<c>GameSession</c>）ため、特定の時計インスタンスのイベントには
    /// 依存しない。ロード直後は新しい1日の始まりなので、満タンで目覚めるのが正しい。
    ///
    /// 漁りは <see cref="IChanneledInteractable"/> であり、完了までに探索時間を要する。途中で歩き出せば
    /// 中断し、そのときは何も消費しない。この「完了までの間」に、後の段階（警官の警告・Phase 3）が載る。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class TrashCan : MonoBehaviour, IChanneledInteractable
    {
        [SerializeField] TrashCanKind _kind = TrashCanKind.Park;
        [SerializeField] TrashCanLootAsset _loot;
        [SerializeField] GameClockDriver _clock;

        [Tooltip("1日に漁れる回数。使い切ると翌日まで空。叩き台。")]
        [SerializeField, Min(1)] int _yieldsPerDay = 3;

        [Tooltip("1回の漁りにかかる秒数。大型のゴミ箱ほど長い。叩き台。")]
        [SerializeField, Min(0.1f)] float _rummageSeconds = 2f;

        [SerializeField] Color _fullTint = new Color(0.55f, 0.5f, 0.35f);
        [SerializeField] Color _depletedTint = new Color(0.32f, 0.32f, 0.32f);

        SpriteRenderer _renderer;
        IRng _rng;
        int _remainingToday;
        int _lastSeenDay = int.MinValue;

        public void Configure(TrashCanKind kind, TrashCanLootAsset loot, GameClockDriver clock,
            int yieldsPerDay, float rummageSeconds)
        {
            _kind = kind;
            _loot = loot;
            _clock = clock;
            _yieldsPerDay = Mathf.Max(1, yieldsPerDay);
            _rummageSeconds = Mathf.Max(0.1f, rummageSeconds);
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _rng = new SystemRng();
            _remainingToday = _yieldsPerDay;
            ApplyTint();
        }

        void Update()
        {
            // リポップ：日付が変わったら満タンに戻す。時計インスタンスの差し替えに頑健なよう、Day を読む。
            var day = _clock != null && _clock.Clock != null ? _clock.Clock.Day : _lastSeenDay;
            if (day != _lastSeenDay)
            {
                _lastSeenDay = day;
                _remainingToday = _yieldsPerDay;
                ApplyTint();
            }
        }

        bool IsNight => _clock != null && _clock.Clock != null && _clock.Clock.Phase == DayPhase.Night;

        bool Depleted => _remainingToday <= 0;

        public bool CanInteract(PlayerContext player) =>
            player.Vitals != null && player.Vitals.Vitals.IsAlive &&
            player.Inventory != null && player.Inventory.Inventory != null &&
            !Depleted && player.Inventory.Inventory.FreeSlots > 0;

        public string Describe(PlayerContext player)
        {
            if (Depleted)
            {
                return "ゴミ箱（もう漁るものはない）";
            }

            if (player.Inventory != null && player.Inventory.Inventory != null &&
                player.Inventory.Inventory.FreeSlots <= 0)
            {
                return "ゴミ箱（カバンが一杯だ）";
            }

            return "ゴミ箱を漁る";
        }

        // 即時のフォールバック。通常はチャネル経由で完了するため呼ばれない。
        public void Interact(PlayerContext player) => Rummage(player);

        public float ChannelSeconds(PlayerContext player) => _rummageSeconds;

        public string CompleteChannel(PlayerContext player) => Rummage(player);

        // 中断は何も消費しない。手を止めただけであり、ゴミ箱の中身は減らない。
        public void CancelChannel(PlayerContext player)
        {
        }

        string Rummage(PlayerContext player)
        {
            if (Depleted || _loot == null || player.Inventory == null || player.Inventory.Inventory == null)
            {
                return null;
            }

            var catalog = player.Inventory.Catalog;
            var draw = _loot.TableFor(_kind, IsNight).Draw(_rng, catalog);

            if (!draw.HasItem)
            {
                Spend();
                return "空っぽだった。";
            }

            var name = catalog != null && catalog.TryGet(draw.Item, out var definition)
                ? definition.DisplayName
                : draw.Item.Value;

            var instance = new ItemInstance(draw.Item, draw.State);
            if (!player.Inventory.Inventory.TryAdd(instance))
            {
                // 入りきらないときは資源を消費せず、正直に伝える（第十四節）。空間を空ければ取り直せる。
                return $"{name}を見つけたが、カバンに入りきらない。";
            }

            Spend();

            // 状態（新鮮／傷み／腐敗）はここでは言わない。読めるかどうかは SAN の問題であり、
            // 食品カードの `??` で読む（第三節）。ここで漏らすと情報機構を迂回してしまう。
            return $"{name}が出た。";
        }

        void Spend()
        {
            _remainingToday--;
            ApplyTint();
        }

        void ApplyTint()
        {
            if (_renderer != null)
            {
                _renderer.color = Depleted ? _depletedTint : _fullTint;
            }
        }
    }
}
