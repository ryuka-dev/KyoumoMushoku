using KyoumoMushoku.Core.Economy;
using KyoumoMushoku.Core.Items;

namespace KyoumoMushoku.Core.Shop
{
    /// <summary>1回の買い取りの結果。店主の断り文句は世界の言葉で語る（第十四節）。</summary>
    public readonly struct BuybackResult
    {
        public BuybackResult(int soldCount, int paidYen, bool capReached)
        {
            SoldCount = soldCount;
            PaidYen = paidYen;
            CapReached = capReached;
        }

        public int SoldCount { get; }
        public int PaidYen { get; }

        /// <summary>1日の買い取り上限に触れて、売り切れずに残した品があるか（「今日はもう勘弁してくれ」）。</summary>
        public bool CapReached { get; }
    }

    /// <summary>
    /// コンビニの店主が渋々買い取る（第十三節）。独立した廃品回収所は置かず、店主が兼ねる。
    /// 「店主は回収業者ではない」という前提から、制約は自然に導かれる。
    ///
    /// - 1日あたりの買い取り上限がある。上限に達すると断られる。
    /// - 価格そのものは各品の売値（<see cref="ItemDefinition.SellPriceYen"/>）である。正規の廃品回収業者は
    ///   商店街（第七節）にいて、同じ廃品がそちらでは高く売れる。それが次の街区へ進む動機になる。
    ///   初版は1街区のため、その割増は将来の街区に属する（第九節）。
    /// </summary>
    public static class SalvageBuyback
    {
        /// <summary>
        /// カバンの換金廃品を、その日の上限に触れるまで順に売る。上限を超えてしまう品は残す。
        /// 後ろから抜くので索引がずれない。飲食物・装備には手を付けない。
        /// </summary>
        public static BuybackResult SellSalvage(
            Wallet wallet, Inventory inventory, SalvageLedger ledger, int dailyCapYen)
        {
            if (wallet is null || inventory is null || ledger is null)
            {
                return new BuybackResult(0, 0, false);
            }

            var soldCount = 0;
            var paidYen = 0;
            var capReached = false;

            for (var i = inventory.Count - 1; i >= 0; i--)
            {
                if (!inventory.TryGetDefinition(i, out var definition))
                {
                    continue;
                }

                if (definition.Category != ItemCategory.Salvage || definition.SellPriceYen <= 0)
                {
                    continue;
                }

                var remaining = ledger.RemainingToday(dailyCapYen);
                if (remaining <= 0)
                {
                    capReached = true;
                    break;
                }

                if (definition.SellPriceYen > remaining)
                {
                    // この品は今日の残り枠に入らない。より安い品はまだ売れるので、打ち切らず飛ばす。
                    capReached = true;
                    continue;
                }

                inventory.TryRemoveAt(i, out _);
                wallet.Add(definition.SellPriceYen);
                ledger.Add(definition.SellPriceYen);
                soldCount++;
                paidYen += definition.SellPriceYen;
            }

            return new BuybackResult(soldCount, paidYen, capReached);
        }
    }
}
