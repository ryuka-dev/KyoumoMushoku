using System;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Randomness;
using KyoumoMushoku.Core.Zones;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.Police;
using TMPro;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.World
{
    /// <summary>
    /// 拠点に置いた保管庫の設置場所（第十二節）。空のうちは、段ボールを背負っているプレイヤーが
    /// ここで設置すると段ボール箱が生まれる。設置済みのあとは、開けてカバンと出し入れする場になる。
    ///
    /// 設置は時間のかかる行為（<see cref="IChanneledInteractable"/>）であり、その間は目立つ
    /// （<see cref="ISuspiciousAct"/>）。注目は既存の警察システムがチャネルを通して読む（新しい機構を足さない）。
    /// 設置済みの箱を開けるのは即時のインタラクトで、<see cref="Opened"/> を合図に <c>StashPanel</c> が開く。
    ///
    /// 保管庫の状態の単一の所有者はこの設置場所であり、<c>GameSession</c> が就寝時にまとめてセーブへ束ねる。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class StashSpot : MonoBehaviour, IChanneledInteractable, ISuspiciousAct
    {
        [SerializeField] string _stashSpotId = "stash_backalley";
        [SerializeField] AlertZoneId _zone = AlertZoneId.Residential;
        [SerializeField] StashKind _kind = StashKind.CardboardBox;

        [Tooltip("段ボールを設置し終えるまでの秒数。叩き台。")]
        [SerializeField, Min(0.1f)] float _placeSeconds = 1.5f;

        [Tooltip("段ボールを設置している姿を見られている間、毎秒どれだけ注目度が上がるか。叩き台。")]
        [SerializeField, Min(0f)] float _suspicionPerSecond = 20f;

        [SerializeField] Color _emptyTint = new Color(0.32f, 0.32f, 0.35f, 0.55f);
        [SerializeField] Color _boxTint = new Color(0.6f, 0.45f, 0.28f, 1f);

        [Tooltip("保管庫イベントの予告・事後説明を世界の中で示すラベル（第十二・十四節）。無くても成立する。")]
        [SerializeField] TMP_Text _notice;

        [Tooltip("路地裏を仕切る先輩ホームレス（因果の解説者・第十四節）。無くても成立する。")]
        [SerializeField] ElderHomeless _elder;

        SpriteRenderer _renderer;
        IItemCatalog _catalog;
        Stash _stash;

        // 先輩ホームレスが語るための一過性の状態。予告の種別と、直近に起きたイベント。
        // いずれもセッション内の一過性であり、確実な経路（貼り紙ラベル）はこれと独立に永続する。
        StashEventKind _forecastKind = StashEventKind.None;
        StashEventKind _aftermathKind = StashEventKind.None;
        int _aftermathLost;

        public string StashSpotId => _stashSpotId;
        public AlertZoneId Zone => _zone;

        /// <summary>ここに保管庫が置かれているか。診断・配線用。</summary>
        public bool HasStash => _stash != null;

        /// <summary>保管点数（イベント抽選の量の入力）。空・未設置なら 0。</summary>
        public int StashItemCount => _stash?.Count ?? 0;

        /// <summary>保管庫が埋まっているマス数（イベント抽選の入力）。空・未設置なら 0。</summary>
        public int StashUsedSlots => _stash?.UsedSlots ?? 0;

        /// <summary>設置済みの箱を開ける合図。<c>StashPanel</c> がこれを購読して開く。</summary>
        public event Action<PlayerContext, Stash> Opened;

        /// <summary>設置している姿は目立つ（第十二節）。警官がチャネル越しにこれを読む。</summary>
        public float SuspicionPerSecond => _suspicionPerSecond;

        public void Configure(string stashSpotId, AlertZoneId zone, StashKind kind, float placeSeconds)
        {
            _stashSpotId = stashSpotId;
            _zone = zone;
            _kind = kind;
            _placeSeconds = Mathf.Max(0.1f, placeSeconds);
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            ApplyVisual();
        }

        static bool IsCarryingBox(PlayerContext player) =>
            player.Carry != null && player.Carry.IsCarrying;

        public bool CanInteract(PlayerContext player)
        {
            if (player.Vitals == null || !player.Vitals.Vitals.IsAlive)
            {
                return false;
            }

            // 置いてあるなら開ける。無いなら、段ボールを背負っているときだけ置ける。
            return _stash != null || IsCarryingBox(player);
        }

        public string Describe(PlayerContext player)
        {
            if (_stash != null)
            {
                return $"段ボール箱（{_stash.UsedSlots}/{_stash.Capacity}マス）を開ける";
            }

            return IsCarryingBox(player) ? "ここに段ボールを置く" : "（段ボールがあればここに置ける）";
        }

        // 設置済みの箱は即時に開ける（チャネル 0）。空で段ボールを背負っているときだけ、設置にチャネル時間がかかる。
        public float ChannelSeconds(PlayerContext player) =>
            _stash == null && IsCarryingBox(player) ? _placeSeconds : 0f;

        // 即時の経路。設置済みの箱を開ける（ChannelSeconds が 0 を返すのでここに来る）。
        public void Interact(PlayerContext player)
        {
            if (_stash == null)
            {
                return;
            }

            // 開けた瞬間、先輩ホームレスが因果を語る（事後説明→噂話→貯め込みの小言）。箱を前にしたこの瞬間が
            // 世界の中の canonical な機会であり、就寝中に起きたイベントの説明はここで初めて本人に届く。
            _elder?.Comment(_aftermathKind, _aftermathLost, _forecastKind, _stash.UsedSlots);
            _aftermathKind = StashEventKind.None;
            _aftermathLost = 0;

            Opened?.Invoke(player, _stash);
        }

        // 設置チャネルの完了。背負っている段ボールを下ろして箱に変える。
        public string CompleteChannel(PlayerContext player)
        {
            if (_stash != null)
            {
                // 通常この経路には来ない（設置済みは ChannelSeconds が 0）。念のため開けるに留める。
                Opened?.Invoke(player, _stash);
                return null;
            }

            var catalog = player.Inventory != null ? player.Inventory.Catalog : _catalog;
            if (catalog == null || player.Carry == null || player.Carry.Slot == null)
            {
                return "ここには置けない。";
            }

            // カタログを先に確かめてから下ろす。下ろしてから失敗して段ボールを失う、を避ける。
            if (!player.Carry.Slot.TryTakeOut(out _))
            {
                return "置く段ボールがない。";
            }

            _catalog = catalog;
            _stash = new Stash(catalog, _kind, _stashSpotId, StashTuning.CapacityFor(_kind));
            ClearNotice(); // 置いたばかりの箱にはまだ予告が無い。
            ApplyVisual();

            // 罰の前にルールを提示する（第十二節）。設置した本人はいまここに立っている。
            _elder?.SayPlacementRule();
            return "段ボール箱を置いた。";
        }

        // 設置の中断は何も消費しない。段ボールは背負ったまま（漁りの中断と同じ・第五節）。
        public void CancelChannel(PlayerContext player)
        {
        }

        /// <summary>就寝時オートセーブへ束ねる。置いていなければ null。</summary>
        public StashState CaptureState() => _stash?.CaptureState();

        /// <summary>
        /// セーブデータから保管庫を復元する。ロードの単一の所有者（<c>GameSession</c>）だけが、
        /// アイテムカタログとともに呼ぶ。中身の範囲外は内包する器が捨て、捨てた個数を返す。
        /// </summary>
        public int Restore(StashState state, IItemCatalog catalog)
        {
            _catalog = catalog;

            if (state is null || catalog is null)
            {
                _stash = null;
                ApplyVisual();
                return 0;
            }

            _stash = new Stash(catalog, state.Kind, _stashSpotId, state.Capacity);
            var dropped = _stash.Restore(state);
            ApplyVisual();
            return dropped;
        }

        public void BindNotice(TMP_Text notice) => _notice = notice;

        public void BindElder(ElderHomeless elder) => _elder = elder;

        /// <summary>
        /// 直近に起きたイベントを、先輩ホームレスが語るために覚えておく（<see cref="StashDirector"/> が発生時に呼ぶ）。
        /// 次に箱を開けたとき本人に伝えて忘れる。確実な経路（貼り紙）はこれと独立に世界へ出る。
        /// </summary>
        public void QueueAftermath(StashEventKind kind, int lost)
        {
            _aftermathKind = kind;
            _aftermathLost = lost;
        }

        /// <summary>
        /// 保管庫イベントの損失をこの保管庫に適用する（第十二節）。どの点を失うかは無作為で、失った点数を返す。
        /// 保管庫（器）そのものは残る――失われるのは中身であって設置場所ではない。
        /// </summary>
        public int ApplyEventLoss(StashEventKind kind, IRng rng)
        {
            if (_stash == null)
            {
                return 0;
            }

            var lost = 0;
            foreach (var index in StashEventRoll.SelectLost(_stash.Count, kind, rng))
            {
                if (_stash.TryWithdrawAt(index, out _))
                {
                    lost++;
                }
            }

            return lost;
        }

        /// <summary>予告を世界の中に出す（第十二節・SAN を問わず必ず見える保証チャネル）。</summary>
        public void ShowForecast(StashEventKind kind)
        {
            _forecastKind = kind;
            SetNotice(kind switch
            {
                StashEventKind.CityCleaning => "清掃予告の貼り紙：明日の朝、この辺りの清掃が入る",
                StashEventKind.ScavengedByPeers => "荒らされた足跡がある……明日あたり誰かに漁られそうだ",
                StashEventKind.PoliceRemoval => "昼間、警官がこの辺りを下見していた。明日、撤去されるかもしれない",
                _ => string.Empty,
            });
        }

        /// <summary>事後説明を世界の中に出す（第十四節・見えない変化を後から世界が語る）。</summary>
        public void ShowAftermath(StashEventKind kind, int lost)
        {
            _forecastKind = StashEventKind.None; // 事後を出す＝先の予告は無い。
            SetNotice(kind switch
            {
                StashEventKind.CityCleaning => $"清掃が入った（{lost}点を失った）",
                StashEventKind.ScavengedByPeers => $"同業者に漁られた（{lost}点を失った）",
                StashEventKind.PoliceRemoval => $"警察に撤去された（{lost}点を失い、警戒度が上がった）",
                _ => string.Empty,
            });
        }

        public void ClearNotice()
        {
            _forecastKind = StashEventKind.None;
            SetNotice(string.Empty);
        }

        void SetNotice(string text)
        {
            if (_notice != null)
            {
                _notice.text = text;
            }
        }

        void ApplyVisual()
        {
            if (_renderer != null)
            {
                _renderer.color = _stash != null ? _boxTint : _emptyTint;
            }
        }
    }
}
