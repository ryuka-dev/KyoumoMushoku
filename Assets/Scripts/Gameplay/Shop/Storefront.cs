using System;
using System.Collections.Generic;
using KyoumoMushoku.Core.Items;
using KyoumoMushoku.Core.Shop;
using KyoumoMushoku.Core.Survival;
using KyoumoMushoku.Gameplay.DayCycle;
using KyoumoMushoku.Gameplay.Interaction;
using KyoumoMushoku.Gameplay.UI;
using UnityEngine;

namespace KyoumoMushoku.Gameplay.Shop
{
    /// <summary>
    /// コンビニ。1軒で「購入」「バイト」「廃品の買い取り」を兼ねる（第九・十三節）。
    /// 昼はここで働き、夜はそのコンビニのゴミから捨てられた弁当を漁る、という皮肉が配置だけで成立する。
    ///
    /// この型は世界の側の権威（店主の帳簿・取引の実処理）だけを持つ。UI と入力は <see cref="StorefrontPanel"/> が持ち、
    /// <see cref="Entered"/> を合図に開く。取引の結果は店主（<see cref="NpcSpeech"/>）が世界の言葉で語る（第十四節）。
    ///
    /// バイトは正当な仕事であり、人目を引く行為ではない。したがって <see cref="ISuspiciousAct"/> は実装しない。
    /// 同じコンビニでも、昼の労働は安全で SAN を削り、夜の漁りはタダだが警察の危険を負う。命題④の張力はこの対比にある。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Storefront : MonoBehaviour, IInteractable
    {
        [Tooltip("店頭に並べる品の識別子。価格はアイテムカタログの買値を使う。")]
        [SerializeField] string[] _offerIds = { "water_bottle", "onigiri", "can_coffee", "backpack" };

        [Tooltip("1日あたりの買い取り上限（円）。店主は回収業者ではない（第十三節）。叩き台。")]
        [SerializeField, Min(0)] int _buybackDailyCapYen = 300;

        [Tooltip("バイト（レジ打ち）のラウンド数。叩き台。")]
        [SerializeField, Min(1)] int _jobRounds = 5;

        [Tooltip("バイト1回で削られる SAN。最も回復させづらい資源を大量に消費する（第四節）。叩き台。")]
        [SerializeField, Min(0f)] float _jobSanityCost = 25f;

        [Tooltip("バイト1回で減る空腹。叩き台。")]
        [SerializeField, Min(0f)] float _jobHungerCost = 15f;

        [Tooltip("バイト1回（1シフト）で進むソフトクロックの秒数。時間はバイトの主要なコストの1つ（第四節）。叩き台。")]
        [SerializeField, Min(0f)] float _jobShiftSeconds = 90f;

        [SerializeField] NpcSpeech _clerk;

        readonly SalvageLedger _ledger = new SalvageLedger();

        // シーン内の唯一の時計。就寝で日付が変わる canonical な経路（GameSession）と同じく、
        // 横断的な単一オブジェクトは参照を直列化せず起動時に一度だけ拾う（SleepSpot が ZoneAlertDirector を拾うのと同じ）。
        GameClockDriver _clock;

        /// <summary>プレイヤーが店に入った（調べた）。<see cref="StorefrontPanel"/> がこれを合図に開く。</summary>
        public event Action<PlayerContext> Entered;

        public IReadOnlyList<string> OfferIds => _offerIds;
        public int BuybackDailyCapYen => _buybackDailyCapYen;
        public int JobRounds => _jobRounds;
        public SalvageLedger Ledger => _ledger;

        public void Configure(string[] offerIds, int buybackDailyCapYen, int jobRounds,
            float jobSanityCost, float jobHungerCost, float jobShiftSeconds)
        {
            _offerIds = offerIds;
            _buybackDailyCapYen = Mathf.Max(0, buybackDailyCapYen);
            _jobRounds = Mathf.Max(1, jobRounds);
            _jobSanityCost = Mathf.Max(0f, jobSanityCost);
            _jobHungerCost = Mathf.Max(0f, jobHungerCost);
            _jobShiftSeconds = Mathf.Max(0f, jobShiftSeconds);
        }

        public void BindClerk(NpcSpeech clerk) => _clerk = clerk;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            _clock = FindFirstObjectByType<GameClockDriver>();
        }

        public bool CanInteract(PlayerContext player) =>
            player.Vitals != null && player.Vitals.Vitals.IsAlive;

        public string Describe(PlayerContext player) => ShopText.StoreName;

        public void Interact(PlayerContext player) => Entered?.Invoke(player);

        /// <summary>店主の一言。取引の結果は世界の中で語る（第十四節）。</summary>
        public void Speak(string line) => _clerk?.Say(line);

        /// <summary>新しい1日。買い取り枠が戻る。就寝経路（GameSession.SleepAt）からのみ呼ぶ。</summary>
        public void BeginNextDay() => _ledger.BeginNextDay();

        public PurchaseResult TryBuy(ItemId id, PlayerContext player, out ItemDefinition bought)
        {
            bought = null;
            if (player.Wallet == null || player.Inventory == null || player.Inventory.Inventory == null)
            {
                return PurchaseResult.Unavailable;
            }

            return StorePurchase.TryBuy(
                player.Wallet.Wallet, player.Inventory.Inventory, player.Inventory.Catalog, id, out bought);
        }

        public BuybackResult SellSalvage(PlayerContext player)
        {
            if (player.Wallet == null || player.Inventory == null || player.Inventory.Inventory == null)
            {
                return default;
            }

            return SalvageBuyback.SellSalvage(
                player.Wallet.Wallet, player.Inventory.Inventory, _ledger, _buybackDailyCapYen);
        }

        /// <summary>
        /// バイト1回ぶんの精算。報酬は出来（<paramref name="performance01"/>）と SAN で決まり、
        /// SAN・空腹の消費は出来によらず必ず起きる（働けば削られる）。
        /// </summary>
        public int ApplyJobOutcome(float performance01, PlayerContext player)
        {
            if (player.Vitals == null || player.Vitals.Vitals == null)
            {
                return 0;
            }

            var vitals = player.Vitals.Vitals;
            var paid = JobReward.Payout(performance01, vitals.Sanity);

            player.Wallet?.Wallet.Add(paid);
            vitals.Apply(new VitalsDelta { Sanity = -_jobSanityCost, Hunger = -_jobHungerCost });

            // 1シフトぶん、ソフトクロックがまとまって進む。時間はバイトの主要なコストの1つであり（第四節）、
            // これがないと働くことがほぼ無時間で、1日に何度でも稼げてしまう。中断（Esc）はここに来ないので消費しない。
            _clock?.Clock?.Advance(_jobShiftSeconds);
            return paid;
        }
    }
}
