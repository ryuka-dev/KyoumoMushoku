namespace KyoumoMushoku.Core.Items
{
    /// <summary>
    /// 食品の状態。入手した瞬間に確定し、その後は一切変化しない（第十一節）。
    /// 所持中に時間経過で腐っていく実時間システムは採用しない。
    ///
    /// 空腹の回復量は状態に依存しない。弁当は弁当であり、腐っていても同じだけ腹にたまる。
    /// 状態が変えるのは、それに加えて HP と SAN を削るかどうかだけである。
    /// </summary>
    public enum FoodState
    {
        /// <summary>新鮮。店で購入した食品は常にこれ。</summary>
        Fresh = 0,

        /// <summary>傷み。危険ではないが、新鮮でもない。</summary>
        Stale = 1,

        /// <summary>腐敗。危険な食品として HP と SAN を削る。</summary>
        Rotten = 2,
    }
}
