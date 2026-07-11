using KyoumoMushoku.Core.DayCycle;
using KyoumoMushoku.Core.Foraging;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Knacks;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Police;
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
    /// 中断し、そのときは何も消費しない。この「完了までの間」に警官の警告が載る（第五節）。
    /// 漁る姿は目立つため、<see cref="ISuspiciousAct"/> として注目度を供給する。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class TrashCan : MonoBehaviour, IChanneledInteractable, ISuspiciousAct
    {
        [SerializeField] TrashCanKind _kind = TrashCanKind.Park;
        [SerializeField] TrashCanLootAsset _loot;
        [SerializeField] GameClockDriver _clock;

        [Tooltip("1日に漁れる回数。使い切ると翌日まで空。叩き台。")]
        [SerializeField, Min(1)] int _yieldsPerDay = 3;

        [Tooltip("1回の漁りにかかる秒数。大型のゴミ箱ほど長い。叩き台。")]
        [SerializeField, Min(0.1f)] float _rummageSeconds = 2f;

        [Tooltip("漁っている姿を見られている間、毎秒どれだけ注目度が上がるか。叩き台。")]
        [SerializeField, Min(0f)] float _suspicionPerSecond = 18f;

        [SerializeField] Color _fullTint = new Color(0.55f, 0.5f, 0.35f);
        [SerializeField] Color _depletedTint = new Color(0.32f, 0.32f, 0.32f);

        SpriteRenderer _renderer;
        IRng _rng;
        int _remainingToday;
        int _lastSeenDay = int.MinValue;

        // 次の1回分を先に引いて確定させておく（あたりの見分け方・第六節）。実際に漁るとこれが出る。
        // 再抽選しないことが「情報は奪ってよいが嘘はつかない」を守る（第三節）。時間帯が変われば引き直す。
        LootDraw _nextDraw;
        bool _hasNextDraw;
        bool _nextDrawNight;

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
                _hasNextDraw = false; // 新しい1日。次に出るものを引き直す。
                ApplyTint();
            }
        }

        /// <summary>ゴミ箱を漁る姿は目立つ（第五節・第1段階）。</summary>
        public float SuspicionPerSecond => _suspicionPerSecond;

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

            return "ゴミ箱を漁る" + PeekSuffix(player);
        }

        /// <summary>
        /// あたりの見分け方（第六節）を持つなら、次に出るものを先に読む。開示の段階は SAN に依存する
        /// （<see cref="ForageSight"/>）。ここに見えるものが実際に漁って出るものである（再抽選しない・第三節）。
        /// `??` の脇には必ず理由を添える。持たない・読めないときは何も足さない。
        /// </summary>
        string PeekSuffix(PlayerContext player)
        {
            if (player.Knacks == null || player.Vitals == null || player.Inventory == null)
            {
                return string.Empty;
            }

            var peek = ForageSight.Read(player.Knacks.Has(KnackId.SpotDuds), player.Vitals.Vitals.Sanity);
            if (peek == ForagePeek.Hidden)
            {
                return string.Empty;
            }

            if (peek == ForagePeek.Unreadable)
            {
                return "（??・よく見えない）";
            }

            EnsureNextDraw(player.Inventory.Catalog);
            if (!_hasNextDraw)
            {
                return string.Empty;
            }

            if (!_nextDraw.HasItem)
            {
                return "（空っぽのようだ）";
            }

            if (peek != ForagePeek.Detailed)
            {
                // 空か当たりかは分かるが、中身までは分からない。
                return "（当たりのようだ）";
            }

            var catalog = player.Inventory.Catalog;
            var name = catalog != null && catalog.TryGet(_nextDraw.Item, out var definition)
                ? definition.DisplayName
                : _nextDraw.Item.Value;
            return $"（{name}が見える）";
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

            // 先引き済みの1回を使う（あたりの見分け方で見えていたものと必ず一致する・第三節）。
            EnsureNextDraw(catalog);
            var draw = _nextDraw;

            if (!draw.HasItem)
            {
                Spend();
                _hasNextDraw = false;
                player.Knacks?.RecordRummage();
                return "空っぽだった。";
            }

            KyoumoMushoku.Core.Items.ItemDefinition definition = null;
            var found = catalog != null && catalog.TryGet(draw.Item, out definition);
            var name = found ? definition.DisplayName : draw.Item.Value;
            var onBack = found && definition.CarriedOnBack;

            var instance = new ItemInstance(draw.Item, draw.State);

            // 段ボールなどの背負い物は鞄ではなく背負いスロットへ（第十一節）。
            var placed = onBack
                ? player.Carry != null && player.Carry.Slot != null && player.Carry.Slot.TryCarry(instance)
                : player.Inventory.Inventory.TryAdd(instance);

            if (!placed)
            {
                // 入りきらないときは資源を消費せず、正直に伝える（第十四節）。次に出るものはそのまま残る。
                return onBack
                    ? $"{name}を見つけたが、もう担げない。"
                    : $"{name}を見つけたが、カバンに入りきらない。";
            }

            Spend();
            _hasNextDraw = false;
            player.Knacks?.RecordRummage();

            // 状態（新鮮／傷み／腐敗）はここでは言わない。読めるかどうかは SAN の問題であり、
            // 食品カードの `??` で読む（第三節）。ここで漏らすと情報機構を迂回してしまう。
            return $"{name}が出た。";
        }

        /// <summary>
        /// 次に出る1回を、まだ引いていなければ引く。時間帯が変わっていたら引き直す（昼夜でテーブルが違う）。
        /// 引くには食品状態の抽選にカタログが要るため、プレイヤーが調べる／漁る瞬間に遅延して引く。
        /// </summary>
        void EnsureNextDraw(IItemCatalog catalog)
        {
            if (Depleted || _loot == null)
            {
                _hasNextDraw = false;
                return;
            }

            if (_hasNextDraw && _nextDrawNight == IsNight)
            {
                return;
            }

            _nextDraw = _loot.TableFor(_kind, IsNight).Draw(_rng, catalog);
            _hasNextDraw = true;
            _nextDrawNight = IsNight;
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
