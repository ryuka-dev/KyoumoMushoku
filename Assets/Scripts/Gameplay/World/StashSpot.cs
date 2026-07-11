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

        [Tooltip("段ボール箱を回収したとき背負いスロットへ戻す物の識別子（第十二節・別の場所へ移す）。")]
        [SerializeField] string _reclaimItemId = "cardboard";

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

        // 場所代の残り効果（日数）。その日に払うと 1 になり、就寝＝日境界で減る。一過性であり永続しない
        // （就寝時のセーブは日境界の減衰の後に走るため、保存される値は常に 0。買い取り枠と同じ扱い・第二節）。
        int _rentDaysRemaining;

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

        /// <summary>保管庫を開ける合図（どの設置場所かを渡す）。<c>StashPanel</c> がこれを購読して開く。</summary>
        public event Action<StashSpot, PlayerContext, Stash> Opened;

        /// <summary>この保管庫の呼び名（UI 表示・第十二節）。種別で変わる。</summary>
        public string KindLabel => _kind == StashKind.CoinLocker ? "コインロッカー" : "段ボール箱";

        /// <summary>料金の呼び名（UI 表示）。段ボール箱は場所代、コインロッカーは使用料。</summary>
        public string RentLabel => _kind == StashKind.CoinLocker ? "使用料" : "場所代";

        // コインロッカーは商業ゾーンの什器で、段ボールを担いで置くのではなく、その場で借りて開ける（初回に器を用意する）。
        bool IsCoinLocker => _kind == StashKind.CoinLocker;

        /// <summary>回収して担ぎ直せる保管庫か（第十二節「別の場所へ移す」）。段ボール箱だけ。コインロッカーは什器で持ち出せない。</summary>
        public bool IsReclaimable => _kind == StashKind.CardboardBox;

        /// <summary>回収の結果。UI はこれを世界の言葉へ翻訳する。</summary>
        public enum ReclaimOutcome { Reclaimed, NotReclaimable, NotEmpty, NoStash, CarryUnavailable }

        /// <summary>いま回収できるか（UI 用）。空の段ボール箱で、背負いスロットが空いているとき。</summary>
        public bool CanReclaim(PlayerContext player) =>
            IsReclaimable && _stash != null && _stash.UsedSlots == 0 &&
            player?.Carry != null && player.Carry.Slot != null && !player.Carry.Slot.IsOccupied;

        /// <summary>設置している姿は目立つ（第十二節）。警官がチャネル越しにこれを読む。</summary>
        public float SuspicionPerSecond => _suspicionPerSecond;

        /// <summary>この保管庫の場所代（1日・円）。0 なら場所代を取らない種別。</summary>
        public int RentCostYen => StashTuning.RentCostFor(_kind);

        /// <summary>場所代を取る保管庫が置かれているか。UI が支払いの導線を出すかを決める。</summary>
        public bool CanPayRent => _stash != null && RentCostYen > 0;

        /// <summary>今日の場所代を払い済みか（安全性が上がっている状態）。</summary>
        public bool RentActive => _stash != null && _rentDaysRemaining > 0;

        /// <summary>保管庫イベントの発生確率にかける安全性の係数（種別＋その日の場所代）。空・未設置なら 1。</summary>
        public float EventChanceMultiplier =>
            _stash != null ? StashSafety.EventChanceMultiplier(_kind, RentActive) : 1f;

        /// <summary>場所代の支払い結果。UI はこれを世界の言葉へ翻訳する。</summary>
        public enum PayRentOutcome { Paid, AlreadyPaid, CannotAfford, NotRentable, NoStash }

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

            // 置いてあるなら開ける。コインロッカーはいつでも借りて開ける。段ボール箱は背負っているときだけ置ける。
            return _stash != null || IsCoinLocker || IsCarryingBox(player);
        }

        public string Describe(PlayerContext player)
        {
            if (_stash != null)
            {
                return $"{KindLabel}（{_stash.UsedSlots}/{_stash.Capacity}マス）を開ける";
            }

            if (IsCoinLocker)
            {
                return "コインロッカーを開ける";
            }

            return IsCarryingBox(player) ? "ここに段ボールを置く" : "（段ボールがあればここに置ける）";
        }

        // 設置済みの箱・コインロッカーは即時に開ける（チャネル 0）。空で段ボールを背負っているときだけ、設置にチャネル時間がかかる。
        public float ChannelSeconds(PlayerContext player) =>
            _stash == null && !IsCoinLocker && IsCarryingBox(player) ? _placeSeconds : 0f;

        // 即時の経路。設置済みの箱を開ける（ChannelSeconds が 0 を返すのでここに来る）。
        public void Interact(PlayerContext player)
        {
            if (_stash == null)
            {
                // コインロッカーは初回に器を用意する（段ボールと同じ機構を、担がず・その場で使う）。
                // 開けるだけなら無料。安全性は使用料を払っている間だけ上がる（第十二節「有料である限り高安全」）。
                if (!IsCoinLocker)
                {
                    return;
                }

                var lockerCatalog = player.Inventory != null ? player.Inventory.Catalog : _catalog;
                if (lockerCatalog == null)
                {
                    return;
                }

                _catalog = lockerCatalog;
                _stash = new Stash(lockerCatalog, _kind, _stashSpotId, StashTuning.CapacityFor(_kind));
                _rentDaysRemaining = 0;
                ApplyVisual();
            }

            // 開けた瞬間、先輩ホームレスが因果を語る（事後説明→噂話→貯め込みの小言）。箱を前にしたこの瞬間が
            // 世界の中の canonical な機会であり、就寝中に起きたイベントの説明はここで初めて本人に届く。
            // コインロッカーには先輩がいないので、事後説明は貼り紙（保証チャネル）だけが担う。
            _elder?.Comment(_aftermathKind, _aftermathLost, _forecastKind, _stash.UsedSlots);
            _aftermathKind = StashEventKind.None;
            _aftermathLost = 0;

            Opened?.Invoke(this, player, _stash);
        }

        // 設置チャネルの完了。背負っている段ボールを下ろして箱に変える。
        public string CompleteChannel(PlayerContext player)
        {
            if (_stash != null)
            {
                // 通常この経路には来ない（設置済みは ChannelSeconds が 0）。念のため開けるに留める。
                Opened?.Invoke(this, player, _stash);
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
            _rentDaysRemaining = 0; // 置いたばかりの箱は場所代を払っていない。
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
            _rentDaysRemaining = 0; // 場所代は一過性。ロードした朝はまだ払っていない状態から始まる。

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

        /// <summary>
        /// 場所代を払う（第十二節）。支払先は世界の中の先輩ホームレスで、払うとその日の安全性が上がり
        /// 保管庫イベントの発生確率が下がる（<see cref="StashSafety"/>）。効果は就寝＝日境界で切れる。
        /// 今日ぶんを既に払っていれば二重に取らない。所持金が足りなければ何も取らない（第三節）。
        /// </summary>
        public PayRentOutcome PayRent(PlayerContext player)
        {
            if (_stash == null)
            {
                return PayRentOutcome.NoStash;
            }

            var cost = RentCostYen;
            if (cost <= 0)
            {
                return PayRentOutcome.NotRentable;
            }

            if (RentActive)
            {
                return PayRentOutcome.AlreadyPaid;
            }

            var wallet = player?.Wallet?.Wallet;
            if (wallet == null || !wallet.TrySpend(cost))
            {
                return PayRentOutcome.CannotAfford;
            }

            _rentDaysRemaining = 1; // 今夜の抽選ぶんを守る。就寝時の日境界で減る。
            _elder?.SayRentPaid(); // 支払先は世界の中の先輩（第十二節）。彼が受け取りを認める。
            return PayRentOutcome.Paid;
        }

        /// <summary>
        /// 日境界で場所代の効果を1日ぶん減らす（<see cref="StashDirector"/> が翌日ぶんの抽選の後に呼ぶ）。
        /// 抽選は払い済みの安全性を読んだあとにこれで切れるので、払った効果はちょうどその夜に効く。
        /// </summary>
        public void TickRentDay()
        {
            if (_rentDaysRemaining > 0)
            {
                _rentDaysRemaining--;
            }
        }

        /// <summary>
        /// 空の段ボール箱を回収して担ぎ直す（第十二節の対抗手段「隠し場所を別の場所へ移す」）。担いで別の場所へ運び、
        /// 置き直せる。担ぐ間の代償（減速・走行不可・注目）は既存の背負いの仕組みが担う（新機構を足さない）。
        /// 中身が残っているうちは回収させない（取りこぼしを避ける・先に引き出す）。箱が世界から消えるので、
        /// この箱への予告済みイベントも取り下げる（新しい箱に降りかからせない・第十二節）。
        /// </summary>
        public ReclaimOutcome Reclaim(PlayerContext player)
        {
            if (_stash == null)
            {
                return ReclaimOutcome.NoStash;
            }

            if (!IsReclaimable)
            {
                return ReclaimOutcome.NotReclaimable;
            }

            if (_stash.UsedSlots > 0)
            {
                return ReclaimOutcome.NotEmpty;
            }

            var slot = player?.Carry?.Slot;
            if (slot == null || slot.IsOccupied)
            {
                return ReclaimOutcome.CarryUnavailable;
            }

            // カバンではなく背負いスロットへ戻す（段ボールは担ぐ物・第十一節）。空きは上で確かめてある。
            if (!slot.TryCarry(new ItemInstance(new ItemId(_reclaimItemId))))
            {
                return ReclaimOutcome.CarryUnavailable;
            }

            // 今日の場所代を払った箱をそのまま持ち出すなら、払ったぶんは無駄になる。清算する前にその代償を
            // 覚えておき、回収後に先輩が世界の言葉で告げる（見えない代償の可視化）。rent の扱いは変えない。
            var forfeitedRent = RentActive;

            _stash = null;
            _rentDaysRemaining = 0;
            FindFirstObjectByType<StashDirector>()?.ClearPendingFor(_stashSpotId);
            ClearNotice();
            ApplyVisual();

            if (forfeitedRent)
            {
                _elder?.SayRentForfeited();
            }

            return ReclaimOutcome.Reclaimed;
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
